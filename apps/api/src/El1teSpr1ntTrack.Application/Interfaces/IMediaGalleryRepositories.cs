using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IMediaRepository
{
    Task<AdminPage<MediaAsset>> GetAsync(AdminMediaOptions options, CancellationToken cancellationToken);
    Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<MediaAsset?> GetActiveAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> IsReferencedAsync(Guid id, string publicUrl, CancellationToken cancellationToken);
    void Delete(MediaAsset asset);
}

public interface IGalleryRepository
{
    Task<AdminPage<GalleryAlbum>> GetAdminAsync(AdminGalleryOptions options, CancellationToken cancellationToken);
    Task<GalleryAlbum?> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(GalleryAlbum album, CancellationToken cancellationToken);
    Task AddMediaAsync(GalleryAlbumMedia albumMedia, CancellationToken cancellationToken);
    Task<MediaAsset?> GetActiveMediaAsync(Guid id, CancellationToken cancellationToken);
    Task<GalleryAlbumMedia?> GetAlbumMediaAsync(Guid albumId, Guid albumMediaId, CancellationToken cancellationToken);
    Task<bool> AlbumContainsMediaAsync(Guid albumId, Guid mediaId, CancellationToken cancellationToken);
    void Delete(GalleryAlbum album);
    void Delete(GalleryAlbumMedia albumMedia);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<PublicGalleryAlbumListItemDto>> GetPublicAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PublicGalleryAlbumDto?> GetPublicAsync(string slug, CancellationToken cancellationToken);
}
