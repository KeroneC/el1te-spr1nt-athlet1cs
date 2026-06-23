namespace El1teSpr1ntTrack.Core.Entities;

public sealed class GalleryAlbum : CmsEntityBase
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CoverMediaAssetId { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? EventDateUtc { get; set; }
    public int DisplayOrder { get; set; }
    public MediaAsset? CoverMediaAsset { get; set; }
    public ICollection<GalleryAlbumMedia> Media { get; set; } = new List<GalleryAlbumMedia>();
}
