using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class PrintifyWebhookService(
    El1teDbContext dbContext,
    IPrintifySignatureVerifier signatureVerifier,
    IClock clock,
    PrintifySettings settings) : IPrintifyWebhookService
{
    public async Task<PrintifyWebhookResult> HandleAsync(
        string rawBody,
        string? suppliedSignature,
        CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
        {
            return PrintifyWebhookResult.Disabled;
        }

        if (!signatureVerifier.IsValid(rawBody, suppliedSignature))
        {
            return PrintifyWebhookResult.InvalidSignature;
        }

        Envelope envelope;
        try
        {
            envelope = Parse(rawBody);
        }
        catch (JsonException)
        {
            return PrintifyWebhookResult.InvalidPayload;
        }

        if (string.IsNullOrWhiteSpace(envelope.Id) ||
            string.IsNullOrWhiteSpace(envelope.Type) ||
            string.IsNullOrWhiteSpace(envelope.ResourceId) ||
            string.IsNullOrWhiteSpace(envelope.ResourceType) ||
            !AllowedEventTypes.Contains(envelope.Type))
        {
            return PrintifyWebhookResult.InvalidPayload;
        }

        if (await dbContext.PrintifyWebhookEvents.AnyAsync(
                value => value.PrintifyEventId == envelope.Id,
                cancellationToken))
        {
            return PrintifyWebhookResult.Duplicate;
        }

        var now = clock.UtcNow;
        var webhookEvent = new PrintifyWebhookEvent
        {
            PrintifyEventId = envelope.Id,
            EventType = envelope.Type,
            ResourceId = envelope.ResourceId,
            ResourceType = envelope.ResourceType,
            ShopId = envelope.ShopId,
            PrintifyCreatedAtUtc = envelope.CreatedAtUtc,
            PayloadSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))),
            CreatedAt = now
        };
        dbContext.PrintifyWebhookEvents.Add(webhookEvent);
        dbContext.CommerceOutboxMessages.Add(new CommerceOutboxMessage
        {
            MessageType = "PrintifyWebhookReceived",
            PayloadJson = JsonSerializer.Serialize(new { eventId = webhookEvent.Id }),
            AvailableAtUtc = now,
            CreatedAt = now
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await dbContext.PrintifyWebhookEvents.AnyAsync(
                    value => value.PrintifyEventId == envelope.Id,
                    cancellationToken))
            {
                return PrintifyWebhookResult.Duplicate;
            }

            throw;
        }

        return PrintifyWebhookResult.Accepted;
    }

    private static readonly HashSet<string> AllowedEventTypes =
    [
        "order:created",
        "order:updated",
        "order:sent-to-production",
        "order:shipment:created",
        "order:shipment:delivered",
        "product:updated",
        "product:deleted",
        "shop:disconnected"
    ];

    private static Envelope Parse(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        var resource = root.GetProperty("resource");
        long? shopId = null;
        if (resource.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("shop_id", out var shop))
        {
            shopId = shop.TryGetInt64(out var value) ? value : null;
        }

        return new Envelope(
            String(root, "id"),
            String(root, "type"),
            String(resource, "id") ?? (resource.TryGetProperty("id", out var numericId) ? numericId.ToString() : null),
            String(resource, "type"),
            shopId,
            DateTimeOffset.TryParse(String(root, "created_at"), out var created) ? created : null);
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record Envelope(
        string? Id,
        string? Type,
        string? ResourceId,
        string? ResourceType,
        long? ShopId,
        DateTimeOffset? CreatedAtUtc);
}
