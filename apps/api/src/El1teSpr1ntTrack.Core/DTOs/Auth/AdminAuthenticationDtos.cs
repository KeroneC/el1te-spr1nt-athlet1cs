using System.ComponentModel.DataAnnotations;

namespace El1teSpr1ntTrack.Core.DTOs.Auth;

public sealed class AdminLoginRequestDto
{
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
    [Required, MaxLength(128)] public string Password { get; init; } = string.Empty;
}

public sealed record AdminLoginResultDto(
    bool RequiresMfa,
    AuthResponseDto? Authentication,
    string? ChallengeToken,
    DateTimeOffset? ChallengeExpiresAtUtc);

public sealed class AdminMfaVerifyRequestDto
{
    [Required, MaxLength(200)] public string ChallengeToken { get; init; } = string.Empty;
    [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; init; } = string.Empty;
}

public sealed class AdminPasswordResetRequestDto
{
    [Required, EmailAddress, MaxLength(256)] public string Email { get; init; } = string.Empty;
}

public sealed class AdminPasswordResetInspectDto
{
    [Required, MaxLength(200)] public string Token { get; init; } = string.Empty;
}

public sealed class AdminPasswordResetCompleteDto
{
    [Required, MaxLength(200)] public string Token { get; init; } = string.Empty;
    [Required, MaxLength(128)] public string Password { get; init; } = string.Empty;
    [Required, MaxLength(128)] public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed record AdminPasswordResetInspectionDto(bool IsValid);
public sealed record GenericAcceptedDto(string Message);
