namespace El1teSpr1ntTrack.Core.DTOs.Auth;

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public UserSummaryDto User { get; init; } = new();
}
