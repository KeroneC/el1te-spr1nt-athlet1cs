using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class AdminInvitation : EntityBase
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Admin;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid InvitedByUserId { get; set; }

    public User? InvitedByUser { get; set; }

    public Guid? AcceptedUserId { get; set; }

    public User? AcceptedUser { get; set; }
}
