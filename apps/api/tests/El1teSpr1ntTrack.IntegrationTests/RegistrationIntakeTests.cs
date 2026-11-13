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
    private static RegistrationSeason Season() => new() { Year = 2027, AgeGroups = "10U\n12U", OpeningDate = new(2026, 11, 27), EndDate = new(2027, 8, 1) };
    private static IntakeRequest Request() => new(2027, Guid.NewGuid(), IntakeKind.Application, "instagram", "guardian@example.test",
        "Jordan", "Test Guardian", null, "12U", PracticeAvailability.Yes, null,
        PriorClubAnswer.Yes, "Previous Club", ["USATF", "AAU"], "2026-10", null);

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
