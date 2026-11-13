using System.ComponentModel.DataAnnotations;
using System.Globalization;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Api.Controllers.Registration;

public sealed record IntakeRequest(int SeasonYear, Guid RequestKey, IntakeKind Kind, string? Channel,
    string? GuardianEmail, string? AthleteFirstName, string? GuardianName,
    string? GuardianPhone, string? AgeGroup, PracticeAvailability? Availability, string? Experience,
    PriorClubAnswer? PriorClub, string? ClubName, string[]? Affiliations, string? LastMeetMonth, string? Website);

public sealed record SeasonRequest(int Year, Guid? Revision, string? Title, DateOnly OpeningDate, DateOnly EndDate,
    string[]? AgeGroups, string? PracticeLocation, string? PracticeSchedule, string? Fees, string? ReplyWindow,
    string? PaperworkEmail, string? PaperworkHandoff, string? ReviewOwner, string? RetentionOwner);

public sealed record ReviewRequest(Guid Id, Guid Revision, IntakeStatus Status, TransferStatus Transfer,
    IntakePhotoConsent PhotoConsent, bool CompetitionReady);

public static class IntakeRules
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    public static DateOnly LocalDate(DateTimeOffset now) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Eastern).DateTime);
    public static string[] Ages(RegistrationSeason season) => season.AgeGroups.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    public static IntakeChannel Channel(string? value) => value switch
    {
        "facebook" => IntakeChannel.Facebook, "instagram" => IntakeChannel.Instagram, _ => IntakeChannel.Direct
    };

    public static bool IsOpen(RegistrationSeason season, DateTimeOffset now) =>
        season.IsOpen && season.PurgedAtUtc is null && LocalDate(now) >= season.OpeningDate && LocalDate(now) <= season.EndDate;

    public static RegistrationSubmission Normalize(IntakeRequest request, RegistrationSeason season, DateTimeOffset now, string? originalAgeGroup = null)
    {
        var errors = new Dictionary<string, string[]>();
        void Error(string field, string message) => errors[field] = [message];
        string? Value(string? value, string field, int max, bool required = false)
        {
            value = value?.Trim();
            if ((required && string.IsNullOrEmpty(value)) || value?.Length > max || value?.Any(char.IsControl) == true)
                Error(field, $"Enter a value of up to {max} characters.");
            return string.IsNullOrEmpty(value) ? null : value;
        }

        if (request.RequestKey == Guid.Empty || !Enum.IsDefined(request.Kind) || !string.IsNullOrEmpty(request.Website))
            Error("form", "This form could not be submitted. Reload and try again.");
        var email = Value(request.GuardianEmail, "guardianEmail", 254, true);
        if (email is null || !new EmailAddressAttribute().IsValid(email)) Error("guardianEmail", "Enter a valid email address.");
        var submission = new RegistrationSubmission
        {
            SeasonYear = season.Year, Kind = request.Kind, Channel = Channel(request.Channel),
            RequestKey = request.RequestKey, GuardianEmail = email?.ToLowerInvariant() ?? "",
            AthleteFirstName = Value(request.AthleteFirstName, "athleteFirstName", 80, true) ?? "",
            SubmittedAtUtc = now
        };
        if (request.Kind == IntakeKind.Application)
        {
            submission.GuardianName = Value(request.GuardianName, "guardianName", 100, true);
            submission.GuardianPhone = Value(request.GuardianPhone, "guardianPhone", 32);
            if (submission.GuardianPhone is { } phone && (!phone.All(c => char.IsAsciiDigit(c) || " +().-".Contains(c)) ||
                phone.Count(char.IsAsciiDigit) is < 7 or > 15))
                Error("guardianPhone", "Enter a valid phone number or leave it blank.");
            submission.AgeGroup = Value(request.AgeGroup, "ageGroup", 40, true);
            if (submission.AgeGroup != originalAgeGroup && !Ages(season).Contains(submission.AgeGroup, StringComparer.Ordinal))
                Error("ageGroup", "Choose an eligible age group.");
            if (request.Availability is null || !Enum.IsDefined(request.Availability.Value))
                Error("availability", "Choose a practice availability answer.");
            submission.Availability = request.Availability;
            submission.Experience = Value(request.Experience, "experience", 250);
            if (request.PriorClub is null || !Enum.IsDefined(request.PriorClub.Value))
                Error("priorClub", "Choose Yes, No, or Not sure.");
            submission.PriorClub = request.PriorClub;
            submission.Transfer = request.PriorClub is PriorClubAnswer.Yes or PriorClubAnswer.Unsure
                ? TransferStatus.NeedsReview : TransferStatus.NotNeeded;
            if (request.PriorClub == PriorClubAnswer.Yes)
            {
                submission.ClubName = Value(request.ClubName, "clubName", 120, true);
                var affiliations = request.Affiliations ?? [];
                if (affiliations.Length is < 1 or > 3 || affiliations.Any(x => x is not ("AAU" or "USATF" or "Other" or "Unsure")) ||
                    (affiliations.Contains("Unsure") && affiliations.Length > 1))
                    Error("affiliations", "Choose the governing bodies or select Not sure on its own.");
                submission.Affiliations = string.Join(",", affiliations.Distinct().Order(StringComparer.Ordinal));
                submission.LastMeetMonth = Value(request.LastMeetMonth, "lastMeetMonth", 7);
                if (submission.LastMeetMonth is { } month &&
                    (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                        date > LocalDate(now) || date.Year < 2000))
                    Error("lastMeetMonth", "Enter a valid past or current month and year.");
            }
        }
        ThrowErrors(errors);
        return submission;
    }

    public static void UpdateSeason(RegistrationSeason season, SeasonRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        string Check(string? value, int max, string key)
        {
            value = value?.Trim() ?? "";
            if (value.Length == 0 || value.Length > max || value.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
                errors[key] = [$"Enter a value of up to {max} characters."];
            return value;
        }
        if (request.Year is < 2026 or > 2100) errors["year"] = ["Choose a valid season year."];
        if (request.OpeningDate == default || request.EndDate <= request.OpeningDate || request.EndDate.Year != request.Year)
            errors["endDate"] = ["The season end must follow opening and fall within the season year."];
        var ages = (request.AgeGroups ?? []).Select(x => x?.Trim() ?? "").ToArray();
        if (ages.Length is < 1 or > 10 || ages.Any(x => x.Length is < 1 or > 40 || x.Any(char.IsControl)) ||
            ages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ages.Length || string.Join('\n', ages).Length > 400)
            errors["ageGroups"] = ["Enter 1–10 distinct eligible age groups, one per line."];
        season.Title = Check(request.Title, 120, "title");
        season.PracticeLocation = Check(request.PracticeLocation, 250, "practiceLocation");
        season.PracticeSchedule = Check(request.PracticeSchedule, 500, "practiceSchedule");
        season.Fees = Check(request.Fees, 250, "fees");
        season.ReplyWindow = Check(request.ReplyWindow, 250, "replyWindow");
        season.PaperworkEmail = Check(request.PaperworkEmail, 254, "paperworkEmail");
        if (season.PaperworkEmail.Any(char.IsControl) || !new EmailAddressAttribute().IsValid(season.PaperworkEmail))
            errors["paperworkEmail"] = ["Enter the approved club inbox."];
        season.PaperworkHandoff = Check(request.PaperworkHandoff, 500, "paperworkHandoff");
        season.ReviewOwner = Check(request.ReviewOwner, 120, "reviewOwner");
        season.RetentionOwner = Check(request.RetentionOwner, 120, "retentionOwner");
        ThrowErrors(errors);
        season.AgeGroups = string.Join('\n', ages);
        season.OpeningDate = request.OpeningDate;
        season.EndDate = request.EndDate;
        season.Revision = Guid.NewGuid();
    }

    public static bool CanTransition(IntakeStatus current, IntakeStatus next) =>
        current == next || (current, next) switch
        {
            (IntakeStatus.New or IntakeStatus.FollowUp or IntakeStatus.Waitlisted,
                IntakeStatus.FollowUp or IntakeStatus.Waitlisted or IntakeStatus.Offered or IntakeStatus.Closed) => true,
            (IntakeStatus.Offered, IntakeStatus.PaperworkReceived or IntakeStatus.Waitlisted or IntakeStatus.Closed) => true,
            (IntakeStatus.PaperworkReceived, IntakeStatus.Enrolled or IntakeStatus.Offered or IntakeStatus.Closed) => true,
            (IntakeStatus.Enrolled, IntakeStatus.PaperworkReceived or IntakeStatus.Closed) => true,
            (IntakeStatus.Closed, IntakeStatus.FollowUp) => true,
            _ => false
        };

    public static void ApplyReview(RegistrationSubmission submission, ReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(request.Status) || !CanTransition(submission.Status, request.Status))
            errors["status"] = ["Choose an available status transition."];
        if (!Enum.IsDefined(request.Transfer) || (submission.Kind == IntakeKind.Returning && request.Transfer != TransferStatus.NotNeeded))
            errors["transfer"] = ["Choose a valid transfer status."];
        if (!Enum.IsDefined(request.PhotoConsent))
            errors["photoConsent"] = ["Choose a valid photo consent status."];
        if (request.CompetitionReady && (request.Status != IntakeStatus.Enrolled || request.Transfer == TransferStatus.NeedsReview))
            errors["competitionReady"] = ["Enrollment and resolved transfer review are required before marking competition-ready."];
        ThrowErrors(errors);
        submission.Status = request.Status;
        submission.Transfer = request.Transfer;
        submission.PhotoConsent = request.PhotoConsent;
        submission.CompetitionReady = request.CompetitionReady;
    }

    private static void ThrowErrors(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }
}
