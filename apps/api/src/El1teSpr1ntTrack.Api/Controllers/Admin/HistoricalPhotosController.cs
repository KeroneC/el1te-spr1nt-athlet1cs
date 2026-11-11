using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
[Route("api/admin/historical-photos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HistoricalPhotosController(El1teDbContext db, LegacyPhotoOptions options, IClock clock,
    IServiceProvider services) : ControllerBase
{
    private static readonly SemaphoreSlim SocialEncoder = new(1);

    [HttpGet]
    public async Task<IActionResult> List(string? search, string? collection, string? review, int page = 1,
        CancellationToken token = default)
    {
        if (!options.Enabled) return NotFound();
        if (search?.Length > 255 || collection?.Length > 450)
            return BadRequest();

        var query = db.LegacyPhotos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(photo => photo.Sources.Any(source => source.OriginalFileName.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(collection))
            query = query.Where(photo => photo.Sources.Any(source => source.Pages.Any(link => link.Collection == collection)));
        if (!string.IsNullOrWhiteSpace(review))
        {
            if (!TryReview(review, out var status)) return BadRequest();
            query = query.Where(photo => photo.Review == status);
        }

        const int pageSize = 24;
        var count = await query.CountAsync(token);
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var photos = await query.Include(photo => photo.Sources).ThenInclude(source => source.Pages)
            .AsSplitQuery().OrderBy(photo => photo.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var collections = await db.LegacyPhotoPages.Select(link => link.Collection).Distinct().OrderBy(name => name).ToListAsync(token);
        return Ok(new
        {
            page, totalPages, totalCount = count, collections,
            items = photos.Select(photo => new
            {
                photo.Id, photo.Width, photo.Height, photo.ReviewedAtUtc, review = photo.Review.ToString(),
                fileNames = photo.Sources.Select(source => source.OriginalFileName).Distinct().Order(),
                collections = photo.Sources.SelectMany(source => source.Pages).Select(link => link.Collection).Distinct().Order()
            })
        });
    }

    public sealed record ReviewRequest(string? Review, bool PermissionConfirmed);

    [HttpPut("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewRequest request, CancellationToken token)
    {
        if (!options.Enabled) return NotFound();
        if (!TryReview(request.Review, out var status))
            return BadRequest(new ProblemDetails { Title = "Choose a valid review status." });
        if (status == LegacyPhotoReview.ApprovedForSocial && !request.PermissionConfirmed)
            return BadRequest(new ProblemDetails { Title = "Confirm club permission for social use first." });

        var photo = await db.LegacyPhotos.SingleOrDefaultAsync(x => x.Id == id, token);
        if (photo is null) return NotFound();
        photo.Review = status;
        photo.ReviewedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        photo.ReviewedAtUtc = clock.UtcNow;
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails { Title = "Another reviewer updated this photo. Refresh and try again." });
        }
        return NoContent();
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken token)
    {
        if (!options.Enabled) return NotFound();
        var hash = await db.LegacyPhotos.Where(x => x.Id == id).Select(x => x.Sha256).SingleOrDefaultAsync(token);
        if (hash is null) return NotFound();
        var stream = await services.GetRequiredService<ILegacyPhotoBlobs>().ReadAsync(hash, preview: true, token);
        return stream is null ? NotFound() : File(stream, "image/webp");
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken token)
    {
        if (!options.Enabled) return NotFound();
        var approved = db.LegacyPhotos.AsNoTracking().Where(x => x.Id == id && x.Review == LegacyPhotoReview.ApprovedForSocial);
        var hash = await approved.Select(x => x.Sha256).SingleOrDefaultAsync(token);
        if (hash is null) return NotFound();
        if (!await SocialEncoder.WaitAsync(0, token))
        {
            Response.Headers.RetryAfter = "3";
            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        try
        {
            await using var source = await services.GetRequiredService<ILegacyPhotoBlobs>().ReadAsync(hash, preview: false, token);
            if (source is null) return NotFound();
            var clean = LegacyPhotoImages.SocialJpeg(await LegacyPhotoImages.ReadBoundedAsync(source, token));
            if (!await approved.AnyAsync(token)) return NotFound();
            return File(clean, "image/jpeg", $"historical-photo-{id:N}.jpg");
        }
        finally { SocialEncoder.Release(); }
    }

    private static bool TryReview(string? value, out LegacyPhotoReview review) =>
        Enum.TryParse(value, out review) && Enum.IsDefined(review) && review.ToString() == value;
}
