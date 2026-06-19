using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("api/public")]
[Tags("Public CMS")]
public sealed class PublicCmsController(IPublicCmsService cmsService) : ControllerBase
{
    [HttpGet("site-settings")]
    [ProducesResponseType(typeof(PublicSiteSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSiteSettings(CancellationToken cancellationToken)
    {
        var settings = await cmsService.GetSiteSettingsAsync(cancellationToken);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpGet("content-blocks")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicContentBlockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContentBlocks(CancellationToken cancellationToken) =>
        Ok(await cmsService.GetContentBlocksAsync(cancellationToken));

    [HttpGet("content-blocks/{key}")]
    [ProducesResponseType(typeof(PublicContentBlockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContentBlock(string key, CancellationToken cancellationToken)
    {
        var block = await cmsService.GetContentBlockAsync(key, cancellationToken);
        return block is null ? NotFound() : Ok(block);
    }

    [HttpGet("announcements")]
    [ProducesResponseType(typeof(PagedResultDto<PublicAnnouncementListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] bool? featured,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        Ok(await cmsService.GetAnnouncementsAsync(
            new AnnouncementQueryOptions(featured, page, pageSize), cancellationToken));

    [HttpGet("announcements/{slug}")]
    [ProducesResponseType(typeof(PublicAnnouncementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnnouncement(string slug, CancellationToken cancellationToken)
    {
        var announcement = await cmsService.GetAnnouncementAsync(slug, cancellationToken);
        return announcement is null ? NotFound() : Ok(announcement);
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(PagedResultDto<PublicEventListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] EventType? eventType,
        [FromQuery] bool? featured,
        [FromQuery] bool upcomingOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        Ok(await cmsService.GetEventsAsync(
            new EventQueryOptions(eventType, featured, upcomingOnly, page, pageSize), cancellationToken));

    [HttpGet("events/{slug}")]
    [ProducesResponseType(typeof(PublicEventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(string slug, CancellationToken cancellationToken)
    {
        var cmsEvent = await cmsService.GetEventAsync(slug, cancellationToken);
        return cmsEvent is null ? NotFound() : Ok(cmsEvent);
    }

    [HttpGet("coaches")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicCoachDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCoaches(CancellationToken cancellationToken) =>
        Ok(await cmsService.GetCoachesAsync(cancellationToken));

    [HttpGet("sponsors")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicSponsorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSponsors(CancellationToken cancellationToken) =>
        Ok(await cmsService.GetSponsorsAsync(cancellationToken));

    [HttpGet("faqs")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicFaqDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFaqs([FromQuery] string? category, CancellationToken cancellationToken) =>
        Ok(await cmsService.GetFaqsAsync(category, cancellationToken));

    [HttpPost("contact-submissions")]
    [ProducesResponseType(typeof(ContactSubmissionCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContactSubmission(
        CreateContactSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cmsService.CreateContactSubmissionAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (CmsRequestValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails(ToModelState(exception.Errors))
            {
                Title = "Contact submission is invalid.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var errorGroup in errors)
        {
            foreach (var error in errorGroup.Value)
            {
                modelState.AddModelError(errorGroup.Key, error);
            }
        }

        return modelState;
    }
}
