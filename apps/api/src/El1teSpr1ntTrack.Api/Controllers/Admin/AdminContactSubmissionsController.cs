using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/contact-submissions")]
[Tags("Admin - Contact Submissions")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminContactSubmissionsController(IAdminCmsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, ContactSubmissionStatus? status, InquiryType? inquiryType, DateTimeOffset? fromDate, DateTimeOffset? toDate, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetContactSubmissionsAsync(new AdminContactOptions(search, status, inquiryType, fromDate, toDate, page, pageSize), token));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetContactSubmissionAsync(id, token));
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateContactSubmissionStatusRequest request, CancellationToken token) => Ok(await service.UpdateContactStatusAsync(id, request, token));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) { await service.DeleteContactSubmissionAsync(id, token); return NoContent(); }
}
