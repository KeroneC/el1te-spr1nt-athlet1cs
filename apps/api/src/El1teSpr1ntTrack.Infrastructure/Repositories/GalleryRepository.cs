using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class GalleryRepository(El1teDbContext dbContext) : IGalleryRepository
{
    public async Task<AdminPage<GalleryAlbum>> GetAdminAsync(AdminGalleryOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.GalleryAlbums.AsNoTracking().Include(album => album.CoverMediaAsset).Include(album => album.Media).AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) { var search = options.Search.Trim(); query = query.Where(album => album.Title.Contains(search) || album.Description.Contains(search)); }
        if (options.IsPublished.HasValue) query = query.Where(album => album.IsPublished == options.IsPublished.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(album => album.DisplayOrder).ThenByDescending(album => album.EventDateUtc ?? album.CreatedAtUtc)
            .Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(cancellationToken);
        return new AdminPage<GalleryAlbum>(items, count);
    }

    public Task<GalleryAlbum?> GetAdminAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.GalleryAlbums.Include(album => album.CoverMediaAsset)
            .Include(album => album.Media).ThenInclude(item => item.MediaAsset)
            .FirstOrDefaultAsync(album => album.Id == id, cancellationToken);
    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        dbContext.GalleryAlbums.AnyAsync(album => album.Slug == slug && (!excludingId.HasValue || album.Id != excludingId), cancellationToken);
    public async Task AddAsync(GalleryAlbum album, CancellationToken cancellationToken) => await dbContext.GalleryAlbums.AddAsync(album, cancellationToken);
    public async Task AddMediaAsync(GalleryAlbumMedia albumMedia, CancellationToken cancellationToken) => await dbContext.GalleryAlbumMedia.AddAsync(albumMedia, cancellationToken);
    public Task<MediaAsset?> GetActiveMediaAsync(Guid id, CancellationToken cancellationToken) => dbContext.MediaAssets.FirstOrDefaultAsync(asset => asset.Id == id && asset.IsActive, cancellationToken);
    public Task<GalleryAlbumMedia?> GetAlbumMediaAsync(Guid albumId, Guid albumMediaId, CancellationToken cancellationToken) =>
        dbContext.GalleryAlbumMedia.FirstOrDefaultAsync(item => item.GalleryAlbumId == albumId && item.Id == albumMediaId, cancellationToken);
    public Task<bool> AlbumContainsMediaAsync(Guid albumId, Guid mediaId, CancellationToken cancellationToken) => dbContext.GalleryAlbumMedia.AnyAsync(item => item.GalleryAlbumId == albumId && item.MediaAssetId == mediaId, cancellationToken);
    public void Delete(GalleryAlbum album) => dbContext.GalleryAlbums.Remove(album);
    public void Delete(GalleryAlbumMedia albumMedia) => dbContext.GalleryAlbumMedia.Remove(albumMedia);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<PagedResultDto<PublicGalleryAlbumListItemDto>> GetPublicAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.GalleryAlbums.AsNoTracking().Where(album => album.IsPublished);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(album => album.DisplayOrder).ThenByDescending(album => album.EventDateUtc ?? album.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(album => new PublicGalleryAlbumListItemDto(album.Title, album.Slug, album.Description,
                album.CoverMediaAsset != null && album.CoverMediaAsset.IsActive ? album.CoverMediaAsset.PublicUrl + "?width=800" : null,
                album.CoverMediaAsset != null && album.CoverMediaAsset.IsActive ? album.CoverMediaAsset.AltText : null,
                album.EventDateUtc, album.Media.Count(item => item.MediaAsset.IsActive)))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<PublicGalleryAlbumListItemDto>(items, page, pageSize, count);
    }

    public Task<PublicGalleryAlbumDto?> GetPublicAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.GalleryAlbums.AsNoTracking().Where(album => album.IsPublished && album.Slug == slug)
            .Select(album => new PublicGalleryAlbumDto(album.Title, album.Slug, album.Description, album.EventDateUtc,
                album.Media.Where(item => item.MediaAsset.IsActive).OrderBy(item => item.DisplayOrder)
                    .Select(item => new PublicGalleryImageDto(item.MediaAsset.PublicUrl + "?width=1200",
                        item.AltTextOverride ?? item.MediaAsset.AltText,
                        item.CaptionOverride ?? item.MediaAsset.Caption,
                        item.MediaAsset.Width, item.MediaAsset.Height, item.DisplayOrder)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
