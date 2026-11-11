namespace El1teSpr1ntTrack.Core.Entities;

public enum LegacyPhotoReview { NeedsReview, ApprovedForSocial, Restricted }

public sealed class LegacyPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sha256 { get; set; } = "";
    public long ByteLength { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateOnly? CapturedOn { get; set; }
    public LegacyPhotoReview Review { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public ICollection<LegacyPhotoSource> Sources { get; set; } = [];
}

public sealed class LegacyPhotoSource
{
    public string UrlSha256 { get; set; } = "";
    public Guid LegacyPhotoId { get; set; }
    public string SourceUrl { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public ICollection<LegacyPhotoPage> Pages { get; set; } = [];
}

public sealed class LegacyPhotoPage
{
    public string SourceUrlSha256 { get; set; } = "";
    public string PageUrlSha256 { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public string Collection { get; set; } = "";
}
