using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class AllAmericanArchiveService(IAllAmericanArchiveRepository repository, IClock clock) : IAllAmericanArchiveService
{
    public async Task<PagedResultDto<AdminAllAmericanYearListItemDto>> GetAdminAsync(AdminAllAmericanYearOptions options, CancellationToken token)
    {
        var page = Math.Max(1, options.Page); var size = Math.Clamp(options.PageSize, 1, 100);
        var result = await repository.GetAdminAsync(options with { Page = page, PageSize = size }, token);
        return new(result.Items.Select(MapList).ToList(), page, size, result.TotalCount);
    }

    public async Task<AdminAllAmericanYearDto> GetAdminAsync(Guid id, CancellationToken token) => Map(await RequireYear(id, token));

    public async Task<AdminAllAmericanYearDto> CreateAsync(AllAmericanYearWriteDto request, CancellationToken token)
    {
        if (await repository.YearExistsAsync(request.Year, null, token)) throw new CmsConflictException("That Junior Olympics year already exists.");
        var year = new AllAmericanYear { Slug = request.Year.ToString(), CreatedAtUtc = clock.UtcNow };
        Apply(year, request); await ValidateYear(year, token);
        await repository.AddAsync(year, token); await repository.SaveChangesAsync(token); return Map(year);
    }

    public async Task<AdminAllAmericanYearDto> UpdateAsync(Guid id, AllAmericanYearWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(id, token);
        if (await repository.YearExistsAsync(request.Year, id, token)) throw new CmsConflictException("That Junior Olympics year already exists.");
        Apply(year, request); year.UpdatedAtUtc = clock.UtcNow; await ValidateYear(year, token);
        await repository.SaveChangesAsync(token); return Map(await RequireYear(id, token));
    }

    public async Task DeactivateAsync(Guid id, CancellationToken token)
    { var year = await RequireYear(id, token); year.IsPublished = false; year.UpdatedAtUtc = clock.UtcNow; await repository.SaveChangesAsync(token); }

    public async Task<AdminAllAmericanYearDto> AddMediaAsync(Guid yearId, AllAmericanYearMediaWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(yearId, token); var media = await RequireMedia(request.MediaAssetId, token); ValidateMedia(request);
        if (year.Media.Any(x => x.MediaAssetId == request.MediaAssetId)) throw new CmsConflictException("This image is already in the annual collection.");
        await repository.AddAsync(new AllAmericanYearMedia { AllAmericanYearId = yearId, MediaAssetId = media.Id,
            AltTextOverride = Clean(request.AltTextOverride), CaptionOverride = Clean(request.CaptionOverride), DisplayOrder = request.DisplayOrder, CreatedAtUtc = clock.UtcNow }, token);
        await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> UpdateMediaAsync(Guid yearId, Guid id, AllAmericanYearMediaWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(yearId, token); ValidateMedia(request);
        var item = await repository.GetMediaAsync(yearId, id, token) ?? throw new CmsNotFoundException("All-American image", id);
        if (item.MediaAssetId != request.MediaAssetId)
        {
            await RequireMedia(request.MediaAssetId, token);
            if (year.Media.Any(x => x.Id != id && x.MediaAssetId == request.MediaAssetId)) throw new CmsConflictException("This image is already in the annual collection.");
        }
        item.MediaAssetId = request.MediaAssetId; item.AltTextOverride = Clean(request.AltTextOverride);
        item.CaptionOverride = Clean(request.CaptionOverride); item.DisplayOrder = request.DisplayOrder; item.UpdatedAtUtc = clock.UtcNow;
        await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> RemoveMediaAsync(Guid yearId, Guid id, CancellationToken token)
    {
        var year = await RequireYear(yearId, token);
        var item = await repository.GetMediaAsync(yearId, id, token) ?? throw new CmsNotFoundException("All-American image", id);
        if (year.HeroMediaAssetId == item.MediaAssetId) throw Validation("Media", "Choose a different hero image before removing this image.");
        if (year.IsPublished && year.Media.Count == 1) throw Validation("Media", "A published year must keep at least one annual image.");
        repository.Delete(item); await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> ReorderMediaAsync(Guid yearId, AllAmericanOrderDto request, CancellationToken token)
    {
        await RequireYear(yearId, token); ValidateOrder(request);
        foreach (var order in request.Items)
        {
            var item = await repository.GetMediaAsync(yearId, order.Id, token) ?? throw new CmsNotFoundException("All-American image", order.Id);
            item.DisplayOrder = order.DisplayOrder; item.UpdatedAtUtc = clock.UtcNow;
        }
        await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> AddRecipientAsync(Guid yearId, AllAmericanRecipientWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(yearId, token); await ValidateRecipient(request, token);
        var item = new AllAmericanRecipient { AllAmericanYearId = yearId, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            PhotoMediaAssetId = request.PhotoMediaAssetId, DisplayOrder = request.DisplayOrder, IsActive = request.IsActive, CreatedAtUtc = clock.UtcNow };
        year.Recipients.Add(item); await ValidateYear(year, token);
        await repository.AddAsync(item, token);
        await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> UpdateRecipientAsync(Guid yearId, Guid id, AllAmericanRecipientWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(yearId, token); await ValidateRecipient(request, token);
        var item = await repository.GetRecipientAsync(yearId, id, token) ?? throw new CmsNotFoundException("All-American athlete", id);
        item.FirstName = request.FirstName.Trim(); item.LastName = request.LastName.Trim(); item.PhotoMediaAssetId = request.PhotoMediaAssetId;
        item.DisplayOrder = request.DisplayOrder; item.IsActive = request.IsActive; item.UpdatedAtUtc = clock.UtcNow;
        await ValidateYear(year, token);
        await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> DeactivateRecipientAsync(Guid yearId, Guid id, CancellationToken token)
    { var year = await RequireYear(yearId, token); var item = await repository.GetRecipientAsync(yearId, id, token) ?? throw new CmsNotFoundException("All-American athlete", id); item.IsActive = false; item.UpdatedAtUtc = clock.UtcNow; await ValidateYear(year, token); await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token)); }

    public async Task<AdminAllAmericanYearDto> AddPerformanceAsync(Guid yearId, AllAmericanPerformanceWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(yearId, token); var recipients = ValidatePerformance(year, request);
        var item = new AllAmericanPerformance { AllAmericanYearId = yearId, EventName = request.EventName.Trim(), Division = Clean(request.Division),
            Placement = request.Placement, IsRelay = request.IsRelay, DisplayOrder = request.DisplayOrder, IsActive = request.IsActive, CreatedAtUtc = clock.UtcNow };
        foreach (var recipient in recipients) item.Recipients.Add(new AllAmericanPerformanceRecipient { Recipient = recipient, AllAmericanRecipientId = recipient.Id, CreatedAtUtc = clock.UtcNow });
        year.Performances.Add(item);
        await ValidateYear(year, token);
        await repository.AddAsync(item, token); await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> UpdatePerformanceAsync(Guid yearId, Guid id, AllAmericanPerformanceWriteDto request, CancellationToken token)
    {
        var year = await RequireYear(yearId, token); var recipients = ValidatePerformance(year, request);
        var item = await repository.GetPerformanceAsync(yearId, id, token) ?? throw new CmsNotFoundException("All-American performance", id);
        foreach (var link in item.Recipients.ToList()) repository.Delete(link);
        item.Recipients.Clear();
        item.EventName = request.EventName.Trim(); item.Division = Clean(request.Division); item.Placement = request.Placement;
        item.IsRelay = request.IsRelay; item.DisplayOrder = request.DisplayOrder; item.IsActive = request.IsActive; item.UpdatedAtUtc = clock.UtcNow;
        foreach (var recipient in recipients) item.Recipients.Add(new AllAmericanPerformanceRecipient { AllAmericanPerformanceId = item.Id, Recipient = recipient, AllAmericanRecipientId = recipient.Id, CreatedAtUtc = clock.UtcNow });
        await ValidateYear(year, token);
        await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token));
    }

    public async Task<AdminAllAmericanYearDto> DeactivatePerformanceAsync(Guid yearId, Guid id, CancellationToken token)
    { var year = await RequireYear(yearId, token); var item = await repository.GetPerformanceAsync(yearId, id, token) ?? throw new CmsNotFoundException("All-American performance", id); item.IsActive = false; item.UpdatedAtUtc = clock.UtcNow; await ValidateYear(year, token); await repository.SaveChangesAsync(token); return Map(await RequireYear(yearId, token)); }

    public async Task<PagedResultDto<PublicAllAmericanYearListItemDto>> GetPublicAsync(int page, int pageSize, CancellationToken token)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50); var result = await repository.GetPublicAsync(page, pageSize, token);
        return new(result.Items.Select(MapPublicList).ToList(), page, pageSize, result.TotalCount);
    }

    public async Task<PublicAllAmericanYearDto?> GetPublicAsync(string slug, CancellationToken token)
    { var item = await repository.GetPublicAsync(slug.Trim(), token); return item is null ? null : MapPublic(item); }

    private async Task ValidateYear(AllAmericanYear year, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>();
        if (year.Year is < 1900 or > 2100) errors[nameof(year.Year)] = ["Year must be between 1900 and 2100."];
        if (string.IsNullOrWhiteSpace(year.Title)) errors[nameof(year.Title)] = ["Title is required."];
        if (string.IsNullOrWhiteSpace(year.Summary)) errors[nameof(year.Summary)] = ["Summary is required."];
        if (year.AthleteCount < 0) errors[nameof(year.AthleteCount)] = ["Athlete count cannot be negative."];
        if (year.MedalCount < 0) errors[nameof(year.MedalCount)] = ["Medal count cannot be negative."];
        if (year.DisplayOrder < 0) errors[nameof(year.DisplayOrder)] = ["Display order cannot be negative."];
        if (year.HeroMediaAssetId.HasValue) await RequireMedia(year.HeroMediaAssetId.Value, token);
        if (year.IsPublished && (!year.HeroMediaAssetId.HasValue || year.Media.Count == 0)) errors[nameof(year.IsPublished)] = ["A published year requires a hero image and at least one archive image."];
        if (year.DetailsComplete)
        {
            var recipients = year.Recipients.Where(x => x.IsActive).ToList();
            var performances = year.Performances.Where(x => x.IsActive).ToList();
            if (recipients.Count != year.AthleteCount) errors[nameof(year.AthleteCount)] = ["Verified athlete total must match the active annual roster."];
            var medalCount = performances.Sum(x => x.Recipients.Count(link => link.Recipient.IsActive));
            if (medalCount != year.MedalCount) errors[nameof(year.MedalCount)] = ["Verified medal total must match active performance recipients."];
            if (performances.Any(x =>
                (!x.IsRelay && x.Recipients.Count(link => link.Recipient.IsActive) != 1) ||
                (x.IsRelay && x.Recipients.Count(link => link.Recipient.IsActive) < 2)))
                errors[nameof(year.DetailsComplete)] = ["Individual performances require one active athlete and relays require at least two active athletes."];
        }
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private async Task ValidateRecipient(AllAmericanRecipientWriteDto request, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.FirstName)) errors[nameof(request.FirstName)] = ["First name is required."];
        if (string.IsNullOrWhiteSpace(request.LastName)) errors[nameof(request.LastName)] = ["Last name is required."];
        if (request.DisplayOrder < 0) errors[nameof(request.DisplayOrder)] = ["Display order cannot be negative."];
        if (request.PhotoMediaAssetId.HasValue) await RequireMedia(request.PhotoMediaAssetId.Value, token);
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private static IReadOnlyList<AllAmericanRecipient> ValidatePerformance(AllAmericanYear year, AllAmericanPerformanceWriteDto request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.EventName)) errors[nameof(request.EventName)] = ["Event name is required."];
        if (request.DisplayOrder < 0) errors[nameof(request.DisplayOrder)] = ["Display order cannot be negative."];
        var ids = request.RecipientIds.Distinct().ToList();
        var recipients = year.Recipients.Where(x => ids.Contains(x.Id)).ToList();
        if (recipients.Count != ids.Count) errors[nameof(request.RecipientIds)] = ["Every selected athlete must belong to this year."];
        if (request.IsActive && recipients.Any(x => !x.IsActive)) errors[nameof(request.RecipientIds)] = ["Active performances may use only active athletes."];
        if (request.IsActive && ((!request.IsRelay && recipients.Count != 1) || (request.IsRelay && recipients.Count < 2))) errors[nameof(request.RecipientIds)] = [request.IsRelay ? "A relay requires at least two athletes." : "An individual performance requires exactly one athlete."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
        return recipients;
    }

    private static void ValidateMedia(AllAmericanYearMediaWriteDto request)
    { if (request.DisplayOrder < 0) throw Validation(nameof(request.DisplayOrder), "Display order cannot be negative."); }
    private static void ValidateOrder(AllAmericanOrderDto request)
    { if (request.Items.Any(x => x.DisplayOrder < 0) || request.Items.Select(x => x.Id).Distinct().Count() != request.Items.Count) throw Validation("Items", "Each image requires one non-negative display order."); }
    private async Task<AllAmericanYear> RequireYear(Guid id, CancellationToken token) => await repository.GetAdminAsync(id, token) ?? throw new CmsNotFoundException("All-American year", id);
    private async Task<MediaAsset> RequireMedia(Guid id, CancellationToken token) => await repository.GetActiveMediaAsync(id, token) ?? throw new CmsNotFoundException("Active media asset", id);
    private static CmsRequestValidationException Validation(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Apply(AllAmericanYear year, AllAmericanYearWriteDto request)
    { year.Year = request.Year; year.Title = request.Title.Trim(); year.Summary = request.Summary.Trim(); year.AthleteCount = request.AthleteCount; year.MedalCount = request.MedalCount; year.HeroMediaAssetId = request.HeroMediaAssetId; year.DisplayOrder = request.DisplayOrder; year.IsPublished = request.IsPublished; year.DetailsComplete = request.DetailsComplete; }

    private static AdminAllAmericanYearListItemDto MapList(AllAmericanYear x) => new(x.Id, x.Year, x.Slug, x.Title, x.Summary, x.AthleteCount, x.MedalCount, x.HeroMediaAssetId, x.HeroMediaAsset?.PublicUrl, x.IsPublished, x.DetailsComplete, x.DisplayOrder, x.Media.Count, x.Recipients.Count, x.Performances.Count, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static AdminAllAmericanYearDto Map(AllAmericanYear x) => new(x.Id, x.Year, x.Slug, x.Title, x.Summary, x.AthleteCount, x.MedalCount, x.HeroMediaAssetId, x.IsPublished, x.DetailsComplete, x.DisplayOrder,
        x.Media.OrderBy(m => m.DisplayOrder).Select(m => new AdminAllAmericanYearMediaDto(m.Id, m.MediaAssetId, m.MediaAsset.PublicUrl, m.MediaAsset.Title, m.MediaAsset.AltText, m.MediaAsset.Caption, m.AltTextOverride, m.CaptionOverride, m.DisplayOrder, m.MediaAsset.Width, m.MediaAsset.Height)).ToList(),
        x.Recipients.OrderBy(r => r.DisplayOrder).Select(r => new AdminAllAmericanRecipientDto(r.Id, r.FirstName, r.LastName, r.PhotoMediaAssetId, r.PhotoMediaAsset?.PublicUrl, r.PhotoMediaAsset?.AltText, r.DisplayOrder, r.IsActive)).ToList(),
        x.Performances.OrderBy(p => p.DisplayOrder).Select(p => new AdminAllAmericanPerformanceDto(p.Id, p.EventName, p.Division, p.Placement, p.IsRelay, p.DisplayOrder, p.IsActive, p.Recipients.Select(r => r.AllAmericanRecipientId).ToList())).ToList(), x.CreatedAtUtc, x.UpdatedAtUtc);
    private static PublicAllAmericanYearListItemDto MapPublicList(AllAmericanYear x) => new(x.Year, x.Slug, x.Title, x.Summary, x.AthleteCount, x.MedalCount, x.HeroMediaAsset?.PublicUrl, x.HeroMediaAsset?.AltText, x.Media.Count(m => m.MediaAsset.IsActive));
    private static PublicAllAmericanYearDto MapPublic(AllAmericanYear x)
    {
        var activePerformances = x.Performances.Where(p => p.IsActive).ToList();
        var recipients = x.DetailsComplete ? x.Recipients.Where(r => r.IsActive).OrderBy(r => r.DisplayOrder).Select(r => new PublicAllAmericanRecipientDto(r.FirstName, r.LastName, r.PhotoMediaAsset?.PublicUrl, r.PhotoMediaAsset?.AltText, r.DisplayOrder,
            activePerformances.Where(p => p.Recipients.Any(link => link.AllAmericanRecipientId == r.Id)).OrderBy(p => p.DisplayOrder).Select(p => new PublicAllAmericanResultDto(p.EventName, p.Division, p.Placement, p.IsRelay, p.DisplayOrder)).ToList())).ToList() : [];
        return new(x.Year, x.Slug, x.Title, x.Summary, x.AthleteCount, x.MedalCount, x.HeroMediaAsset?.PublicUrl, x.HeroMediaAsset?.AltText, x.DetailsComplete,
            x.Media.Where(m => m.MediaAsset.IsActive).OrderBy(m => m.DisplayOrder).Select(m => new PublicAllAmericanImageDto(m.MediaAsset.PublicUrl, m.AltTextOverride ?? m.MediaAsset.AltText, m.CaptionOverride ?? m.MediaAsset.Caption, m.MediaAsset.Width, m.MediaAsset.Height, m.DisplayOrder)).ToList(), recipients);
    }
}
