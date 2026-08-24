using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/all-americans")]
[Tags("Admin - All-Americans")]
public sealed class AdminAllAmericanArchiveController(IAllAmericanArchiveService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(string? search, bool? isPublished, int page = 1, int pageSize = 20, CancellationToken token = default) => Ok(await service.GetAdminAsync(new AdminAllAmericanYearOptions(search, isPublished, page, pageSize), token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetAdminAsync(id, token));
    [HttpPost] public async Task<IActionResult> Create(AllAmericanYearWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateAsync(request, token));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, AllAmericanYearWriteDto request, CancellationToken token) => Ok(await service.UpdateAsync(id, request, token));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Deactivate(Guid id, CancellationToken token) { await service.DeactivateAsync(id, token); return NoContent(); }
    [HttpPost("{id:guid}/media")] public async Task<IActionResult> AddMedia(Guid id, AllAmericanYearMediaWriteDto request, CancellationToken token) => StatusCode(201, await service.AddMediaAsync(id, request, token));
    [HttpPut("{id:guid}/media/{childId:guid}")] public async Task<IActionResult> UpdateMedia(Guid id, Guid childId, AllAmericanYearMediaWriteDto request, CancellationToken token) => Ok(await service.UpdateMediaAsync(id, childId, request, token));
    [HttpDelete("{id:guid}/media/{childId:guid}")] public async Task<IActionResult> RemoveMedia(Guid id, Guid childId, CancellationToken token) => Ok(await service.RemoveMediaAsync(id, childId, token));
    [HttpPut("{id:guid}/media/order")] public async Task<IActionResult> ReorderMedia(Guid id, AllAmericanOrderDto request, CancellationToken token) => Ok(await service.ReorderMediaAsync(id, request, token));
    [HttpPost("{id:guid}/recipients")] public async Task<IActionResult> AddRecipient(Guid id, AllAmericanRecipientWriteDto request, CancellationToken token) => StatusCode(201, await service.AddRecipientAsync(id, request, token));
    [HttpPut("{id:guid}/recipients/{childId:guid}")] public async Task<IActionResult> UpdateRecipient(Guid id, Guid childId, AllAmericanRecipientWriteDto request, CancellationToken token) => Ok(await service.UpdateRecipientAsync(id, childId, request, token));
    [HttpDelete("{id:guid}/recipients/{childId:guid}")] public async Task<IActionResult> DeactivateRecipient(Guid id, Guid childId, CancellationToken token) => Ok(await service.DeactivateRecipientAsync(id, childId, token));
    [HttpPost("{id:guid}/performances")] public async Task<IActionResult> AddPerformance(Guid id, AllAmericanPerformanceWriteDto request, CancellationToken token) => StatusCode(201, await service.AddPerformanceAsync(id, request, token));
    [HttpPut("{id:guid}/performances/{childId:guid}")] public async Task<IActionResult> UpdatePerformance(Guid id, Guid childId, AllAmericanPerformanceWriteDto request, CancellationToken token) => Ok(await service.UpdatePerformanceAsync(id, childId, request, token));
    [HttpDelete("{id:guid}/performances/{childId:guid}")] public async Task<IActionResult> DeactivatePerformance(Guid id, Guid childId, CancellationToken token) => Ok(await service.DeactivatePerformanceAsync(id, childId, token));
}
