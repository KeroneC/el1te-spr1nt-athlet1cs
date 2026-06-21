using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IAdminCmsService
{
    Task<AdminSiteSettingsDto> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<AdminSiteSettingsDto> UpdateSiteSettingsAsync(SiteSettingWriteDto request, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminContentBlockDto>> GetContentBlocksAsync(AdminContentBlockOptions options, CancellationToken cancellationToken);
    Task<AdminContentBlockDto> GetContentBlockAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminContentBlockDto> CreateContentBlockAsync(ContentBlockWriteDto request, CancellationToken cancellationToken);
    Task<AdminContentBlockDto> UpdateContentBlockAsync(Guid id, ContentBlockWriteDto request, CancellationToken cancellationToken);
    Task DeleteContentBlockAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminAnnouncementDto>> GetAnnouncementsAsync(AdminAnnouncementOptions options, CancellationToken cancellationToken);
    Task<AdminAnnouncementDto> GetAnnouncementAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminAnnouncementDto> CreateAnnouncementAsync(AnnouncementWriteDto request, CancellationToken cancellationToken);
    Task<AdminAnnouncementDto> UpdateAnnouncementAsync(Guid id, AnnouncementWriteDto request, CancellationToken cancellationToken);
    Task DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminEventDto>> GetEventsAsync(AdminEventOptions options, CancellationToken cancellationToken);
    Task<AdminEventDto> GetEventAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminEventDto> CreateEventAsync(EventWriteDto request, CancellationToken cancellationToken);
    Task<AdminEventDto> UpdateEventAsync(Guid id, EventWriteDto request, CancellationToken cancellationToken);
    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminCoachDto>> GetCoachesAsync(AdminCoachOptions options, CancellationToken cancellationToken);
    Task<AdminCoachDto> GetCoachAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminCoachDto> CreateCoachAsync(CoachWriteDto request, CancellationToken cancellationToken);
    Task<AdminCoachDto> UpdateCoachAsync(Guid id, CoachWriteDto request, CancellationToken cancellationToken);
    Task DeactivateCoachAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminSponsorDto>> GetSponsorsAsync(AdminSponsorOptions options, CancellationToken cancellationToken);
    Task<AdminSponsorDto> GetSponsorAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminSponsorDto> CreateSponsorAsync(SponsorWriteDto request, CancellationToken cancellationToken);
    Task<AdminSponsorDto> UpdateSponsorAsync(Guid id, SponsorWriteDto request, CancellationToken cancellationToken);
    Task DeactivateSponsorAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminFaqDto>> GetFaqsAsync(AdminFaqOptions options, CancellationToken cancellationToken);
    Task<AdminFaqDto> GetFaqAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminFaqDto> CreateFaqAsync(FaqWriteDto request, CancellationToken cancellationToken);
    Task<AdminFaqDto> UpdateFaqAsync(Guid id, FaqWriteDto request, CancellationToken cancellationToken);
    Task DeactivateFaqAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminContactSubmissionDto>> GetContactSubmissionsAsync(AdminContactOptions options, CancellationToken cancellationToken);
    Task<AdminContactSubmissionDto> GetContactSubmissionAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminContactSubmissionDto> UpdateContactStatusAsync(Guid id, UpdateContactSubmissionStatusRequest request, CancellationToken cancellationToken);
    Task DeleteContactSubmissionAsync(Guid id, CancellationToken cancellationToken);
}
