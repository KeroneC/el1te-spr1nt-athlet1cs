using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed partial class AdminCmsService
{
    public async Task<PagedResultDto<AdminCoachDto>> GetCoachesAsync(AdminCoachOptions options, CancellationToken cancellationToken)
    {
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        return MapPage(await _repository.GetCoachesAsync(normalized, cancellationToken), page, size, Map);
    }

    public async Task<AdminCoachDto> GetCoachAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<Coach>(id, cancellationToken), id, "Coach"));

    public async Task<AdminCoachDto> CreateCoachAsync(CoachWriteDto request, CancellationToken cancellationToken)
    {
        var item = new Coach { CreatedAtUtc = _clock.UtcNow };
        Apply(item, request);
        ValidateCoach(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminCoachDto> UpdateCoachAsync(Guid id, CoachWriteDto request, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Coach>(id, cancellationToken), id, "Coach");
        Apply(item, request);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateCoach(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeactivateCoachAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Coach>(id, cancellationToken), id, "Coach");
        item.IsActive = false;
        item.UpdatedAtUtc = _clock.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<AdminSponsorDto>> GetSponsorsAsync(AdminSponsorOptions options, CancellationToken cancellationToken)
    {
        if (options.Tier.HasValue && !Enum.IsDefined(options.Tier.Value))
            throw new Common.Exceptions.CmsRequestValidationException(new Dictionary<string, string[]> { [nameof(options.Tier)] = ["Tier is invalid."] });
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        return MapPage(await _repository.GetSponsorsAsync(normalized, cancellationToken), page, size, Map);
    }

    public async Task<AdminSponsorDto> GetSponsorAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<Sponsor>(id, cancellationToken), id, "Sponsor"));

    public async Task<AdminSponsorDto> CreateSponsorAsync(SponsorWriteDto request, CancellationToken cancellationToken)
    {
        var slug = await _slugGenerator.GenerateUniqueAsync(
            request.Name,
            (candidate, token) => _repository.ExistsAsync<Sponsor>(item => item.Slug == candidate, token),
            cancellationToken);
        var item = new Sponsor { Slug = slug, CreatedAtUtc = _clock.UtcNow };
        Apply(item, request);
        ValidateSponsor(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminSponsorDto> UpdateSponsorAsync(Guid id, SponsorWriteDto request, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Sponsor>(id, cancellationToken), id, "Sponsor");
        Apply(item, request);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateSponsor(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeactivateSponsorAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Sponsor>(id, cancellationToken), id, "Sponsor");
        item.IsActive = false;
        item.UpdatedAtUtc = _clock.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Coach item, CoachWriteDto request)
    {
        item.FirstName = request.FirstName.Trim();
        item.LastName = request.LastName.Trim();
        item.Role = request.Role.Trim();
        item.Bio = request.Bio.Trim();
        item.ImageUrl = Clean(request.ImageUrl);
        item.Email = Clean(request.Email);
        item.IsEmailPublic = request.IsEmailPublic;
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive;
    }

    private void ValidateCoach(Coach item)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateDisplayOrder(errors, item.DisplayOrder);
        ValidateUrl(errors, nameof(item.ImageUrl), item.ImageUrl);
        ValidateAndThrow(item, errors);
    }

    private static void Apply(Sponsor item, SponsorWriteDto request)
    {
        item.Name = request.Name.Trim();
        item.Tier = request.Tier;
        item.LogoUrl = Clean(request.LogoUrl);
        item.WebsiteUrl = Clean(request.WebsiteUrl);
        item.Description = Clean(request.Description);
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive;
    }

    private void ValidateSponsor(Sponsor item)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(item.Tier)) errors[nameof(item.Tier)] = ["Tier is invalid."];
        ValidateDisplayOrder(errors, item.DisplayOrder);
        ValidateUrl(errors, nameof(item.LogoUrl), item.LogoUrl);
        ValidateUrl(errors, nameof(item.WebsiteUrl), item.WebsiteUrl);
        ValidateAndThrow(item, errors);
    }
}
