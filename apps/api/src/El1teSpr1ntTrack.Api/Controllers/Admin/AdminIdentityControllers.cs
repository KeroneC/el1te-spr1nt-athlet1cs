using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
[Route("api/admin/users")]
[Tags("Admin - Users")]
public sealed class AdminUsersController(IAdminIdentityService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, UserRole? role, bool? isActive, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetUsersAsync(new AdminUserOptions(search, role, isActive, page, pageSize), token));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateAdminUserRequest request, CancellationToken token) =>
        Ok(await service.UpdateUserAsync(CurrentUserId(), id, request, HttpContext.TraceIdentifier, token));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
[Route("api/admin/invitations")]
[Tags("Admin - Invitations")]
public sealed class AdminInvitationsController(IAdminIdentityService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, string? status, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetInvitationsAsync(new AdminInvitationOptions(search, status, page, pageSize), token));

    [HttpPost]
    public async Task<IActionResult> Create(CreateAdminInvitationRequest request, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateInvitationAsync(CurrentUserId(), request, HttpContext.TraceIdentifier, token));

    [HttpPost("{id:guid}/reissue")]
    public async Task<IActionResult> Reissue(Guid id, CancellationToken token) =>
        Ok(await service.ResendInvitationAsync(CurrentUserId(), id, HttpContext.TraceIdentifier, token));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken token)
    {
        await service.RevokeInvitationAsync(CurrentUserId(), id, HttpContext.TraceIdentifier, token);
        return NoContent();
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
[Route("api/admin/activity")]
[Tags("Admin - Activity")]
public sealed class AdminActivityController(IAdminIdentityService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, string? action, DateTimeOffset? fromDate, DateTimeOffset? toDate, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetActivityAsync(new AdminActivityOptions(search, action, fromDate, toDate, page, pageSize), token));
}
