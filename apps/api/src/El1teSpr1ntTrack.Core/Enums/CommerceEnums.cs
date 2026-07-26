namespace El1teSpr1ntTrack.Core.Enums;

public enum StoreProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum StoreOrderStatus
{
    AwaitingPayment = 0,
    Paid = 1,
    NeedsReview = 2,
    ReadyForProduction = 3,
    InProduction = 4,
    NeedsCustomerInfo = 5,
    ReadyForHandoff = 6,
    Completed = 7,
    Canceled = 8,
    Refunded = 9
}

public enum InventoryAdjustmentReason
{
    Receipt = 0,
    Sale = 1,
    Correction = 2,
    Damage = 3,
    ReturnRestock = 4,
    ReturnWithoutRestock = 5,
    ReservationRelease = 6
}

public enum ProductModifierType
{
    Choice = 0,
    Color = 1,
    ShortText = 2,
    Number = 3
}

public enum ProductMediaRole
{
    Gallery = 0,
    MockupBase = 1,
    LogoOverlay = 2
}

public enum CommerceRefundStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}

public enum CommerceEmailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public enum SquareCatalogImportStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2
}
