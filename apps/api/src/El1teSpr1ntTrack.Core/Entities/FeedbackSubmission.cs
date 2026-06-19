namespace El1teSpr1ntTrack.Core.Entities;

public sealed class FeedbackSubmission : EntityBase
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string Message { get; set; } = string.Empty;
}
