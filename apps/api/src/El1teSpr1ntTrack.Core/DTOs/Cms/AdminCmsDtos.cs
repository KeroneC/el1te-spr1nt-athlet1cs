using System.ComponentModel.DataAnnotations;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Cms;

public sealed record AdminSiteSettingsDto(
    Guid Id, string ClubName, string Slogan, string ContactEmail, string? PhoneNumber,
    string? AddressLine1, string? AddressLine2, string? City, string? State, string? ZipCode,
    string? FacebookUrl, string? InstagramUrl, string? YouTubeUrl,
    string PrimaryCtaText, string PrimaryCtaUrl, string SecondaryCtaText, string SecondaryCtaUrl,
    string? LogoUrl, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminContentBlockDto(
    Guid Id, string Key, string Title, string? Summary, string Body, string? ImageUrl,
    string? CtaText, string? CtaUrl, int DisplayOrder, bool IsPublished,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminAnnouncementDto(
    Guid Id, string Title, string Slug, string Summary, string Body, string? ImageUrl,
    bool IsFeatured, bool IsPublished, DateTimeOffset? PublishDateUtc, DateTimeOffset? ExpirationDateUtc,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminEventDto(
    Guid Id, string Title, string Slug, EventType EventType, DateTimeOffset StartDateTimeUtc,
    DateTimeOffset? EndDateTimeUtc, string LocationName, string? Address, string Description,
    string? RegistrationUrl, string? ImageUrl, bool IsFeatured, bool IsPublished,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminCoachDto(
    Guid Id, string FirstName, string LastName, string Role, string Bio, string? ImageUrl,
    string? Email, bool IsEmailPublic, int DisplayOrder, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminSponsorDto(
    Guid Id, string Name, string Slug, SponsorTier Tier, string? LogoUrl, string? WebsiteUrl,
    string? Description, int DisplayOrder, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminFaqDto(
    Guid Id, string Question, string Answer, string Category, int DisplayOrder, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AdminContactSubmissionDto(
    Guid Id, string Name, string Email, string? Phone, InquiryType InquiryType, string Message,
    ContactSubmissionStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed class UpdateContactSubmissionStatusRequest
{
    [Required]
    public ContactSubmissionStatus Status { get; init; }
}
