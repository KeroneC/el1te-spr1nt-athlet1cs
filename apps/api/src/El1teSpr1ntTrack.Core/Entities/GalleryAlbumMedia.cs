namespace El1teSpr1ntTrack.Core.Entities;

public sealed class GalleryAlbumMedia : CmsEntityBase
{
    public Guid GalleryAlbumId { get; set; }
    public Guid MediaAssetId { get; set; }
    public string? CaptionOverride { get; set; }
    public string? AltTextOverride { get; set; }
    public int DisplayOrder { get; set; }
    public GalleryAlbum GalleryAlbum { get; set; } = null!;
    public MediaAsset MediaAsset { get; set; } = null!;
}
