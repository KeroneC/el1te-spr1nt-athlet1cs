using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Order : EntityBase
{
    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.Unknown;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public decimal Total { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
