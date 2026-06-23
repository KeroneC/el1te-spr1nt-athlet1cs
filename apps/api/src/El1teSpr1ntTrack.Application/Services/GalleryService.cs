using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class GalleryService(IGalleryRepository repository, ISlugGenerator slugGenerator, IClock clock) : IGalleryService
{
    public async Task<PagedResultDto<AdminGalleryAlbumListItemDto>> GetAdminAsync(AdminGalleryOptions options, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, options.Page); var size = Math.Clamp(options.PageSize, 1, 100);
        var result = await repository.GetAdminAsync(options with { Page = page, PageSize = size }, cancellationToken);
        return new(result.Items.Select(MapList).ToList(), page, size, result.TotalCount);
    }

    public async Task<AdminGalleryAlbumDto> GetAdminAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await RequireAlbum(id, cancellationToken));

    public async Task<AdminGalleryAlbumDto> CreateAsync(GalleryAlbumWriteDto request, CancellationToken cancellationToken)
    {
        await Validate(request, null, cancellationToken);
        var slugSource = string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug;
        var slug = await slugGenerator.GenerateUniqueAsync(slugSource!, (value, token) => repository.SlugExistsAsync(value, null, token), cancellationToken);
        var album = new GalleryAlbum { Title = request.Title.Trim(), Slug = slug, Description = request.Description.Trim(),
            CoverMediaAssetId = request.CoverMediaAssetId, IsPublished = request.IsPublished,
            EventDateUtc = request.EventDateUtc, DisplayOrder = request.DisplayOrder, CreatedAtUtc = clock.UtcNow };
        await repository.AddAsync(album, cancellationToken); await repository.SaveChangesAsync(cancellationToken); return Map(album);
    }

    public async Task<AdminGalleryAlbumDto> UpdateAsync(Guid id, GalleryAlbumWriteDto request, CancellationToken cancellationToken)
    {
        var album = await RequireAlbum(id, cancellationToken); await Validate(request, id, cancellationToken);
        var requestedSlug = slugGenerator.Generate(string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug);
        if (await repository.SlugExistsAsync(requestedSlug, id, cancellationToken)) throw new CmsConflictException("Another gallery album already uses this slug.");
        album.Title = request.Title.Trim(); album.Slug = requestedSlug; album.Description = request.Description.Trim();
        album.CoverMediaAssetId = request.CoverMediaAssetId; album.IsPublished = request.IsPublished;
        album.EventDateUtc = request.EventDateUtc; album.DisplayOrder = request.DisplayOrder; album.UpdatedAtUtc = clock.UtcNow;
        await repository.SaveChangesAsync(cancellationToken); return Map(album);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    { var album = await RequireAlbum(id, cancellationToken); repository.Delete(album); await repository.SaveChangesAsync(cancellationToken); }

    public async Task<AdminGalleryAlbumDto> AddMediaAsync(Guid albumId, GalleryAlbumMediaWriteDto request, CancellationToken cancellationToken)
    {
        var album = await RequireAlbum(albumId, cancellationToken); var media = await RequireActiveMedia(request.MediaAssetId, cancellationToken); ValidateMedia(request);
        if (await repository.AlbumContainsMediaAsync(albumId, request.MediaAssetId, cancellationToken)) throw new CmsConflictException("This media asset is already in the album.");
        var albumMedia = new GalleryAlbumMedia { GalleryAlbumId = album.Id, GalleryAlbum = album,
            MediaAssetId = request.MediaAssetId, MediaAsset = media, AltTextOverride = Clean(request.AltTextOverride),
            CaptionOverride = Clean(request.CaptionOverride), DisplayOrder = request.DisplayOrder, CreatedAtUtc = clock.UtcNow };
        await repository.AddMediaAsync(albumMedia, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken); return Map(await RequireAlbum(albumId, cancellationToken));
    }

    public async Task<AdminGalleryAlbumDto> UpdateMediaAsync(Guid albumId, Guid albumMediaId, GalleryAlbumMediaWriteDto request, CancellationToken cancellationToken)
    {
        await RequireAlbum(albumId, cancellationToken); ValidateMedia(request);
        var item = await repository.GetAlbumMediaAsync(albumId, albumMediaId, cancellationToken) ?? throw new CmsNotFoundException("Gallery image", albumMediaId);
        if (item.MediaAssetId != request.MediaAssetId)
        {
            await RequireActiveMedia(request.MediaAssetId, cancellationToken);
            if (await repository.AlbumContainsMediaAsync(albumId, request.MediaAssetId, cancellationToken)) throw new CmsConflictException("This media asset is already in the album.");
        }
        item.MediaAssetId = request.MediaAssetId; item.AltTextOverride = Clean(request.AltTextOverride);
        item.CaptionOverride = Clean(request.CaptionOverride); item.DisplayOrder = request.DisplayOrder; item.UpdatedAtUtc = clock.UtcNow;
        await repository.SaveChangesAsync(cancellationToken); return Map(await RequireAlbum(albumId, cancellationToken));
    }

    public async Task<AdminGalleryAlbumDto> RemoveMediaAsync(Guid albumId, Guid albumMediaId, CancellationToken cancellationToken)
    {
        var album = await RequireAlbum(albumId, cancellationToken);
        var item = await repository.GetAlbumMediaAsync(albumId, albumMediaId, cancellationToken) ?? throw new CmsNotFoundException("Gallery image", albumMediaId);
        if (album.CoverMediaAssetId == item.MediaAssetId) album.CoverMediaAssetId = null;
        repository.Delete(item); await repository.SaveChangesAsync(cancellationToken); return Map(await RequireAlbum(albumId, cancellationToken));
    }

    public async Task<AdminGalleryAlbumDto> ReorderAsync(Guid albumId, GalleryMediaOrderDto request, CancellationToken cancellationToken)
    {
        await RequireAlbum(albumId, cancellationToken);
        if (request.Items.Any(item => item.DisplayOrder < 0) || request.Items.Select(item => item.AlbumMediaId).Distinct().Count() != request.Items.Count)
            throw new CmsRequestValidationException(new Dictionary<string, string[]> { ["Items"] = ["Each gallery image requires one non-negative display order."] });
        foreach (var order in request.Items)
        {
            var item = await repository.GetAlbumMediaAsync(albumId, order.AlbumMediaId, cancellationToken) ?? throw new CmsNotFoundException("Gallery image", order.AlbumMediaId);
            item.DisplayOrder = order.DisplayOrder; item.UpdatedAtUtc = clock.UtcNow;
        }
        await repository.SaveChangesAsync(cancellationToken); return Map(await RequireAlbum(albumId, cancellationToken));
    }

    public Task<PagedResultDto<PublicGalleryAlbumListItemDto>> GetPublicAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        repository.GetPublicAsync(Math.Max(1, page), Math.Clamp(pageSize, 1, 50), cancellationToken);
    public Task<PublicGalleryAlbumDto?> GetPublicAsync(string slug, CancellationToken cancellationToken) => repository.GetPublicAsync(slug.Trim(), cancellationToken);

    private async Task Validate(GalleryAlbumWriteDto request, Guid? id, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title)) errors[nameof(request.Title)] = ["Title is required."];
        if (string.IsNullOrWhiteSpace(request.Description)) errors[nameof(request.Description)] = ["Description is required."];
        if (request.DisplayOrder < 0) errors[nameof(request.DisplayOrder)] = ["Display order cannot be negative."];
        if (request.CoverMediaAssetId.HasValue) await RequireActiveMedia(request.CoverMediaAssetId.Value, cancellationToken);
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }
    private static void ValidateMedia(GalleryAlbumMediaWriteDto request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.DisplayOrder < 0) errors[nameof(request.DisplayOrder)] = ["Display order cannot be negative."];
        if (request.AltTextOverride?.Length > 500) errors[nameof(request.AltTextOverride)] = ["Alt text must be 500 characters or fewer."];
        if (request.CaptionOverride?.Length > 1000) errors[nameof(request.CaptionOverride)] = ["Caption must be 1,000 characters or fewer."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }
    private async Task<GalleryAlbum> RequireAlbum(Guid id, CancellationToken token) => await repository.GetAdminAsync(id, token) ?? throw new CmsNotFoundException("Gallery album", id);
    private async Task<MediaAsset> RequireActiveMedia(Guid id, CancellationToken token) => await repository.GetActiveMediaAsync(id, token) ?? throw new CmsNotFoundException("Active media asset", id);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AdminGalleryAlbumListItemDto MapList(GalleryAlbum album) => new(album.Id, album.Title, album.Slug, album.Description,
        album.CoverMediaAssetId, album.CoverMediaAsset?.IsActive == true ? album.CoverMediaAsset.PublicUrl : null,
        album.IsPublished, album.EventDateUtc, album.DisplayOrder, album.Media.Count, album.CreatedAtUtc, album.UpdatedAtUtc);
    private static AdminGalleryAlbumDto Map(GalleryAlbum album) => new(album.Id, album.Title, album.Slug, album.Description,
        album.CoverMediaAssetId, album.IsPublished, album.EventDateUtc, album.DisplayOrder,
        album.Media.OrderBy(item => item.DisplayOrder).Select(item => new AdminGalleryAlbumMediaDto(item.Id, item.MediaAssetId,
            item.MediaAsset?.PublicUrl ?? string.Empty, item.MediaAsset?.Title ?? string.Empty, item.MediaAsset?.AltText ?? string.Empty,
            item.MediaAsset?.Caption, item.AltTextOverride, item.CaptionOverride, item.DisplayOrder,
            item.MediaAsset?.IsActive ?? false, item.MediaAsset?.Width ?? 0, item.MediaAsset?.Height ?? 0)).ToList(),
        album.CreatedAtUtc, album.UpdatedAtUtc);
}
