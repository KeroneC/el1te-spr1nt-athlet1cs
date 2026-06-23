using System.ComponentModel.DataAnnotations;

namespace El1teSpr1ntTrack.Core.DTOs.Cms;

public sealed record AdminMediaAssetDto(
    Guid Id, string OriginalFileName, string ContentType, string FileExtension,
    long FileSizeBytes, int Width, int Height, string Title, string AltText,
    string? Caption, string PublicUrl, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed class MediaMetadataUpdateDto
{
    [Required, StringLength(200)] public string Title { get; init; } = string.Empty;
    [StringLength(500)] public string AltText { get; init; } = string.Empty;
    [StringLength(1000)] public string? Caption { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record AdminGalleryAlbumListItemDto(
    Guid Id, string Title, string Slug, string Description, Guid? CoverMediaAssetId,
    string? CoverImageUrl, bool IsPublished, DateTimeOffset? EventDateUtc,
    int DisplayOrder, int ImageCount, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminGalleryAlbumMediaDto(
    Guid Id, Guid MediaAssetId, string PublicUrl, string Title, string AltText,
    string? Caption, string? AltTextOverride, string? CaptionOverride,
    int DisplayOrder, bool IsActive, int Width, int Height);

public sealed record AdminGalleryAlbumDto(
    Guid Id, string Title, string Slug, string Description, Guid? CoverMediaAssetId,
    bool IsPublished, DateTimeOffset? EventDateUtc, int DisplayOrder,
    IReadOnlyList<AdminGalleryAlbumMediaDto> Media,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed class GalleryAlbumWriteDto
{
    [Required, StringLength(200)] public string Title { get; init; } = string.Empty;
    [StringLength(200)] public string? Slug { get; init; }
    [Required, StringLength(4000)] public string Description { get; init; } = string.Empty;
    public Guid? CoverMediaAssetId { get; init; }
    public bool IsPublished { get; init; }
    public DateTimeOffset? EventDateUtc { get; init; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; init; }
}

public sealed class GalleryAlbumMediaWriteDto
{
    [Required] public Guid MediaAssetId { get; init; }
    [StringLength(500)] public string? AltTextOverride { get; init; }
    [StringLength(1000)] public string? CaptionOverride { get; init; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; init; }
}

public sealed record GalleryMediaOrderItemDto(Guid AlbumMediaId, int DisplayOrder);
public sealed record GalleryMediaOrderDto(IReadOnlyList<GalleryMediaOrderItemDto> Items);

public sealed record PublicGalleryAlbumListItemDto(
    string Title, string Slug, string Description, string? CoverImageUrl,
    string? CoverAltText, DateTimeOffset? EventDateUtc, int ImageCount);

public sealed record PublicGalleryImageDto(
    string PublicUrl, string AltText, string? Caption, int Width, int Height, int DisplayOrder);

public sealed record PublicGalleryAlbumDto(
    string Title, string Slug, string Description, DateTimeOffset? EventDateUtc,
    IReadOnlyList<PublicGalleryImageDto> Images);
