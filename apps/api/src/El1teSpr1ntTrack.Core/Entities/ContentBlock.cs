namespace El1teSpr1ntTrack.Core.Entities;

public sealed class ContentBlock : CmsEntityBase
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string Body { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? CtaText { get; set; }

    public string? CtaUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }
}
