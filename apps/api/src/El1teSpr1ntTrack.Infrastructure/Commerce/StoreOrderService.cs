using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class StoreOrderService(
    El1teDbContext dbContext,
    ISquareClient squareClient,
    ITransactionalEmailSender emailSender,
    IClock clock,
    StoreSettings settings,
    SquareSettings squareSettings,
    ILogger<StoreOrderService> logger) : IStoreOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PublicStoreCheckoutResultDto> CheckoutAsync(
        PublicStoreCheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureCheckoutEnabled();
        var normalized = NormalizeCheckout(request);
        ValidateCheckout(normalized);
        var payloadHash = Hash(JsonSerializer.Serialize(normalized, JsonOptions));

        var existing = await dbContext.Orders
            .Include(value => value.OrderItems)
            .Include(value => value.Reservations)
            .SingleOrDefaultAsync(value => value.CheckoutAttemptId == normalized.CheckoutAttemptId, cancellationToken);
        if (existing is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(existing.CheckoutPayloadHash),
                    Convert.FromHexString(payloadHash)))
            {
                throw new CmsConflictException("That checkout attempt was already used for different order details.");
            }

            if (existing.Status != StoreOrderStatus.AwaitingPayment || existing.PaymentStatus != PaymentStatus.Pending)
            {
                throw new CmsConflictException("That checkout attempt can no longer be retried. Start checkout again from your cart.");
            }

            var retryToken = Token();
            existing.CheckoutReturnTokenHash = Hash(retryToken);
            existing.CheckoutReturnTokenExpiresAtUtc = clock.UtcNow.AddHours(1);
            existing.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(existing.SquarePaymentLinkUrl)
                ? await PrepareSquareCheckoutAsync(existing, null, retryToken, cancellationToken)
                : CheckoutResult(existing, retryToken);
        }

        var resolved = await ResolveCheckoutAsync(normalized, cancellationToken);
        var now = clock.UtcNow;
        var returnToken = Token();
        var order = new Order
        {
            PublicNumber = await UniqueOrderNumberAsync(cancellationToken),
            CheckoutAttemptId = normalized.CheckoutAttemptId,
            CheckoutPayloadHash = payloadHash,
            CheckoutReturnTokenHash = Hash(returnToken),
            CheckoutReturnTokenExpiresAtUtc = now.AddHours(1),
            CustomerName = normalized.CustomerName,
            CustomerEmail = normalized.CustomerEmail,
            CustomerPhone = normalized.CustomerPhone,
            AthleteTeamNote = normalized.AthleteTeamNote,
            Status = StoreOrderStatus.AwaitingPayment,
            PaymentProvider = PaymentProvider.Square,
            PaymentStatus = PaymentStatus.Pending,
            Currency = settings.Currency,
            SubtotalMinor = resolved.Sum(value => value.LineTotalMinor),
            TaxMinor = 0,
            TotalMinor = resolved.Sum(value => value.LineTotalMinor),
            HasUnusualRequest = resolved.Any(value => value.HasPersonalization),
            CreatedAt = now
        };
        foreach (var line in resolved)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = line.ProductId,
                ProductVariantId = line.VariantId,
                ProductName = line.ProductName,
                VariantName = line.VariantName,
                Sku = line.Sku,
                ConfigurationJson = JsonSerializer.Serialize(line.Configuration, JsonOptions),
                Quantity = line.Quantity,
                UnitPriceMinor = line.UnitPriceMinor,
                ModifierTotalMinor = line.ModifierTotalMinor,
                LineTotalMinor = line.LineTotalMinor,
                CreatedAt = now
            });
        }

        var reservation = new InventoryReservation
        {
            Order = order,
            ExpiresAtUtc = now.AddMinutes(Math.Clamp(settings.ReservationMinutes, 5, 120)),
            CreatedAt = now
        };
        foreach (var group in resolved.GroupBy(value => value.VariantId))
        {
            reservation.Items.Add(new InventoryReservationItem
            {
                ProductVariantId = group.Key,
                Quantity = group.Sum(value => value.Quantity),
                CreatedAt = now
            });
        }
        order.Reservations.Add(reservation);

        await using (var transaction = await BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            var requested = reservation.Items.ToDictionary(value => value.ProductVariantId, value => value.Quantity);
            var variants = await dbContext.ProductVariants
                .Where(value => requested.Keys.Contains(value.Id))
                .ToListAsync(cancellationToken);
            foreach (var variant in variants)
            {
                var quantity = requested[variant.Id];
                if (!variant.IsActive || variant.OnHandQuantity - variant.ReservedQuantity < quantity)
                    throw new CmsConflictException("One or more items no longer have enough stock. Refresh your cart.");
                variant.ReservedQuantity += quantity;
                variant.UpdatedAt = now;
            }
            dbContext.Orders.Add(order);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new CmsConflictException("One or more items changed while checkout was starting. Refresh your cart.");
            }
            catch (DbUpdateException)
            {
                throw new CmsConflictException("This checkout attempt or inventory selection is already being processed.");
            }
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }

        return await PrepareSquareCheckoutAsync(order, resolved, returnToken, cancellationToken);
    }

    public async Task<PublicStoreOrderStatusDto> GetPublicStatusAsync(
        string token,
        CancellationToken cancellationToken) =>
        MapPublic(await OrderByTrackingTokenAsync(token, cancellationToken), clock.UtcNow);

    public async Task<PublicStoreOrderStatusDto> CancelPublicOrderAsync(
        string token,
        CancellationToken cancellationToken)
    {
        EnsureCheckoutEnabled();
        var order = await OrderByTrackingTokenAsync(token, cancellationToken);
        var now = clock.UtcNow;
        if (order.CustomerCancellationExpiresAtUtc is null ||
            order.CustomerCancellationExpiresAtUtc <= now ||
            order.CustomerCancellationRequestedAtUtc is not null ||
            order.PaymentStatus != PaymentStatus.Paid ||
            order.Status is not (StoreOrderStatus.Paid or StoreOrderStatus.NeedsReview))
        {
            throw new CmsConflictException("This order is no longer eligible for automatic cancellation.");
        }

        await using var transaction = await BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        order.CustomerCancellationRequestedAtUtc = now;
        ChangeStatus(order, StoreOrderStatus.Canceled, null, "Canceled by the customer during the 30-minute hold.", now);
        order.PaymentStatus = PaymentStatus.Refunding;
        RestoreCommittedInventory(order, now, "Customer cancellation during production hold.");
        var refund = CreateRefund(
            order,
            order.TotalMinor,
            "Customer cancellation during the 30-minute hold.",
            null,
            order.OrderItems.Select(value => new RefundLine(value, value.Quantity, 0)),
            now);
        QueueOutbox("StoreRefundRequested", new { refundId = refund.Id }, now);
        QueueEmail(order, "OrderCancellation", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return MapPublic(order, now);
    }

    public async Task<PublicCheckoutReturnStatusDto?> GetReturnStatusAsync(
        string returnToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(returnToken)) return null;
        var hash = Hash(returnToken.Trim());
        var order = await dbContext.Orders.AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.CheckoutReturnTokenHash == hash &&
                value.CheckoutReturnTokenExpiresAtUtc > clock.UtcNow,
                cancellationToken);
        if (order is null) return null;
        var final = order.PaymentStatus is PaymentStatus.Paid or PaymentStatus.Refunded or PaymentStatus.Canceled or PaymentStatus.Failed;
        return new PublicCheckoutReturnStatusDto(
            order.PublicNumber,
            order.PaymentStatus,
            order.Status,
            final,
            order.PaymentStatus == PaymentStatus.Paid
                ? "Payment confirmed. Check your email for the secure order-status link."
                : order.PaymentStatus == PaymentStatus.Failed
                    ? "Square did not complete the payment. Your card was not accepted for this order."
                    : "Square is still confirming the payment. This page will update automatically.");
    }

    public async Task<AdminStoreOperationsDashboardDto> GetOperationsDashboardAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        return new AdminStoreOperationsDashboardDto(
            await dbContext.Orders.CountAsync(value => value.Status == StoreOrderStatus.AwaitingPayment, cancellationToken),
            await dbContext.Orders.CountAsync(value =>
                value.CustomerCancellationExpiresAtUtc > now &&
                value.CustomerCancellationRequestedAtUtc == null &&
                value.PaymentStatus == PaymentStatus.Paid, cancellationToken),
            await dbContext.Orders.CountAsync(value => value.Status == StoreOrderStatus.NeedsReview, cancellationToken),
            await dbContext.Orders.CountAsync(value => value.Status == StoreOrderStatus.InProduction, cancellationToken),
            await dbContext.Orders.CountAsync(value => value.Status == StoreOrderStatus.ReadyForHandoff, cancellationToken),
            await dbContext.CommerceRefunds.CountAsync(value => value.Status == CommerceRefundStatus.Failed, cancellationToken),
            await dbContext.CommerceEmailMessages.CountAsync(value => value.Status == CommerceEmailStatus.Failed, cancellationToken));
    }

    public async Task<PagedResultDto<AdminStoreOrderSummaryDto>> GetOrdersAsync(
        AdminStoreOrderOptions options,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, options.Page);
        var pageSize = Math.Clamp(options.PageSize, 1, 100);
        var query = dbContext.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(value =>
                value.PublicNumber.Contains(search) ||
                value.CustomerName.Contains(search) ||
                value.CustomerEmail.Contains(search));
        }
        if (options.Status.HasValue) query = query.Where(value => value.Status == options.Status.Value);
        if (options.PaymentStatus.HasValue) query = query.Where(value => value.PaymentStatus == options.PaymentStatus.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(value => value.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(value => new AdminStoreOrderSummaryDto(
                value.Id, value.PublicNumber, value.CustomerName, value.CustomerEmail,
                value.Status, value.PaymentStatus, value.TotalMinor, value.Currency,
                value.HasUnusualRequest, value.CustomerCancellationExpiresAtUtc, value.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<AdminStoreOrderSummaryDto>(items, page, pageSize, count);
    }

    public async Task<AdminStoreOrderDto> GetOrderAsync(Guid id, CancellationToken cancellationToken) =>
        MapAdmin(await OrderGraphAsync(id, cancellationToken));

    public async Task<AdminStoreOrderDto> TransitionAsync(
        Guid id,
        AdminStoreOrderTransitionDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var order = await OrderGraphAsync(id, cancellationToken);
        var now = clock.UtcNow;
        if (!AllowedTransitions(order.Status).Contains(request.Status))
            throw new CmsConflictException($"An order cannot move from {order.Status} to {request.Status}.");
        if (order.CustomerCancellationExpiresAtUtc > now &&
            request.Status is StoreOrderStatus.ReadyForProduction or StoreOrderStatus.InProduction)
            throw new CmsConflictException("The customer cancellation hold must expire before production begins.");
        ChangeStatus(order, request.Status, actorUserId, Clean(request.Note), now);
        if (request.Status == StoreOrderStatus.Completed)
            order.TrackingExpiresAtUtc = now.AddDays(90);
        if (request.Status == StoreOrderStatus.NeedsCustomerInfo) QueueEmail(order, "CustomerInformationRequired", now);
        if (request.Status == StoreOrderStatus.ReadyForHandoff) QueueEmail(order, "ReadyForHandoff", now);
        if (request.Status == StoreOrderStatus.Completed) QueueEmail(order, "OrderCompleted", now);
        AddActivity(actorUserId, "store.order.transitioned", order, $"Moved order to {request.Status}.", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAdmin(order);
    }

    public async Task<AdminStoreOrderDto> AddNoteAsync(
        Guid id,
        AdminStoreOrderNoteWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var order = await OrderGraphAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Note))
            throw Validation("note", "Enter a note.");
        var now = clock.UtcNow;
        order.InternalNotes.Add(new OrderInternalNote
        {
            ActorUserId = actorUserId,
            Note = request.Note.Trim(),
            CreatedAt = now
        });
        AddActivity(actorUserId, "store.order.note-added", order, "Added an internal order note.", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAdmin(order);
    }

    public async Task<AdminStoreRefundDto> RefundAsync(
        Guid id,
        AdminStoreRefundWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        EnsureCheckoutEnabled();
        var order = await OrderGraphAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(order.SquarePaymentId) ||
            order.PaymentStatus is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            throw new CmsConflictException("Only a completed Square payment can be refunded.");
        if (order.Refunds.Count >= 20)
            throw new CmsConflictException("Square's refund limit has been reached for this payment.");
        var alreadyRefunded = order.Refunds
            .Where(value => value.Status != CommerceRefundStatus.Failed)
            .Sum(value => value.AmountMinor);
        if (request.AmountMinor <= 0 || request.AmountMinor > order.TotalMinor - alreadyRefunded)
            throw Validation("amountMinor", "The refund amount exceeds the remaining paid amount.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw Validation("reason", "Enter a refund reason.");

        if (request.Lines.Count == 0)
            throw Validation("lines", "Choose at least one order line for the refund.");
        var lines = new List<RefundLine>();
        foreach (var input in request.Lines)
        {
            var item = order.OrderItems.SingleOrDefault(value => value.Id == input.OrderItemId)
                ?? throw Validation("lines", "A refund line does not belong to this order.");
            if (input.Quantity <= 0 || input.Quantity > item.Quantity ||
                input.RestockQuantity < 0 || input.RestockQuantity > input.Quantity)
                throw Validation("lines", "Refund and restock quantities are invalid.");
            var quantityAlreadyRequested = order.Refunds
                .Where(value => value.Status != CommerceRefundStatus.Failed)
                .SelectMany(value => value.Lines)
                .Where(value => value.OrderItemId == item.Id)
                .Sum(value => value.Quantity);
            if (input.Quantity > item.Quantity - quantityAlreadyRequested)
                throw Validation("lines", "A refund line exceeds the remaining item quantity.");
            lines.Add(new RefundLine(item, input.Quantity, input.RestockQuantity));
        }
        if (lines.Select(value => value.Item.Id).Distinct().Count() != lines.Count)
            throw Validation("lines", "Each order item may appear only once.");

        var now = clock.UtcNow;
        var refund = CreateRefund(order, request.AmountMinor, request.Reason.Trim(), actorUserId, lines, now);
        QueueOutbox("StoreRefundRequested", new { refundId = refund.Id }, now);
        AddActivity(actorUserId, "store.order.refund-requested", order, "Requested a Square refund.", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapRefund(refund);
    }

    public async Task RetryRefundAsync(
        Guid id,
        Guid refundId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        EnsureCheckoutEnabled();
        var refund = await dbContext.CommerceRefunds
            .Include(value => value.Order)
            .SingleOrDefaultAsync(value => value.Id == refundId && value.OrderId == id, cancellationToken)
            ?? throw new CmsNotFoundException("Store refund", refundId);
        if (refund.Status != CommerceRefundStatus.Failed)
            throw new CmsConflictException("Only a failed refund can be retried.");
        var now = clock.UtcNow;
        refund.Status = CommerceRefundStatus.Pending;
        refund.SafeFailureCode = null;
        refund.UpdatedAt = now;
        refund.Order.PaymentStatus = PaymentStatus.Refunding;
        QueueOutbox("StoreRefundRequested", new { refundId = refund.Id }, now);
        AddActivity(actorUserId, "store.order.refund-retried", refund.Order, "Retried a failed Square refund.", now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminTrackingLinkResultDto> RotateTrackingLinkAsync(
        Guid id,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var order = await OrderGraphAsync(id, cancellationToken);
        var token = Token();
        order.TrackingTokenHash = Hash(token);
        order.TrackingExpiresAtUtc = order.Status == StoreOrderStatus.Completed
            ? clock.UtcNow.AddDays(90)
            : null;
        order.UpdatedAt = clock.UtcNow;
        AddActivity(actorUserId, "store.order.tracking-rotated", order, "Rotated the secure order-status link.", clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdminTrackingLinkResultDto(BuildTrackingUrl(token));
    }

    public async Task RetryEmailAsync(
        Guid id,
        Guid emailId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        EnsureCheckoutEnabled();
        var email = await dbContext.CommerceEmailMessages
            .SingleOrDefaultAsync(value => value.Id == emailId && value.OrderId == id, cancellationToken)
            ?? throw new CmsNotFoundException("Order email", emailId);
        if (email.Status != CommerceEmailStatus.Failed)
            throw new CmsConflictException("Only a failed email can be retried.");
        email.Status = CommerceEmailStatus.Pending;
        email.SafeFailureCode = null;
        email.UpdatedAt = clock.UtcNow;
        QueueOutbox("StoreEmailRequested", new { emailId = email.Id }, clock.UtcNow);
        var order = await dbContext.Orders.SingleAsync(value => value.Id == id, cancellationToken);
        AddActivity(actorUserId, "store.order.email-retried", order, "Retried a failed order email.", clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminCommerceIntegrationHealthDto> GetIntegrationHealthAsync(CancellationToken cancellationToken)
    {
        var configured = !string.IsNullOrWhiteSpace(squareSettings.AccessToken) &&
                         !string.IsNullOrWhiteSpace(squareSettings.LocationId);
        var reachable = false;
        if (configured)
        {
            try { reachable = await squareClient.CheckConnectionAsync(cancellationToken); }
            catch { reachable = false; }
        }
        return new AdminCommerceIntegrationHealthDto(
            settings.CommerceOperationsEnabled,
            configured,
            reachable,
            await dbContext.CommerceOutboxMessages.CountAsync(value => value.ProcessedAtUtc == null, cancellationToken),
            await dbContext.CommerceRefunds.CountAsync(value => value.Status == CommerceRefundStatus.Failed, cancellationToken),
            await dbContext.CommerceEmailMessages.CountAsync(value => value.Status == CommerceEmailStatus.Failed, cancellationToken));
    }

    public async Task ProcessSquareWebhookAsync(Guid webhookEventId, CancellationToken cancellationToken)
    {
        var webhook = await dbContext.SquareWebhookEvents
            .SingleAsync(value => value.Id == webhookEventId, cancellationToken);
        if (webhook.ProcessedAtUtc is not null) return;
        if (webhook.EventType is "payment.created" or "payment.updated" && !string.IsNullOrWhiteSpace(webhook.ObjectId))
        {
            var payment = await squareClient.RetrievePaymentAsync(webhook.ObjectId, cancellationToken);
            await ApplyPaymentAsync(payment, cancellationToken);
        }
        else if (webhook.EventType is "refund.created" or "refund.updated" && !string.IsNullOrWhiteSpace(webhook.ObjectId))
        {
            var status = await squareClient.RetrieveRefundAsync(webhook.ObjectId, cancellationToken);
            await ApplyRefundStatusAsync(status, cancellationToken);
        }
        webhook.ProcessedAtUtc = clock.UtcNow;
        webhook.UpdatedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessRefundAsync(Guid refundId, CancellationToken cancellationToken)
    {
        var refund = await dbContext.CommerceRefunds
            .Include(value => value.Order)
            .Include(value => value.Lines).ThenInclude(value => value.OrderItem)
            .SingleAsync(value => value.Id == refundId, cancellationToken);
        if (refund.Status == CommerceRefundStatus.Completed) return;
        try
        {
            SquareRefundStatusResult status;
            if (string.IsNullOrWhiteSpace(refund.SquareRefundId))
            {
                var result = await squareClient.RefundPaymentAsync(new SquareRefundCommand(
                    refund.Id.ToString("N"),
                    refund.Order.SquarePaymentId!,
                    refund.AmountMinor,
                    refund.Currency,
                    refund.Reason), cancellationToken);
                refund.SquareRefundId = result.RefundId;
                refund.UpdatedAt = clock.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                status = new SquareRefundStatusResult(result.RefundId, result.Status);
            }
            else
            {
                status = await squareClient.RetrieveRefundAsync(refund.SquareRefundId, cancellationToken);
            }
            await ApplyRefundStatusAsync(status, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            refund.Status = CommerceRefundStatus.Failed;
            refund.SafeFailureCode = exception is SquareIntegrationException square
                ? square.SafeCode
                : exception.GetType().Name;
            refund.Order.PaymentStatus = PaymentStatus.Refunding;
            refund.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task SendOrderEmailAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var email = await dbContext.CommerceEmailMessages
            .Include(value => value.Order)
            .SingleAsync(value => value.Id == emailId, cancellationToken);
        if (email.Status == CommerceEmailStatus.Sent) return;
        string? trackingUrl = null;
        if (email.TemplateName == "PaymentConfirmation")
        {
            var token = Token();
            email.Order.TrackingTokenHash = Hash(token);
            email.Order.TrackingExpiresAtUtc = null;
            trackingUrl = BuildTrackingUrl(token);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        var message = ComposeEmail(email, trackingUrl);
        try
        {
            var sendResult = await emailSender.SendAsync(message, cancellationToken);
            email.Status = CommerceEmailStatus.Sent;
            email.ProviderMessageId = sendResult.ProviderMessageId;
            email.SentAtUtc = clock.UtcNow;
            email.SafeFailureCode = null;
            email.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            email.Status = CommerceEmailStatus.Failed;
            email.SafeFailureCode = exception.GetType().Name;
            email.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        if (!settings.CommerceOperationsEnabled) return 0;
        var now = clock.UtcNow;
        var processed = 0;
        var expired = await dbContext.Orders
            .Include(value => value.OrderItems)
            .Include(value => value.Reservations).ThenInclude(value => value.Items)
                .ThenInclude(value => value.ProductVariant)
            .Where(value => value.Status == StoreOrderStatus.AwaitingPayment &&
                value.Reservations.Any(reservation =>
                    reservation.ReleasedAtUtc == null &&
                    reservation.CommittedAtUtc == null &&
                    reservation.ExpiresAtUtc <= now))
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var order in expired)
        {
            if (string.IsNullOrWhiteSpace(order.SquarePaymentLinkId) || string.IsNullOrWhiteSpace(order.SquareOrderId))
            {
                try
                {
                    await PrepareSquareCheckoutAsync(order, null, Token(), cancellationToken);
                }
                catch (CmsRequestValidationException)
                {
                    // Preparation releases deterministic invalid checkouts before surfacing validation.
                    processed++;
                    continue;
                }
                catch (SquareIntegrationException exception) when (exception.IsDeterministicClientFailure)
                {
                    // Preparation releases deterministic provider failures exactly once.
                    processed++;
                    continue;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(
                        "Expired checkout {OrderReference} could not be reconciled to a deletable Square link ({FailureType}).",
                        order.PublicNumber,
                        exception.GetType().Name);
                    continue;
                }
            }
            try
            {
                var squareOrder = await squareClient.RetrieveOrderAsync(order.SquareOrderId!, cancellationToken);
                var completedPayment = await FirstCompletedPaymentAsync(squareOrder, cancellationToken);
                if (completedPayment is not null)
                {
                    await ApplyPaymentAsync(completedPayment, cancellationToken);
                    processed++;
                    continue;
                }
                await squareClient.DeletePaymentLinkAsync(order.SquarePaymentLinkId!, cancellationToken);
                ReleaseReservation(order, now);
                order.SquarePaymentLinkDeletedAtUtc = now;
                ChangeStatus(order, StoreOrderStatus.Canceled, null, "Checkout expired before payment.", now);
                order.PaymentStatus = PaymentStatus.Canceled;
                await dbContext.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    "Expired checkout reconciliation failed for order {OrderReference} with {FailureType}.",
                    order.PublicNumber,
                    exception.GetType().Name);
            }
        }

        var holds = await dbContext.Orders
            .Where(value => value.Status == StoreOrderStatus.Paid &&
                value.PaymentStatus == PaymentStatus.Paid &&
                value.CustomerCancellationExpiresAtUtc <= now &&
                value.CustomerCancellationRequestedAtUtc == null)
            .Take(50).ToListAsync(cancellationToken);
        foreach (var order in holds)
        {
            ChangeStatus(order, StoreOrderStatus.ReadyForProduction, null, "Customer cancellation hold expired.", now);
            processed++;
        }
        if (holds.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);

        var pendingOrders = await dbContext.Orders
            .Where(value => value.Status == StoreOrderStatus.AwaitingPayment && value.SquareOrderId != null)
            .OrderBy(value => value.CreatedAt).Take(20).ToListAsync(cancellationToken);
        foreach (var order in pendingOrders)
        {
            try
            {
                var squareOrder = await squareClient.RetrieveOrderAsync(order.SquareOrderId!, cancellationToken);
                var payment = await FirstCompletedPaymentAsync(squareOrder, cancellationToken);
                if (payment is null) continue;
                await ApplyPaymentAsync(payment, cancellationToken);
                processed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    "Square payment reconciliation failed for order {OrderReference} with {FailureType}.",
                    order.PublicNumber,
                    exception.GetType().Name);
            }
        }

        var pendingRefunds = await dbContext.CommerceRefunds.AsNoTracking()
            .Where(value => value.Status == CommerceRefundStatus.Pending && value.SquareRefundId != null)
            .Select(value => value.Id).Take(20).ToListAsync(cancellationToken);
        foreach (var refundId in pendingRefunds)
        {
            await ProcessRefundAsync(refundId, cancellationToken);
            processed++;
        }
        return processed;
    }

    private async Task ApplyPaymentAsync(SquarePaymentResult payment, CancellationToken cancellationToken)
    {
        if (!string.Equals(payment.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(payment.OrderId)) return;
        var order = await dbContext.Orders
            .Include(value => value.OrderItems)
            .Include(value => value.Reservations).ThenInclude(value => value.Items)
                .ThenInclude(value => value.ProductVariant)
            .SingleOrDefaultAsync(value => value.SquareOrderId == payment.OrderId, cancellationToken);
        if (order is null || order.PaymentStatus is not (
                PaymentStatus.Pending or
                PaymentStatus.Authorized or
                PaymentStatus.Failed)) return;
        if (!string.Equals(payment.Currency, order.Currency, StringComparison.OrdinalIgnoreCase) ||
            payment.AmountMinor != order.TotalMinor)
            throw new SquareIntegrationException("PAYMENT_VERIFICATION_MISMATCH");
        var now = clock.UtcNow;
        foreach (var reservation in order.Reservations.Where(value => value.CommittedAtUtc is null && value.ReleasedAtUtc is null))
        {
            foreach (var item in reservation.Items)
            {
                var variant = item.ProductVariant;
                if (variant.ReservedQuantity < item.Quantity || variant.OnHandQuantity < item.Quantity)
                    throw new CmsConflictException("Reserved inventory could not be committed for the paid order.");
                variant.ReservedQuantity -= item.Quantity;
                variant.OnHandQuantity -= item.Quantity;
                variant.UpdatedAt = now;
                dbContext.InventoryAdjustments.Add(new InventoryAdjustment
                {
                    ProductVariantId = variant.Id,
                    OrderId = order.Id,
                    Reason = InventoryAdjustmentReason.Sale,
                    QuantityDelta = -item.Quantity,
                    ResultingOnHandQuantity = variant.OnHandQuantity,
                    Note = "Committed after verified Square payment.",
                    CreatedAt = now
                });
            }
            reservation.CommittedAtUtc = now;
            reservation.UpdatedAt = now;
        }
        order.SquarePaymentId = payment.PaymentId;
        order.PaymentStatus = PaymentStatus.Paid;
        order.PaymentVerifiedAtUtc = now;
        order.CustomerCancellationExpiresAtUtc = now.AddMinutes(30);
        ChangeStatus(
            order,
            order.HasUnusualRequest ? StoreOrderStatus.NeedsReview : StoreOrderStatus.Paid,
            null,
            "Square payment verified.",
            now);
        QueueEmail(order, "PaymentConfirmation", now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyRefundStatusAsync(
        SquareRefundStatusResult status,
        CancellationToken cancellationToken)
    {
        var refund = await dbContext.CommerceRefunds
            .Include(value => value.Order).ThenInclude(value => value.OrderItems)
            .Include(value => value.Lines).ThenInclude(value => value.OrderItem)
            .SingleOrDefaultAsync(value => value.SquareRefundId == status.RefundId, cancellationToken);
        if (refund is null || refund.Status == CommerceRefundStatus.Completed) return;
        if (string.Equals(status.Status, "FAILED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status.Status, "REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            refund.Status = CommerceRefundStatus.Failed;
            refund.SafeFailureCode = "SQUARE_REFUND_FAILED";
            refund.Order.PaymentStatus = PaymentStatus.Refunding;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }
        if (!string.Equals(status.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)) return;
        var now = clock.UtcNow;
        refund.Status = CommerceRefundStatus.Completed;
        refund.SafeFailureCode = null;
        refund.UpdatedAt = now;
        foreach (var line in refund.Lines.Where(value => value.RestockQuantity > 0 && value.InventoryAdjustmentId == null))
        {
            var variantId = line.OrderItem.ProductVariantId;
            if (variantId is null) continue;
            var variant = await dbContext.ProductVariants.SingleAsync(value => value.Id == variantId, cancellationToken);
            variant.OnHandQuantity += line.RestockQuantity;
            variant.UpdatedAt = now;
            var adjustment = new InventoryAdjustment
            {
                ProductVariantId = variant.Id,
                OrderId = refund.OrderId,
                Reason = InventoryAdjustmentReason.ReturnRestock,
                QuantityDelta = line.RestockQuantity,
                ResultingOnHandQuantity = variant.OnHandQuantity,
                Note = "Restocked after completed Square refund.",
                CreatedAt = now
            };
            dbContext.InventoryAdjustments.Add(adjustment);
            line.InventoryAdjustmentId = adjustment.Id;
            line.UpdatedAt = now;
        }
        var completedTotal = await dbContext.CommerceRefunds
            .Where(value => value.OrderId == refund.OrderId &&
                (value.Status == CommerceRefundStatus.Completed || value.Id == refund.Id))
            .SumAsync(value => value.AmountMinor, cancellationToken);
        if (completedTotal >= refund.Order.TotalMinor)
        {
            refund.Order.PaymentStatus = PaymentStatus.Refunded;
            ChangeStatus(refund.Order, StoreOrderStatus.Refunded, refund.ActorUserId, "Square refund completed.", now);
        }
        else
        {
            refund.Order.PaymentStatus = PaymentStatus.PartiallyRefunded;
            refund.Order.UpdatedAt = now;
        }
        QueueEmail(refund.Order, "OrderRefund", now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SquarePaymentResult?> FirstCompletedPaymentAsync(
        SquareOrderResult order,
        CancellationToken cancellationToken)
    {
        foreach (var paymentId in order.PaymentIds)
        {
            var payment = await squareClient.RetrievePaymentAsync(paymentId, cancellationToken);
            if (string.Equals(payment.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)) return payment;
        }
        return null;
    }

    private async Task<IReadOnlyList<ResolvedCheckoutLine>> ResolveCheckoutAsync(
        PublicStoreCheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var variantIds = request.Lines.Select(value => value.ProductVariantId).Distinct().ToList();
        var variants = await dbContext.ProductVariants.AsNoTracking()
            .Include(value => value.Product).ThenInclude(value => value.ModifierGroups)
                .ThenInclude(value => value.Values)
            .Include(value => value.OptionValues).ThenInclude(value => value.ProductOptionValue)
                .ThenInclude(value => value.ProductOption)
            .Where(value => variantIds.Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, cancellationToken);
        if (variants.Count != variantIds.Count)
            throw Validation("lines", "A product configuration is no longer available.");

        var result = new List<ResolvedCheckoutLine>();
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var input = request.Lines[index];
            var variant = variants[input.ProductVariantId];
            var product = variant.Product;
            if (!variant.IsActive || product.Status != StoreProductStatus.Published)
                throw Validation($"lines[{index}]", "This product configuration is no longer available.");
            if (input.Quantity is < 1 or > 10)
                throw Validation($"lines[{index}].quantity", "Choose a quantity from 1 to 10.");

            var activeGroups = product.ModifierGroups.Where(value => value.IsActive).ToList();
            var activeValues = activeGroups.SelectMany(value => value.Values.Where(item => item.IsActive))
                .ToDictionary(value => value.Id);
            if (input.ModifierValueIds.Distinct().Count() != input.ModifierValueIds.Count ||
                input.ModifierValueIds.Any(value => !activeValues.ContainsKey(value)))
                throw Validation($"lines[{index}].modifierValueIds", "A selected option is invalid.");
            var selectedIds = input.ModifierValueIds.ToHashSet();
            var customInputs = input.CustomInputs.ToDictionary(value => value.ModifierGroupId, value => value.Value.Trim());
            if (customInputs.Count != input.CustomInputs.Count)
                throw Validation($"lines[{index}].customInputs", "Each personalization field may appear only once.");

            var configurations = variant.OptionValues
                .OrderBy(value => value.ProductOptionValue.ProductOption.DisplayOrder)
                .Select(value => new ConfigurationValue(
                    value.ProductOptionValue.ProductOption.Name,
                    value.ProductOptionValue.Name))
                .ToList();
            var squareModifiers = new List<SquareCheckoutModifier>();
            long modifierTotal = 0;
            var hasPersonalization = false;
            foreach (var group in activeGroups.OrderBy(value => value.DisplayOrder))
            {
                if (group.Type is ProductModifierType.ShortText or ProductModifierType.Number)
                {
                    customInputs.TryGetValue(group.Id, out var customValue);
                    if (group.IsRequired && string.IsNullOrWhiteSpace(customValue))
                        throw Validation($"lines[{index}].customInputs", $"{group.Name} is required.");
                    if (!string.IsNullOrWhiteSpace(customValue))
                    {
                        if (!product.AllowsSpecialRequests)
                            throw Validation($"lines[{index}].customInputs", "This product does not accept personalization.");
                        if (customValue.Length > 40 ||
                            group.Type == ProductModifierType.Number &&
                            (!int.TryParse(customValue, out var number) || number is < 0 or > 99))
                            throw Validation($"lines[{index}].customInputs", $"{group.Name} is invalid.");
                        configurations.Add(new ConfigurationValue(group.Name, customValue));
                        squareModifiers.Add(new SquareCheckoutModifier($"{group.Name}: {customValue}", 0));
                        hasPersonalization = true;
                    }
                    continue;
                }
                var values = group.Values.Where(value => selectedIds.Contains(value.Id)).ToList();
                if (values.Count < group.MinimumSelections || values.Count > group.MaximumSelections)
                    throw Validation($"lines[{index}].modifierValueIds", $"Choose a valid number of {group.Name} options.");
                foreach (var value in values)
                {
                    modifierTotal += value.PriceAdjustmentMinor;
                    configurations.Add(new ConfigurationValue(group.Name, value.Name));
                    squareModifiers.Add(new SquareCheckoutModifier($"{group.Name}: {value.Name}", value.PriceAdjustmentMinor));
                }
            }
            if (customInputs.Keys.Any(id => activeGroups.All(group => group.Id != id ||
                    group.Type is not (ProductModifierType.ShortText or ProductModifierType.Number))))
                throw Validation($"lines[{index}].customInputs", "A personalization field is invalid.");

            var unitPrice = variant.PriceOverrideMinor ?? product.BasePriceMinor;
            result.Add(new ResolvedCheckoutLine(
                product.Id, variant.Id, product.Name, variant.Name, variant.Sku,
                input.Quantity, unitPrice, modifierTotal,
                checked((unitPrice + modifierTotal) * input.Quantity),
                configurations, squareModifiers, hasPersonalization));
        }
        return result;
    }

    private async Task<Order> OrderByTrackingTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) throw Validation("token", "This order link is invalid or expired.");
        var hash = Hash(token.Trim());
        var order = await dbContext.Orders
            .Include(value => value.OrderItems)
            .Include(value => value.StatusHistory)
            .Include(value => value.Reservations).ThenInclude(value => value.Items)
                .ThenInclude(value => value.ProductVariant)
            .Include(value => value.Refunds).ThenInclude(value => value.Lines)
            .SingleOrDefaultAsync(value => value.TrackingTokenHash == hash, cancellationToken);
        if (order is null || order.TrackingExpiresAtUtc <= clock.UtcNow)
            throw Validation("token", "This order link is invalid or expired.");
        return order;
    }

    private async Task<Order> OrderGraphAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(value => value.OrderItems)
            .Include(value => value.StatusHistory)
            .Include(value => value.InternalNotes)
            .Include(value => value.EmailHistory)
            .Include(value => value.Refunds).ThenInclude(value => value.Lines)
            .Include(value => value.Reservations).ThenInclude(value => value.Items)
                .ThenInclude(value => value.ProductVariant)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
        ?? throw new CmsNotFoundException("Store order", id);

    private void RestoreCommittedInventory(Order order, DateTimeOffset now, string note)
    {
        foreach (var reservation in order.Reservations.Where(value => value.CommittedAtUtc is not null))
        {
            foreach (var item in reservation.Items)
            {
                item.ProductVariant.OnHandQuantity += item.Quantity;
                item.ProductVariant.UpdatedAt = now;
                dbContext.InventoryAdjustments.Add(new InventoryAdjustment
                {
                    ProductVariantId = item.ProductVariantId,
                    OrderId = order.Id,
                    Reason = InventoryAdjustmentReason.ReturnRestock,
                    QuantityDelta = item.Quantity,
                    ResultingOnHandQuantity = item.ProductVariant.OnHandQuantity,
                    Note = note,
                    CreatedAt = now
                });
            }
            reservation.UpdatedAt = now;
        }
    }

    private CommerceRefund CreateRefund(
        Order order,
        long amountMinor,
        string reason,
        Guid? actorUserId,
        IEnumerable<RefundLine> lines,
        DateTimeOffset now)
    {
        var refund = new CommerceRefund
        {
            Order = order,
            ActorUserId = actorUserId,
            AmountMinor = amountMinor,
            Currency = order.Currency,
            Status = CommerceRefundStatus.Pending,
            Reason = reason,
            CreatedAt = now
        };
        foreach (var line in lines)
            refund.Lines.Add(new CommerceRefundLine
            {
                OrderItemId = line.Item.Id,
                Quantity = line.Quantity,
                RestockQuantity = line.RestockQuantity,
                CreatedAt = now
            });
        order.Refunds.Add(refund);
        dbContext.CommerceRefunds.Add(refund);
        return refund;
    }

    private void ReleaseReservation(Order order, DateTimeOffset now)
    {
        foreach (var reservation in order.Reservations.Where(value => value.ReleasedAtUtc is null && value.CommittedAtUtc is null))
        {
            foreach (var item in reservation.Items)
            {
                item.ProductVariant.ReservedQuantity = Math.Max(0, item.ProductVariant.ReservedQuantity - item.Quantity);
                item.ProductVariant.UpdatedAt = now;
            }
            reservation.ReleasedAtUtc = now;
            reservation.UpdatedAt = now;
        }
    }

    private async Task<PublicStoreCheckoutResultDto> PrepareSquareCheckoutAsync(
        Order order,
        IReadOnlyList<ResolvedCheckoutLine>? resolved,
        string returnToken,
        CancellationToken cancellationToken)
    {
        var lines = resolved is not null
            ? resolved.Select(value => new SquareCheckoutLineItem(
                value.ProductName, value.VariantName, value.Quantity,
                value.UnitPriceMinor, value.SquareModifiers)).ToList()
            : order.OrderItems.Select(value => new SquareCheckoutLineItem(
                value.ProductName,
                value.VariantName,
                value.Quantity,
                value.UnitPriceMinor + value.ModifierTotalMinor,
                [])).ToList();
        if (!UsPhoneNumber.TryNormalize(order.CustomerPhone, out var normalizedPhone))
        {
            await ReleaseFailedCheckoutAsync(order.Id, cancellationToken);
            throw PhoneValidationException();
        }

        order.CustomerPhone = normalizedPhone;
        SquarePaymentLinkResult square;
        try
        {
            square = await squareClient.CreatePaymentLinkAsync(new SquarePaymentLinkCommand(
                order.Id.ToString("N"),
                order.PublicNumber,
                BuildReturnUrl(order.PublicNumber),
                order.Currency,
                order.CustomerEmail,
                normalizedPhone,
                lines), cancellationToken);
        }
        catch (SquareIntegrationException exception) when (exception.IsDeterministicClientFailure)
        {
            logger.LogWarning(
                "Square checkout was deterministically rejected. ProviderCode: {ProviderCode}; ProviderField: {ProviderField}; OperationId: {OperationId}.",
                exception.SafeCode,
                exception.SafeField,
                Activity.Current?.TraceId.ToString());
            await ReleaseFailedCheckoutAsync(order.Id, cancellationToken);
            if (string.Equals(exception.SafeCode, "INVALID_PHONE_NUMBER", StringComparison.Ordinal))
                throw PhoneValidationException();
            throw;
        }

        if (square.TotalMinor < order.SubtotalMinor ||
            square.TaxMinor != square.TotalMinor - order.SubtotalMinor)
        {
            try
            {
                await squareClient.DeletePaymentLinkAsync(square.PaymentLinkId, cancellationToken);
                await ReleaseFailedCheckoutAsync(order.Id, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    "Square checkout {OrderReference} returned an invalid total and could not be safely deleted ({FailureType}).",
                    order.PublicNumber,
                    exception.GetType().Name);
            }
            throw new SquareIntegrationException("ORDER_TOTAL_MISMATCH");
        }

        order.SquarePaymentLinkId = square.PaymentLinkId;
        order.SquarePaymentLinkUrl = square.Url;
        order.SquareOrderId = square.SquareOrderId;
        order.TaxMinor = square.TaxMinor;
        order.TotalMinor = square.TotalMinor;
        order.UpdatedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CheckoutResult(order, returnToken);
    }

    private async Task ReleaseFailedCheckoutAsync(Guid orderId, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var order = await dbContext.Orders
            .Include(value => value.Reservations).ThenInclude(value => value.Items)
                .ThenInclude(value => value.ProductVariant)
            .SingleAsync(value => value.Id == orderId, cancellationToken);
        ReleaseReservation(order, clock.UtcNow);
        ChangeStatus(order, StoreOrderStatus.Canceled, null, "Square checkout could not be created.", clock.UtcNow);
        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void QueueEmail(Order order, string template, DateTimeOffset now)
    {
        var email = new CommerceEmailMessage
        {
            Order = order,
            TemplateName = template,
            RecipientEmail = order.CustomerEmail,
            Status = CommerceEmailStatus.Pending,
            CreatedAt = now
        };
        order.EmailHistory.Add(email);
        dbContext.CommerceEmailMessages.Add(email);
        QueueOutbox("StoreEmailRequested", new { emailId = email.Id }, now);
    }

    private void QueueOutbox(string type, object payload, DateTimeOffset now) =>
        dbContext.CommerceOutboxMessages.Add(new CommerceOutboxMessage
        {
            MessageType = type,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            AvailableAtUtc = now,
            CreatedAt = now
        });

    private void ChangeStatus(
        Order order,
        StoreOrderStatus status,
        Guid? actorUserId,
        string? note,
        DateTimeOffset now)
    {
        if (order.Status == status) return;
        var history = new OrderStatusHistory
        {
            FromStatus = order.Status,
            ToStatus = status,
            ActorUserId = actorUserId,
            Note = note,
            CreatedAt = now
        };
        order.StatusHistory.Add(history);
        dbContext.OrderStatusHistory.Add(history);
        order.Status = status;
        order.UpdatedAt = now;
    }

    private static IReadOnlySet<StoreOrderStatus> AllowedTransitions(StoreOrderStatus status) => status switch
    {
        StoreOrderStatus.Paid => new HashSet<StoreOrderStatus> { StoreOrderStatus.ReadyForProduction, StoreOrderStatus.NeedsReview },
        StoreOrderStatus.NeedsReview => new HashSet<StoreOrderStatus> { StoreOrderStatus.ReadyForProduction, StoreOrderStatus.NeedsCustomerInfo },
        StoreOrderStatus.NeedsCustomerInfo => new HashSet<StoreOrderStatus> { StoreOrderStatus.NeedsReview, StoreOrderStatus.ReadyForProduction },
        StoreOrderStatus.ReadyForProduction => new HashSet<StoreOrderStatus> { StoreOrderStatus.InProduction, StoreOrderStatus.NeedsCustomerInfo },
        StoreOrderStatus.InProduction => new HashSet<StoreOrderStatus> { StoreOrderStatus.ReadyForHandoff, StoreOrderStatus.NeedsCustomerInfo },
        StoreOrderStatus.ReadyForHandoff => new HashSet<StoreOrderStatus> { StoreOrderStatus.Completed },
        _ => new HashSet<StoreOrderStatus>()
    };

    private void AddActivity(
        Guid actorUserId,
        string action,
        Order order,
        string summary,
        DateTimeOffset now) =>
        dbContext.AdminActivityLogs.Add(new AdminActivityLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = "StoreOrder",
            TargetId = order.Id,
            Summary = $"{summary} Reference {order.PublicNumber}.",
            CreatedAt = now
        });

    private TransactionalEmail ComposeEmail(CommerceEmailMessage email, string? trackingUrl)
    {
        var order = email.Order;
        var (subject, message) = email.TemplateName switch
        {
            "PaymentConfirmation" => (
                $"Payment received for {order.PublicNumber}",
                $"We received your payment. You may cancel from the secure status page until {order.CustomerCancellationExpiresAtUtc:MMMM d, yyyy 'at' h:mm tt 'UTC'}.\n\nTrack your order: {trackingUrl}"),
            "CustomerInformationRequired" => ($"Information needed for {order.PublicNumber}", "The gear team needs more information before production. Please reply to this email."),
            "ReadyForHandoff" => ($"Your order {order.PublicNumber} is ready", "Your order is ready for an arranged practice or event handoff."),
            "OrderCompleted" => ($"Order {order.PublicNumber} completed", "Your order has been marked completed. Thank you for supporting El1te."),
            "OrderCancellation" => ($"Order {order.PublicNumber} canceled", "Your order was canceled during the 30-minute hold. Your Square refund is being processed."),
            "OrderRefund" => ($"Refund update for {order.PublicNumber}", "A Square refund for your order has completed. Bank processing time may vary."),
            _ => ($"Update for order {order.PublicNumber}", "There is an update to your El1te merchandise order.")
        };
        var plain = $"Hello {order.CustomerName},\n\n{message}\n\nEl1te Spr1nt Athlet1cs";
        var html = $"<p>Hello {Encode(order.CustomerName)},</p><p>{Encode(message).Replace("\n", "<br>")}</p><p>El1te Spr1nt Athlet1cs</p>";
        return new TransactionalEmail(order.CustomerEmail, subject, plain, html);
    }

    private static PublicStoreCheckoutRequestDto NormalizeCheckout(PublicStoreCheckoutRequestDto request) => new()
    {
        CheckoutAttemptId = request.CheckoutAttemptId.Trim(),
        CustomerName = request.CustomerName.Trim(),
        CustomerEmail = request.CustomerEmail.Trim().ToLowerInvariant(),
        CustomerPhone = UsPhoneNumber.TryNormalize(request.CustomerPhone, out var normalizedPhone)
            ? normalizedPhone
            : request.CustomerPhone.Trim(),
        AthleteTeamNote = Clean(request.AthleteTeamNote),
        ConfirmsAdultBuyer = request.ConfirmsAdultBuyer,
        AcceptsStorePolicy = request.AcceptsStorePolicy,
        Lines = request.Lines.Select(value => new PublicStoreCheckoutLineDto
        {
            ProductVariantId = value.ProductVariantId,
            Quantity = value.Quantity,
            ModifierValueIds = value.ModifierValueIds.Order().ToList(),
            CustomInputs = value.CustomInputs.OrderBy(input => input.ModifierGroupId)
                .Select(input => new PublicStoreCustomInputDto
                {
                    ModifierGroupId = input.ModifierGroupId,
                    Value = input.Value.Trim()
                }).ToList()
        }).OrderBy(value => value.ProductVariantId).ThenBy(value =>
            string.Join(',', value.ModifierValueIds)).ToList()
    };

    private static void ValidateCheckout(PublicStoreCheckoutRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Guid.TryParse(request.CheckoutAttemptId, out _)) errors["checkoutAttemptId"] = ["Start checkout again from your cart."];
        if (request.CustomerName.Length is < 2 or > 200) errors["customerName"] = ["Enter the adult buyer's name."];
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.CustomerEmail)) errors["customerEmail"] = ["Enter a valid email address."];
        if (!UsPhoneNumber.TryNormalize(request.CustomerPhone, out _)) errors["customerPhone"] = ["Enter a valid U.S. phone number."];
        if (!request.ConfirmsAdultBuyer) errors["confirmsAdultBuyer"] = ["An adult buyer must confirm the order."];
        if (!request.AcceptsStorePolicy) errors["acceptsStorePolicy"] = ["Accept the Store Policy to continue."];
        if (request.Lines.Count is < 1 or > 50) errors["lines"] = ["Add at least one valid item to the cart."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private static CmsRequestValidationException PhoneValidationException() =>
        new(new Dictionary<string, string[]>
        {
            ["customerPhone"] = ["Enter a valid U.S. phone number."]
        });

    private void EnsureCheckoutEnabled()
    {
        if (!settings.CommerceOperationsEnabled)
            throw new CmsConflictException("Secure checkout is not currently enabled.");
    }

    private PublicStoreCheckoutResultDto CheckoutResult(Order order, string returnToken)
    {
        var expires = order.Reservations.Single(value => value.ReleasedAtUtc is null).ExpiresAtUtc;
        return new PublicStoreCheckoutResultDto(
            order.PublicNumber,
            order.SquarePaymentLinkUrl!,
            returnToken,
            expires,
            order.SubtotalMinor,
            order.TaxMinor,
            order.TotalMinor,
            order.Currency);
    }

    private static PublicStoreOrderStatusDto MapPublic(Order order, DateTimeOffset now)
    {
        var timeline = new List<PublicStoreOrderTimelineDto>
        {
            new(StoreOrderStatus.AwaitingPayment, "Checkout started", order.CreatedAt)
        };
        timeline.AddRange(order.StatusHistory.OrderBy(value => value.CreatedAt)
            .Select(value => new PublicStoreOrderTimelineDto(
                value.ToStatus,
                StatusLabel(value.ToStatus),
                value.CreatedAt)));
        return new PublicStoreOrderStatusDto(
            order.PublicNumber,
            order.Status,
            order.PaymentStatus,
            order.SubtotalMinor,
            order.TaxMinor,
            order.TotalMinor,
            order.Currency,
            order.HasUnusualRequest,
            order.CustomerCancellationExpiresAtUtc,
            order.CustomerCancellationExpiresAtUtc > now &&
                order.CustomerCancellationRequestedAtUtc is null &&
                order.PaymentStatus == PaymentStatus.Paid &&
                order.Status is StoreOrderStatus.Paid or StoreOrderStatus.NeedsReview,
            order.OrderItems.Select(MapPublicItem).ToList(),
            timeline);
    }

    private static PublicStoreOrderItemDto MapPublicItem(OrderItem item) => new(
        item.ProductName,
        item.VariantName,
        item.Quantity,
        item.UnitPriceMinor + item.ModifierTotalMinor,
        item.LineTotalMinor,
        ParseConfiguration(item.ConfigurationJson));

    private static AdminStoreOrderDto MapAdmin(Order order) => new(
        order.Id,
        order.PublicNumber,
        order.CustomerName,
        order.CustomerEmail,
        order.CustomerPhone,
        order.AthleteTeamNote,
        order.FulfillmentNote,
        order.Status,
        order.PaymentStatus,
        order.SubtotalMinor,
        order.TaxMinor,
        order.TotalMinor,
        order.Currency,
        order.HasUnusualRequest,
        order.CustomerCancellationExpiresAtUtc,
        order.SquareOrderId,
        order.SquarePaymentId,
        order.OrderItems.Select(value => new AdminStoreOrderItemDto(
            value.Id, value.ProductVariantId, value.ProductName, value.VariantName, value.Sku,
            value.Quantity, value.UnitPriceMinor + value.ModifierTotalMinor, value.LineTotalMinor,
            ParseConfiguration(value.ConfigurationJson))).ToList(),
        order.StatusHistory.OrderBy(value => value.CreatedAt).Select(value => new AdminStoreOrderTimelineDto(
            value.FromStatus, value.ToStatus, value.Note, value.CreatedAt)).ToList(),
        order.InternalNotes.OrderByDescending(value => value.CreatedAt).Select(value => new AdminStoreOrderNoteDto(
            value.Id, value.Note, value.CreatedAt)).ToList(),
        order.Refunds.OrderByDescending(value => value.CreatedAt).Select(MapRefund).ToList(),
        order.EmailHistory.OrderByDescending(value => value.CreatedAt).Select(value => new AdminStoreEmailDto(
            value.Id, value.TemplateName, value.Status, value.ProviderMessageId, value.SafeFailureCode, value.CreatedAt, value.SentAtUtc)).ToList(),
        order.CreatedAt,
        order.UpdatedAt);

    private static AdminStoreRefundDto MapRefund(CommerceRefund refund) => new(
        refund.Id, refund.AmountMinor, refund.Status, refund.Reason,
        refund.SafeFailureCode, refund.CreatedAt);

    private static IReadOnlyList<PublicStoreConfigurationValueDto> ParseConfiguration(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<List<ConfigurationValue>>(json, JsonOptions) ?? [])
                .Select(value => new PublicStoreConfigurationValueDto(value.Label, value.Value)).ToList();
        }
        catch (JsonException) { return []; }
    }

    private string BuildReturnUrl(string orderReference)
    {
        var separator = squareSettings.CheckoutReturnUrl!.Contains('?') ? '&' : '?';
        return $"{squareSettings.CheckoutReturnUrl}{separator}order={Uri.EscapeDataString(orderReference)}";
    }

    private string BuildTrackingUrl(string token) =>
        $"{settings.PublicSiteUrl.TrimEnd('/')}/shop/order-status#token={Uri.EscapeDataString(token)}";

    private async Task<string> UniqueOrderNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var number = $"ESA-ORD-{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            if (!await dbContext.Orders.AnyAsync(value => value.PublicNumber == number, cancellationToken)) return number;
        }
        throw new InvalidOperationException("Could not allocate a unique order reference.");
    }

    private Task<IDbContextTransaction?> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational()
            ? BeginRelationalTransactionAsync(isolationLevel, cancellationToken)
            : Task.FromResult<IDbContextTransaction?>(null);

    private async Task<IDbContextTransaction?> BeginRelationalTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken) =>
        await dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

    private static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
    private static CmsRequestValidationException Validation(string field, string message) => new(
        new Dictionary<string, string[]> { [field] = [message] });
    private static string StatusLabel(StoreOrderStatus status) => status switch
    {
        StoreOrderStatus.Paid => "Payment confirmed",
        StoreOrderStatus.NeedsReview => "Configuration review",
        StoreOrderStatus.ReadyForProduction => "Ready for production",
        StoreOrderStatus.InProduction => "In production",
        StoreOrderStatus.NeedsCustomerInfo => "Information needed",
        StoreOrderStatus.ReadyForHandoff => "Ready for handoff",
        StoreOrderStatus.Completed => "Completed",
        StoreOrderStatus.Canceled => "Canceled",
        StoreOrderStatus.Refunded => "Refunded",
        _ => "Awaiting payment"
    };

    private sealed record ConfigurationValue(string Label, string Value);
    private sealed record RefundLine(OrderItem Item, int Quantity, int RestockQuantity);
    private sealed record ResolvedCheckoutLine(
        Guid ProductId,
        Guid VariantId,
        string ProductName,
        string VariantName,
        string Sku,
        int Quantity,
        long UnitPriceMinor,
        long ModifierTotalMinor,
        long LineTotalMinor,
        IReadOnlyList<ConfigurationValue> Configuration,
        IReadOnlyList<SquareCheckoutModifier> SquareModifiers,
        bool HasPersonalization);
}
