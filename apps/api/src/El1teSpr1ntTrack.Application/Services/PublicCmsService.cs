using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class PublicCmsService(
    IPublicCmsRepository repository,
    ICmsValidationService validationService,
    IClock clock) : IPublicCmsService
{
    public Task<PublicSiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken) =>
        repository.GetSiteSettingsAsync(cancellationToken);

    public Task<IReadOnlyList<PublicContentBlockDto>> GetContentBlocksAsync(CancellationToken cancellationToken) =>
        repository.GetContentBlocksAsync(cancellationToken);

    public Task<PublicContentBlockDto?> GetContentBlockAsync(string key, CancellationToken cancellationToken) =>
        repository.GetContentBlockAsync(key.Trim(), cancellationToken);

    public Task<PagedResultDto<PublicAnnouncementListItemDto>> GetAnnouncementsAsync(
        AnnouncementQueryOptions options,
        CancellationToken cancellationToken) =>
        repository.GetAnnouncementsAsync(Normalize(options), clock.UtcNow, cancellationToken);

    public Task<PublicAnnouncementDetailDto?> GetAnnouncementAsync(string slug, CancellationToken cancellationToken) =>
        repository.GetAnnouncementAsync(slug.Trim(), clock.UtcNow, cancellationToken);

    public Task<PagedResultDto<PublicEventListItemDto>> GetEventsAsync(
        EventQueryOptions options,
        CancellationToken cancellationToken) =>
        repository.GetEventsAsync(Normalize(options), clock.UtcNow, cancellationToken);

    public Task<PublicEventDetailDto?> GetEventAsync(string slug, CancellationToken cancellationToken) =>
        repository.GetEventAsync(slug.Trim(), cancellationToken);

    public Task<IReadOnlyList<PublicCoachDto>> GetCoachesAsync(CancellationToken cancellationToken) =>
        repository.GetCoachesAsync(cancellationToken);

    public Task<IReadOnlyList<PublicSponsorDto>> GetSponsorsAsync(CancellationToken cancellationToken) =>
        repository.GetSponsorsAsync(cancellationToken);

    public Task<IReadOnlyList<PublicFaqDto>> GetFaqsAsync(string? category, CancellationToken cancellationToken) =>
        repository.GetFaqsAsync(string.IsNullOrWhiteSpace(category) ? null : category.Trim(), cancellationToken);

    public async Task<ContactSubmissionCreatedResponse> CreateContactSubmissionAsync(
        CreateContactSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var submission = new ContactSubmission
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            InquiryType = request.InquiryType,
            Message = request.Message.Trim(),
            Status = ContactSubmissionStatus.New,
            CreatedAtUtc = clock.UtcNow
        };

        var errors = validationService.Validate(submission).ToDictionary(pair => pair.Key, pair => pair.Value);
        if (!Enum.IsDefined(request.InquiryType))
        {
            errors[nameof(request.InquiryType)] = ["InquiryType is invalid."];
        }

        if (errors.Count > 0)
        {
            throw new CmsRequestValidationException(errors);
        }

        await repository.AddContactSubmissionAsync(submission, cancellationToken);
        return new ContactSubmissionCreatedResponse(submission.Id, "Your message has been received.");
    }

    private static AnnouncementQueryOptions Normalize(AnnouncementQueryOptions options) =>
        options with { Page = Math.Max(1, options.Page), PageSize = Math.Clamp(options.PageSize, 1, 50) };

    private static EventQueryOptions Normalize(EventQueryOptions options) =>
        options with { Page = Math.Max(1, options.Page), PageSize = Math.Clamp(options.PageSize, 1, 50) };
}
