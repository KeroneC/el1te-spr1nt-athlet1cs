using Azure.Identity;
using Azure.Storage.Blobs;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed class AzureBlobMediaStorage : IMediaStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobMediaStorage(MediaStorageOptions options)
    {
        if (!Uri.TryCreate(options.BlobServiceUri, UriKind.Absolute, out var serviceUri))
            throw new InvalidOperationException("MediaStorage:BlobServiceUri must be an absolute URI.");
        if (string.IsNullOrWhiteSpace(options.ContainerName))
            throw new InvalidOperationException("MediaStorage:ContainerName is required.");

        _container = new BlobServiceClient(serviceUri, new DefaultAzureCredential())
            .GetBlobContainerClient(options.ContainerName);
    }

    public async Task<StoredMediaFile> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var key = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        await _container.GetBlobClient(key).UploadAsync(stream, overwrite: false, cancellationToken);
        return new StoredMediaFile(key);
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(ValidateKey(storageKey));
        if (!await blob.ExistsAsync(cancellationToken)) return null;
        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        await _container.GetBlobClient(ValidateKey(storageKey)).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken) =>
        await _container.GetBlobClient(ValidateKey(storageKey)).ExistsAsync(cancellationToken);

    private static string ValidateKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.StartsWith('/') || storageKey.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("The media storage key is invalid.");
        return storageKey.Replace('\\', '/');
    }
}
