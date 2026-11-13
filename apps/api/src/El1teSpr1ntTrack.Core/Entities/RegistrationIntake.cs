namespace El1teSpr1ntTrack.Core.Entities;

public enum IntakeKind { Application = 1, Returning }
public enum IntakeChannel { Direct = 0, Facebook, Instagram }
public enum PriorClubAnswer { No = 1, Yes, Unsure }
public enum PracticeAvailability { Yes = 1, No, Unsure }
public enum IntakeStatus { New, FollowUp, Waitlisted, Offered, PaperworkReceived, Enrolled, Closed }
public enum TransferStatus { NotNeeded, NeedsReview, Cleared }
public enum IntakePhotoConsent { NotRecorded, Granted, Declined }

public sealed class RegistrationSeason
{
    public int Year { get; set; }
    public string Title { get; set; } = "";
    public DateOnly OpeningDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string AgeGroups { get; set; } = "";
    public string PracticeLocation { get; set; } = "";
    public string PracticeSchedule { get; set; } = "";
    public string Fees { get; set; } = "";
    public string ReplyWindow { get; set; } = "";
    public string PaperworkEmail { get; set; } = "";
    public string PaperworkHandoff { get; set; } = "";
    public string ReviewOwner { get; set; } = "";
    public string RetentionOwner { get; set; } = "";
    public bool IsOpen { get; set; }
    public DateTimeOffset? OpenedAtUtc { get; set; }
    public Guid? OpenedByUserId { get; set; }
    public DateTimeOffset? PurgedAtUtc { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}

public sealed class RegistrationSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SeasonYear { get; set; }
    public IntakeKind Kind { get; set; }
    public IntakeChannel Channel { get; set; }
    public string ReceiptReference { get; set; } = "";
    public Guid RequestKey { get; set; }
    public string RequestDigest { get; set; } = "";
    public string GuardianEmail { get; set; } = "";
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string AthleteFirstName { get; set; } = "";
    public string? AgeGroup { get; set; }
    public PracticeAvailability? Availability { get; set; }
    public string? Experience { get; set; }
    public PriorClubAnswer? PriorClub { get; set; }
    public string? ClubName { get; set; }
    public string? Affiliations { get; set; }
    public string? LastMeetMonth { get; set; }
    public IntakeStatus Status { get; set; }
    public TransferStatus Transfer { get; set; }
    public IntakePhotoConsent PhotoConsent { get; set; }
    public bool CompetitionReady { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public ICollection<RegistrationReview> History { get; set; } = [];
}

public sealed class RegistrationReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
    public IntakeStatus Status { get; set; }
    public TransferStatus Transfer { get; set; }
    public IntakePhotoConsent PhotoConsent { get; set; }
    public bool CompetitionReady { get; set; }
}

public sealed class RegistrationCount
{
    public int SeasonYear { get; set; }
    public IntakeChannel Channel { get; set; }
    public long Visits { get; set; }
    public int Applications { get; set; }
    public int Returning { get; set; }
    public int Enrolled { get; set; }
}
