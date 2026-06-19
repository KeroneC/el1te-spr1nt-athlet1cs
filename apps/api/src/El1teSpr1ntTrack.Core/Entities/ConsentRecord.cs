using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class ConsentRecord : EntityBase
{
    public Guid AthleteId { get; set; }

    public Athlete? Athlete { get; set; }

    public ConsentType Type { get; set; }

    public Guid? ConsentedByUserId { get; set; }

    public User? ConsentedByUser { get; set; }

    public DateTimeOffset ConsentedAt { get; set; } = DateTimeOffset.UtcNow;
}
