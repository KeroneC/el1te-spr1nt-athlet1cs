using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Api.Controllers.Registration;

internal static class RegistrationDatabase
{
    // One short transaction per season prevents races with closing, submission retries and cleanup.
    // UPDLOCK serializes writers without taking the database offline or blocking the read-only page.
    public static Task<RegistrationSeason?> LockSeason(El1teDbContext db, int year, CancellationToken token) =>
        db.RegistrationSeasons.FromSqlInterpolated($"SELECT * FROM dbo.RegistrationSeasons WITH (UPDLOCK, HOLDLOCK) WHERE [Year] = {year}")
            .SingleOrDefaultAsync(token);
}

[ApiController]
[Route("api/public/registration")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegistrationPublicController(El1teDbContext db, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Season(CancellationToken token)
    {
        var season = await db.RegistrationSeasons.AsNoTracking()
            .Where(x => x.PurgedAtUtc == null).OrderByDescending(x => x.Year).FirstOrDefaultAsync(token);
        return season is null ? NoContent() : Ok(new
        {
            season.Year, season.OpeningDate,
            title = season.OpenedAtUtc is null ? null : season.Title,
            practiceLocation = season.OpenedAtUtc is null ? null : season.PracticeLocation,
            practiceSchedule = season.OpenedAtUtc is null ? null : season.PracticeSchedule,
            fees = season.OpenedAtUtc is null ? null : season.Fees,
            replyWindow = season.OpenedAtUtc is null ? null : season.ReplyWindow,
            ageGroups = season.OpenedAtUtc is null ? Array.Empty<string>() : IntakeRules.Ages(season),
            isOpen = IntakeRules.IsOpen(season, clock.UtcNow)
        });
    }

    [HttpPost("applications")]
    [EnableRateLimiting("registration-intake")]
    [Consumes("application/json")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Submit(IntakeRequest request, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        var season = await RegistrationDatabase.LockSeason(db, request.SeasonYear, token);
        if (season is null || season.PurgedAtUtc is not null)
            return Conflict(new ProblemDetails { Title = "Applications are not open." });
        var now = clock.UtcNow;
        var existing = await db.RegistrationSubmissions.AsNoTracking()
            .Where(x => x.SeasonYear == season.Year && x.RequestKey == request.RequestKey)
            .Select(x => new { x.RequestDigest, x.ReceiptReference, x.AgeGroup }).SingleOrDefaultAsync(token);
        if (existing is null && (!IntakeRules.IsOpen(season, now) ||
            await db.RegistrationSeasons.AnyAsync(x => x.Year > season.Year && x.PurgedAtUtc == null, token)))
            return Conflict(new ProblemDetails { Title = "Applications are not open." });
        var submission = IntakeRules.Normalize(request, season, now, existing?.AgeGroup);
        submission.RequestDigest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            submission.RequestKey, submission.Kind, submission.GuardianEmail, submission.GuardianName,
            submission.GuardianPhone, submission.AthleteFirstName, submission.AgeGroup, submission.Availability,
            submission.Experience, submission.PriorClub, submission.ClubName, submission.Affiliations, submission.LastMeetMonth
        })));
        if (existing is not null)
            return existing.RequestDigest == submission.RequestDigest
                ? Ok(new { reference = existing.ReceiptReference })
                : Conflict(new ProblemDetails { Title = "This retry does not match the original application. Start a new form for another athlete." });

        if (await db.RegistrationSubmissions.CountAsync(x => x.SeasonYear == season.Year &&
            x.GuardianEmail == submission.GuardianEmail && x.SubmittedAtUtc >= now.AddHours(-1), token) >= 10)
        {
            Response.Headers.RetryAfter = "3600";
            return StatusCode(429, new ProblemDetails { Title = "Too many submissions. Try again later or contact the club." });
        }
        submission.ReceiptReference = "EL-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        db.RegistrationSubmissions.Add(submission);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return StatusCode(201, new { reference = submission.ReceiptReference });
    }

    public sealed record VisitRequest(int Year, string? Channel);

    [HttpPost("visit")]
    [EnableRateLimiting("registration-visits")]
    [Consumes("application/json")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> Visit(VisitRequest request, CancellationToken token)
    {
        var channel = IntakeRules.Channel(request.Channel);
        await db.RegistrationCounts.Where(x => x.SeasonYear == request.Year && x.Channel == channel &&
            db.RegistrationSeasons.Any(s => s.Year == x.SeasonYear && s.PurgedAtUtc == null))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Visits, x => x.Visits + 1), token);
        return NoContent();
    }
}
