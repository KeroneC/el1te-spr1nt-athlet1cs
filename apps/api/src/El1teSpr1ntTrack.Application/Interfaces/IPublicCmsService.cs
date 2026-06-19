using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IPublicCmsService
{
    Task<PublicSiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicContentBlockDto>> GetContentBlocksAsync(CancellationToken cancellationToken);
    Task<PublicContentBlockDto?> GetContentBlockAsync(string key, CancellationToken cancellationToken);
    Task<PagedResultDto<PublicAnnouncementListItemDto>> GetAnnouncementsAsync(AnnouncementQueryOptions options, CancellationToken cancellationToken);
    Task<PublicAnnouncementDetailDto?> GetAnnouncementAsync(string slug, CancellationToken cancellationToken);
    Task<PagedResultDto<PublicEventListItemDto>> GetEventsAsync(EventQueryOptions options, CancellationToken cancellationToken);
    Task<PublicEventDetailDto?> GetEventAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicCoachDto>> GetCoachesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicSponsorDto>> GetSponsorsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicFaqDto>> GetFaqsAsync(string? category, CancellationToken cancellationToken);
    Task<ContactSubmissionCreatedResponse> CreateContactSubmissionAsync(CreateContactSubmissionRequest request, CancellationToken cancellationToken);
}
