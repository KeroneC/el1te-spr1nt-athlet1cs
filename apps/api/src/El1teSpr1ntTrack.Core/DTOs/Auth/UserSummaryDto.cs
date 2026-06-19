using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Auth;

public sealed class UserSummaryDto
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }
}
