namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IPrintifyClient
{
    Task<PrintifyShopSnapshot> GetShopAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PrintifyProductSnapshot>> GetProductsAsync(CancellationToken cancellationToken);
    Task<PrintifyProductSnapshot> GetProductAsync(string productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PrintifyWebhookSubscription>> GetWebhooksAsync(CancellationToken cancellationToken);
}

public sealed record PrintifyShopSnapshot(long Id, string Title, string SalesChannel);

public sealed record PrintifyProductSnapshot(
    string Id,
    string Title,
    string? Description,
    int BlueprintId,
    int PrintProviderId,
    IReadOnlyList<PrintifyOptionSnapshot> Options,
    IReadOnlyList<PrintifyVariantSnapshot> Variants,
    IReadOnlyList<PrintifyImageSnapshot> Images);

public sealed record PrintifyOptionSnapshot(
    string Name,
    int DisplayOrder,
    IReadOnlyList<PrintifyOptionValueSnapshot> Values);

public sealed record PrintifyOptionValueSnapshot(int Id, string Title, string? ColorHex, int DisplayOrder);

public sealed record PrintifyVariantSnapshot(
    int Id,
    string Title,
    string? Sku,
    long ProviderCostMinor,
    bool IsEnabled,
    bool IsAvailable,
    IReadOnlyList<int> OptionValueIds);

public sealed record PrintifyImageSnapshot(string SourceUrl, IReadOnlyList<int> VariantIds, bool IsDefault);

public sealed record PrintifyWebhookSubscription(string Id, string Topic, string Url);

public interface IPrintifyCatalogImageImporter
{
    Task<Guid?> ImportAsync(
        PrintifyImageSnapshot image,
        string productName,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

public interface IPrintifySignatureVerifier
{
    bool IsValid(string rawBody, string? suppliedSignature);
}

public interface IPrintifyWebhookService
{
    Task<PrintifyWebhookResult> HandleAsync(
        string rawBody,
        string? suppliedSignature,
        CancellationToken cancellationToken);
}

public enum PrintifyWebhookResult
{
    Accepted,
    Duplicate,
    Disabled,
    InvalidSignature,
    InvalidPayload
}
