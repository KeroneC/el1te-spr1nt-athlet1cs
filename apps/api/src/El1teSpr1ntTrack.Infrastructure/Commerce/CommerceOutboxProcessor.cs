using System.Text.Json;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class CommerceOutboxProcessor(
    El1teDbContext dbContext,
    IClock clock,
    StoreSettings storeSettings,
    IStoreOrderService orderService,
    ILogger<CommerceOutboxProcessor> logger) : ICommerceOutboxProcessor
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        if (!storeSettings.CommerceOperationsEnabled)
        {
            return false;
        }

        var now = clock.UtcNow;
        var message = await dbContext.CommerceOutboxMessages
            .Where(item =>
                item.ProcessedAtUtc == null &&
                item.AvailableAtUtc <= now &&
                (item.LockedUntilUtc == null || item.LockedUntilUtc < now))
            .OrderBy(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (message is null)
        {
            return false;
        }

        var lockId = Guid.NewGuid();
        message.LockId = lockId;
        message.LockedUntilUtc = now.AddMinutes(2);
        message.AttemptCount++;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            if (message.MessageType == "SquareWebhookReceived")
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                var eventId = payload.RootElement.GetProperty("eventId").GetGuid();
                await orderService.ProcessSquareWebhookAsync(eventId, cancellationToken);
            }
            else if (message.MessageType == "StoreRefundRequested")
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                await orderService.ProcessRefundAsync(
                    payload.RootElement.GetProperty("refundId").GetGuid(),
                    cancellationToken);
            }
            else if (message.MessageType == "StoreEmailRequested")
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                await orderService.SendOrderEmailAsync(
                    payload.RootElement.GetProperty("emailId").GetGuid(),
                    cancellationToken);
            }

            message.ProcessedAtUtc = clock.UtcNow;
            message.LockId = null;
            message.LockedUntilUtc = null;
            message.SafeLastError = null;
            message.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                "Commerce outbox message {MessageId} failed with {FailureType}.",
                message.Id,
                exception.GetType().Name);
            message.LockId = null;
            message.LockedUntilUtc = null;
            message.SafeLastError = exception.GetType().Name;
            message.AvailableAtUtc = clock.UtcNow.AddSeconds(
                Math.Min(300, Math.Pow(2, Math.Min(message.AttemptCount, 8))));
            message.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
