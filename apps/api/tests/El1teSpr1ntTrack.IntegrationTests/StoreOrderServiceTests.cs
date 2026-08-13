using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class StoreOrderServiceTests
{
    [Fact]
    public async Task Checkout_NormalizesPhoneBeforePersistenceAndSquarePrepopulation()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var square = new FakeSquareClient();
        var service = Service(db, square, new RecordingEmailSender(), new TestClock(DateTimeOffset.UtcNow));
        var request = Request(product);
        request = CopyRequest(request, "(412) 555-0100");

        await service.CheckoutAsync(request, CancellationToken.None);

        Assert.Equal("+14125550100", (await db.Orders.SingleAsync()).CustomerPhone);
        Assert.Equal("+14125550100", square.CreateCommands.Single().BuyerPhone);
    }

    [Fact]
    public async Task Checkout_PreservesRequiredLogoColorInSnapshotAndSquareDetailsWithoutSplittingStock()
    {
        await using var db = Context();
        var product = ProductGraph();
        var baseRequest = Request(product);
        var logoColor = new ProductModifierGroup
        {
            Product = product,
            Name = "Logo Color",
            Type = ProductModifierType.Color,
            IsRequired = true,
            MinimumSelections = 1,
            MaximumSelections = 1,
            IsActive = true
        };
        var white = new ProductModifierValue
        {
            ProductModifierGroup = logoColor,
            Name = "White",
            PriceAdjustmentMinor = 0,
            IsActive = true
        };
        logoColor.Values.Add(white);
        product.ModifierGroups.Add(logoColor);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var square = new FakeSquareClient();
        var service = Service(db, square, new RecordingEmailSender(), new TestClock(DateTimeOffset.UtcNow));
        var line = baseRequest.Lines.Single();
        var request = new PublicStoreCheckoutRequestDto
        {
            CheckoutAttemptId = baseRequest.CheckoutAttemptId,
            CustomerName = baseRequest.CustomerName,
            CustomerEmail = baseRequest.CustomerEmail,
            CustomerPhone = baseRequest.CustomerPhone,
            ConfirmsAdultBuyer = true,
            AcceptsStorePolicy = true,
            Lines = [new PublicStoreCheckoutLineDto
            {
                ProductVariantId = line.ProductVariantId,
                Quantity = line.Quantity,
                ModifierValueIds = [white.Id],
                CustomInputs = line.CustomInputs
            }]
        };

        await service.CheckoutAsync(request, CancellationToken.None);

        var orderItem = await db.OrderItems.SingleAsync();
        Assert.Contains("\"label\":\"Logo Color\"", orderItem.ConfigurationJson);
        Assert.Contains("\"value\":\"White\"", orderItem.ConfigurationJson);
        Assert.Contains(square.CreateCommands.Single().Items.Single().Modifiers,
            value => value.Name == "Logo Color: White" && value.BasePriceMinor == 0);
        Assert.Single(product.Variants);
    }

    [Fact]
    public async Task Checkout_RejectsMalformedPhoneBeforeCreatingOrderOrReservation()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var service = Service(db, new FakeSquareClient(), new RecordingEmailSender(), new TestClock(DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<El1teSpr1ntTrack.Application.Common.Exceptions.CmsRequestValidationException>(() =>
            service.CheckoutAsync(CopyRequest(Request(product), "555-0100"), CancellationToken.None));

        Assert.Contains("customerPhone", exception.Errors);
        Assert.Empty(await db.Orders.ToListAsync());
        Assert.Equal(0, (await db.ProductVariants.AsNoTracking().SingleAsync()).ReservedQuantity);
    }

    [Fact]
    public async Task SquareRejectedPhone_CancelsOrderAndReleasesReservationExactlyOnce()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var square = new FakeSquareClient
        {
            CreateFailure = new SquareIntegrationException(
                "INVALID_PHONE_NUMBER", 400, "pre_populated_data.buyer_phone_number")
        };
        var service = Service(db, square, new RecordingEmailSender(), new TestClock(DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<El1teSpr1ntTrack.Application.Common.Exceptions.CmsRequestValidationException>(() =>
            service.CheckoutAsync(Request(product), CancellationToken.None));

        Assert.Contains("customerPhone", exception.Errors);
        var order = await db.Orders.Include(value => value.Reservations).SingleAsync();
        Assert.Equal(StoreOrderStatus.Canceled, order.Status);
        Assert.Equal(PaymentStatus.Failed, order.PaymentStatus);
        Assert.Equal(0, (await db.ProductVariants.AsNoTracking().SingleAsync()).ReservedQuantity);
        Assert.NotNull(order.Reservations.Single().ReleasedAtUtc);
        Assert.Equal(0, await service.RunMaintenanceAsync(CancellationToken.None));
        Assert.Equal(0, (await db.ProductVariants.AsNoTracking().SingleAsync()).ReservedQuantity);
    }

    [Fact]
    public async Task CheckoutPaymentAndCancellation_AreIdempotentAndRestoreStockOnce()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var variant = product.Variants.Single();
        var personalization = product.ModifierGroups.Single();
        var square = new FakeSquareClient();
        var email = new RecordingEmailSender();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero));
        var service = Service(db, square, email, clock);
        var request = new PublicStoreCheckoutRequestDto
        {
            CheckoutAttemptId = Guid.NewGuid().ToString(),
            CustomerName = "Adult Buyer",
            CustomerEmail = "buyer@example.com",
            CustomerPhone = "+14125550100",
            ConfirmsAdultBuyer = true,
            AcceptsStorePolicy = true,
            Lines = [new PublicStoreCheckoutLineDto
            {
                ProductVariantId = variant.Id,
                Quantity = 1,
                CustomInputs = [new PublicStoreCustomInputDto
                {
                    ModifierGroupId = personalization.Id,
                    Value = "12"
                }]
            }]
        };

        var checkout = await service.CheckoutAsync(request, CancellationToken.None);
        var retry = await service.CheckoutAsync(request, CancellationToken.None);

        Assert.Equal(checkout.OrderReference, retry.OrderReference);
        Assert.Single(await db.Orders.ToListAsync());
        Assert.Equal(1, variant.ReservedQuantity);
        Assert.Equal(2700, checkout.TotalMinor);

        var order = await db.Orders.SingleAsync();
        var webhook = new SquareWebhookEvent
        {
            SquareEventId = "event-1",
            EventType = "payment.updated",
            ObjectId = "payment-1",
            PayloadSha256 = new string('A', 64),
            CreatedAt = clock.UtcNow
        };
        db.SquareWebhookEvents.Add(webhook);
        await db.SaveChangesAsync();

        await service.ProcessSquareWebhookAsync(webhook.Id, CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(StoreOrderStatus.NeedsReview, order.Status);
        Assert.Equal(2, variant.OnHandQuantity);
        Assert.Equal(0, variant.ReservedQuantity);
        Assert.Equal(clock.UtcNow.AddMinutes(30), order.CustomerCancellationExpiresAtUtc);

        var confirmation = await db.CommerceEmailMessages.SingleAsync(value => value.TemplateName == "PaymentConfirmation");
        await service.SendOrderEmailAsync(confirmation.Id, CancellationToken.None);
        Assert.Equal("provider-message-1", confirmation.ProviderMessageId);
        Assert.Equal(CommerceEmailStatus.Sent, confirmation.Status);
        var token = email.Last!.PlainText.Split("#token=", StringSplitOptions.None)[1]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];

        await service.CancelPublicOrderAsync(token, CancellationToken.None);

        Assert.Equal(StoreOrderStatus.Canceled, order.Status);
        Assert.Equal(PaymentStatus.Refunding, order.PaymentStatus);
        Assert.Equal(3, variant.OnHandQuantity);
        Assert.Single(order.Refunds);
        Assert.Equal(0, order.Refunds.Single().Lines.Single().RestockQuantity);

        var refundId = order.Refunds.Single().Id;
        await service.ProcessRefundAsync(refundId, CancellationToken.None);
        await service.ProcessRefundAsync(refundId, CancellationToken.None);

        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
        Assert.Equal(StoreOrderStatus.Refunded, order.Status);
        Assert.Equal(3, variant.OnHandQuantity);
        Assert.Equal(CommerceRefundStatus.Completed, order.Refunds.Single().Status);

        db.SquareWebhookEvents.Add(new SquareWebhookEvent
        {
            SquareEventId = "event-after-refund",
            EventType = "payment.updated",
            ObjectId = "payment-1",
            PayloadSha256 = new string('B', 64),
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();
        var delayedPayment = await db.SquareWebhookEvents.SingleAsync(value => value.SquareEventId == "event-after-refund");

        await service.ProcessSquareWebhookAsync(delayedPayment.Id, CancellationToken.None);

        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
        Assert.Equal(StoreOrderStatus.Refunded, order.Status);
        Assert.Equal(3, variant.OnHandQuantity);
    }

    [Fact]
    public async Task Checkout_RejectsServerSidePriceAndAvailabilityChanges()
    {
        await using var db = Context();
        var product = ProductGraph();
        product.Variants.Single().OnHandQuantity = 0;
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var service = Service(db, new FakeSquareClient(), new RecordingEmailSender(),
            new TestClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<El1teSpr1ntTrack.Application.Common.Exceptions.CmsConflictException>(() =>
            service.CheckoutAsync(new PublicStoreCheckoutRequestDto
            {
                CheckoutAttemptId = Guid.NewGuid().ToString(),
                CustomerName = "Adult Buyer",
                CustomerEmail = "buyer@example.com",
                CustomerPhone = "+14125550100",
                ConfirmsAdultBuyer = true,
                AcceptsStorePolicy = true,
                Lines = [new PublicStoreCheckoutLineDto
                {
                    ProductVariantId = product.Variants.Single().Id,
                    Quantity = 1,
                    CustomInputs = [new PublicStoreCustomInputDto
                    {
                        ModifierGroupId = product.ModifierGroups.Single().Id,
                        Value = "8"
                    }]
                }]
            }, CancellationToken.None));
    }

    [Fact]
    public async Task AmbiguousSquareCheckout_KeepsStockReservedAndRetriesWithStableIdempotencyKey()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var square = new FakeSquareClient { CreateFailuresRemaining = 1 };
        var service = Service(db, square, new RecordingEmailSender(),
            new TestClock(new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero)));
        var request = Request(product);

        await Assert.ThrowsAsync<SquareIntegrationException>(() =>
            service.CheckoutAsync(request, CancellationToken.None));

        Assert.Equal(1, product.Variants.Single().ReservedQuantity);
        Assert.Equal(StoreOrderStatus.AwaitingPayment, (await db.Orders.SingleAsync()).Status);

        var retry = await service.CheckoutAsync(request, CancellationToken.None);

        Assert.Equal(2, square.CreateCommands.Count);
        Assert.Equal(square.CreateCommands[0].IdempotencyKey, square.CreateCommands[1].IdempotencyKey);
        Assert.Equal(1, product.Variants.Single().ReservedQuantity);
        Assert.Equal("https://square.test/link", retry.CheckoutUrl);
    }

    [Fact]
    public async Task ExpiredCheckout_ReleasesStockOnlyAfterSquareLinkDeletionSucceeds()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var square = new FakeSquareClient
        {
            DeleteFailuresRemaining = 1,
            OrderPaymentIds = []
        };
        var clock = new TestClock(new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero));
        var service = Service(db, square, new RecordingEmailSender(), clock);

        await service.CheckoutAsync(Request(product), CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(31);

        Assert.Equal(0, await service.RunMaintenanceAsync(CancellationToken.None));
        Assert.Equal(1, product.Variants.Single().ReservedQuantity);
        Assert.Equal(StoreOrderStatus.AwaitingPayment, (await db.Orders.SingleAsync()).Status);

        Assert.Equal(1, await service.RunMaintenanceAsync(CancellationToken.None));
        Assert.Equal(0, product.Variants.Single().ReservedQuantity);
        Assert.Equal(StoreOrderStatus.Canceled, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(2, square.DeleteCalls);
    }

    [Fact]
    public async Task ExpiredCheckout_WithNoLinkAndDeterministicFailure_ReleasesStock()
    {
        await using var db = Context();
        var product = ProductGraph();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var square = new FakeSquareClient { CreateFailuresRemaining = 1 };
        var clock = new TestClock(new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero));
        var service = Service(db, square, new RecordingEmailSender(), clock);

        await Assert.ThrowsAsync<SquareIntegrationException>(() =>
            service.CheckoutAsync(Request(product), CancellationToken.None));
        clock.UtcNow = clock.UtcNow.AddMinutes(31);
        square.CreateFailure = new SquareIntegrationException("BAD_REQUEST", 400, "order.line_items");

        Assert.Equal(1, await service.RunMaintenanceAsync(CancellationToken.None));
        var order = await db.Orders.SingleAsync();
        Assert.Equal(StoreOrderStatus.Canceled, order.Status);
        Assert.Equal(PaymentStatus.Failed, order.PaymentStatus);
        Assert.Equal(0, (await db.ProductVariants.AsNoTracking().SingleAsync()).ReservedQuantity);
    }

    private static PublicStoreCheckoutRequestDto CopyRequest(PublicStoreCheckoutRequestDto source, string phone) => new()
    {
        CheckoutAttemptId = source.CheckoutAttemptId,
        CustomerName = source.CustomerName,
        CustomerEmail = source.CustomerEmail,
        CustomerPhone = phone,
        AthleteTeamNote = source.AthleteTeamNote,
        ConfirmsAdultBuyer = source.ConfirmsAdultBuyer,
        AcceptsStorePolicy = source.AcceptsStorePolicy,
        Lines = source.Lines
    };

    private static PublicStoreCheckoutRequestDto Request(Product product) => new()
    {
        CheckoutAttemptId = Guid.NewGuid().ToString(),
        CustomerName = "Adult Buyer",
        CustomerEmail = "buyer@example.com",
        CustomerPhone = "+14125550100",
        ConfirmsAdultBuyer = true,
        AcceptsStorePolicy = true,
        Lines = [new PublicStoreCheckoutLineDto
        {
            ProductVariantId = product.Variants.Single().Id,
            Quantity = 1,
            CustomInputs = [new PublicStoreCustomInputDto
            {
                ModifierGroupId = product.ModifierGroups.Single().Id,
                Value = "8"
            }]
        }]
    };

    private static Product ProductGraph()
    {
        var product = new Product
        {
            Name = "Team tee",
            Slug = "team-tee",
            Status = StoreProductStatus.Published,
            BasePriceMinor = 2500,
            Currency = "USD",
            AllowsSpecialRequests = true
        };
        product.Variants.Add(new ProductVariant
        {
            Product = product,
            Name = "Medium / Red",
            Sku = "TEE-M-RED",
            OnHandQuantity = 3,
            IsActive = true
        });
        product.ModifierGroups.Add(new ProductModifierGroup
        {
            Product = product,
            Name = "Number",
            Type = ProductModifierType.Number,
            IsRequired = true,
            MinimumSelections = 1,
            MaximumSelections = 1,
            IsActive = true
        });
        return product;
    }

    private static StoreOrderService Service(
        El1teDbContext db,
        ISquareClient square,
        ITransactionalEmailSender email,
        IClock clock) => new(
        db,
        square,
        email,
        clock,
        new StoreSettings
        {
            Enabled = true,
            CheckoutEnabled = true,
            Currency = "USD",
            ReservationMinutes = 30,
            PublicSiteUrl = "https://example.test"
        },
        new SquareSettings
        {
            AccessToken = "test",
            LocationId = "location",
            CheckoutReturnUrl = "https://example.test/shop/order-confirmation"
        },
        NullLogger<StoreOrderService>.Instance);

    private static El1teDbContext Context() => new(
        new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"store-orders-{Guid.NewGuid():N}")
            .Options);

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed class RecordingEmailSender : ITransactionalEmailSender
    {
        public TransactionalEmail? Last { get; private set; }
        public Task<TransactionalEmailSendResult> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
        {
            Last = message;
            return Task.FromResult(new TransactionalEmailSendResult("provider-message-1"));
        }
    }

    private sealed class FakeSquareClient : ISquareClient
    {
        public int CreateFailuresRemaining { get; set; }
        public int DeleteFailuresRemaining { get; set; }
        public int DeleteCalls { get; private set; }
        public SquareIntegrationException? CreateFailure { get; set; }
        public IReadOnlyList<string> OrderPaymentIds { get; set; } = ["payment-1"];
        public List<SquarePaymentLinkCommand> CreateCommands { get; } = [];
        public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<SquareCatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SquarePaymentLinkResult> CreatePaymentLinkAsync(SquarePaymentLinkCommand command, CancellationToken cancellationToken)
        {
            CreateCommands.Add(command);
            if (CreateFailure is not null)
            {
                var failure = CreateFailure;
                CreateFailure = null;
                throw failure;
            }
            if (CreateFailuresRemaining-- > 0) throw new SquareIntegrationException("SQUARE_TIMEOUT");
            return Task.FromResult(new SquarePaymentLinkResult("link-1", "square-order-1", "https://square.test/link", 200, 2700));
        }
        public Task<SquarePaymentResult> RetrievePaymentAsync(string paymentId, CancellationToken cancellationToken) =>
            Task.FromResult(new SquarePaymentResult(paymentId, "COMPLETED", "square-order-1", 2700, "USD"));
        public Task<SquareOrderResult> RetrieveOrderAsync(string orderId, CancellationToken cancellationToken) =>
            Task.FromResult(new SquareOrderResult(orderId, "COMPLETED", 2700, "USD", OrderPaymentIds));
        public Task<SquarePaymentLinkDeleteResult> DeletePaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            if (DeleteFailuresRemaining-- > 0) throw new SquareIntegrationException("SQUARE_TIMEOUT");
            return Task.FromResult(new SquarePaymentLinkDeleteResult(paymentLinkId, "square-order-1"));
        }
        public Task<SquareRefundResult> RefundPaymentAsync(SquareRefundCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new SquareRefundResult("refund-1", "COMPLETED"));
        public Task<SquareRefundStatusResult> RetrieveRefundAsync(string refundId, CancellationToken cancellationToken) =>
            Task.FromResult(new SquareRefundStatusResult(refundId, "COMPLETED"));
    }
}
