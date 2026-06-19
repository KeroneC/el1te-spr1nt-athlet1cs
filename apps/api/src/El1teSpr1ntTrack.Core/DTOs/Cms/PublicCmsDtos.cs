using System.ComponentModel.DataAnnotations;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Cms;

public sealed record PublicSiteSettingsDto(
    string ClubName,
    string Slogan,
    string ContactEmail,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? FacebookUrl,
    string? InstagramUrl,
    string? YouTubeUrl,
    string PrimaryCtaText,
    string PrimaryCtaUrl,
    string SecondaryCtaText,
    string SecondaryCtaUrl,
    string? LogoUrl);

public sealed record PublicContentBlockDto(
    string Key,
    string Title,
    string? Summary,
    string Body,
    string? ImageUrl,
    string? CtaText,
    string? CtaUrl,
    int DisplayOrder);

public sealed record PublicAnnouncementListItemDto(
    string Title,
    string Slug,
    string Summary,
    string? ImageUrl,
    bool IsFeatured,
    DateTimeOffset? PublishDateUtc);

public sealed record PublicAnnouncementDetailDto(
    string Title,
    string Slug,
    string Summary,
    string Body,
    string? ImageUrl,
    bool IsFeatured,
    DateTimeOffset? PublishDateUtc);

public sealed record PublicEventListItemDto(
    string Title,
    string Slug,
    EventType EventType,
    DateTimeOffset StartDateTimeUtc,
    DateTimeOffset? EndDateTimeUtc,
    string LocationName,
    string? ImageUrl,
    bool IsFeatured);

public sealed record PublicEventDetailDto(
    string Title,
    string Slug,
    EventType EventType,
    DateTimeOffset StartDateTimeUtc,
    DateTimeOffset? EndDateTimeUtc,
    string LocationName,
    string? Address,
    string Description,
    string? RegistrationUrl,
    string? ImageUrl,
    bool IsFeatured);

public sealed record PublicCoachDto(
    string FirstName,
    string LastName,
    string Role,
    string Bio,
    string? ImageUrl,
    string? Email,
    int DisplayOrder);

public sealed record PublicSponsorDto(
    string Name,
    string Slug,
    SponsorTier Tier,
    string? LogoUrl,
    string? WebsiteUrl,
    string? Description,
    int DisplayOrder);

public sealed record PublicFaqDto(
    string Question,
    string Answer,
    string Category,
    int DisplayOrder);

public sealed class CreateContactSubmissionRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(50)]
    public string? Phone { get; init; }

    public InquiryType InquiryType { get; init; } = InquiryType.General;

    [Required, StringLength(4000)]
    public string Message { get; init; } = string.Empty;
}

public sealed record ContactSubmissionCreatedResponse(Guid Id, string Message);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
