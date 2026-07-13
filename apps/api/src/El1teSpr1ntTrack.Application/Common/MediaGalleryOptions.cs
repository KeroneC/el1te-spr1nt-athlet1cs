namespace El1teSpr1ntTrack.Application.Common;

public sealed record AdminMediaOptions(string? Search, bool? IsActive, int Page = 1, int PageSize = 20);
public sealed record AdminGalleryOptions(string? Search, bool? IsPublished, int Page = 1, int PageSize = 20);

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";
    public string Provider { get; set; } = "Local";
    public string LocalRoot { get; set; } = "uploads";
    public string BlobServiceUri { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "media";
    public string PublicBaseUrl { get; set; } = "http://localhost:5126";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}

public sealed record StoredMediaFile(string StorageKey);
public sealed record InspectedImage(string ContentType, string Extension, int Width, int Height);
public sealed record PublicMediaFile(Stream Stream, string ContentType, long Length);
