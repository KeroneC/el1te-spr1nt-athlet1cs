using System.Linq.Expressions;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CmsEvent = El1teSpr1ntTrack.Core.Entities.Event;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class AdminCmsRepository(El1teDbContext dbContext) : IAdminCmsRepository
{
    public Task<SiteSetting?> GetSiteSettingsAsync(CancellationToken cancellationToken) =>
        dbContext.SiteSettings.OrderBy(item => item.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);

    public async Task<AdminPage<ContentBlock>> GetContentBlocksAsync(AdminContentBlockOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.ContentBlocks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            query = query.Where(item => item.Key.Contains(options.Search) || item.Title.Contains(options.Search));
        }
        if (options.IsPublished.HasValue) query = query.Where(item => item.IsPublished == options.IsPublished.Value);
        return await PageAsync(query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Key), options.Page, options.PageSize, cancellationToken);
    }

    public async Task<AdminPage<Announcement>> GetAnnouncementsAsync(AdminAnnouncementOptions options, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var query = dbContext.Announcements.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(item => item.Title.Contains(options.Search) || item.Summary.Contains(options.Search));
        if (options.IsPublished.HasValue) query = query.Where(item => item.IsPublished == options.IsPublished.Value);
        if (options.IsFeatured.HasValue) query = query.Where(item => item.IsFeatured == options.IsFeatured.Value);
        if (!options.IncludeExpired) query = query.Where(item => item.ExpirationDateUtc == null || item.ExpirationDateUtc > now);
        return await PageAsync(query.OrderByDescending(item => item.PublishDateUtc ?? item.CreatedAtUtc), options.Page, options.PageSize, cancellationToken);
    }

    public async Task<AdminPage<CmsEvent>> GetEventsAsync(AdminEventOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.Events.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(item => item.Title.Contains(options.Search) || item.LocationName.Contains(options.Search));
        if (options.EventType.HasValue) query = query.Where(item => item.EventType == options.EventType.Value);
        if (options.IsPublished.HasValue) query = query.Where(item => item.IsPublished == options.IsPublished.Value);
        if (options.IsFeatured.HasValue) query = query.Where(item => item.IsFeatured == options.IsFeatured.Value);
        if (options.FromDate.HasValue) query = query.Where(item => item.StartDateTimeUtc >= options.FromDate.Value);
        if (options.ToDate.HasValue) query = query.Where(item => item.StartDateTimeUtc <= options.ToDate.Value);
        return await PageAsync(query.OrderByDescending(item => item.StartDateTimeUtc), options.Page, options.PageSize, cancellationToken);
    }

    public async Task<AdminPage<Coach>> GetCoachesAsync(AdminCoachOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.Coaches.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(item => item.FirstName.Contains(options.Search) || item.LastName.Contains(options.Search) || item.Role.Contains(options.Search));
        if (options.IsActive.HasValue) query = query.Where(item => item.IsActive == options.IsActive.Value);
        return await PageAsync(query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.LastName), options.Page, options.PageSize, cancellationToken);
    }

    public async Task<AdminPage<Sponsor>> GetSponsorsAsync(AdminSponsorOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.Sponsors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(item => item.Name.Contains(options.Search));
        if (options.Tier.HasValue) query = query.Where(item => item.Tier == options.Tier.Value);
        if (options.IsActive.HasValue) query = query.Where(item => item.IsActive == options.IsActive.Value);
        return await PageAsync(query.OrderBy(item => item.Tier).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Name), options.Page, options.PageSize, cancellationToken);
    }

    public async Task<AdminPage<Faq>> GetFaqsAsync(AdminFaqOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.Faqs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(item => item.Question.Contains(options.Search) || item.Answer.Contains(options.Search));
        if (!string.IsNullOrWhiteSpace(options.Category)) query = query.Where(item => item.Category == options.Category);
        if (options.IsActive.HasValue) query = query.Where(item => item.IsActive == options.IsActive.Value);
        return await PageAsync(query.OrderBy(item => item.Category).ThenBy(item => item.DisplayOrder), options.Page, options.PageSize, cancellationToken);
    }

    public async Task<AdminPage<ContactSubmission>> GetContactSubmissionsAsync(AdminContactOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.ContactSubmissions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(item => item.Name.Contains(options.Search) || item.Email.Contains(options.Search) || item.Message.Contains(options.Search));
        if (options.Status.HasValue) query = query.Where(item => item.Status == options.Status.Value);
        if (options.InquiryType.HasValue) query = query.Where(item => item.InquiryType == options.InquiryType.Value);
        if (options.FromDate.HasValue) query = query.Where(item => item.CreatedAtUtc >= options.FromDate.Value);
        if (options.ToDate.HasValue) query = query.Where(item => item.CreatedAtUtc <= options.ToDate.Value);
        return await PageAsync(query.OrderByDescending(item => item.CreatedAtUtc), options.Page, options.PageSize, cancellationToken);
    }

    public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken) where T : CmsEntityBase =>
        dbContext.Set<T>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : CmsEntityBase =>
        dbContext.Set<T>().AsNoTracking().AnyAsync(predicate, cancellationToken);

    public async Task AddAsync<T>(T entity, CancellationToken cancellationToken) where T : CmsEntityBase =>
        await dbContext.Set<T>().AddAsync(entity, cancellationToken);

    public void Remove<T>(T entity) where T : CmsEntityBase => dbContext.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private static async Task<AdminPage<T>> PageAsync<T>(
        IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new AdminPage<T>(items, totalCount);
    }
}
