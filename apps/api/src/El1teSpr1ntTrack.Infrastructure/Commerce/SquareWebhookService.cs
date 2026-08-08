using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class SquareWebhookService(
    El1teDbContext dbContext,
    ISquareSignatureVerifier signatureVerifier,
    IClock clock,
    StoreSettings storeSettings) : ISquareWebhookService
{
    public async Task<SquareWebhookResult> HandleAsync(
        string rawBody,
        string? suppliedSignature,
        CancellationToken cancellationToken)
    {
        if (!storeSettings.CommerceOperationsEnabled)
        {
            return SquareWebhookResult.Disabled;
        }

        if (!signatureVerifier.IsValid(rawBody, suppliedSignature))
        {
            return SquareWebhookResult.InvalidSignature;
        }

        WebhookEnvelope envelope;
        try
        {
            envelope = Parse(rawBody);
        }
        catch (JsonException)
        {
            return SquareWebhookResult.InvalidPayload;
        }

        if (string.IsNullOrWhiteSpace(envelope.EventId) ||
            string.IsNullOrWhiteSpace(envelope.EventType))
        {
            return SquareWebhookResult.InvalidPayload;
        }

        if (await dbContext.SquareWebhookEvents
                .AnyAsync(item => item.SquareEventId == envelope.EventId, cancellationToken))
        {
            return SquareWebhookResult.Duplicate;
        }

        var now = clock.UtcNow;
        var webhookEvent = new SquareWebhookEvent
        {
            SquareEventId = envelope.EventId,
            EventType = envelope.EventType,
            MerchantId = envelope.MerchantId,
            ObjectId = envelope.ObjectId,
            SquareCreatedAtUtc = envelope.CreatedAtUtc,
            PayloadSha256 = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))),
            CreatedAt = now
        };
        dbContext.SquareWebhookEvents.Add(webhookEvent);
        dbContext.CommerceOutboxMessages.Add(new CommerceOutboxMessage
        {
            MessageType = "SquareWebhookReceived",
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
            if (await dbContext.SquareWebhookEvents
                    .AnyAsync(item => item.SquareEventId == envelope.EventId, cancellationToken))
            {
                return SquareWebhookResult.Duplicate;
            }

            throw;
        }

        return SquareWebhookResult.Accepted;
    }

    private static WebhookEnvelope Parse(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        var eventId = GetString(root, "event_id");
        var eventType = GetString(root, "type");
        var merchantId = GetString(root, "merchant_id");
        DateTimeOffset? createdAt = DateTimeOffset.TryParse(
            GetString(root, "created_at"),
            out var parsedCreatedAt)
            ? parsedCreatedAt
            : null;

        string? objectId = null;
        if (root.TryGetProperty("data", out var data))
        {
            objectId = GetString(data, "id");
            if (objectId is null &&
                data.TryGetProperty("object", out var objectElement) &&
                objectElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in objectElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        objectId = GetString(property.Value, "id");
                        if (objectId is not null)
                        {
                            break;
                        }
                    }
                }
            }
        }

        return new WebhookEnvelope(eventId, eventType, merchantId, objectId, createdAt);
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record WebhookEnvelope(
        string? EventId,
        string? EventType,
        string? MerchantId,
        string? ObjectId,
        DateTimeOffset? CreatedAtUtc);
}
