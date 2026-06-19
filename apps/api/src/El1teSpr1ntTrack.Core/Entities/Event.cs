using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Event : CmsEntityBase
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public EventType EventType { get; set; } = EventType.Other;

    public DateTimeOffset StartDateTimeUtc { get; set; }

    public DateTimeOffset? EndDateTimeUtc { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? RegistrationUrl { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPublished { get; set; }
}
