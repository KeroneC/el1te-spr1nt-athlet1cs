using System.ComponentModel.DataAnnotations;

namespace El1teSpr1ntTrack.Core.DTOs.Cms;

public sealed record AdminAllAmericanYearListItemDto(
    Guid Id, int Year, string Slug, string Title, string Summary, int AthleteCount, int MedalCount,
    Guid? HeroMediaAssetId, string? HeroImageUrl, bool IsPublished, bool DetailsComplete,
    int DisplayOrder, int ImageCount, int RecipientCount, int PerformanceCount,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminAllAmericanYearMediaDto(
    Guid Id, Guid MediaAssetId, string PublicUrl, string Title, string AltText, string? Caption,
    string? AltTextOverride, string? CaptionOverride, int DisplayOrder, int Width, int Height);

public sealed record AdminAllAmericanRecipientDto(
    Guid Id, string FirstName, string LastName, Guid? PhotoMediaAssetId, string? PhotoUrl,
    string? PhotoAltText, int DisplayOrder, bool IsActive);

public sealed record AdminAllAmericanPerformanceDto(
    Guid Id, string EventName, string? Division, int? Placement, bool IsRelay,
    int DisplayOrder, bool IsActive, IReadOnlyList<Guid> RecipientIds);

public sealed record AdminAllAmericanYearDto(
    Guid Id, int Year, string Slug, string Title, string Summary, int AthleteCount, int MedalCount,
    Guid? HeroMediaAssetId, bool IsPublished, bool DetailsComplete, int DisplayOrder,
    IReadOnlyList<AdminAllAmericanYearMediaDto> Media,
    IReadOnlyList<AdminAllAmericanRecipientDto> Recipients,
    IReadOnlyList<AdminAllAmericanPerformanceDto> Performances,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed class AllAmericanYearWriteDto
{
    [Range(1900, 2100)] public int Year { get; init; }
    [Required, StringLength(200)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(4000)] public string Summary { get; init; } = string.Empty;
    [Range(0, int.MaxValue)] public int AthleteCount { get; init; }
    [Range(0, int.MaxValue)] public int MedalCount { get; init; }
    public Guid? HeroMediaAssetId { get; init; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; init; }
    public bool IsPublished { get; init; }
    public bool DetailsComplete { get; init; }
}

public sealed class AllAmericanYearMediaWriteDto
{
    [Required] public Guid MediaAssetId { get; init; }
    [StringLength(500)] public string? AltTextOverride { get; init; }
    [StringLength(1000)] public string? CaptionOverride { get; init; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; init; }
}

public sealed class AllAmericanRecipientWriteDto
{
    [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
    public Guid? PhotoMediaAssetId { get; init; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class AllAmericanPerformanceWriteDto
{
    [Required, StringLength(200)] public string EventName { get; init; } = string.Empty;
    [StringLength(200)] public string? Division { get; init; }
    [Range(1, int.MaxValue)] public int? Placement { get; init; }
    public bool IsRelay { get; init; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
    [MinLength(1)] public IReadOnlyList<Guid> RecipientIds { get; init; } = [];
}

public sealed record AllAmericanOrderItemDto(Guid Id, int DisplayOrder);
public sealed record AllAmericanOrderDto(IReadOnlyList<AllAmericanOrderItemDto> Items);

public sealed record PublicAllAmericanYearListItemDto(
    int Year, string Slug, string Title, string Summary, int AthleteCount, int MedalCount,
    string? HeroImageUrl, string? HeroAltText, int ImageCount);

public sealed record PublicAllAmericanImageDto(
    string PublicUrl, string AltText, string? Caption, int Width, int Height, int DisplayOrder);

public sealed record PublicAllAmericanResultDto(
    string EventName, string? Division, int? Placement, bool IsRelay, int DisplayOrder);

public sealed record PublicAllAmericanRecipientDto(
    string FirstName, string LastName, string? PhotoUrl, string? PhotoAltText,
    int DisplayOrder, IReadOnlyList<PublicAllAmericanResultDto> Results);

public sealed record PublicAllAmericanYearDto(
    int Year, string Slug, string Title, string Summary, int AthleteCount, int MedalCount,
    string? HeroImageUrl, string? HeroAltText, bool DetailsComplete,
    IReadOnlyList<PublicAllAmericanImageDto> Images,
    IReadOnlyList<PublicAllAmericanRecipientDto> Recipients);
