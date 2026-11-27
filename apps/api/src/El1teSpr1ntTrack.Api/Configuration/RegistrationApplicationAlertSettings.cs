using System.Net;
using System.Net.Mail;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Api.Configuration;

public sealed class RegistrationApplicationAlertSettings
{
    public const string RecipientsKey = "RegistrationAlerts:Recipients";
    private readonly string[] recipients;
    private readonly Uri? adminQueue;

    public RegistrationApplicationAlertSettings(string? configuredRecipients, string? adminSite, bool allowLocalOrigin = false)
    {
        var configured = (configuredRecipients ?? "").Split(';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var distinct = configured.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (distinct.Length is < 1 or > 10 ||
            distinct.Any(value => value.Length > 254 || !MailAddress.TryCreate(value, out var address) || address.Address != value) ||
            !Uri.TryCreate(adminSite, UriKind.Absolute, out var site) || !string.IsNullOrEmpty(site.UserInfo) ||
            (site.IsLoopback ? !allowLocalOrigin || site.Scheme is not ("http" or "https") : site.Scheme != Uri.UriSchemeHttps))
        {
            recipients = [];
            InvalidConfiguration = configured.Length > 0;
            return;
        }
        recipients = distinct;
        adminQueue = new Uri(site, "/admin/registration");
    }

    public bool Enabled => recipients.Length > 0;
    public bool InvalidConfiguration { get; }

    public bool Allows(string recipient) => recipients.Contains(recipient, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<RegistrationApplicationAlert> ForNewSubmission(RegistrationSubmission submission) =>
        submission.Kind == IntakeKind.Application
            ? recipients.Select(recipient => new RegistrationApplicationAlert
            {
                SubmissionId = submission.Id,
                Recipient = recipient,
                AvailableAtUtc = submission.SubmittedAtUtc
            })
            : [];

    public TransactionalEmail CreateEmail(string recipient)
    {
        if (!Allows(recipient) || adminQueue is null)
            throw new InvalidOperationException("Registration alert recipient is not configured.");

        var link = adminQueue.AbsoluteUri;
        const string subject = "New El1te registration application";
        const string message = "A new application is ready for review. Sign in and open New Applications in Registration.";
        return new TransactionalEmail(recipient, subject, message + "\n\n" + link,
            $"<p>{message}</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Open Registration in Admin</a></p>");
    }
}
