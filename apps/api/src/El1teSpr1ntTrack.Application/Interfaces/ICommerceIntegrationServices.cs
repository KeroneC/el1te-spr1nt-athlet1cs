namespace El1teSpr1ntTrack.Application.Interfaces;

public interface ISquareClient
{
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken);
    Task<SquareCatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken);
    Task<SquarePaymentLinkResult> CreatePaymentLinkAsync(
        SquarePaymentLinkCommand command,
        CancellationToken cancellationToken);
    Task<SquarePaymentResult> RetrievePaymentAsync(string paymentId, CancellationToken cancellationToken);
    Task<SquareOrderResult> RetrieveOrderAsync(string orderId, CancellationToken cancellationToken);
    Task<SquarePaymentLinkDeleteResult> DeletePaymentLinkAsync(
        string paymentLinkId,
        CancellationToken cancellationToken);
    Task<SquareRefundResult> RefundPaymentAsync(
        SquareRefundCommand command,
        CancellationToken cancellationToken);
    Task<SquareRefundStatusResult> RetrieveRefundAsync(
        string refundId,
        CancellationToken cancellationToken);
}

public sealed record SquarePaymentLinkCommand(
    string IdempotencyKey,
    string ReferenceId,
    string RedirectUrl,
    string Currency,
    string BuyerEmail,
    string BuyerPhone,
    IReadOnlyList<SquareCheckoutLineItem> Items);

public sealed record SquareCheckoutLineItem(
    string Name,
    string VariationName,
    int Quantity,
    long BasePriceMinor,
    IReadOnlyList<SquareCheckoutModifier> Modifiers);

public sealed record SquareCheckoutModifier(string Name, long BasePriceMinor);

public sealed record SquarePaymentLinkResult(
    string PaymentLinkId,
    string SquareOrderId,
    string Url,
    long TaxMinor,
    long TotalMinor);

public sealed record SquarePaymentResult(
    string PaymentId,
    string Status,
    string? OrderId,
    long AmountMinor,
    string Currency);

public sealed record SquareOrderResult(
    string OrderId,
    string State,
    long TotalMinor,
    string Currency,
    IReadOnlyList<string> PaymentIds);

public sealed record SquareRefundCommand(
    string IdempotencyKey,
    string PaymentId,
    long AmountMinor,
    string Currency,
    string Reason);

public sealed record SquareRefundResult(string RefundId, string Status);

public sealed record SquareRefundStatusResult(string RefundId, string Status);

public sealed record SquarePaymentLinkDeleteResult(string PaymentLinkId, string? CanceledOrderId);

public interface ISquareSignatureVerifier
{
    bool IsValid(string rawBody, string? suppliedSignature);
}

public interface ISquareWebhookService
{
    Task<SquareWebhookResult> HandleAsync(
        string rawBody,
        string? suppliedSignature,
        CancellationToken cancellationToken);
}

public enum SquareWebhookResult
{
    Accepted,
    Duplicate,
    Disabled,
    InvalidSignature,
    InvalidPayload
}

public interface ICommerceOutboxProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}
