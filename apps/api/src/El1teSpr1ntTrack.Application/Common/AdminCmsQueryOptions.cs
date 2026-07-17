using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Application.Common;

public sealed record AdminListOptions(string? Search, int Page = 1, int PageSize = 20);
public sealed record AdminContentBlockOptions(string? Search, bool? IsPublished, int Page = 1, int PageSize = 20);
public sealed record AdminAnnouncementOptions(string? Search, bool? IsPublished, bool? IsFeatured, bool IncludeExpired, int Page = 1, int PageSize = 20);
public sealed record AdminEventOptions(string? Search, EventType? EventType, bool? IsPublished, bool? IsFeatured, DateTimeOffset? FromDate, DateTimeOffset? ToDate, int Page = 1, int PageSize = 20);
public sealed record AdminCoachOptions(string? Search, bool? IsActive, int Page = 1, int PageSize = 20);
public sealed record AdminSponsorOptions(string? Search, SponsorTier? Tier, bool? IsActive, int Page = 1, int PageSize = 20);
public sealed record AdminFaqOptions(string? Search, string? Category, bool? IsActive, int Page = 1, int PageSize = 20);
public sealed record AdminContactOptions(string? Search, ContactSubmissionStatus? Status, InquiryType? InquiryType, DateTimeOffset? FromDate, DateTimeOffset? ToDate, int Page = 1, int PageSize = 20);
public sealed record AdminUserOptions(string? Search, UserRole? Role, bool? IsActive, int Page = 1, int PageSize = 20);
public sealed record AdminInvitationOptions(string? Search, string? Status, int Page = 1, int PageSize = 20);
public sealed record AdminActivityOptions(string? Search, string? Action, DateTimeOffset? FromDate, DateTimeOffset? ToDate, int Page = 1, int PageSize = 20);
public sealed record AdminPage<T>(IReadOnlyList<T> Items, int TotalCount);
