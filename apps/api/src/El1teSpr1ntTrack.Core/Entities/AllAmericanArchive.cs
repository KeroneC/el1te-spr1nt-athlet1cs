namespace El1teSpr1ntTrack.Core.Entities;

public sealed class AllAmericanYear : CmsEntityBase
{
    public int Year { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int AthleteCount { get; set; }
    public int MedalCount { get; set; }
    public Guid? HeroMediaAssetId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public bool DetailsComplete { get; set; }
    public MediaAsset? HeroMediaAsset { get; set; }
    public ICollection<AllAmericanYearMedia> Media { get; set; } = new List<AllAmericanYearMedia>();
    public ICollection<AllAmericanRecipient> Recipients { get; set; } = new List<AllAmericanRecipient>();
    public ICollection<AllAmericanPerformance> Performances { get; set; } = new List<AllAmericanPerformance>();
}

public sealed class AllAmericanYearMedia : CmsEntityBase
{
    public Guid AllAmericanYearId { get; set; }
    public Guid MediaAssetId { get; set; }
    public string? AltTextOverride { get; set; }
    public string? CaptionOverride { get; set; }
    public int DisplayOrder { get; set; }
    public AllAmericanYear AllAmericanYear { get; set; } = null!;
    public MediaAsset MediaAsset { get; set; } = null!;
}

public sealed class AllAmericanRecipient : CmsEntityBase
{
    public Guid AllAmericanYearId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? PhotoMediaAssetId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public AllAmericanYear AllAmericanYear { get; set; } = null!;
    public MediaAsset? PhotoMediaAsset { get; set; }
    public ICollection<AllAmericanPerformanceRecipient> PerformanceRecipients { get; set; } = new List<AllAmericanPerformanceRecipient>();
}

public sealed class AllAmericanPerformance : CmsEntityBase
{
    public Guid AllAmericanYearId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Division { get; set; }
    public int? Placement { get; set; }
    public bool IsRelay { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public AllAmericanYear AllAmericanYear { get; set; } = null!;
    public ICollection<AllAmericanPerformanceRecipient> Recipients { get; set; } = new List<AllAmericanPerformanceRecipient>();
}

public sealed class AllAmericanPerformanceRecipient : CmsEntityBase
{
    public Guid AllAmericanPerformanceId { get; set; }
    public Guid AllAmericanRecipientId { get; set; }
    public AllAmericanPerformance Performance { get; set; } = null!;
    public AllAmericanRecipient Recipient { get; set; } = null!;
}
