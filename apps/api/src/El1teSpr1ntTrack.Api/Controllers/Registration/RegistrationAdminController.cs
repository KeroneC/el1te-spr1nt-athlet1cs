using System.Data;
using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Api.Controllers.Registration;

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/registration")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(8192)]
public sealed class RegistrationAdminController(El1teDbContext db, IClock clock) : ControllerBase
{
    private Guid StaffId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IActionResult ConflictMessage(string message) => Conflict(new ProblemDetails { Title = message });

    [HttpGet("seasons")]
    public async Task<IActionResult> Seasons(CancellationToken token)
    {
        var seasons = await db.RegistrationSeasons.AsNoTracking().OrderByDescending(x => x.Year).ToListAsync(token);
        var counts = await db.RegistrationCounts.AsNoTracking().OrderBy(x => x.Channel).ToListAsync(token);
        var liveCounts = await db.RegistrationSubmissions.GroupBy(x => new { x.SeasonYear, x.Channel })
            .Select(group => new { group.Key.SeasonYear, group.Key.Channel,
                Applications = group.Count(x => x.Kind == IntakeKind.Application),
                Returning = group.Count(x => x.Kind == IntakeKind.Returning),
                Enrolled = group.Count(x => x.Status == IntakeStatus.Enrolled) }).ToListAsync(token);
        foreach (var live in liveCounts)
        {
            var count = counts.Single(x => x.SeasonYear == live.SeasonYear && x.Channel == live.Channel);
            (count.Applications, count.Returning, count.Enrolled) = (live.Applications, live.Returning, live.Enrolled);
        }
        var today = IntakeRules.LocalDate(clock.UtcNow);
        return Ok(new
        {
            seasons, counts,
            closeoutEligibleYears = seasons.Where(x => !x.IsOpen && x.PurgedAtUtc is null &&
                today.DayNumber - x.EndDate.DayNumber >= 90).Select(x => x.Year)
        });
    }

    [HttpPut("season")]
    public async Task<IActionResult> SaveSeason(SeasonRequest request, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        var season = await RegistrationDatabase.LockSeason(db, request.Year, token);
        if (season?.PurgedAtUtc is not null || season?.IsOpen == true || season?.Revision != request.Revision)
            return ConflictMessage("Close intake before editing, or refresh to load the latest season.");
        if (season is null && await db.RegistrationSeasons.AnyAsync(x => x.IsOpen, token))
            return ConflictMessage("Close the current intake before adding another season.");
        var created = season is null;
        season ??= new RegistrationSeason { Year = request.Year };
        IntakeRules.UpdateSeason(season, request);
        // Changed copy must be approved again by opening; historical submissions are unaffected.
        season.OpenedAtUtc = null;
        season.OpenedByUserId = null;
        if (created)
        {
            db.RegistrationSeasons.Add(season);
            db.RegistrationCounts.AddRange(Enum.GetValues<IntakeChannel>()
                .Select(channel => new RegistrationCount { SeasonYear = season.Year, Channel = channel }));
        }
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Ok(season);
    }

    public sealed record OpeningRequest(int Year, Guid Revision, bool Open, bool LaunchChecksApproved);

    [HttpPut("opening")]
    public async Task<IActionResult> Opening(OpeningRequest request, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        // Lock the small season set in one order to guard against two seasons being opened concurrently.
        var seasons = await db.RegistrationSeasons.FromSqlRaw("SELECT * FROM dbo.RegistrationSeasons WITH (UPDLOCK, HOLDLOCK)")
            .OrderByDescending(x => x.Year).ToListAsync(token);
        var season = seasons.FirstOrDefault(x => x.Year == request.Year);
        if (season is null) return NotFound();
        if (season.Revision != request.Revision || season.PurgedAtUtc is not null)
            return ConflictMessage("Refresh to load the latest season state.");
        var today = IntakeRules.LocalDate(clock.UtcNow);
        if (request.Open && (!request.LaunchChecksApproved || today < season.OpeningDate || today > season.EndDate ||
            seasons.First(x => x.PurgedAtUtc is null).Year != season.Year || seasons.Any(x => x.Year != season.Year && x.IsOpen)))
            return ConflictMessage("Approve the launch checks and open the latest season on or after its opening date. Close any prior intake first.");
        season.IsOpen = request.Open;
        if (request.Open)
        {
            season.OpenedAtUtc = clock.UtcNow;
            season.OpenedByUserId = StaffId;
        }
        season.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Ok(season);
    }

    public sealed record QueueRequest(int Year, IntakeKind Kind, int Page, string? Search, string? AgeGroup,
        IntakeStatus? Status, TransferStatus? Transfer);

