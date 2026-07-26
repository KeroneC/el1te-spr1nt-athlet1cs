using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Order : EntityBase
{
    public string PublicNumber { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public string? AthleteTeamNote { get; set; }

    public string? FulfillmentNote { get; set; }

    public StoreOrderStatus Status { get; set; } = StoreOrderStatus.AwaitingPayment;

    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.Unknown;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public string Currency { get; set; } = "USD";

    public long SubtotalMinor { get; set; }

    public long TaxMinor { get; set; }

    public long TotalMinor { get; set; }

    public bool HasUnusualRequest { get; set; }

    public string? TrackingTokenHash { get; set; }

    public DateTimeOffset? TrackingExpiresAtUtc { get; set; }

    public string? SquareOrderId { get; set; }

    public string? SquarePaymentId { get; set; }

    public string? SquarePaymentLinkId { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    public ICollection<InventoryReservation> Reservations { get; set; } = new List<InventoryReservation>();

    public ICollection<CommerceRefund> Refunds { get; set; } = new List<CommerceRefund>();

    public ICollection<OrderInternalNote> InternalNotes { get; set; } = new List<OrderInternalNote>();

    public ICollection<CommerceEmailMessage> EmailHistory { get; set; } = new List<CommerceEmailMessage>();
}
