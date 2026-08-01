using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class User : EntityBase
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Parent;

    public bool IsActive { get; set; } = true;

    public int SecurityVersion { get; set; } = 1;

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? FailedLoginWindowStartedUtc { get; set; }

    public DateTimeOffset? LockoutEndUtc { get; set; }

    public DateTimeOffset? LastSuccessfulLoginUtc { get; set; }

    public ICollection<Athlete> Athletes { get; set; } = new List<Athlete>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<MediaAsset> UploadedMedia { get; set; } = new List<MediaAsset>();

    public ICollection<AdminPasswordReset> PasswordResets { get; set; } = new List<AdminPasswordReset>();

    public ICollection<AdminMfaChallenge> MfaChallenges { get; set; } = new List<AdminMfaChallenge>();
}
