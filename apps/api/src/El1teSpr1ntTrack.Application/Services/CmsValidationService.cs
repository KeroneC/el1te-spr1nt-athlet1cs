using System.ComponentModel.DataAnnotations;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class CmsValidationService : ICmsValidationService
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public IReadOnlyDictionary<string, string[]> Validate(CmsEntityBase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        switch (entity)
        {
            case SiteSetting setting:
                Required(errors, nameof(setting.ClubName), setting.ClubName);
                Required(errors, nameof(setting.Slogan), setting.Slogan);
                Email(errors, nameof(setting.ContactEmail), setting.ContactEmail, required: true);
                Required(errors, nameof(setting.PrimaryCtaText), setting.PrimaryCtaText);
                Required(errors, nameof(setting.PrimaryCtaUrl), setting.PrimaryCtaUrl);
                Required(errors, nameof(setting.SecondaryCtaText), setting.SecondaryCtaText);
                Required(errors, nameof(setting.SecondaryCtaUrl), setting.SecondaryCtaUrl);
                break;
            case ContentBlock block:
                Required(errors, nameof(block.Key), block.Key);
                Required(errors, nameof(block.Title), block.Title);
                Required(errors, nameof(block.Body), block.Body);
                break;
            case Announcement announcement:
                Required(errors, nameof(announcement.Title), announcement.Title);
                Required(errors, nameof(announcement.Slug), announcement.Slug);
                Required(errors, nameof(announcement.Summary), announcement.Summary);
                Required(errors, nameof(announcement.Body), announcement.Body);
                if (announcement.PublishDateUtc is not null &&
                    announcement.ExpirationDateUtc <= announcement.PublishDateUtc)
                {
                    Add(errors, nameof(announcement.ExpirationDateUtc), "Expiration must be after publication.");
                }

                break;
            case Event cmsEvent:
                Required(errors, nameof(cmsEvent.Title), cmsEvent.Title);
                Required(errors, nameof(cmsEvent.Slug), cmsEvent.Slug);
                Required(errors, nameof(cmsEvent.LocationName), cmsEvent.LocationName);
                Required(errors, nameof(cmsEvent.Description), cmsEvent.Description);
                if (cmsEvent.EndDateTimeUtc <= cmsEvent.StartDateTimeUtc)
                {
                    Add(errors, nameof(cmsEvent.EndDateTimeUtc), "End date and time must be after the start.");
                }

                break;
            case Coach coach:
                Required(errors, nameof(coach.FirstName), coach.FirstName);
                Required(errors, nameof(coach.LastName), coach.LastName);
                Required(errors, nameof(coach.Role), coach.Role);
                Required(errors, nameof(coach.Bio), coach.Bio);
                Email(errors, nameof(coach.Email), coach.Email, coach.IsEmailPublic);
                break;
            case Sponsor sponsor:
                Required(errors, nameof(sponsor.Name), sponsor.Name);
                Required(errors, nameof(sponsor.Slug), sponsor.Slug);
                break;
            case Faq faq:
                Required(errors, nameof(faq.Question), faq.Question);
                Required(errors, nameof(faq.Answer), faq.Answer);
                Required(errors, nameof(faq.Category), faq.Category);
                break;
            case ContactSubmission submission:
                Required(errors, nameof(submission.Name), submission.Name);
                Email(errors, nameof(submission.Email), submission.Email, required: true);
                Required(errors, nameof(submission.Message), submission.Message);
                break;
        }

        return errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void Required(IDictionary<string, List<string>> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, $"{field} is required.");
        }
    }

    private static void Email(
        IDictionary<string, List<string>> errors,
        string field,
        string? value,
        bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                Add(errors, field, $"{field} is required.");
            }

            return;
        }

        if (!EmailValidator.IsValid(value))
        {
            Add(errors, field, $"{field} must be a valid email address.");
        }
    }

    private static void Add(IDictionary<string, List<string>> errors, string field, string error)
    {
        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = [];
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(error);
    }
}
