using System.Linq.Expressions;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class AdminCmsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SiteSettingsUpdate_UpdatesSingletonWithoutAddingRow()
    {
        var repository = new FakeRepository { SiteSetting = ValidSiteSetting() };
        var service = CreateService(repository);

        var result = await service.UpdateSiteSettingsAsync(ValidSiteRequest(), CancellationToken.None);

        Assert.Equal(repository.SiteSetting.Id, result.Id);
        Assert.Equal(Now, result.UpdatedAtUtc);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task ContentBlockCreate_RejectsDuplicateKey()
    {
        var repository = new FakeRepository();
        repository.ContentBlocks.Add(new ContentBlock { Key = "home-hero" });
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CmsConflictException>(() => service.CreateContentBlockAsync(
            new ContentBlockWriteDto { Key = "home-hero", Title = "Hero", Body = "Body" }, CancellationToken.None));
    }

    [Fact]
    public async Task AnnouncementCreate_GeneratesUniqueSlugAndKeepsDraft()
    {
        var repository = new FakeRepository();
        repository.Announcements.Add(new Announcement { Slug = "team-news" });
        var service = CreateService(repository);

        var result = await service.CreateAnnouncementAsync(new AnnouncementWriteDto
        {
            Title = "Team News", Summary = "Summary", Body = "Body", IsPublished = false
        }, CancellationToken.None);

        Assert.Equal("team-news-2", result.Slug);
        Assert.False(result.IsPublished);
    }

    [Fact]
    public async Task EventCreate_RejectsEndBeforeStart()
    {
        var service = CreateService(new FakeRepository());

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.CreateEventAsync(
            new EventWriteDto
            {
                Title = "Meet", LocationName = "Track", Description = "Description",
                StartDateTimeUtc = Now, EndDateTimeUtc = Now.AddMinutes(-1), EventType = EventType.Meet
            }, CancellationToken.None));

        Assert.Contains(nameof(Event.EndDateTimeUtc), exception.Errors.Keys);
    }

    [Fact]
    public async Task CoachCreate_RejectsPublicInvalidEmail()
    {
        var service = CreateService(new FakeRepository());

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.CreateCoachAsync(
            new CoachWriteDto
            {
                FirstName = "A", LastName = "Coach", Role = "Coach", Bio = "Bio",
                Email = "invalid", IsEmailPublic = true
            }, CancellationToken.None));

        Assert.Contains(nameof(Coach.Email), exception.Errors.Keys);
    }

    [Fact]
    public async Task SponsorCreate_RejectsInvalidWebsiteAndFaqRejectsNegativeOrder()
    {
        var service = CreateService(new FakeRepository());

        await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.CreateSponsorAsync(
            new SponsorWriteDto { Name = "Sponsor", WebsiteUrl = "not-a-url" }, CancellationToken.None));
        await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.CreateFaqAsync(
            new FaqWriteDto { Question = "Question?", Answer = "Answer", Category = "General", DisplayOrder = -1 }, CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateCoach_PreservesRecordAndMarksInactive()
    {
        var repository = new FakeRepository();
        var coach = new Coach { FirstName = "A", LastName = "Coach", Role = "Coach", Bio = "Bio" };
        repository.Coaches.Add(coach);
        var service = CreateService(repository);

        await service.DeactivateCoachAsync(coach.Id, CancellationToken.None);

        Assert.False(coach.IsActive);
        Assert.Contains(coach, repository.Coaches);
        Assert.Equal(Now, coach.UpdatedAtUtc);
    }

    [Fact]
    public async Task ContactStatus_RejectsInvalidEnumAndUpdatesValidStatus()
    {
        var repository = new FakeRepository();
        var submission = new ContactSubmission { Name = "Parent", Email = "p@example.com", Message = "Hello" };
        repository.ContactSubmissions.Add(submission);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.UpdateContactStatusAsync(
            submission.Id, new UpdateContactSubmissionStatusRequest { Status = (ContactSubmissionStatus)999 }, CancellationToken.None));
        var result = await service.UpdateContactStatusAsync(
            submission.Id, new UpdateContactSubmissionStatusRequest { Status = ContactSubmissionStatus.Resolved }, CancellationToken.None);

        Assert.Equal(ContactSubmissionStatus.Resolved, result.Status);
    }

    [Fact]
    public async Task ListPagination_IsBoundedToOneHundred()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetContentBlocksAsync(new AdminContentBlockOptions(null, null, -2, 500), CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, repository.LastPageSize);
    }

    private static AdminCmsService CreateService(FakeRepository repository) =>
        new(repository, new CmsValidationService(), new SlugGenerator(), new FixedClock());

    private static SiteSetting ValidSiteSetting() => new()
    {
        ClubName = "Club", Slogan = "Run", ContactEmail = "club@example.com",
        PrimaryCtaText = "Join", PrimaryCtaUrl = "/join", SecondaryCtaText = "Learn", SecondaryCtaUrl = "/learn"
    };

    private static SiteSettingWriteDto ValidSiteRequest() => new()
    {
        ClubName = "Club", Slogan = "Run", ContactEmail = "club@example.com",
        PrimaryCtaText = "Join", PrimaryCtaUrl = "/join", SecondaryCtaText = "Learn", SecondaryCtaUrl = "/learn"
    };

    private sealed class FixedClock : IClock { public DateTimeOffset UtcNow => Now; }

    private sealed class FakeRepository : IAdminCmsRepository
    {
        public SiteSetting? SiteSetting { get; set; }
        public List<ContentBlock> ContentBlocks { get; } = [];
        public List<Announcement> Announcements { get; } = [];
        public List<Event> Events { get; } = [];
        public List<Coach> Coaches { get; } = [];
        public List<Sponsor> Sponsors { get; } = [];
        public List<Faq> Faqs { get; } = [];
        public List<ContactSubmission> ContactSubmissions { get; } = [];
        public int AddCount { get; private set; }
        public int LastPageSize { get; private set; }

        public Task<SiteSetting?> GetSiteSettingsAsync(CancellationToken token) => Task.FromResult(SiteSetting);
        public Task<AdminPage<ContentBlock>> GetContentBlocksAsync(AdminContentBlockOptions options, CancellationToken token) { LastPageSize = options.PageSize; return Page(ContentBlocks); }
        public Task<AdminPage<Announcement>> GetAnnouncementsAsync(AdminAnnouncementOptions options, DateTimeOffset now, CancellationToken token) => Page(Announcements);
        public Task<AdminPage<Event>> GetEventsAsync(AdminEventOptions options, CancellationToken token) => Page(Events);
        public Task<AdminPage<Coach>> GetCoachesAsync(AdminCoachOptions options, CancellationToken token) => Page(Coaches);
        public Task<AdminPage<Sponsor>> GetSponsorsAsync(AdminSponsorOptions options, CancellationToken token) => Page(Sponsors);
        public Task<AdminPage<Faq>> GetFaqsAsync(AdminFaqOptions options, CancellationToken token) => Page(Faqs);
        public Task<AdminPage<ContactSubmission>> GetContactSubmissionsAsync(AdminContactOptions options, CancellationToken token) => Page(ContactSubmissions);
        public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken token) where T : CmsEntityBase => Task.FromResult(All<T>().FirstOrDefault(item => item.Id == id));
        public Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken token) where T : CmsEntityBase => Task.FromResult(All<T>().Any(predicate.Compile()));
        public Task AddAsync<T>(T entity, CancellationToken token) where T : CmsEntityBase { All<T>().Add(entity); AddCount++; return Task.CompletedTask; }
        public void Remove<T>(T entity) where T : CmsEntityBase => All<T>().Remove(entity);
        public Task<int> SaveChangesAsync(CancellationToken token) => Task.FromResult(1);

        private List<T> All<T>() where T : CmsEntityBase
        {
            if (typeof(T) == typeof(SiteSetting)) return (List<T>)(object)new List<SiteSetting>(SiteSetting is null ? [] : [SiteSetting]);
            if (typeof(T) == typeof(ContentBlock)) return (List<T>)(object)ContentBlocks;
            if (typeof(T) == typeof(Announcement)) return (List<T>)(object)Announcements;
            if (typeof(T) == typeof(Event)) return (List<T>)(object)Events;
            if (typeof(T) == typeof(Coach)) return (List<T>)(object)Coaches;
            if (typeof(T) == typeof(Sponsor)) return (List<T>)(object)Sponsors;
            if (typeof(T) == typeof(Faq)) return (List<T>)(object)Faqs;
            if (typeof(T) == typeof(ContactSubmission)) return (List<T>)(object)ContactSubmissions;
            throw new NotSupportedException(typeof(T).Name);
        }

        private static Task<AdminPage<T>> Page<T>(List<T> items) => Task.FromResult(new AdminPage<T>(items, items.Count));
    }
}
