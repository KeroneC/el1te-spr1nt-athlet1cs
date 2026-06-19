using System.ComponentModel.DataAnnotations;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Cms;

public sealed class SiteSettingWriteDto
{
    [Required, MaxLength(200)]
    public string ClubName { get; init; } = string.Empty;

    [Required, MaxLength(300)]
    public string Slogan { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string ContactEmail { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [MaxLength(200)]
    public string? AddressLine1 { get; init; }

    [MaxLength(200)]
    public string? AddressLine2 { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(50)]
    public string? State { get; init; }

    [MaxLength(20)]
    public string? ZipCode { get; init; }

    [Url, MaxLength(500)]
    public string? FacebookUrl { get; init; }

    [Url, MaxLength(500)]
    public string? InstagramUrl { get; init; }

    [Url, MaxLength(500)]
    public string? YouTubeUrl { get; init; }

    [Required, MaxLength(100)]
    public string PrimaryCtaText { get; init; } = string.Empty;

    [Required, MaxLength(500)]
    public string PrimaryCtaUrl { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string SecondaryCtaText { get; init; } = string.Empty;

    [Required, MaxLength(500)]
    public string SecondaryCtaUrl { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; init; }
}

public sealed class ContentBlockWriteDto
{
    [Required, MaxLength(150)]
    public string Key { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Summary { get; init; }

    [Required]
    public string Body { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    [MaxLength(100)]
    public string? CtaText { get; init; }

    [MaxLength(500)]
    public string? CtaUrl { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsPublished { get; init; }
}

public sealed class AnnouncementWriteDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Summary { get; init; } = string.Empty;

    [Required]
    public string Body { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsFeatured { get; init; }

    public bool IsPublished { get; init; }

    public DateTimeOffset? PublishDateUtc { get; init; }

    public DateTimeOffset? ExpirationDateUtc { get; init; }
}

public sealed class EventWriteDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    public EventType EventType { get; init; }

    public DateTimeOffset StartDateTimeUtc { get; init; }

    public DateTimeOffset? EndDateTimeUtc { get; init; }

    [Required, MaxLength(200)]
    public string LocationName { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; init; }

    [Required]
    public string Description { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? RegistrationUrl { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsFeatured { get; init; }

    public bool IsPublished { get; init; }
}

public sealed class CoachWriteDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string Role { get; init; } = string.Empty;

    [Required]
    public string Bio { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    public bool IsEmailPublic { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class SponsorWriteDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    public SponsorTier Tier { get; init; }

    [MaxLength(500)]
    public string? LogoUrl { get; init; }

    [Url, MaxLength(500)]
    public string? WebsiteUrl { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class FaqWriteDto
{
    [Required, MaxLength(500)]
    public string Question { get; init; } = string.Empty;

    [Required]
    public string Answer { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Category { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class ContactSubmissionCreateDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Phone { get; init; }

    public InquiryType InquiryType { get; init; }

    [Required, MaxLength(5000)]
    public string Message { get; init; } = string.Empty;
}
