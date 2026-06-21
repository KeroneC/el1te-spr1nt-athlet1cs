using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed partial class AdminCmsService
{
    public async Task<AdminSiteSettingsDto> GetSiteSettingsAsync(CancellationToken cancellationToken)
    {
        var item = await _repository.GetSiteSettingsAsync(cancellationToken)
            ?? throw new CmsNotFoundException("Site settings", Guid.Empty);
        return Map(item);
    }

    public async Task<AdminSiteSettingsDto> UpdateSiteSettingsAsync(SiteSettingWriteDto request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetSiteSettingsAsync(cancellationToken)
            ?? throw new CmsNotFoundException("Site settings", Guid.Empty);
        item.ClubName = request.ClubName.Trim();
        item.Slogan = request.Slogan.Trim();
        item.ContactEmail = request.ContactEmail.Trim();
        item.PhoneNumber = Clean(request.PhoneNumber);
        item.AddressLine1 = Clean(request.AddressLine1);
        item.AddressLine2 = Clean(request.AddressLine2);
        item.City = Clean(request.City);
        item.State = Clean(request.State);
        item.ZipCode = Clean(request.ZipCode);
        item.FacebookUrl = Clean(request.FacebookUrl);
        item.InstagramUrl = Clean(request.InstagramUrl);
        item.YouTubeUrl = Clean(request.YouTubeUrl);
        item.PrimaryCtaText = request.PrimaryCtaText.Trim();
        item.PrimaryCtaUrl = request.PrimaryCtaUrl.Trim();
        item.SecondaryCtaText = request.SecondaryCtaText.Trim();
        item.SecondaryCtaUrl = request.SecondaryCtaUrl.Trim();
        item.LogoUrl = Clean(request.LogoUrl);
        item.UpdatedAtUtc = _clock.UtcNow;

        var errors = new Dictionary<string, string[]>();
        ValidateUrl(errors, nameof(item.FacebookUrl), item.FacebookUrl);
        ValidateUrl(errors, nameof(item.InstagramUrl), item.InstagramUrl);
        ValidateUrl(errors, nameof(item.YouTubeUrl), item.YouTubeUrl);
        ValidateUrl(errors, nameof(item.PrimaryCtaUrl), item.PrimaryCtaUrl);
        ValidateUrl(errors, nameof(item.SecondaryCtaUrl), item.SecondaryCtaUrl);
        ValidateUrl(errors, nameof(item.LogoUrl), item.LogoUrl);
        ValidateAndThrow(item, errors);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<PagedResultDto<AdminContentBlockDto>> GetContentBlocksAsync(AdminContentBlockOptions options, CancellationToken cancellationToken)
    {
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        var result = await _repository.GetContentBlocksAsync(normalized, cancellationToken);
        return MapPage(result, page, size, Map);
    }

    public async Task<AdminContentBlockDto> GetContentBlockAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<ContentBlock>(id, cancellationToken), id, "Content block"));

    public async Task<AdminContentBlockDto> CreateContentBlockAsync(ContentBlockWriteDto request, CancellationToken cancellationToken)
    {
        var key = request.Key.Trim();
        var normalizedKey = key.ToLowerInvariant();
        if (await _repository.ExistsAsync<ContentBlock>(item => item.Key.ToLower() == normalizedKey, cancellationToken))
            throw new CmsConflictException($"Content block key '{key}' already exists.");
        var item = new ContentBlock { CreatedAtUtc = _clock.UtcNow };
        Apply(item, request, includeKey: true);
        ValidateContentBlock(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminContentBlockDto> UpdateContentBlockAsync(Guid id, ContentBlockWriteDto request, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<ContentBlock>(id, cancellationToken), id, "Content block");
        if (!string.Equals(item.Key, request.Key.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new CmsConflictException("Content block keys are stable and cannot be changed.");
        Apply(item, request, includeKey: false);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateContentBlock(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteContentBlockAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<ContentBlock>(id, cancellationToken), id, "Content block");
        _repository.Remove(item);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(ContentBlock item, ContentBlockWriteDto request, bool includeKey)
    {
        if (includeKey) item.Key = request.Key.Trim();
        item.Title = request.Title.Trim();
        item.Summary = Clean(request.Summary);
        item.Body = request.Body.Trim();
        item.ImageUrl = Clean(request.ImageUrl);
        item.CtaText = Clean(request.CtaText);
        item.CtaUrl = Clean(request.CtaUrl);
        item.DisplayOrder = request.DisplayOrder;
        item.IsPublished = request.IsPublished;
    }

    private void ValidateContentBlock(ContentBlock item)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateDisplayOrder(errors, item.DisplayOrder);
        ValidateUrl(errors, nameof(item.ImageUrl), item.ImageUrl);
        ValidateUrl(errors, nameof(item.CtaUrl), item.CtaUrl);
        ValidateAndThrow(item, errors);
    }
}
