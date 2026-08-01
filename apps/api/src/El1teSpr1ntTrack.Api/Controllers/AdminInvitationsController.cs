using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("api/admin-invitations")]
[Tags("Admin Invitations")]
public sealed class AdminInvitationsController(IAdminIdentityService service) : ControllerBase
{
    [HttpPost("inspect")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Inspect(InspectAdminInvitationRequest request, CancellationToken token) =>
        Ok(await service.InspectInvitationAsync(request, token));

    [HttpPost("accept")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Accept(AcceptAdminInvitationRequest request, CancellationToken token)
    {
        await service.AcceptInvitationAsync(request, HttpContext.TraceIdentifier, token);
        return NoContent();
    }
}
