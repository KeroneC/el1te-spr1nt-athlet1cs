namespace El1teSpr1ntTrack.Core.Entities;

public sealed class AdminPasswordReset : EntityBase
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

public sealed class AdminMfaChallenge : EntityBase
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string ChallengeTokenHash { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public int FailedAttemptCount { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
}

public sealed class AuthenticationAttempt : EntityBase
{
    public string Purpose { get; set; } = string.Empty;
    public string PartitionHash { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
}

public sealed class MediaDerivative : EntityBase
{
    public Guid MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;
    public int RequestedWidth { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string ContentType { get; set; } = "image/webp";
    public string StorageKey { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}
