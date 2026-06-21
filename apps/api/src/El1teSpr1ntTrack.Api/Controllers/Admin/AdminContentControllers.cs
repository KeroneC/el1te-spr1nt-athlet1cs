using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/site-settings")]
[Tags("Admin - Site Settings")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminSiteSettingsController(IAdminCmsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AdminSiteSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken token) => Ok(await service.GetSiteSettingsAsync(token));

    [HttpPut]
    [ProducesResponseType(typeof(AdminSiteSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(SiteSettingWriteDto request, CancellationToken token) =>
        Ok(await service.UpdateSiteSettingsAsync(request, token));
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/content-blocks")]
[Tags("Admin - Content Blocks")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminContentBlocksController(IAdminCmsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, bool? isPublished, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetContentBlocksAsync(new AdminContentBlockOptions(search, isPublished, page, pageSize), token));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetContentBlockAsync(id, token));

    [HttpPost]
    [ProducesResponseType(typeof(AdminContentBlockDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(ContentBlockWriteDto request, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateContentBlockAsync(request, token));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ContentBlockWriteDto request, CancellationToken token) =>
        Ok(await service.UpdateContentBlockAsync(id, request, token));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await service.DeleteContentBlockAsync(id, token);
        return NoContent();
    }
}
