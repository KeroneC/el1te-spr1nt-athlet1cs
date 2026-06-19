namespace El1teSpr1ntTrack.Core.Entities;

public sealed class ContactSubmission : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
