using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.DTOs.Cms;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IAdminIdentityService
{
    Task<PagedResultDto<AdminUserDto>> GetUsersAsync(AdminUserOptions options, CancellationToken cancellationToken);
    Task<AdminUserDto> UpdateUserAsync(Guid actorUserId, Guid targetUserId, UpdateAdminUserRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminInvitationDto>> GetInvitationsAsync(AdminInvitationOptions options, CancellationToken cancellationToken);
    Task<AdminInvitationCreatedDto> CreateInvitationAsync(Guid actorUserId, CreateAdminInvitationRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<AdminInvitationCreatedDto> ResendInvitationAsync(Guid actorUserId, Guid invitationId, string? correlationId, CancellationToken cancellationToken);
    Task RevokeInvitationAsync(Guid actorUserId, Guid invitationId, string? correlationId, CancellationToken cancellationToken);
    Task<AdminInvitationDetailsDto> InspectInvitationAsync(InspectAdminInvitationRequest request, CancellationToken cancellationToken);
    Task AcceptInvitationAsync(AcceptAdminInvitationRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminActivityLogDto>> GetActivityAsync(AdminActivityOptions options, CancellationToken cancellationToken);
}
