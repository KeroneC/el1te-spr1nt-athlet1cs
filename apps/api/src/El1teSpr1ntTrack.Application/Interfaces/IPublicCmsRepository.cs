using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IPublicCmsRepository
{
    Task<PublicSiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicContentBlockDto>> GetContentBlocksAsync(CancellationToken cancellationToken);
    Task<PublicContentBlockDto?> GetContentBlockAsync(string key, CancellationToken cancellationToken);
    Task<PagedResultDto<PublicAnnouncementListItemDto>> GetAnnouncementsAsync(AnnouncementQueryOptions options, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PublicAnnouncementDetailDto?> GetAnnouncementAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PagedResultDto<PublicEventListItemDto>> GetEventsAsync(EventQueryOptions options, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PublicEventDetailDto?> GetEventAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicCoachDto>> GetCoachesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicSponsorDto>> GetSponsorsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicFaqDto>> GetFaqsAsync(string? category, CancellationToken cancellationToken);
    Task AddContactSubmissionAsync(ContactSubmission submission, CancellationToken cancellationToken);
}
