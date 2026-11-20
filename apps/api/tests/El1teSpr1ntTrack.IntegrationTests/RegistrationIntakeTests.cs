using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Api.Controllers.Registration;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class RegistrationIntakeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private static RegistrationSeason Season() => new()
    {
        Year = 2027, AgeGroups = "10U\n12U", OpeningDate = new(2026, 11, 27),
        FirstPracticeDate = new(2026, 12, 5), EndDate = new(2027, 8, 1)
    };
    private static IntakeRequest Request() => new(2027, Guid.NewGuid(), IntakeKind.Application, "instagram", "guardian@example.test",
        "Jordan", "Test Guardian", null, "12U", PracticeAvailability.Yes, null,
        PriorClubAnswer.Yes, "Previous Club", ["USATF", "AAU"], "2026-10", null);

    private static IntakeRequest FullRequest() => Request() with
    {
        ApplicationVersion = IntakeRules.CurrentApplicationVersion, AthleteLastName = "Test",
        DateOfBirth = "2019-12-05", SchoolGrade = "Not applicable", SchoolDistrict = "Not sure / not applicable",
        GuardianPhone = "412-555-0100", StreetAddress = "1 Synthetic Lane", City = "Sample City", State = "PA",
        PostalCode = "15201", EmergencyContactName = "Example Contact", EmergencyContactPhone = "412-555-0101",
        EmergencyContactRelationship = "Family friend", AgeGroup = null
    };

    [Theory]
    [InlineData("2019-12-06")]
    [InlineData("2020-12-05")]
    [InlineData("2007-12-05")]
    [InlineData("2027-01-01")]
    [InlineData("2020-02-30")]
    [InlineData("12/05/2020")]
    [InlineData("")]
    public void FullApplicationRejectsBirthDatesThatAreInvalidFutureOrOutsideTheAgeRange(string birthDate)
    {
        var error = Assert.Throws<CmsRequestValidationException>(() => IntakeRules.Normalize(
            FullRequest() with { DateOfBirth = birthDate }, Season(), Now));
        Assert.Contains("dateOfBirth", error.Errors.Keys);
    }

    [Fact]
    public void FullApplicationAcceptsExactSeventhBirthdayByFirstPracticeAndLeavesOptionalFieldsBlank()
    {
        var request = FullRequest();
        var item = IntakeRules.Normalize(request, Season(), Now);
        Assert.Equal(new DateOnly(2019, 12, 5), item.DateOfBirth);
        Assert.Equal("Not applicable", item.SchoolGrade);
        Assert.Equal("Not sure / not applicable", item.SchoolDistrict);
        Assert.Null(item.AgeGroup);
        Assert.Null(item.Gender);
        Assert.Null(item.SecondaryGuardianName);
        Assert.Equal("Family friend", item.EmergencyContactRelationship);
        Assert.NotNull(IntakeRules.Normalize(request with { DateOfBirth = "2008-12-05" }, Season(), Now).DateOfBirth);
        Assert.NotNull(IntakeRules.Normalize(request with { DateOfBirth = "2007-12-06" }, Season(), Now).DateOfBirth);
    }

    [Fact]
    public void AgeRuleDoesNotInvalidatePreviouslySavedApplications()
    {
        var request = FullRequest() with { DateOfBirth = "2020-12-05" };
        var item = IntakeRules.Normalize(request, Season(), Now, enforceCurrentEligibility: false);
        Assert.Equal(new DateOnly(2020, 12, 5), item.DateOfBirth);
    }

    [Theory]
    [InlineData("2020-02-29", true)]
    [InlineData("2008-02-29", false)]
    public void LeapDayUsesFebruary28InANonLeapYear(string birthDate, bool eligible)
    {
        var season = Season();
        season.FirstPracticeDate = new DateOnly(2027, 2, 28);
        var request = FullRequest() with { DateOfBirth = birthDate };
        if (eligible)
            Assert.NotNull(IntakeRules.Normalize(request, season, Now).DateOfBirth);
        else
            Assert.Contains("dateOfBirth", Assert.Throws<CmsRequestValidationException>(() =>
                IntakeRules.Normalize(request, season, Now)).Errors.Keys);
    }

    [Fact]
    public void FullApplicationRequiresHouseholdAndEmergencyContactButReturningIgnoresThem()
    {
        var request = FullRequest() with { GuardianPhone = "", StreetAddress = "", EmergencyContactPhone = "", SchoolGrade = "14" };
        var error = Assert.Throws<CmsRequestValidationException>(() => IntakeRules.Normalize(request, Season(), Now));
        foreach (var field in new[] { "guardianPhone", "streetAddress", "emergencyContactPhone", "schoolGrade" })
            Assert.Contains(field, error.Errors.Keys);
        var returning = IntakeRules.Normalize(request with { Kind = IntakeKind.Returning }, Season(), Now);
        Assert.Null(returning.ApplicationVersion);
        Assert.Null(returning.DateOfBirth);
        Assert.Null(returning.StreetAddress);
        Assert.Null(returning.EmergencyContactName);
        Assert.Null(returning.SchoolDistrict);
    }

    [Fact]
    public void StaleReceiptDigestStaysCompatibleAndNewDigestIncludesPrivateFields()
    {
        foreach (var kind in new[] { IntakeKind.Application, IntakeKind.Returning })
        {
            var item = IntakeRules.Normalize(Request() with { Kind = kind }, Season(), Now);
            var legacyBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
            {
                item.RequestKey, item.Kind, item.GuardianEmail, item.GuardianName,
                item.GuardianPhone, item.AthleteFirstName, item.AgeGroup, item.Availability,
                item.Experience, item.PriorClub, item.ClubName, item.Affiliations, item.LastMeetMonth
            });
            Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(legacyBytes)), IntakeRules.RequestDigest(item));
        }
        Assert.False(IntakeRules.IsCurrentApplication(Request()));

        var request = FullRequest();
        var season = Season();
        var original = IntakeRules.RequestDigest(IntakeRules.Normalize(request, season, Now));
        var changed = IntakeRules.RequestDigest(IntakeRules.Normalize(request with { EmergencyContactPhone = "412-555-0199" }, season, Now));
        Assert.NotEqual(original, changed);
        season.FirstPracticeDate = season.OpeningDate;
        Assert.Equal(original, IntakeRules.RequestDigest(IntakeRules.Normalize(request, season, Now, enforceCurrentEligibility: false)));
    }

    [Fact]
    public void ExistingSeasonWithoutFirstPracticeFailsClosedWithoutLosingManualState()
    {
        var season = Season();
        season.IsOpen = true;
        season.FirstPracticeDate = null;
        Assert.False(IntakeRules.IsOpen(season, Now));
        var status = RegistrationAdminController.GetAvailability(season, [season], IntakeRules.LocalDate(Now));
        Assert.Equal("Closed", status.PublicState);
        Assert.Contains("first practice", status.OpeningBlockedReason);
        Assert.True(season.IsOpen);
    }

    [Fact]
    public void ApplicationFlagsPriorClubAndPreservesBothAffiliations()
    {
        var item = IntakeRules.Normalize(Request(), Season(), Now);
        Assert.Equal(TransferStatus.NeedsReview, item.Transfer);
        Assert.Equal("AAU,USATF", item.Affiliations);
        Assert.Equal("Previous Club", item.ClubName);
        Assert.Equal("2026-10", item.LastMeetMonth);
        Assert.Equal(IntakeChannel.Instagram, item.Channel);
        Assert.Equal(IntakeStatus.New, item.Status);
    }

    [Fact]
    public void ReturningIntakeDoesNotRetainExtraApplicantFieldsOrCampaignValues()
    {
        var item = IntakeRules.Normalize(Request() with { Kind = IntakeKind.Returning, Channel = "unknown-campaign" }, Season(), Now);
        Assert.Null(item.GuardianName);
        Assert.Null(item.AgeGroup);
        Assert.Null(item.PriorClub);
        Assert.Null(item.ClubName);
        Assert.Null(item.Affiliations);
        Assert.Equal(IntakeChannel.Direct, item.Channel);
    }

    [Fact]
    public void NotSureFlagsStaffAndDoesNotStoreHiddenClubFields()
    {
        var item = IntakeRules.Normalize(Request() with { PriorClub = PriorClubAnswer.Unsure }, Season(), Now);
        Assert.Equal(TransferStatus.NeedsReview, item.Transfer);
        Assert.Null(item.ClubName);
        Assert.Null(item.LastMeetMonth);
    }

    [Theory]
    [InlineData(null, "2026-10", "clubName")]
    [InlineData("Test", "2026-13", "lastMeetMonth")]
    [InlineData("Test", "2027-01", "lastMeetMonth")]
    public void ValidatesConditionalClubFields(string? club, string lastMeet, string invalidField)
    {
        var error = Assert.Throws<CmsRequestValidationException>(() => IntakeRules.Normalize(
            Request() with { ClubName = club, LastMeetMonth = lastMeet }, Season(), Now));
        Assert.Contains(invalidField, error.Errors.Keys);
    }

    [Fact]
    public void ConditionalOfferAndEnrollmentDoNotRequireTransferClearanceOrPhotoConsent()
    {
        var item = IntakeRules.Normalize(Request(), Season(), Now);
        ReviewRequest Change(IntakeStatus status, TransferStatus transfer = TransferStatus.NeedsReview, bool ready = false)
            => new(item.Id, item.Revision, status, transfer, IntakePhotoConsent.Declined, ready);
        IntakeRules.ApplyReview(item, Change(IntakeStatus.Offered));
        IntakeRules.ApplyReview(item, Change(IntakeStatus.PaperworkReceived));
        IntakeRules.ApplyReview(item, Change(IntakeStatus.Enrolled));
        Assert.Equal(IntakePhotoConsent.Declined, item.PhotoConsent);
        Assert.False(item.CompetitionReady);
        Assert.Throws<CmsRequestValidationException>(() => IntakeRules.ApplyReview(item, Change(IntakeStatus.Enrolled, ready: true)));
        IntakeRules.ApplyReview(item, Change(IntakeStatus.Enrolled, TransferStatus.Cleared, ready: true));
        Assert.True(item.CompetitionReady);
    }

    [Fact]
    public void ReturningAthleteCanEnrollAfterAnOfferWithoutPaperwork()
    {
        var returning = IntakeRules.Normalize(Request() with { Kind = IntakeKind.Returning }, Season(), Now);
        var newcomer = IntakeRules.Normalize(Request(), Season(), Now);
        ReviewRequest Change(RegistrationSubmission item, IntakeStatus status) =>
            new(item.Id, item.Revision, status, item.Transfer, IntakePhotoConsent.NotRecorded, false);

        foreach (var item in new[] { returning, newcomer })
            IntakeRules.ApplyReview(item, Change(item, IntakeStatus.Offered));

        Assert.Throws<CmsRequestValidationException>(() => IntakeRules.ApplyReview(returning, Change(returning, IntakeStatus.PaperworkReceived)));
        Assert.Throws<CmsRequestValidationException>(() => IntakeRules.ApplyReview(newcomer, Change(newcomer, IntakeStatus.Enrolled)));
        IntakeRules.ApplyReview(returning, Change(returning, IntakeStatus.Enrolled));
        Assert.Equal(IntakeStatus.Enrolled, returning.Status);
        Assert.Equal(IntakePhotoConsent.NotRecorded, returning.PhotoConsent);
    }

    [Fact]
    public void RequiresManualOpeningAndUsesEasternDates()
    {
        var season = Season();
        Assert.False(IntakeRules.IsOpen(season, Now));
        season.IsOpen = true;
        Assert.False(IntakeRules.IsOpen(season, new DateTimeOffset(2026, 11, 27, 4, 59, 0, TimeSpan.Zero)));
        Assert.True(IntakeRules.IsOpen(season, Now));
        Assert.False(IntakeRules.IsOpen(season, Now.AddYears(1)));
        season.PurgedAtUtc = Now;
        Assert.False(IntakeRules.IsOpen(season, Now));
    }

    [Fact]
    public void StaffAvailabilityDistinguishesThePublicWindowFromTheManualSwitch()
    {
        var season = Season();
        var beforeOpening = IntakeRules.LocalDate(new DateTimeOffset(2026, 11, 27, 4, 59, 0, TimeSpan.Zero));
        var openingDay = IntakeRules.LocalDate(Now);
        var upcoming = RegistrationAdminController.GetAvailability(season, [season], beforeOpening);
        Assert.Equal("Upcoming", upcoming.PublicState);
        Assert.Contains("Eastern time", upcoming.OpeningBlockedReason);
        Assert.Equal("Closed", RegistrationAdminController.GetAvailability(season, [season], openingDay).PublicState);

        season.IsOpen = true;
        Assert.Equal("Open", RegistrationAdminController.GetAvailability(season, [season], openingDay).PublicState);
        Assert.Equal("Open", RegistrationAdminController.GetAvailability(season, [season], season.EndDate).PublicState);
        var ended = RegistrationAdminController.GetAvailability(season, [season], season.EndDate.AddDays(1));
        Assert.Equal("Ended", ended.PublicState);
        Assert.True(season.IsOpen);
        Assert.Equal("This season has already ended.", ended.OpeningBlockedReason);
    }

    [Fact]
    public void OnlyTheLatestUnpurgedSeasonCanOpenAndSwitchesCannotOverlap()
    {
        var season = Season();
        var older = new RegistrationSeason { Year = 2026, IsOpen = true };
        var newer = new RegistrationSeason { Year = 2028 };
        var today = IntakeRules.LocalDate(Now);

        Assert.Equal("Only the latest season can be opened.",
            RegistrationAdminController.GetAvailability(season, [season, newer], today).OpeningBlockedReason);
        newer.PurgedAtUtc = Now;
        Assert.Equal("Close the other season’s intake before opening this one.",
            RegistrationAdminController.GetAvailability(season, [older, season, newer], today).OpeningBlockedReason);
        older.IsOpen = false;
        Assert.Null(RegistrationAdminController.GetAvailability(season, [older, season, newer], today).OpeningBlockedReason);
    }

    [Fact]
    public void DatabaseScopesIdempotencyToSeasonAndCascadesPrivateHistory()
    {
        using var db = new El1teDbContext(new DbContextOptionsBuilder<El1teDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RegistrationModelTest;Trusted_Connection=True").Options);
        var model = db.Model.FindEntityType(typeof(RegistrationSubmission))!;
        Assert.Contains(model.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(x => x.Name).SequenceEqual(["SeasonYear", "RequestKey"]));
        Assert.DoesNotContain(model.GetIndexes(), index => index.IsUnique && index.Properties.Any(x => x.Name == "GuardianEmail"));
        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(db.Model.FindEntityType(typeof(RegistrationReview))!.GetForeignKeys()).DeleteBehavior);
        Assert.Equal(CmsAdminAuthorization.PolicyName,
            Assert.Single(typeof(RegistrationAdminController).GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>()).Policy);
        Assert.Equal(CmsAdminAuthorization.SuperAdminPolicyName,
            Assert.Single(typeof(RegistrationAdminController).GetMethod(nameof(RegistrationAdminController.Closeout))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>()).Policy);
        Assert.All(typeof(RegistrationPublicController).GetMethods().Where(x => x.GetCustomAttributes(typeof(HttpGetAttribute), false).Length != 0),
            method => Assert.Equal(nameof(RegistrationPublicController.Season), method.Name));
    }
}
