using El1teSpr1ntTrack.Application.Common;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IMediaStorage
{
    Task<StoredMediaFile> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken);
}

public interface IImageInspector
{
    InspectedImage Inspect(Stream stream, string originalFileName, string declaredContentType);
}

public interface IImageResizer
{
    PublicMediaFile Resize(Stream stream, string contentType, int width);
}
