using System.Security.Cryptography;
using System.Text;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class SquareWebhookPersistenceTests
{
    [Fact]
    public async Task HandleAsync_PersistsSafeMetadataAndQueuesOnce()
    {
        await using var dbContext = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 7, 26, 5, 0, 0, TimeSpan.Zero));
        var service = new SquareWebhookService(
            dbContext,
            new FixedVerifier(true),
            clock,
            new StoreSettings { Enabled = true });
        var rawBody =
            """{"event_id":"square-event-1","type":"payment.updated","merchant_id":"merchant-1","created_at":"2026-07-26T04:59:00Z","data":{"object":{"payment":{"id":"payment-1","buyer_email_address":"private@example.com"}}}}""";

        var first = await service.HandleAsync(rawBody, "valid", CancellationToken.None);
        var duplicate = await service.HandleAsync(rawBody, "valid", CancellationToken.None);

        Assert.Equal(SquareWebhookResult.Accepted, first);
        Assert.Equal(SquareWebhookResult.Duplicate, duplicate);
        var storedEvent = Assert.Single(await dbContext.SquareWebhookEvents.ToListAsync());
        Assert.Equal("square-event-1", storedEvent.SquareEventId);
        Assert.Equal("payment.updated", storedEvent.EventType);
        Assert.Equal("payment-1", storedEvent.ObjectId);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))),
            storedEvent.PayloadSha256);
        Assert.DoesNotContain("private@example.com", storedEvent.PayloadSha256);
        var outbox = Assert.Single(await dbContext.CommerceOutboxMessages.ToListAsync());
        Assert.DoesNotContain("private@example.com", outbox.PayloadJson);
    }

    [Fact]
    public async Task HandleAsync_RejectsBeforePersistenceAndHidesDisabledEndpoint()
    {
        await using var dbContext = CreateDbContext();
        var disabled = new SquareWebhookService(
            dbContext,
            new FixedVerifier(true),
            new FixedClock(DateTimeOffset.UtcNow),
            new StoreSettings { Enabled = false });

        Assert.Equal(
            SquareWebhookResult.Disabled,
            await disabled.HandleAsync("{}", "anything", CancellationToken.None));

        var enabled = new SquareWebhookService(
            dbContext,
            new FixedVerifier(false),
            new FixedClock(DateTimeOffset.UtcNow),
            new StoreSettings { Enabled = true });
        Assert.Equal(
            SquareWebhookResult.InvalidSignature,
            await enabled.HandleAsync("{}", "invalid", CancellationToken.None));
        Assert.Empty(await dbContext.SquareWebhookEvents.ToListAsync());
        Assert.Empty(await dbContext.CommerceOutboxMessages.ToListAsync());
    }

    private static El1teDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new El1teDbContext(options);
    }

    private sealed class FixedVerifier(bool result) : ISquareSignatureVerifier
    {
        public bool IsValid(string rawBody, string? suppliedSignature) => result;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
