using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/announcements")]
[Tags("Admin - Announcements")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminAnnouncementsController(IAdminCmsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, bool? isPublished, bool? isFeatured, bool includeExpired = false, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetAnnouncementsAsync(new AdminAnnouncementOptions(search, isPublished, isFeatured, includeExpired, page, pageSize), token));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetAnnouncementAsync(id, token));
    [HttpPost]
    public async Task<IActionResult> Create(AnnouncementWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateAnnouncementAsync(request, token));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AnnouncementWriteDto request, CancellationToken token) => Ok(await service.UpdateAnnouncementAsync(id, request, token));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) { await service.DeleteAnnouncementAsync(id, token); return NoContent(); }
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/events")]
[Tags("Admin - Events")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminEventsController(IAdminCmsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, EventType? eventType, bool? isPublished, bool? isFeatured, DateTimeOffset? fromDate, DateTimeOffset? toDate, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetEventsAsync(new AdminEventOptions(search, eventType, isPublished, isFeatured, fromDate, toDate, page, pageSize), token));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetEventAsync(id, token));
    [HttpPost]
    public async Task<IActionResult> Create(EventWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateEventAsync(request, token));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EventWriteDto request, CancellationToken token) => Ok(await service.UpdateEventAsync(id, request, token));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) { await service.DeleteEventAsync(id, token); return NoContent(); }
}
