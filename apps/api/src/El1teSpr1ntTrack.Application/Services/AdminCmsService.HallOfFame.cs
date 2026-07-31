using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed partial class AdminCmsService
{
    public async Task<PagedResultDto<AdminHallOfFameInducteeDto>> GetHallOfFameInducteesAsync(
        AdminHallOfFameInducteeOptions options,
        CancellationToken cancellationToken)
    {
        if (options.InductionYear is < 1900 or > 2100)
            throw new Common.Exceptions.CmsRequestValidationException(new Dictionary<string, string[]> { [nameof(options.InductionYear)] = ["InductionYear must be between 1900 and 2100."] });

        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        return MapPage(await _repository.GetHallOfFameInducteesAsync(normalized, cancellationToken), page, size, Map);
    }

    public async Task<AdminHallOfFameInducteeDto> GetHallOfFameInducteeAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<HallOfFameInductee>(id, cancellationToken), id, "Hall of Fame inductee"));

    public async Task<AdminHallOfFameInducteeDto> CreateHallOfFameInducteeAsync(
        HallOfFameInducteeWriteDto request,
        CancellationToken cancellationToken)
    {
        var slug = await _slugGenerator.GenerateUniqueAsync(
            request.Name,
            (candidate, token) => _repository.ExistsAsync<HallOfFameInductee>(item => item.Slug == candidate, token),
            cancellationToken);
        var item = new HallOfFameInductee { Slug = slug, CreatedAtUtc = _clock.UtcNow };
        Apply(item, request);
        ValidateHallOfFameInductee(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminHallOfFameInducteeDto> UpdateHallOfFameInducteeAsync(
        Guid id,
        HallOfFameInducteeWriteDto request,
        CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<HallOfFameInductee>(id, cancellationToken), id, "Hall of Fame inductee");
        Apply(item, request);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateHallOfFameInductee(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeactivateHallOfFameInducteeAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<HallOfFameInductee>(id, cancellationToken), id, "Hall of Fame inductee");
        item.IsActive = false;
        item.UpdatedAtUtc = _clock.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(HallOfFameInductee item, HallOfFameInducteeWriteDto request)
    {
        item.Name = request.Name.Trim();
        item.Affiliation = request.Affiliation.Trim();
        item.Summary = request.Summary.Trim();
        item.PhotoUrl = Clean(request.PhotoUrl);
        item.PhotoAlt = Clean(request.PhotoAlt);
        item.InductionYear = request.InductionYear;
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive;
    }

    private void ValidateHallOfFameInductee(HallOfFameInductee item)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateDisplayOrder(errors, item.DisplayOrder);
        ValidateUrl(errors, nameof(item.PhotoUrl), item.PhotoUrl);
        if (item.InductionYear is < 1900 or > 2100)
            errors[nameof(item.InductionYear)] = ["InductionYear must be between 1900 and 2100."];
        if (item.IsActive && string.IsNullOrWhiteSpace(item.PhotoUrl))
            errors[nameof(item.PhotoUrl)] = ["PhotoUrl is required before an inductee can be active."];
        if (item.IsActive && (string.IsNullOrWhiteSpace(item.PhotoAlt) || item.PhotoAlt.Length < 10))
            errors[nameof(item.PhotoAlt)] = ["PhotoAlt must meaningfully describe the photo before an inductee can be active."];
        ValidateAndThrow(item, errors);
    }

    private static AdminHallOfFameInducteeDto Map(HallOfFameInductee item) => new(
        item.Id, item.Name, item.Slug, item.Affiliation, item.Summary, item.PhotoUrl, item.PhotoAlt,
        item.InductionYear, item.DisplayOrder, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
}
