namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Faq : CmsEntityBase
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
