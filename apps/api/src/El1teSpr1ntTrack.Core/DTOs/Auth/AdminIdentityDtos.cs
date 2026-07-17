using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Auth;

public sealed record AdminUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record UpdateAdminUserRequest(UserRole Role, bool IsActive);

public sealed record CreateAdminInvitationRequest(
    string FirstName,
    string LastName,
    string Email,
    UserRole Role);

public sealed record AdminInvitationDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    string Status,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    string InvitedByDisplayName);

public sealed record AdminInvitationCreatedDto(
    AdminInvitationDto Invitation,
    string InvitationUrl);

public sealed record InspectAdminInvitationRequest(string Token);

public sealed record AdminInvitationDetailsDto(
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    DateTimeOffset ExpiresAtUtc);

public sealed record AcceptAdminInvitationRequest(
    string Token,
    string Password,
    string ConfirmPassword);

public sealed record AdminActivityLogDto(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    string ActorDisplayName,
    string Action,
    string TargetType,
    Guid? TargetId,
    string Summary,
    string? CorrelationId);
