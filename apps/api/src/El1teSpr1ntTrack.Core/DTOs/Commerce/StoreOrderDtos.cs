using System.ComponentModel.DataAnnotations;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Commerce;

public sealed class PublicStoreCheckoutRequestDto
{
    [Required, MaxLength(64)]
    public string CheckoutAttemptId { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string CustomerName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required, Phone, MaxLength(40)]
    public string CustomerPhone { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? AthleteTeamNote { get; init; }

    public bool ConfirmsAdultBuyer { get; init; }
    public bool AcceptsStorePolicy { get; init; }
    public IReadOnlyList<PublicStoreCheckoutLineDto> Lines { get; init; } = [];
}

public sealed class PublicStoreCheckoutLineDto
{
    public Guid ProductVariantId { get; init; }

    [Range(1, 10)]
    public int Quantity { get; init; }

    public IReadOnlyList<Guid> ModifierValueIds { get; init; } = [];
    public IReadOnlyList<PublicStoreCustomInputDto> CustomInputs { get; init; } = [];
}

public sealed class PublicStoreCustomInputDto
{
    public Guid ModifierGroupId { get; init; }

    [Required, MaxLength(40)]
    public string Value { get; init; } = string.Empty;
}

public sealed record PublicStoreCheckoutResultDto(
    string OrderReference,
    string CheckoutUrl,
    string ReturnToken,
    DateTimeOffset ReservationExpiresAtUtc,
    long SubtotalMinor,
    long TaxMinor,
    long TotalMinor,
    string Currency);

public sealed class PublicStoreOrderTokenDto
{
    [Required, MaxLength(200)]
    public string Token { get; init; } = string.Empty;
}

public sealed record PublicStoreOrderStatusDto(
    string OrderReference,
    StoreOrderStatus Status,
    PaymentStatus PaymentStatus,
    long SubtotalMinor,
    long TaxMinor,
    long TotalMinor,
    string Currency,
    bool HasPersonalization,
    DateTimeOffset? CustomerCancellationExpiresAtUtc,
    bool CanCustomerCancel,
    IReadOnlyList<PublicStoreOrderItemDto> Items,
    IReadOnlyList<PublicStoreOrderTimelineDto> Timeline);

public sealed record PublicStoreOrderItemDto(
    string ProductName,
    string VariantName,
    int Quantity,
    long UnitPriceMinor,
    long LineTotalMinor,
    IReadOnlyList<PublicStoreConfigurationValueDto> Configuration);

public sealed record PublicStoreConfigurationValueDto(string Label, string Value);

public sealed record PublicStoreOrderTimelineDto(
    StoreOrderStatus Status,
    string Label,
    DateTimeOffset CreatedAtUtc);

public sealed record PublicCheckoutReturnStatusDto(
    string OrderReference,
    PaymentStatus PaymentStatus,
    StoreOrderStatus Status,
    bool IsFinal,
    string Message);

public sealed record AdminStoreOperationsDashboardDto(
    int AwaitingPayment,
    int CancellationHold,
    int NeedsReview,
    int InProduction,
    int ReadyForHandoff,
    int RefundFailures,
    int EmailFailures);

public sealed record AdminStoreOrderSummaryDto(
    Guid Id,
    string OrderReference,
    string CustomerName,
    string CustomerEmail,
    StoreOrderStatus Status,
    PaymentStatus PaymentStatus,
    long TotalMinor,
    string Currency,
    bool HasPersonalization,
    DateTimeOffset? CustomerCancellationExpiresAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminStoreOrderDto(
    Guid Id,
    string OrderReference,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string? AthleteTeamNote,
    string? FulfillmentNote,
    StoreOrderStatus Status,
    PaymentStatus PaymentStatus,
    long SubtotalMinor,
    long TaxMinor,
    long TotalMinor,
    string Currency,
    bool HasPersonalization,
    DateTimeOffset? CustomerCancellationExpiresAtUtc,
    string? SquareOrderId,
    string? SquarePaymentId,
    IReadOnlyList<AdminStoreOrderItemDto> Items,
    IReadOnlyList<AdminStoreOrderTimelineDto> Timeline,
    IReadOnlyList<AdminStoreOrderNoteDto> Notes,
    IReadOnlyList<AdminStoreRefundDto> Refunds,
    IReadOnlyList<AdminStoreEmailDto> Emails,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AdminStoreOrderItemDto(
    Guid Id,
    Guid? ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    long UnitPriceMinor,
    long LineTotalMinor,
    IReadOnlyList<PublicStoreConfigurationValueDto> Configuration);

public sealed record AdminStoreOrderTimelineDto(
    StoreOrderStatus FromStatus,
    StoreOrderStatus ToStatus,
    string? Note,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminStoreOrderNoteDto(Guid Id, string Note, DateTimeOffset CreatedAtUtc);

public sealed record AdminStoreRefundDto(
    Guid Id,
    long AmountMinor,
    CommerceRefundStatus Status,
    string Reason,
    string? SafeFailureCode,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminStoreEmailDto(
    Guid Id,
    string TemplateName,
    CommerceEmailStatus Status,
    string? SafeFailureCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SentAtUtc);

public sealed record AdminStoreOrderOptions(
    string? Search,
    StoreOrderStatus? Status,
    PaymentStatus? PaymentStatus,
    int Page = 1,
    int PageSize = 20);

public sealed class AdminStoreOrderTransitionDto
{
    public StoreOrderStatus Status { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }
}

public sealed class AdminStoreOrderNoteWriteDto
{
    [Required, MaxLength(2000)]
    public string Note { get; init; } = string.Empty;
}

public sealed class AdminStoreRefundWriteDto
{
    [Range(1, long.MaxValue)]
    public long AmountMinor { get; init; }

    [Required, MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    public IReadOnlyList<AdminStoreRefundLineWriteDto> Lines { get; init; } = [];
}

public sealed record AdminStoreRefundLineWriteDto(
    Guid OrderItemId,
    int Quantity,
    int RestockQuantity);

public sealed record AdminTrackingLinkResultDto(string TrackingUrl);

public sealed record AdminCommerceIntegrationHealthDto(
    bool CheckoutEnabled,
    bool SquareConfigured,
    bool SquareReachable,
    int PendingOutboxMessages,
    int FailedRefunds,
    int FailedEmails);
