using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed partial class AdminCmsService : IAdminCmsService
{
    private readonly IAdminCmsRepository _repository;
    private readonly ICmsValidationService _validationService;
    private readonly ISlugGenerator _slugGenerator;
    private readonly IClock _clock;

    public AdminCmsService(
        IAdminCmsRepository repository,
        ICmsValidationService validationService,
        ISlugGenerator slugGenerator,
        IClock clock)
    {
        _repository = repository;
        _validationService = validationService;
        _slugGenerator = slugGenerator;
        _clock = clock;
    }

    private void ValidateAndThrow(CmsEntityBase entity, IDictionary<string, string[]>? additionalErrors = null)
    {
        var errors = _validationService.Validate(entity).ToDictionary(pair => pair.Key, pair => pair.Value);
        if (additionalErrors is not null)
        {
            foreach (var pair in additionalErrors) errors[pair.Key] = pair.Value;
        }
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private static void ValidateUrl(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (value.StartsWith('/') && Uri.TryCreate(value, UriKind.Relative, out _)) return;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") return;
        errors[field] = ["A valid HTTP(S) or root-relative URL is required."];
    }

    private static void ValidateDisplayOrder(IDictionary<string, string[]> errors, int displayOrder)
    {
        if (displayOrder < 0) errors[nameof(displayOrder)] = ["DisplayOrder cannot be negative."];
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PagedResultDto<TDto> MapPage<TEntity, TDto>(
        AdminPage<TEntity> page,
        int pageNumber,
        int pageSize,
        Func<TEntity, TDto> map) =>
        new(page.Items.Select(map).ToList(), pageNumber, pageSize, page.TotalCount);

    private static T Require<T>(T? entity, Guid id, string name) where T : CmsEntityBase =>
        entity ?? throw new CmsNotFoundException(name, id);

    private static AdminSiteSettingsDto Map(SiteSetting item) => new(
        item.Id, item.ClubName, item.Slogan, item.ContactEmail, item.PhoneNumber,
        item.AddressLine1, item.AddressLine2, item.City, item.State, item.ZipCode,
        item.FacebookUrl, item.InstagramUrl, item.YouTubeUrl,
        item.PrimaryCtaText, item.PrimaryCtaUrl, item.SecondaryCtaText, item.SecondaryCtaUrl,
        item.LogoUrl, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminContentBlockDto Map(ContentBlock item) => new(
        item.Id, item.Key, item.Title, item.Summary, item.Body, item.ImageUrl,
        item.CtaText, item.CtaUrl, item.DisplayOrder, item.IsPublished, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminAnnouncementDto Map(Announcement item) => new(
        item.Id, item.Title, item.Slug, item.Summary, item.Body, item.ImageUrl,
        item.IsFeatured, item.IsPublished, item.PublishDateUtc, item.ExpirationDateUtc,
        item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminEventDto Map(Event item) => new(
        item.Id, item.Title, item.Slug, item.EventType, item.StartDateTimeUtc, item.EndDateTimeUtc,
        item.LocationName, item.Address, item.Description, item.RegistrationUrl, item.ImageUrl,
        item.IsFeatured, item.IsPublished, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminCoachDto Map(Coach item) => new(
        item.Id, item.FirstName, item.LastName, item.Role, item.Bio, item.ImageUrl, item.Email,
        item.IsEmailPublic, item.DisplayOrder, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminSponsorDto Map(Sponsor item) => new(
        item.Id, item.Name, item.Slug, item.Tier, item.LogoUrl, item.WebsiteUrl, item.Description,
        item.DisplayOrder, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminFaqDto Map(Faq item) => new(
        item.Id, item.Question, item.Answer, item.Category, item.DisplayOrder, item.IsActive,
        item.CreatedAtUtc, item.UpdatedAtUtc);

    private static AdminContactSubmissionDto Map(ContactSubmission item) => new(
        item.Id, item.Name, item.Email, item.Phone, item.InquiryType, item.Message, item.Status,
        item.CreatedAtUtc, item.UpdatedAtUtc);
}
