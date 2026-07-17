using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class AdminIdentityService(
    IAdminIdentityRepository repository,
    IClock clock,
    AdminInvitationSettings settings) : IAdminIdentityService
{
    public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(AdminUserOptions options, CancellationToken cancellationToken)
    {
        ValidatePrivilegedRoleFilter(options.Role);
        var normalized = Normalize(options.Page, options.PageSize);
        var page = await repository.GetUsersAsync(options with { Page = normalized.Page, PageSize = normalized.PageSize }, cancellationToken);
        return new PagedResultDto<AdminUserDto>(page.Items.Select(MapUser).ToList(), normalized.Page, normalized.PageSize, page.TotalCount);
    }

    public async Task<AdminUserDto> UpdateUserAsync(Guid actorUserId, Guid targetUserId, UpdateAdminUserRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        ValidatePrivilegedRole(request.Role);
        AdminUserDto? result = null;
        await repository.ExecuteSerializableAsync(async token =>
        {
            var target = await repository.GetPrivilegedUserAsync(targetUserId, tracked: true, token)
                ?? throw new CmsNotFoundException("Admin user", targetUserId);
            var roleChanged = target.Role != request.Role;
            var activeChanged = target.IsActive != request.IsActive;
            if (!roleChanged && !activeChanged)
            {
                result = MapUser(target);
                return;
            }
            if (target.Id == actorUserId)
            {
                throw new CmsConflictException("You cannot change your own role or active status.");
            }
            if (target.Role == UserRole.SuperAdmin && target.IsActive && (request.Role != UserRole.SuperAdmin || !request.IsActive) &&
                await repository.CountActiveSuperAdminsAsync(token) <= 1)
            {
                throw new CmsConflictException("The final active SuperAdmin cannot be demoted or deactivated.");
            }

            var changes = new List<string>();
            if (roleChanged) changes.Add($"role {target.Role} to {request.Role}");
            if (activeChanged) changes.Add($"status {(target.IsActive ? "active" : "inactive")} to {(request.IsActive ? "active" : "inactive")}");
            target.Role = request.Role;
            target.IsActive = request.IsActive;
            target.UpdatedAt = clock.UtcNow;
            await repository.AddActivityAsync(Activity(actorUserId, "AdminUserUpdated", "User", target.Id,
                $"Changed {target.Email}: {string.Join("; ", changes)}.", correlationId), token);
            await repository.SaveChangesAsync(token);
            result = MapUser(target);
        }, cancellationToken);
        return result!;
    }

    public async Task<PagedResultDto<AdminInvitationDto>> GetInvitationsAsync(AdminInvitationOptions options, CancellationToken cancellationToken)
    {
        ValidateInvitationStatus(options.Status);
        var normalized = Normalize(options.Page, options.PageSize);
        var page = await repository.GetInvitationsAsync(options with { Page = normalized.Page, PageSize = normalized.PageSize }, clock.UtcNow, cancellationToken);
        return new PagedResultDto<AdminInvitationDto>(page.Items.Select(MapInvitation).ToList(), normalized.Page, normalized.PageSize, page.TotalCount);
    }

    public async Task<AdminInvitationCreatedDto> CreateInvitationAsync(Guid actorUserId, CreateAdminInvitationRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        var normalizedEmail = ValidateInvitationRequest(request);
        var token = CreateToken();
        AdminInvitation? invitation = null;
        await repository.ExecuteSerializableAsync(async ct =>
        {
            if (await repository.GetUserByEmailAsync(normalizedEmail, ct) is not null)
            {
                throw new CmsConflictException("A user with this email address already exists.");
            }
            if (await repository.HasPendingInvitationAsync(normalizedEmail, clock.UtcNow, ct))
            {
                throw new CmsConflictException("A pending invitation already exists for this email address.");
            }
            invitation = new AdminInvitation
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                Role = request.Role,
                TokenHash = HashToken(token),
                ExpiresAt = ExpiresAt(),
                InvitedByUserId = actorUserId,
                CreatedAt = clock.UtcNow
            };
            await repository.AddInvitationAsync(invitation, ct);
            await repository.AddActivityAsync(Activity(actorUserId, "AdminInvitationCreated", "AdminInvitation", invitation.Id,
                $"Created a {request.Role} invitation for {normalizedEmail}.", correlationId), ct);
            await repository.SaveChangesAsync(ct);
        }, cancellationToken);
        invitation = await repository.GetInvitationAsync(invitation!.Id, tracked: false, cancellationToken) ?? invitation;
        return new AdminInvitationCreatedDto(MapInvitation(invitation), BuildInvitationUrl(token));
    }

    public async Task<AdminInvitationCreatedDto> ResendInvitationAsync(Guid actorUserId, Guid invitationId, string? correlationId, CancellationToken cancellationToken)
    {
        var token = CreateToken();
        AdminInvitation? invitation = null;
        await repository.ExecuteSerializableAsync(async ct =>
        {
            invitation = await repository.GetInvitationAsync(invitationId, tracked: true, ct)
                ?? throw new CmsNotFoundException("Admin invitation", invitationId);
            if (invitation.AcceptedAt.HasValue) throw new CmsConflictException("An accepted invitation cannot be reissued.");
            if (invitation.RevokedAt.HasValue) throw new CmsConflictException("A revoked invitation cannot be reissued. Create a new invitation instead.");
            invitation.TokenHash = HashToken(token);
            invitation.ExpiresAt = ExpiresAt();
            invitation.UpdatedAt = clock.UtcNow;
            await repository.AddActivityAsync(Activity(actorUserId, "AdminInvitationReissued", "AdminInvitation", invitation.Id,
                $"Reissued the {invitation.Role} invitation for {invitation.Email}.", correlationId), ct);
            await repository.SaveChangesAsync(ct);
        }, cancellationToken);
        invitation = await repository.GetInvitationAsync(invitation!.Id, tracked: false, cancellationToken) ?? invitation;
        return new AdminInvitationCreatedDto(MapInvitation(invitation), BuildInvitationUrl(token));
    }

    public async Task RevokeInvitationAsync(Guid actorUserId, Guid invitationId, string? correlationId, CancellationToken cancellationToken)
    {
        await repository.ExecuteSerializableAsync(async ct =>
        {
            var invitation = await repository.GetInvitationAsync(invitationId, tracked: true, ct)
                ?? throw new CmsNotFoundException("Admin invitation", invitationId);
            if (invitation.AcceptedAt.HasValue) throw new CmsConflictException("An accepted invitation cannot be revoked.");
            if (invitation.RevokedAt.HasValue) return;
            invitation.RevokedAt = clock.UtcNow;
            invitation.UpdatedAt = clock.UtcNow;
            await repository.AddActivityAsync(Activity(actorUserId, "AdminInvitationRevoked", "AdminInvitation", invitation.Id,
                $"Revoked the {invitation.Role} invitation for {invitation.Email}.", correlationId), ct);
            await repository.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    public async Task<AdminInvitationDetailsDto> InspectInvitationAsync(InspectAdminInvitationRequest request, CancellationToken cancellationToken)
    {
        var invitation = await RequireValidInvitation(request.Token, tracked: false, cancellationToken);
        return new AdminInvitationDetailsDto(invitation.FirstName, invitation.LastName, invitation.Email, invitation.Role, invitation.ExpiresAt);
    }

    public async Task AcceptInvitationAsync(AcceptAdminInvitationRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        ValidatePassword(request.Password, request.ConfirmPassword);
        var tokenHash = ValidateAndHashToken(request.Token);
        await repository.ExecuteSerializableAsync(async ct =>
        {
            var invitation = await repository.GetInvitationByTokenHashAsync(tokenHash, tracked: true, ct);
            EnsureInvitationIsValid(invitation);
            if (await repository.GetUserByEmailAsync(invitation!.Email, ct) is not null)
            {
                throw new CmsConflictException("An account already exists for this email address.");
            }
            var user = new User
            {
                FirstName = invitation.FirstName,
                LastName = invitation.LastName,
                Email = invitation.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = invitation.Role,
                IsActive = true,
                CreatedAt = clock.UtcNow
            };
            await repository.AddUserAsync(user, ct);
            invitation.AcceptedAt = clock.UtcNow;
            invitation.AcceptedUserId = user.Id;
            invitation.UpdatedAt = clock.UtcNow;
            await repository.AddActivityAsync(Activity(user.Id, "AdminInvitationAccepted", "User", user.Id,
                $"Accepted a {user.Role} invitation.", correlationId), ct);
            await repository.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    public async Task<PagedResultDto<AdminActivityLogDto>> GetActivityAsync(AdminActivityOptions options, CancellationToken cancellationToken)
    {
        var normalized = Normalize(options.Page, options.PageSize);
        var page = await repository.GetActivityAsync(options with { Page = normalized.Page, PageSize = normalized.PageSize }, cancellationToken);
        return new PagedResultDto<AdminActivityLogDto>(page.Items.Select(MapActivity).ToList(), normalized.Page, normalized.PageSize, page.TotalCount);
    }

    private async Task<AdminInvitation> RequireValidInvitation(string token, bool tracked, CancellationToken cancellationToken)
    {
        var invitation = await repository.GetInvitationByTokenHashAsync(ValidateAndHashToken(token), tracked, cancellationToken);
        EnsureInvitationIsValid(invitation);
        return invitation!;
    }

    private void EnsureInvitationIsValid(AdminInvitation? invitation)
    {
        if (invitation is null || invitation.AcceptedAt.HasValue || invitation.RevokedAt.HasValue || invitation.ExpiresAt <= clock.UtcNow)
        {
            throw new CmsConflictException("This invitation is invalid, expired, revoked, or already accepted.");
        }
    }

    private string ValidateInvitationRequest(CreateAdminInvitationRequest request)
    {
        ValidatePrivilegedRole(request.Role);
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.FirstName)) errors[nameof(request.FirstName)] = ["First name is required."];
        if (string.IsNullOrWhiteSpace(request.LastName)) errors[nameof(request.LastName)] = ["Last name is required."];
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!IsValidEmail(email)) errors[nameof(request.Email)] = ["A valid email address is required."];
        if (request.FirstName?.Trim().Length > 100) errors[nameof(request.FirstName)] = ["First name must be 100 characters or fewer."];
        if (request.LastName?.Trim().Length > 100) errors[nameof(request.LastName)] = ["Last name must be 100 characters or fewer."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
        return email;
    }

    private static void ValidatePassword(string password, string confirmation)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors[nameof(AcceptAdminInvitationRequest.Password)] = ["Password must be 12 to 128 characters and include uppercase, lowercase, number, and symbol characters."];
        }
        if (password != confirmation) errors[nameof(AcceptAdminInvitationRequest.ConfirmPassword)] = ["Password and confirmation password do not match."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }

    private static void ValidatePrivilegedRoleFilter(UserRole? role)
    {
        if (role.HasValue) ValidatePrivilegedRole(role.Value);
    }

    private static void ValidatePrivilegedRole(UserRole role)
    {
        if (role is not UserRole.Admin and not UserRole.SuperAdmin)
        {
            throw new CmsRequestValidationException(new Dictionary<string, string[]> { ["Role"] = ["Role must be Admin or SuperAdmin."] });
        }
    }

    private static void ValidateInvitationStatus(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status) && status.Trim().ToLowerInvariant() is not ("pending" or "accepted" or "expired" or "revoked"))
        {
            throw new CmsRequestValidationException(new Dictionary<string, string[]> { ["Status"] = ["Status is invalid."] });
        }
    }

    private static (int Page, int PageSize) Normalize(int page, int pageSize) => (Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));
    private DateTimeOffset ExpiresAt() => clock.UtcNow.AddHours(Math.Clamp(settings.ExpiresHours, 1, 168));

    private string BuildInvitationUrl(string token) =>
        $"{settings.SiteUrl.TrimEnd('/')}/admin/invitations/accept#token={Uri.EscapeDataString(token)}";

    private static string CreateToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return token.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ValidateAndHashToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length is < 32 or > 200)
        {
            throw new CmsConflictException("This invitation is invalid, expired, revoked, or already accepted.");
        }
        return HashToken(token);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static AdminUserDto MapUser(User user) => new(user.Id, user.FirstName, user.LastName, user.Email, user.Role, user.IsActive, user.CreatedAt, user.UpdatedAt);

    private AdminInvitationDto MapInvitation(AdminInvitation value) => new(
        value.Id, value.FirstName, value.LastName, value.Email, value.Role, InvitationStatus(value), value.ExpiresAt, value.CreatedAt,
        value.InvitedByUser is null ? "Unknown administrator" : $"{value.InvitedByUser.FirstName} {value.InvitedByUser.LastName}".Trim());

    private string InvitationStatus(AdminInvitation value) => value.AcceptedAt.HasValue ? "Accepted" : value.RevokedAt.HasValue ? "Revoked" : value.ExpiresAt <= clock.UtcNow ? "Expired" : "Pending";

    private static AdminActivityLogDto MapActivity(AdminActivityLog value) => new(
        value.Id, value.CreatedAt,
        value.ActorUser is null ? "System" : $"{value.ActorUser.FirstName} {value.ActorUser.LastName}".Trim(),
        value.Action, value.TargetType, value.TargetId, value.Summary, value.CorrelationId);

    private AdminActivityLog Activity(Guid? actorUserId, string action, string targetType, Guid? targetId, string summary, string? correlationId) => new()
    {
        ActorUserId = actorUserId,
        Action = action,
        TargetType = targetType,
        TargetId = targetId,
        Summary = summary,
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId[..Math.Min(correlationId.Length, 100)],
        CreatedAt = clock.UtcNow
    };
}
