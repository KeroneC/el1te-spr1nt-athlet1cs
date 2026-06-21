using System.Linq.Expressions;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IAdminCmsRepository
{
    Task<SiteSetting?> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<AdminPage<ContentBlock>> GetContentBlocksAsync(AdminContentBlockOptions options, CancellationToken cancellationToken);
    Task<AdminPage<Announcement>> GetAnnouncementsAsync(AdminAnnouncementOptions options, DateTimeOffset now, CancellationToken cancellationToken);
    Task<AdminPage<Event>> GetEventsAsync(AdminEventOptions options, CancellationToken cancellationToken);
    Task<AdminPage<Coach>> GetCoachesAsync(AdminCoachOptions options, CancellationToken cancellationToken);
    Task<AdminPage<Sponsor>> GetSponsorsAsync(AdminSponsorOptions options, CancellationToken cancellationToken);
    Task<AdminPage<Faq>> GetFaqsAsync(AdminFaqOptions options, CancellationToken cancellationToken);
    Task<AdminPage<ContactSubmission>> GetContactSubmissionsAsync(AdminContactOptions options, CancellationToken cancellationToken);
    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken) where T : CmsEntityBase;
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : CmsEntityBase;
    Task AddAsync<T>(T entity, CancellationToken cancellationToken) where T : CmsEntityBase;
    void Remove<T>(T entity) where T : CmsEntityBase;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
