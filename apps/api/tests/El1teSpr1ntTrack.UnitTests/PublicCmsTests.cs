using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class PublicCmsTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AnnouncementVisibility_RequiresPublishedCurrentContent()
    {
        var visible = PublicCmsVisibility.CurrentAnnouncement(Now).Compile();

        Assert.True(visible(Announcement(true, Now.AddDays(-1), Now.AddDays(1))));
        Assert.False(visible(Announcement(false, Now.AddDays(-1), null)));
        Assert.False(visible(Announcement(true, Now.AddMinutes(1), null)));
        Assert.False(visible(Announcement(true, Now.AddDays(-1), Now)));
    }

    [Fact]
    public void EventVisibility_RequiresPublishedAndNotEnded()
    {
        var published = PublicCmsVisibility.PublishedEvent.Compile();
        var upcoming = PublicCmsVisibility.UpcomingEvent(Now).Compile();

        Assert.True(published(new Event { IsPublished = true }));
        Assert.False(published(new Event { IsPublished = false }));
        Assert.True(upcoming(new Event { StartDateTimeUtc = Now.AddDays(-1), EndDateTimeUtc = Now.AddMinutes(1) }));
        Assert.False(upcoming(new Event { StartDateTimeUtc = Now.AddMinutes(-1) }));
    }

    [Fact]
    public void ActiveVisibility_ExcludesInactiveRecords()
    {
        Assert.False(PublicCmsVisibility.ActiveCoach.Compile()(new Coach { IsActive = false }));
        Assert.False(PublicCmsVisibility.ActiveSponsor.Compile()(new Sponsor { IsActive = false }));
        Assert.False(PublicCmsVisibility.ActiveFaq.Compile()(new Faq { IsActive = false }));
        Assert.False(PublicCmsVisibility.PublishedContentBlock.Compile()(new ContentBlock { IsPublished = false }));
    }

    [Fact]
    public void CoachProjection_HidesEmailUnlessItIsPublic()
    {
        var project = PublicCmsVisibility.PublicCoach.Compile();

        Assert.Null(project(new Coach { Email = "private@example.com", IsEmailPublic = false }).Email);
        Assert.Equal(
            "public@example.com",
            project(new Coach { Email = "public@example.com", IsEmailPublic = true }).Email);
    }

    [Fact]
    public async Task ContactSubmission_IsNormalizedAndForcedToNew()
    {
        var repository = new RecordingRepository();
        var service = CreateService(repository);

        var response = await service.CreateContactSubmissionAsync(new CreateContactSubmissionRequest
        {
            Name = "  Parent Name  ",
            Email = " parent@example.com ",
            InquiryType = InquiryType.Parent,
            Message = "  Please contact me.  "
        }, CancellationToken.None);

        Assert.NotNull(repository.Submission);
        Assert.Equal(response.Id, repository.Submission.Id);
        Assert.Equal(ContactSubmissionStatus.New, repository.Submission.Status);
        Assert.Equal("Parent Name", repository.Submission.Name);
        Assert.Equal(Now, repository.Submission.CreatedAtUtc);
    }

    [Fact]
    public async Task ContactSubmission_RejectsInvalidInputWithoutSaving()
    {
        var repository = new RecordingRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            service.CreateContactSubmissionAsync(new CreateContactSubmissionRequest
            {
                Name = "Parent",
                Email = "not-an-email",
                Message = " "
            }, CancellationToken.None));

        Assert.Contains(nameof(ContactSubmission.Email), exception.Errors.Keys);
        Assert.Contains(nameof(ContactSubmission.Message), exception.Errors.Keys);
        Assert.Null(repository.Submission);
    }

    private static PublicCmsService CreateService(RecordingRepository repository) =>
        new(repository, new CmsValidationService(), new FixedClock(Now));

    private static Announcement Announcement(bool published, DateTimeOffset? publish, DateTimeOffset? expiry) =>
        new() { IsPublished = published, PublishDateUtc = publish, ExpirationDateUtc = expiry };

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class RecordingRepository : IPublicCmsRepository
    {
        public ContactSubmission? Submission { get; private set; }

        public Task AddContactSubmissionAsync(ContactSubmission submission, CancellationToken cancellationToken)
        {
            Submission = submission;
            return Task.CompletedTask;
        }

        public Task<PublicSiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PublicContentBlockDto>> GetContentBlocksAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PublicContentBlockDto?> GetContentBlockAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PagedResultDto<PublicAnnouncementListItemDto>> GetAnnouncementsAsync(AnnouncementQueryOptions options, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PublicAnnouncementDetailDto?> GetAnnouncementAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PagedResultDto<PublicEventListItemDto>> GetEventsAsync(EventQueryOptions options, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PublicEventDetailDto?> GetEventAsync(string slug, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PublicCoachDto>> GetCoachesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PublicSponsorDto>> GetSponsorsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PublicFaqDto>> GetFaqsAsync(string? category, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
