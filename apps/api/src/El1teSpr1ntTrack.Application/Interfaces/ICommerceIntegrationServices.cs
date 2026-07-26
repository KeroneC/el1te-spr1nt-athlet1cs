namespace El1teSpr1ntTrack.Application.Interfaces;

public interface ISquareClient
{
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken);
    Task<SquareCatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken);
    Task<SquarePaymentLinkResult> CreatePaymentLinkAsync(
        SquarePaymentLinkCommand command,
        CancellationToken cancellationToken);
    Task<SquarePaymentResult> RetrievePaymentAsync(string paymentId, CancellationToken cancellationToken);
    Task<SquareRefundResult> RefundPaymentAsync(
        SquareRefundCommand command,
        CancellationToken cancellationToken);
}

public sealed record SquarePaymentLinkCommand(
    string IdempotencyKey,
    string ReferenceId,
    string RedirectUrl,
    string Currency,
    IReadOnlyList<SquareCheckoutLineItem> Items);

public sealed record SquareCheckoutLineItem(
    string Name,
    string VariationName,
    int Quantity,
    long BasePriceMinor,
    IReadOnlyList<SquareCheckoutModifier> Modifiers);

public sealed record SquareCheckoutModifier(string Name, long BasePriceMinor);

public sealed record SquarePaymentLinkResult(string PaymentLinkId, string SquareOrderId, string Url);

public sealed record SquarePaymentResult(
    string PaymentId,
    string Status,
    string? OrderId,
    long AmountMinor,
    string Currency);

public sealed record SquareRefundCommand(
    string IdempotencyKey,
    string PaymentId,
    long AmountMinor,
    string Currency,
    string Reason);

public sealed record SquareRefundResult(string RefundId, string Status);

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
