using El1teSpr1ntTrack.Api.Configuration;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class RegistrationApplicationAlertTests
{
    [Fact]
    public void NewApplicationsQueueIndividualAlertsWithoutApplicantDetails()
    {
        var settings = new RegistrationApplicationAlertSettings(
            "staff@example.test ; second@example.test; STAFF@example.test", "https://club.example.test");
        var submission = new RegistrationSubmission
        {
            Kind = IntakeKind.Application, AthleteFirstName = "Synthetic Athlete",
            GuardianEmail = "family@example.test", GuardianPhone = "412-555-0100",
            ReceiptReference = "EL-0123456789ABCDEF", SubmittedAtUtc = DateTimeOffset.UtcNow
        };
        var alerts = settings.ForNewSubmission(submission).ToArray();
        Assert.Equal(new[] { "staff@example.test", "second@example.test" }, alerts.Select(x => x.Recipient));
        Assert.All(alerts, x =>
        {
            Assert.Equal(submission.Id, x.SubmissionId);
            Assert.Equal(submission.SubmittedAtUtc, x.AvailableAtUtc);
            var mail = settings.CreateEmail(x.Recipient);
            Assert.Equal(x.Recipient, mail.Recipient);
            Assert.Contains("https://club.example.test/admin/registration", mail.PlainText);
            foreach (var text in new[] { mail.Subject, mail.PlainText, mail.Html })
            {
                Assert.DoesNotContain(submission.AthleteFirstName, text);
                Assert.DoesNotContain(submission.GuardianEmail, text);
                Assert.DoesNotContain(submission.GuardianPhone!, text);
                Assert.DoesNotContain(submission.ReceiptReference, text);
                Assert.DoesNotContain("second@example.test", text);
            }
        });
        submission.Kind = IntakeKind.Returning;
        Assert.Empty(settings.ForNewSubmission(submission));
        Assert.Throws<InvalidOperationException>(() => settings.CreateEmail("family@example.test"));
    }

    [Theory]
    [InlineData(null, "https://club.example.test", false)]
    [InlineData("staff@example.test;not-an-address", "https://club.example.test", true)]
    [InlineData("staff@example.test", "http://localhost:3000", true)]
    public void MissingOrInvalidSettingsDisableAlerts(string? recipients, string origin, bool invalid)
    {
        var settings = new RegistrationApplicationAlertSettings(recipients, origin);
        Assert.False(settings.Enabled);
        Assert.Equal(invalid, settings.InvalidConfiguration);
        Assert.Empty(settings.ForNewSubmission(new RegistrationSubmission { Kind = IntakeKind.Application }));
    }
}
