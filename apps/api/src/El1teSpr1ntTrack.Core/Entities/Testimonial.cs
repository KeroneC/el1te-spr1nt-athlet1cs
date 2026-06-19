using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Testimonial : EntityBase
{
    public string DisplayName { get; set; } = string.Empty;

    public string Quote { get; set; } = string.Empty;

    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;
}
