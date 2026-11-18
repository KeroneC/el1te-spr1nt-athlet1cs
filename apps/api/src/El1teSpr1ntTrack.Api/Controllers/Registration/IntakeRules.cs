using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Api.Controllers.Registration;

public sealed record IntakeRequest(int SeasonYear, Guid RequestKey, IntakeKind Kind, string? Channel,
    string? GuardianEmail, string? AthleteFirstName, string? GuardianName,
    string? GuardianPhone, string? AgeGroup, PracticeAvailability? Availability, string? Experience,
    PriorClubAnswer? PriorClub, string? ClubName, string[]? Affiliations, string? LastMeetMonth, string? Website)
{
    public int? ApplicationVersion { get; init; }
    public string? AthleteLastName { get; init; }
    public string? DateOfBirth { get; init; }
    public string? SchoolGrade { get; init; }
    public string? SchoolDistrict { get; init; }
    public string? Gender { get; init; }
    public string? StreetAddress { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? SecondaryGuardianName { get; init; }
    public string? SecondaryGuardianEmail { get; init; }
    public string? SecondaryGuardianPhone { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }
}

public sealed record SeasonRequest(int Year, Guid? Revision, string? Title, DateOnly OpeningDate, DateOnly EndDate,
    string[]? AgeGroups, string? PracticeLocation, string? PracticeSchedule, string? Fees, string? ReplyWindow,
    string? PaperworkEmail, string? PaperworkHandoff, string? ReviewOwner, string? RetentionOwner, string? FirstPracticeDate = null);

public sealed record ReviewRequest(Guid Id, Guid Revision, IntakeStatus Status, TransferStatus Transfer,
    IntakePhotoConsent PhotoConsent, bool CompetitionReady);

public static class IntakeRules
{
    public const int CurrentApplicationVersion = 2;
    private const int MinimumAge = 7;
    private const int MaximumAge = 18;
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly HashSet<string> SchoolGrades = new(
        new[] { "Pre-K", "K", "Not applicable" }.Concat(Enumerable.Range(1, 12).Select(i => i.ToString(CultureInfo.InvariantCulture))),
        StringComparer.Ordinal);
    public static DateOnly LocalDate(DateTimeOffset now) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Eastern).DateTime);
    public static string[] Ages(RegistrationSeason season) => season.AgeGroups.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    public static IntakeChannel Channel(string? value) => value switch
    {
        "facebook" => IntakeChannel.Facebook, "instagram" => IntakeChannel.Instagram, _ => IntakeChannel.Direct
    };

    public static bool HasValidFirstPracticeDate(RegistrationSeason season) =>
        season.FirstPracticeDate is { } date && date >= season.OpeningDate && date <= season.EndDate;

    public static bool IsOpen(RegistrationSeason season, DateTimeOffset now) =>
        season.IsOpen && season.PurgedAtUtc is null && HasValidFirstPracticeDate(season)
        && LocalDate(now) >= season.OpeningDate && LocalDate(now) <= season.EndDate;

    public static RegistrationSubmission Normalize(IntakeRequest request, RegistrationSeason season, DateTimeOffset now,
        string? originalAgeGroup = null, bool enforceCurrentEligibility = true)
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

        string? Phone(string? value, string field, bool required = false)
        {
            var phone = Value(value, field, 32, required);
            if ((phone is null && required) || (phone is not null &&
                (!phone.All(c => char.IsAsciiDigit(c) || " +().-".Contains(c)) || phone.Count(char.IsAsciiDigit) is < 7 or > 15)))
                Error(field, required ? "Enter a valid phone number." : "Enter a valid phone number or leave it blank.");
            return phone;
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
            var fullApplication = request.ApplicationVersion == CurrentApplicationVersion;
            submission.GuardianName = Value(request.GuardianName, "guardianName", 100, true);
            submission.GuardianPhone = Phone(request.GuardianPhone, "guardianPhone", fullApplication);
            if (fullApplication)
            {
                submission.ApplicationVersion = CurrentApplicationVersion;
                submission.AthleteLastName = Value(request.AthleteLastName, "athleteLastName", 80, true);
                if (!DateOnly.TryParseExact(request.DateOfBirth?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var birthDate) || (enforceCurrentEligibility && birthDate > LocalDate(now)))
                {
                    Error("dateOfBirth", "Enter a valid date of birth that is not in the future.");
                }
                else
                {
                    submission.DateOfBirth = birthDate;
                    if (enforceCurrentEligibility && HasValidFirstPracticeDate(season) &&
                        (birthDate.AddYears(MinimumAge) > season.FirstPracticeDate ||
                         birthDate.AddYears(MaximumAge + 1) <= season.FirstPracticeDate))
                        Error("dateOfBirth", $"Athletes must be ages {MinimumAge}–{MaximumAge} on the first practice date.");
                }
                submission.SchoolGrade = Value(request.SchoolGrade, "schoolGrade", 20, true);
                if (submission.SchoolGrade is null || !SchoolGrades.Contains(submission.SchoolGrade))
                    Error("schoolGrade", "Choose a school grade or Not applicable.");
                submission.SchoolDistrict = Value(request.SchoolDistrict, "schoolDistrict", 120, true);
                submission.Gender = Value(request.Gender, "gender", 25);
                if (submission.Gender is not (null or "Male" or "Female" or "Prefer not to answer"))
                    Error("gender", "Choose one of the available options or leave this blank.");
                submission.StreetAddress = Value(request.StreetAddress, "streetAddress", 160, true);
                submission.AddressLine2 = Value(request.AddressLine2, "addressLine2", 120);
                submission.City = Value(request.City, "city", 80, true);
                submission.State = Value(request.State, "state", 60, true);
                submission.PostalCode = Value(request.PostalCode, "postalCode", 20, true);
                submission.SecondaryGuardianName = Value(request.SecondaryGuardianName, "secondaryGuardianName", 100);
                submission.SecondaryGuardianEmail = Value(request.SecondaryGuardianEmail, "secondaryGuardianEmail", 254)?.ToLowerInvariant();
                if (submission.SecondaryGuardianEmail is { } secondaryEmail && !new EmailAddressAttribute().IsValid(secondaryEmail))
                    Error("secondaryGuardianEmail", "Enter a valid email address or leave it blank.");
                submission.SecondaryGuardianPhone = Phone(request.SecondaryGuardianPhone, "secondaryGuardianPhone");
                submission.EmergencyContactName = Value(request.EmergencyContactName, "emergencyContactName", 100, true);
                submission.EmergencyContactPhone = Phone(request.EmergencyContactPhone, "emergencyContactPhone", true);
                submission.EmergencyContactRelationship = Value(request.EmergencyContactRelationship, "emergencyContactRelationship", 80, true);
            }
            else
            {
                submission.AgeGroup = Value(request.AgeGroup, "ageGroup", 40, true);
                if (submission.AgeGroup != originalAgeGroup && !Ages(season).Contains(submission.AgeGroup, StringComparer.Ordinal))
                    Error("ageGroup", "Choose an eligible age group.");
            }
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

    public static string RequestDigest(RegistrationSubmission submission)
    {
        // Keep the legacy JSON names and order: old application and returning receipts must remain replayable.
        var legacy = new
        {
            submission.RequestKey, submission.Kind, submission.GuardianEmail, submission.GuardianName,
            submission.GuardianPhone, submission.AthleteFirstName, submission.AgeGroup, submission.Availability,
            submission.Experience, submission.PriorClub, submission.ClubName, submission.Affiliations, submission.LastMeetMonth
        };
        var payload = submission.ApplicationVersion == CurrentApplicationVersion
            ? JsonSerializer.SerializeToUtf8Bytes(new
            {
                legacy, submission.ApplicationVersion, submission.AthleteLastName, submission.DateOfBirth, submission.SchoolGrade,
                submission.SchoolDistrict, submission.Gender, submission.StreetAddress, submission.AddressLine2, submission.City,
                submission.State, submission.PostalCode, submission.SecondaryGuardianName, submission.SecondaryGuardianEmail,
                submission.SecondaryGuardianPhone, submission.EmergencyContactName, submission.EmergencyContactPhone,
                submission.EmergencyContactRelationship
            })
            : JsonSerializer.SerializeToUtf8Bytes(legacy);
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    public static bool IsCurrentApplication(IntakeRequest request) =>
        request.Kind != IntakeKind.Application || request.ApplicationVersion == CurrentApplicationVersion;

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
        // A draft may be saved without a date, but public intake cannot open until a valid date is set.
        DateOnly? firstPracticeDate = null;
        if (!string.IsNullOrWhiteSpace(request.FirstPracticeDate))
        {
            if (!DateOnly.TryParseExact(request.FirstPracticeDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var practiceDate) || practiceDate < request.OpeningDate || practiceDate > request.EndDate)
                errors["firstPracticeDate"] = ["Choose a first practice date between registration opening and season end."];
            else
                firstPracticeDate = practiceDate;
        }
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
        // Legacy clients can send age groups; do not erase earlier seasons' historical values.
        season.OpeningDate = request.OpeningDate;
        season.EndDate = request.EndDate;
        season.FirstPracticeDate = firstPracticeDate;
        season.Revision = Guid.NewGuid();
    }

    public static bool CanTransition(IntakeKind kind, IntakeStatus current, IntakeStatus next)
    {
        if (current == next) return true;
        if (kind == IntakeKind.Returning && next == IntakeStatus.PaperworkReceived) return false;
        return (current, next) switch
        {
            (IntakeStatus.New or IntakeStatus.FollowUp or IntakeStatus.Waitlisted,
                IntakeStatus.FollowUp or IntakeStatus.Waitlisted or IntakeStatus.Offered or IntakeStatus.Closed) => true,
            (IntakeStatus.Offered, IntakeStatus.Enrolled) => kind == IntakeKind.Returning,
            (IntakeStatus.Offered, IntakeStatus.PaperworkReceived or IntakeStatus.Waitlisted or IntakeStatus.Closed) => true,
            (IntakeStatus.PaperworkReceived, IntakeStatus.Enrolled or IntakeStatus.Offered or IntakeStatus.Closed) => true,
            (IntakeStatus.Enrolled, IntakeStatus.PaperworkReceived or IntakeStatus.Closed) => true,
            (IntakeStatus.Closed, IntakeStatus.FollowUp) => true,
            _ => false
        };
    }

    public static void ApplyReview(RegistrationSubmission submission, ReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(request.Status) || !CanTransition(submission.Kind, submission.Status, request.Status))
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
