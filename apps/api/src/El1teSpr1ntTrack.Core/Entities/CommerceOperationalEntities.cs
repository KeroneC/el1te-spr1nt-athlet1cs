using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class InventoryAdjustment : EntityBase
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public InventoryAdjustmentReason Reason { get; set; }
    public int QuantityDelta { get; set; }
    public int ResultingOnHandQuantity { get; set; }
    public string? Note { get; set; }
}

public sealed class InventoryStocktake : EntityBase
{
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public string? Note { get; set; }
    public int VariantCount { get; set; }
    public int ChangedVariantCount { get; set; }
    public ICollection<InventoryStocktakeLine> Lines { get; set; } = new List<InventoryStocktakeLine>();
}

public sealed class InventoryStocktakeLine : EntityBase
{
    public Guid InventoryStocktakeId { get; set; }
    public InventoryStocktake InventoryStocktake { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int ExpectedOnHandQuantity { get; set; }
    public int CountedOnHandQuantity { get; set; }
    public Guid? InventoryAdjustmentId { get; set; }
    public InventoryAdjustment? InventoryAdjustment { get; set; }
}

public sealed class SquareCatalogImportRun : EntityBase
{
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public SquareCatalogImportStatus Status { get; set; } = SquareCatalogImportStatus.Running;
    public int ProductsDiscovered { get; set; }
    public int ProductsCreated { get; set; }
    public int ProductsSkipped { get; set; }
    public int ImagesImported { get; set; }
    public string? SafeFailureCode { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class InventoryReservation : EntityBase
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public DateTimeOffset? CommittedAtUtc { get; set; }
    public ICollection<InventoryReservationItem> Items { get; set; } = new List<InventoryReservationItem>();
}

public sealed class InventoryReservationItem : EntityBase
{
    public Guid InventoryReservationId { get; set; }
    public InventoryReservation InventoryReservation { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
}

public sealed class OrderStatusHistory : EntityBase
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public StoreOrderStatus FromStatus { get; set; }
    public StoreOrderStatus ToStatus { get; set; }
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string? Note { get; set; }
}

public sealed class CommerceRefund : EntityBase
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "USD";
    public CommerceRefundStatus Status { get; set; } = CommerceRefundStatus.Pending;
    public string Reason { get; set; } = string.Empty;
    public string? SquareRefundId { get; set; }
    public string? SafeFailureCode { get; set; }
}

public sealed class OrderInternalNote : EntityBase
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public string Note { get; set; } = string.Empty;
}

public sealed class CommerceEmailMessage : EntityBase
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string TemplateName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public CommerceEmailStatus Status { get; set; } = CommerceEmailStatus.Pending;
    public string? ProviderMessageId { get; set; }
    public string? SafeFailureCode { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
}

public sealed class SquareWebhookEvent : EntityBase
{
    public string SquareEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? MerchantId { get; set; }
    public string? ObjectId { get; set; }
    public string PayloadSha256 { get; set; } = string.Empty;
    public DateTimeOffset? SquareCreatedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}

public sealed class CommerceOutboxMessage : EntityBase
{
    public string MessageType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset AvailableAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }
    public Guid? LockId { get; set; }
    public int AttemptCount { get; set; }
    public string? SafeLastError { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
