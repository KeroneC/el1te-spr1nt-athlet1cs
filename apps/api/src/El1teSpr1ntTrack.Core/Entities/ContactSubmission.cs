using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class ContactSubmission : CmsEntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public InquiryType InquiryType { get; set; } = InquiryType.General;

    public string Message { get; set; } = string.Empty;

    public ContactSubmissionStatus Status { get; set; } = ContactSubmissionStatus.New;
}
