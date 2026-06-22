using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class MediaRepository(El1teDbContext dbContext) : IMediaRepository
{
    public async Task<AdminPage<MediaAsset>> GetAsync(AdminMediaOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.MediaAssets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(asset => asset.Title.Contains(search) || asset.OriginalFileName.Contains(search) || asset.AltText.Contains(search));
        }
        if (options.IsActive.HasValue) query = query.Where(asset => asset.IsActive == options.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(asset => asset.CreatedAtUtc)
            .Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(cancellationToken);
        return new AdminPage<MediaAsset>(items, count);
    }

    public Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MediaAssets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);
    public Task<MediaAsset?> GetActiveAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MediaAssets.AsNoTracking().FirstOrDefaultAsync(asset => asset.Id == id && asset.IsActive, cancellationToken);
    public async Task AddAsync(MediaAsset asset, CancellationToken cancellationToken) => await dbContext.MediaAssets.AddAsync(asset, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    public void Delete(MediaAsset asset) => dbContext.MediaAssets.Remove(asset);

    public async Task<bool> IsReferencedAsync(Guid id, string publicUrl, CancellationToken cancellationToken) =>
        await dbContext.GalleryAlbumMedia.AnyAsync(item => item.MediaAssetId == id, cancellationToken) ||
        await dbContext.GalleryAlbums.AnyAsync(album => album.CoverMediaAssetId == id, cancellationToken) ||
        await dbContext.Announcements.AnyAsync(item => item.ImageUrl == publicUrl, cancellationToken) ||
        await dbContext.Events.AnyAsync(item => item.ImageUrl == publicUrl, cancellationToken) ||
        await dbContext.Coaches.AnyAsync(item => item.ImageUrl == publicUrl, cancellationToken) ||
        await dbContext.Sponsors.AnyAsync(item => item.LogoUrl == publicUrl, cancellationToken) ||
        await dbContext.ContentBlocks.AnyAsync(item => item.ImageUrl == publicUrl, cancellationToken) ||
        await dbContext.SiteSettings.AnyAsync(item => item.LogoUrl == publicUrl, cancellationToken);
}
