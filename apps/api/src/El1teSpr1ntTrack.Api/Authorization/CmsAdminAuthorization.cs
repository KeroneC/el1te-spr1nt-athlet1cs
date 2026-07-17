using System.Security.Claims;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace El1teSpr1ntTrack.Api.Authorization;

public static class CmsAdminAuthorization
{
    public const string PolicyName = "CmsAdmin";
    public const string SuperAdminPolicyName = "SuperAdmin";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(PolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.SuperAdmin));
            policy.AddRequirements(new ActiveCmsAdminRequirement());
        });
        options.AddPolicy(SuperAdminPolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ActiveSuperAdminRequirement());
        });
    }
}

public sealed class ActiveCmsAdminRequirement : IAuthorizationRequirement;

public sealed class ActiveSuperAdminRequirement : IAuthorizationRequirement;

public sealed class ActiveCmsAdminHandler(IUserRepository userRepository)
    : AuthorizationHandler<ActiveCmsAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveCmsAdminRequirement requirement)
    {
        var identifier = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(identifier, out var userId))
        {
            return;
        }

        var user = await userRepository.GetByIdAsync(userId);
        if (user is { IsActive: true } && user.Role is UserRole.Admin or UserRole.SuperAdmin)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class ActiveSuperAdminHandler(IUserRepository userRepository)
    : AuthorizationHandler<ActiveSuperAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSuperAdminRequirement requirement)
    {
        var identifier = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(identifier, out var userId)) return;
        var user = await userRepository.GetByIdAsync(userId);
        if (user is { IsActive: true, Role: UserRole.SuperAdmin }) context.Succeed(requirement);
    }
}
