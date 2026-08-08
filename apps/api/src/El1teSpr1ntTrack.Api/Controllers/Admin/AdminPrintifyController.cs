using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
[Route("api/admin/store/printify")]
[Tags("Admin - Store - Printify")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminPrintifyController(IPrintifyAdminService service) : ControllerBase
{
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken) =>
        Ok(await service.GetHealthAsync(cancellationToken));

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        Ok(await service.PreviewAsync(cancellationToken));

    [HttpPost("import")]
    public async Task<IActionResult> Import(
        PrintifyCatalogImportRequestDto request,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await service.ImportAsync(request, CurrentUserId(), cancellationToken));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken) =>
        Ok(await service.RefreshMappingsAsync(cancellationToken));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
