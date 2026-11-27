namespace El1teSpr1ntTrack.Core.Entities;

public sealed class RegistrationApplicationAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public string Recipient { get; set; } = "";
    public DateTimeOffset AvailableAtUtc { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }
    public Guid? LockId { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? SafeLastError { get; set; }
}
