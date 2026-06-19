using System.Linq.Expressions;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Common;

public static class PublicCmsVisibility
{
    public static readonly Expression<Func<ContentBlock, bool>> PublishedContentBlock = block => block.IsPublished;
    public static readonly Expression<Func<Event, bool>> PublishedEvent = item => item.IsPublished;
    public static readonly Expression<Func<Coach, bool>> ActiveCoach = coach => coach.IsActive;
    public static readonly Expression<Func<Sponsor, bool>> ActiveSponsor = sponsor => sponsor.IsActive;
    public static readonly Expression<Func<Faq, bool>> ActiveFaq = faq => faq.IsActive;
    public static readonly Expression<Func<Coach, PublicCoachDto>> PublicCoach = coach =>
        new PublicCoachDto(
            coach.FirstName,
            coach.LastName,
            coach.Role,
            coach.Bio,
            coach.ImageUrl,
            coach.IsEmailPublic ? coach.Email : null,
            coach.DisplayOrder);

    public static Expression<Func<Announcement, bool>> CurrentAnnouncement(DateTimeOffset now) =>
        item => item.IsPublished &&
            (item.PublishDateUtc == null || item.PublishDateUtc <= now) &&
            (item.ExpirationDateUtc == null || item.ExpirationDateUtc > now);

    public static Expression<Func<Event, bool>> UpcomingEvent(DateTimeOffset now) =>
        item => (item.EndDateTimeUtc ?? item.StartDateTimeUtc) >= now;
}
