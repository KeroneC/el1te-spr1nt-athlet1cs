using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class PublicCmsRepository(El1teDbContext dbContext) : IPublicCmsRepository
{
    public Task<PublicSiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken) =>
        dbContext.SiteSettings.AsNoTracking()
            .OrderBy(setting => setting.CreatedAtUtc)
            .Select(setting => new PublicSiteSettingsDto(
                setting.ClubName, setting.Slogan, setting.ContactEmail, setting.PhoneNumber,
                setting.AddressLine1, setting.AddressLine2, setting.City, setting.State, setting.ZipCode,
                setting.FacebookUrl, setting.InstagramUrl, setting.YouTubeUrl,
                setting.PrimaryCtaText, setting.PrimaryCtaUrl,
                setting.SecondaryCtaText, setting.SecondaryCtaUrl, setting.LogoUrl))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicContentBlockDto>> GetContentBlocksAsync(CancellationToken cancellationToken) =>
        await dbContext.ContentBlocks.AsNoTracking()
            .Where(PublicCmsVisibility.PublishedContentBlock)
            .OrderBy(block => block.DisplayOrder).ThenBy(block => block.Key)
            .Select(block => new PublicContentBlockDto(
                block.Key, block.Title, block.Summary, block.Body, block.ImageUrl,
                block.CtaText, block.CtaUrl, block.DisplayOrder))
            .ToListAsync(cancellationToken);

    public Task<PublicContentBlockDto?> GetContentBlockAsync(string key, CancellationToken cancellationToken) =>
        dbContext.ContentBlocks.AsNoTracking()
            .Where(block => block.IsPublished && block.Key == key)
            .Select(block => new PublicContentBlockDto(
                block.Key, block.Title, block.Summary, block.Body, block.ImageUrl,
                block.CtaText, block.CtaUrl, block.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResultDto<PublicAnnouncementListItemDto>> GetAnnouncementsAsync(
        AnnouncementQueryOptions options,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Announcements.AsNoTracking()
            .Where(PublicCmsVisibility.CurrentAnnouncement(now));

        if (options.Featured.HasValue)
        {
            query = query.Where(item => item.IsFeatured == options.Featured.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.PublishDateUtc ?? item.CreatedAtUtc)
            .ThenBy(item => item.Title)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(item => new PublicAnnouncementListItemDto(
                item.Title, item.Slug, item.Summary, item.ImageUrl,
                item.IsFeatured, item.PublishDateUtc))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<PublicAnnouncementListItemDto>(items, options.Page, options.PageSize, totalCount);
    }

    public Task<PublicAnnouncementDetailDto?> GetAnnouncementAsync(
        string slug,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.Announcements.AsNoTracking()
            .Where(PublicCmsVisibility.CurrentAnnouncement(now))
            .Where(item => item.Slug == slug)
            .Select(item => new PublicAnnouncementDetailDto(
                item.Title, item.Slug, item.Summary, item.Body, item.ImageUrl,
                item.IsFeatured, item.PublishDateUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResultDto<PublicEventListItemDto>> GetEventsAsync(
        EventQueryOptions options,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Events.AsNoTracking().Where(PublicCmsVisibility.PublishedEvent);
        if (options.EventType.HasValue)
        {
            query = query.Where(item => item.EventType == options.EventType.Value);
        }

        if (options.Featured.HasValue)
        {
            query = query.Where(item => item.IsFeatured == options.Featured.Value);
        }

        if (options.UpcomingOnly)
        {
            query = query.Where(PublicCmsVisibility.UpcomingEvent(now));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => (item.EndDateTimeUtc ?? item.StartDateTimeUtc) < now)
            .ThenBy(item => item.StartDateTimeUtc)
            .ThenBy(item => item.Title)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(item => new PublicEventListItemDto(
                item.Title, item.Slug, item.EventType, item.StartDateTimeUtc,
                item.EndDateTimeUtc, item.LocationName, item.ImageUrl, item.IsFeatured))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<PublicEventListItemDto>(items, options.Page, options.PageSize, totalCount);
    }

    public Task<PublicEventDetailDto?> GetEventAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Events.AsNoTracking()
            .Where(item => item.Slug == slug && item.IsPublished)
            .Select(item => new PublicEventDetailDto(
                item.Title, item.Slug, item.EventType, item.StartDateTimeUtc,
                item.EndDateTimeUtc, item.LocationName, item.Address, item.Description,
                item.RegistrationUrl, item.ImageUrl, item.IsFeatured))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicCoachDto>> GetCoachesAsync(CancellationToken cancellationToken) =>
        await dbContext.Coaches.AsNoTracking()
            .Where(PublicCmsVisibility.ActiveCoach)
            .OrderBy(coach => coach.DisplayOrder).ThenBy(coach => coach.LastName).ThenBy(coach => coach.FirstName)
            .Select(PublicCmsVisibility.PublicCoach)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicSponsorDto>> GetSponsorsAsync(CancellationToken cancellationToken) =>
        await dbContext.Sponsors.AsNoTracking()
            .Where(PublicCmsVisibility.ActiveSponsor)
            .OrderBy(sponsor => sponsor.Tier).ThenBy(sponsor => sponsor.DisplayOrder).ThenBy(sponsor => sponsor.Name)
            .Select(sponsor => new PublicSponsorDto(
                sponsor.Name, sponsor.Slug, sponsor.Tier, sponsor.LogoUrl,
                sponsor.WebsiteUrl, sponsor.Description, sponsor.DisplayOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicFaqDto>> GetFaqsAsync(string? category, CancellationToken cancellationToken)
    {
        var query = dbContext.Faqs.AsNoTracking().Where(PublicCmsVisibility.ActiveFaq);
        if (category is not null)
        {
            query = query.Where(faq => faq.Category == category);
        }

        return await query
            .OrderBy(faq => faq.Category).ThenBy(faq => faq.DisplayOrder).ThenBy(faq => faq.Question)
            .Select(faq => new PublicFaqDto(faq.Question, faq.Answer, faq.Category, faq.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task AddContactSubmissionAsync(ContactSubmission submission, CancellationToken cancellationToken)
    {
        await dbContext.ContactSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
