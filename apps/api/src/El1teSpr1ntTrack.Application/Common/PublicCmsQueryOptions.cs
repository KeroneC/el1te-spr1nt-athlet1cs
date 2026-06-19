using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Application.Common;

public sealed record AnnouncementQueryOptions(bool? Featured, int Page, int PageSize);

public sealed record EventQueryOptions(
    EventType? EventType,
    bool? Featured,
    bool UpcomingOnly,
    int Page,
    int PageSize);
