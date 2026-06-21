using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName), Route("api/admin/coaches"), Tags("Admin - Coaches")]
[ProducesResponseType(StatusCodes.Status401Unauthorized), ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminCoachesController(IAdminCmsService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(string? search, bool? isActive, int page = 1, int pageSize = 20, CancellationToken token = default) => Ok(await service.GetCoachesAsync(new AdminCoachOptions(search, isActive, page, pageSize), token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetCoachAsync(id, token));
    [HttpPost] public async Task<IActionResult> Create(CoachWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateCoachAsync(request, token));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, CoachWriteDto request, CancellationToken token) => Ok(await service.UpdateCoachAsync(id, request, token));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Deactivate(Guid id, CancellationToken token) { await service.DeactivateCoachAsync(id, token); return NoContent(); }
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName), Route("api/admin/sponsors"), Tags("Admin - Sponsors")]
[ProducesResponseType(StatusCodes.Status401Unauthorized), ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminSponsorsController(IAdminCmsService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(string? search, SponsorTier? tier, bool? isActive, int page = 1, int pageSize = 20, CancellationToken token = default) => Ok(await service.GetSponsorsAsync(new AdminSponsorOptions(search, tier, isActive, page, pageSize), token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetSponsorAsync(id, token));
    [HttpPost] public async Task<IActionResult> Create(SponsorWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateSponsorAsync(request, token));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, SponsorWriteDto request, CancellationToken token) => Ok(await service.UpdateSponsorAsync(id, request, token));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Deactivate(Guid id, CancellationToken token) { await service.DeactivateSponsorAsync(id, token); return NoContent(); }
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName), Route("api/admin/faqs"), Tags("Admin - FAQs")]
[ProducesResponseType(StatusCodes.Status401Unauthorized), ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminFaqsController(IAdminCmsService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(string? search, string? category, bool? isActive, int page = 1, int pageSize = 20, CancellationToken token = default) => Ok(await service.GetFaqsAsync(new AdminFaqOptions(search, category, isActive, page, pageSize), token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetFaqAsync(id, token));
    [HttpPost] public async Task<IActionResult> Create(FaqWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateFaqAsync(request, token));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, FaqWriteDto request, CancellationToken token) => Ok(await service.UpdateFaqAsync(id, request, token));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Deactivate(Guid id, CancellationToken token) { await service.DeactivateFaqAsync(id, token); return NoContent(); }
}
