using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class AthleteDocument : EntityBase
{
    public Guid AthleteId { get; set; }

    public Athlete? Athlete { get; set; }

    public DocumentType Type { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