    [HttpPost("queue")]
    public async Task<IActionResult> Queue(QueueRequest request, CancellationToken token)
    {
        if (!Enum.IsDefined(request.Kind) || request.Search?.Length > 120 || request.AgeGroup?.Length > 40)
            return BadRequest(new ProblemDetails { Title = "Choose valid filters." });
        var query = db.RegistrationSubmissions.AsNoTracking().Where(x => x.SeasonYear == request.Year && x.Kind == request.Kind);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.ReceiptReference.Contains(search) || x.GuardianEmail.Contains(search)
                || x.AthleteFirstName.Contains(search) || (x.GuardianName != null && x.GuardianName.Contains(search)));
        }
        if (!string.IsNullOrEmpty(request.AgeGroup)) query = query.Where(x => x.AgeGroup == request.AgeGroup);
        if (request.Status is { } status) query = query.Where(x => x.Status == status);
        if (request.Transfer is { } transfer) query = query.Where(x => x.Transfer == transfer);
        var count = await query.CountAsync(token);
        const int pageSize = 20;
        var totalPages = Math.Max(1, (count + pageSize - 1) / pageSize);
        var page = Math.Clamp(request.Page, 1, totalPages);
        var items = await query.OrderBy(x => x.SubmittedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.ReceiptReference, x.AthleteFirstName, x.AgeGroup, x.Status, x.Transfer, x.CompetitionReady, x.SubmittedAtUtc })
            .ToListAsync(token);
        return Ok(new { items, totalPages, page, count });
    }

    public sealed record DetailRequest(Guid Id);

    [HttpPost("detail")]
    public async Task<IActionResult> Detail(DetailRequest request, CancellationToken token)
    {
        var item = await db.RegistrationSubmissions.AsNoTracking().Where(x => x.Id == request.Id).Select(x => new
        {
            x.Id, x.SeasonYear, x.Kind, x.Revision, x.ReceiptReference, x.GuardianEmail, x.GuardianName, x.GuardianPhone,
            x.AthleteFirstName, x.AgeGroup, x.Availability, x.Experience, x.PriorClub, x.ClubName, x.Affiliations, x.LastMeetMonth,
            x.Status, x.Transfer, x.PhotoConsent, x.CompetitionReady, x.SubmittedAtUtc,
            history = x.History.OrderByDescending(h => h.ChangedAtUtc).ThenByDescending(h => h.Id).Select(h => new
            {
                h.ChangedAtUtc, h.ChangedByUserId, h.Status, h.Transfer, h.PhotoConsent, h.CompetitionReady,
                reviewer = db.Users.Where(u => u.Id == h.ChangedByUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()
            }).ToList()
        }).SingleOrDefaultAsync(token);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("review")]
    public async Task<IActionResult> Review(ReviewRequest request, CancellationToken token)
    {
        var year = await db.RegistrationSubmissions.AsNoTracking().Where(x => x.Id == request.Id)
            .Select(x => (int?)x.SeasonYear).SingleOrDefaultAsync(token);
        if (year is null) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        var season = await RegistrationDatabase.LockSeason(db, year.Value, token);
        if (season is null || season.PurgedAtUtc is not null) return NotFound();
        var item = await db.RegistrationSubmissions.SingleOrDefaultAsync(x => x.Id == request.Id, token);
        if (item is null) return NotFound();
        if (item.Revision != request.Revision) return ConflictMessage("Another reviewer made a change. Reopen the application and try again.");
        var before = (item.Status, item.Transfer, item.PhotoConsent, item.CompetitionReady);
        IntakeRules.ApplyReview(item, request);
        if (before == (item.Status, item.Transfer, item.PhotoConsent, item.CompetitionReady)) return NoContent();
        item.Revision = Guid.NewGuid();
        db.RegistrationReviews.Add(new RegistrationReview
        {
            SubmissionId = item.Id, ChangedAtUtc = clock.UtcNow, ChangedByUserId = StaffId,
            Status = item.Status, Transfer = item.Transfer, PhotoConsent = item.PhotoConsent, CompetitionReady = item.CompetitionReady
        });
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { return ConflictMessage("Another reviewer made a change. Reopen the application and try again."); }
        await transaction.CommitAsync(token);
        return NoContent();
    }

    public sealed record CloseoutRequest(int Year, Guid Revision, string? ConfirmYear);

    [HttpPost("closeout")]
    [Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
    public async Task<IActionResult> Closeout(CloseoutRequest request, CancellationToken token)
    {
        if (request.ConfirmYear != request.Year.ToString(System.Globalization.CultureInfo.InvariantCulture))
            return BadRequest(new ProblemDetails { Title = "Enter the season year to confirm removal." });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var season = await RegistrationDatabase.LockSeason(db, request.Year, token);
        if (season is null) return NotFound();
        if (season.Revision != request.Revision || season.IsOpen || season.PurgedAtUtc is not null ||
            IntakeRules.LocalDate(clock.UtcNow).DayNumber - season.EndDate.DayNumber < 90)
            return ConflictMessage("Closeout requires a closed season that ended at least 90 days ago.");
        foreach (var count in await db.RegistrationCounts.Where(x => x.SeasonYear == season.Year).ToListAsync(token))
        {
            var submissions = db.RegistrationSubmissions.Where(x => x.SeasonYear == season.Year && x.Channel == count.Channel);
            count.Applications = await submissions.CountAsync(x => x.Kind == IntakeKind.Application, token);
            count.Returning = await submissions.CountAsync(x => x.Kind == IntakeKind.Returning, token);
            count.Enrolled = await submissions.CountAsync(x => x.Status == IntakeStatus.Enrolled, token);
        }
        var removed = await db.RegistrationSubmissions.Where(x => x.SeasonYear == season.Year).ExecuteDeleteAsync(token);
        season.PurgedAtUtc = clock.UtcNow;
        season.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Ok(new { removed });
    }
}
