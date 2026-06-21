using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed partial class AdminCmsService
{
    public async Task<PagedResultDto<AdminFaqDto>> GetFaqsAsync(AdminFaqOptions options, CancellationToken cancellationToken)
    {
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Category = Clean(options.Category), Page = page, PageSize = size };
        return MapPage(await _repository.GetFaqsAsync(normalized, cancellationToken), page, size, Map);
    }

    public async Task<AdminFaqDto> GetFaqAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<Faq>(id, cancellationToken), id, "FAQ"));

    public async Task<AdminFaqDto> CreateFaqAsync(FaqWriteDto request, CancellationToken cancellationToken)
    {
        var item = new Faq { CreatedAtUtc = _clock.UtcNow };
        Apply(item, request);
        ValidateFaq(item);
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<AdminFaqDto> UpdateFaqAsync(Guid id, FaqWriteDto request, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Faq>(id, cancellationToken), id, "FAQ");
        Apply(item, request);
        item.UpdatedAtUtc = _clock.UtcNow;
        ValidateFaq(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeactivateFaqAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<Faq>(id, cancellationToken), id, "FAQ");
        item.IsActive = false;
        item.UpdatedAtUtc = _clock.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<AdminContactSubmissionDto>> GetContactSubmissionsAsync(AdminContactOptions options, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (options.Status.HasValue && !Enum.IsDefined(options.Status.Value)) errors[nameof(options.Status)] = ["Status is invalid."];
        if (options.InquiryType.HasValue && !Enum.IsDefined(options.InquiryType.Value)) errors[nameof(options.InquiryType)] = ["InquiryType is invalid."];
        if (options.FromDate > options.ToDate) errors[nameof(options.ToDate)] = ["ToDate must be on or after FromDate."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
        var (page, size) = NormalizePage(options.Page, options.PageSize);
        var normalized = options with { Search = Clean(options.Search), Page = page, PageSize = size };
        return MapPage(await _repository.GetContactSubmissionsAsync(normalized, cancellationToken), page, size, Map);
    }

    public async Task<AdminContactSubmissionDto> GetContactSubmissionAsync(Guid id, CancellationToken cancellationToken) =>
        Map(Require(await _repository.GetByIdAsync<ContactSubmission>(id, cancellationToken), id, "Contact submission"));

    public async Task<AdminContactSubmissionDto> UpdateContactStatusAsync(Guid id, UpdateContactSubmissionStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
            throw new CmsRequestValidationException(new Dictionary<string, string[]> { [nameof(request.Status)] = ["Status is invalid."] });
        var item = Require(await _repository.GetByIdAsync<ContactSubmission>(id, cancellationToken), id, "Contact submission");
        item.Status = request.Status;
        item.UpdatedAtUtc = _clock.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteContactSubmissionAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = Require(await _repository.GetByIdAsync<ContactSubmission>(id, cancellationToken), id, "Contact submission");
        _repository.Remove(item);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Faq item, FaqWriteDto request)
    {
        item.Question = request.Question.Trim();
        item.Answer = request.Answer.Trim();
        item.Category = request.Category.Trim();
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive;
    }

    private void ValidateFaq(Faq item)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateDisplayOrder(errors, item.DisplayOrder);
        ValidateAndThrow(item, errors);
    }
}
