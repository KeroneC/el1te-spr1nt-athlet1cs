using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IMediaService
{
    Task<PagedResultDto<AdminMediaAssetDto>> GetAsync(AdminMediaOptions options, CancellationToken cancellationToken);
    Task<AdminMediaAssetDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMediaAssetDto> UploadAsync(Stream stream, long length, string originalFileName,
        string contentType, string title, string altText, string? caption, Guid uploadedByUserId,
        CancellationToken cancellationToken);
    Task<AdminMediaAssetDto> UpdateAsync(Guid id, MediaMetadataUpdateDto request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<PublicMediaFile?> OpenPublicAsync(Guid id, CancellationToken cancellationToken);
}

public interface IGalleryService
{
    Task<PagedResultDto<AdminGalleryAlbumListItemDto>> GetAdminAsync(AdminGalleryOptions options, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> CreateAsync(GalleryAlbumWriteDto request, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> UpdateAsync(Guid id, GalleryAlbumWriteDto request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> AddMediaAsync(Guid albumId, GalleryAlbumMediaWriteDto request, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> UpdateMediaAsync(Guid albumId, Guid albumMediaId, GalleryAlbumMediaWriteDto request, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> RemoveMediaAsync(Guid albumId, Guid albumMediaId, CancellationToken cancellationToken);
    Task<AdminGalleryAlbumDto> ReorderAsync(Guid albumId, GalleryMediaOrderDto request, CancellationToken cancellationToken);
    Task<PagedResultDto<PublicGalleryAlbumListItemDto>> GetPublicAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PublicGalleryAlbumDto?> GetPublicAsync(string slug, CancellationToken cancellationToken);
}
