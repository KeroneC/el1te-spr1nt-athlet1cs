namespace El1teSpr1ntTrack.Core.Entities;

public sealed class SiteSetting : CmsEntityBase
{
    public string ClubName { get; set; } = string.Empty;

    public string Slogan { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public string? FacebookUrl { get; set; }

    public string? InstagramUrl { get; set; }

    public string? YouTubeUrl { get; set; }

    public string PrimaryCtaText { get; set; } = string.Empty;

    public string PrimaryCtaUrl { get; set; } = string.Empty;

    public string SecondaryCtaText { get; set; } = string.Empty;

    public string SecondaryCtaUrl { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }
}
