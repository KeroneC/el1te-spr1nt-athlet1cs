using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed partial class AdminCmsService
{
    public async Task<PagedResultDto<AdminAnnouncementDto>> GetAnnouncementsAsync(AdminAnnouncementOptions options, CancellationToken cancellationToken)
    {
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        var result = await _repository.GetAnnouncementsAsync(normalized, _clock.UtcNow, cancellationToken);
        return MapPage(result, page, size, Map);
    }

    public async Task<AdminAnnouncementDto> GetAnnouncementAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<Announcement>(id, cancellationToken), id, "Announcement"));

    public async Task<AdminAnnouncementDto> CreateAnnouncementAsync(AnnouncementWriteDto request, CancellationToken cancellationToken)
    {
        var slug = await _slugGenerator.GenerateUniqueAsync(
            request.Title,
            (candidate, token) => _repository.ExistsAsync<Announcement>(item => item.Slug == candidate, token),
            cancellationToken);
        var item = new Announcement { Slug = slug, CreatedAtUtc = _clock.UtcNow };
        Apply(item, request);
        ValidateAnnouncement(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminAnnouncementDto> UpdateAnnouncementAsync(Guid id, AnnouncementWriteDto request, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Announcement>(id, cancellationToken), id, "Announcement");
        Apply(item, request);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateAnnouncement(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Announcement>(id, cancellationToken), id, "Announcement");
        _repository.Remove(item);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<AdminEventDto>> GetEventsAsync(AdminEventOptions options, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (options.FromDate > options.ToDate) errors[nameof(options.ToDate)] = ["ToDate must be on or after FromDate."];
        if (options.EventType.HasValue && !Enum.IsDefined(options.EventType.Value)) errors[nameof(options.EventType)] = ["EventType is invalid."];
        if (errors.Count > 0) throw new Application.Common.Exceptions.CmsRequestValidationException(errors);
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        var result = await _repository.GetEventsAsync(normalized, cancellationToken);
        return MapPage(result, page, size, Map);
    }

    public async Task<AdminEventDto> GetEventAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<Event>(id, cancellationToken), id, "Event"));

    public async Task<AdminEventDto> CreateEventAsync(EventWriteDto request, CancellationToken cancellationToken)
    {
        var slug = await _slugGenerator.GenerateUniqueAsync(
            request.Title,
            (candidate, token) => _repository.ExistsAsync<Event>(item => item.Slug == candidate, token),
            cancellationToken);
        var item = new Event { Slug = slug, CreatedAtUtc = _clock.UtcNow };
        Apply(item, request);
        ValidateEvent(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminEventDto> UpdateEventAsync(Guid id, EventWriteDto request, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Event>(id, cancellationToken), id, "Event");
        Apply(item, request);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateEvent(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Event>(id, cancellationToken), id, "Event");
        _repository.Remove(item);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Announcement item, AnnouncementWriteDto request)
    {
        item.Title = request.Title.Trim();
        item.Summary = request.Summary.Trim();
        item.Body = request.Body.Trim();
        item.ImageUrl = Clean(request.ImageUrl);
        item.IsFeatured = request.IsFeatured;
        item.IsPublished = request.IsPublished;
        item.PublishDateUtc = request.PublishDateUtc;
        item.ExpirationDateUtc = request.ExpirationDateUtc;
    }

    private void ValidateAnnouncement(Announcement item)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateUrl(errors, nameof(item.ImageUrl), item.ImageUrl);
        ValidateAndThrow(item, errors);
    }

    private static void Apply(Event item, EventWriteDto request)
    {
        item.Title = request.Title.Trim();
        item.EventType = request.EventType;
        item.StartDateTimeUtc = request.StartDateTimeUtc;
        item.EndDateTimeUtc = request.EndDateTimeUtc;
        item.LocationName = request.LocationName.Trim();
        item.Address = Clean(request.Address);
        item.Description = request.Description.Trim();
        item.RegistrationUrl = Clean(request.RegistrationUrl);
        item.ImageUrl = Clean(request.ImageUrl);
        item.IsFeatured = request.IsFeatured;
        item.IsPublished = request.IsPublished;
    }

    private void ValidateEvent(Event item)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(item.EventType)) errors[nameof(item.EventType)] = ["EventType is invalid."];
        if (item.StartDateTimeUtc == default) errors[nameof(item.StartDateTimeUtc)] = ["StartDateTimeUtc is required."];
        ValidateUrl(errors, nameof(item.RegistrationUrl), item.RegistrationUrl);
        ValidateUrl(errors, nameof(item.ImageUrl), item.ImageUrl);
        ValidateAndThrow(item, errors);
    }
}
