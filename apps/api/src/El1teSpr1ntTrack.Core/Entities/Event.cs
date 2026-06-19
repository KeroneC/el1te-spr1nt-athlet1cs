namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Event : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public decimal RegistrationFee { get; set; }
}
