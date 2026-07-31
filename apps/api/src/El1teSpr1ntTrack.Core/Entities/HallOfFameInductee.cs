namespace El1teSpr1ntTrack.Core.Entities;

public sealed class HallOfFameInductee : CmsEntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? PhotoAlt { get; set; }
    public int? InductionYear { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
