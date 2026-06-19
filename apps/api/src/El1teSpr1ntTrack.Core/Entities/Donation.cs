using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Donation : EntityBase
{
    public string? DonorName { get; set; }

    public string? DonorEmail { get; set; }

    public decimal Amount { get; set; }

    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.Unknown;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
}
