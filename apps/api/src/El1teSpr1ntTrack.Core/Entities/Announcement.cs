namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Announcement : CmsEntityBase
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishDateUtc { get; set; }

    public DateTimeOffset? ExpirationDateUtc { get; set; }
}
