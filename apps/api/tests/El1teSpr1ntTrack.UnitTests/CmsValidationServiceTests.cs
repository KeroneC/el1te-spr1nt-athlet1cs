using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class CmsValidationServiceTests
{
    private readonly CmsValidationService _validator = new();

    [Fact]
    public void Validate_EventRejectsEndBeforeStart()
    {
        var cmsEvent = ValidEvent();
        cmsEvent.EndDateTimeUtc = cmsEvent.StartDateTimeUtc.AddMinutes(-1);

        var errors = _validator.Validate(cmsEvent);

        Assert.Contains(nameof(Event.EndDateTimeUtc), errors.Keys);
    }

    [Fact]
    public void Validate_EventAcceptsEndAfterStart()
    {
        var cmsEvent = ValidEvent();

        var errors = _validator.Validate(cmsEvent);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ContactSubmissionRejectsInvalidEmail()
    {
        var submission = new ContactSubmission
        {
            Name = "Parent Name",
            Email = "not-an-email",
            Message = "I would like registration information."
        };

        var errors = _validator.Validate(submission);

        Assert.Contains(nameof(ContactSubmission.Email), errors.Keys);
    }

    [Fact]
    public void Validate_ContentBlockRequiresUniqueKeyCandidate()
    {
        var block = new ContentBlock
        {
            Key = " ",
            Title = "Mission",
            Body = "Our mission statement."
        };

        var errors = _validator.Validate(block);

        Assert.Contains(nameof(ContentBlock.Key), errors.Keys);
    }

    private static Event ValidEvent()
    {
        var start = new DateTimeOffset(2026, 7, 1, 22, 0, 0, TimeSpan.Zero);

        return new Event
        {
            Title = "Team Practice",
            Slug = "team-practice",
            StartDateTimeUtc = start,
            EndDateTimeUtc = start.AddHours(2),
            LocationName = "Community Track",
            Description = "Team practice session."
        };
    }
}
