namespace El1teSpr1ntTrack.Core.Entities;

public sealed class MediaAsset : CmsEntityBase
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public User UploadedByUser { get; set; } = null!;
    public ICollection<GalleryAlbumMedia> AlbumMedia { get; set; } = new List<GalleryAlbumMedia>();
    public ICollection<GalleryAlbum> CoverForAlbums { get; set; } = new List<GalleryAlbum>();
}
