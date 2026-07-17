namespace El1teSpr1ntTrack.Core.Entities;

public sealed class AdminActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? ActorUserId { get; set; }

    public User? ActorUser { get; set; }

    public string Action { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public Guid? TargetId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }
}
