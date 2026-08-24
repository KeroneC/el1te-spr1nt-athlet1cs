using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class SquareClient(HttpClient httpClient, SquareSettings settings) : ISquareClient
{
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = Request(HttpMethod.Get, $"v2/locations/{Uri.EscapeDataString(settings.LocationId!)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<SquarePaymentLinkResult> CreatePaymentLinkAsync(
        SquarePaymentLinkCommand command,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var body = new CreatePaymentLinkRequest(
            command.IdempotencyKey,
            new SquareOrder(
                settings.LocationId!,
                command.ReferenceId,
                command.Items.Select(item => new SquareLineItem(
                    item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    item.Name,
                    item.VariationName,
                    new SquareMoney(item.BasePriceMinor, command.Currency),
                    item.Modifiers.Select(modifier => new SquareModifier(
                        modifier.Name,
                        new SquareMoney(modifier.BasePriceMinor, command.Currency))).ToArray())).ToArray(),
                new SquarePricingOptions(true)),
            new SquareCheckoutOptions(command.RedirectUrl, false, false, false),
            new SquarePrePopulatedData(command.BuyerEmail, command.BuyerPhone));

        using var request = Request(HttpMethod.Post, "v2/online-checkout/payment-links");
        request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadPayloadAsync<CreatePaymentLinkResponse>(response, cancellationToken);
        var link = payload.PaymentLink ?? throw new SquareIntegrationException("MISSING_PAYMENT_LINK");
        if (string.IsNullOrWhiteSpace(link.Id) ||
            string.IsNullOrWhiteSpace(link.OrderId) ||
            string.IsNullOrWhiteSpace(link.Url))
        {
            throw new SquareIntegrationException("INCOMPLETE_PAYMENT_LINK");
        }

        var order = payload.RelatedResources?.Orders?
            .FirstOrDefault(value => string.Equals(value.Id, link.OrderId, StringComparison.Ordinal));
        var taxMinor = order?.TotalTaxMoney?.Amount ?? 0;
        var totalMinor = order?.TotalMoney?.Amount ?? 0;
        if (totalMinor <= 0)
        {
            throw new SquareIntegrationException("MISSING_ORDER_TOTAL");
        }

        return new SquarePaymentLinkResult(link.Id, link.OrderId, link.Url, taxMinor, totalMinor);
    }

    public async Task<SquarePaymentResult> RetrievePaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = Request(HttpMethod.Get, $"v2/payments/{Uri.EscapeDataString(paymentId)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadPayloadAsync<RetrievePaymentResponse>(response, cancellationToken);
        var payment = payload.Payment ?? throw new SquareIntegrationException("MISSING_PAYMENT");
        var amount = payment.AmountMoney ?? throw new SquareIntegrationException("MISSING_PAYMENT_AMOUNT");
        return new SquarePaymentResult(
            payment.Id ?? paymentId,
            payment.Status ?? "UNKNOWN",
            payment.OrderId,
            amount.Amount,
            amount.Currency);
    }

    public async Task<SquareOrderResult> RetrieveOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = Request(HttpMethod.Get, $"v2/orders/{Uri.EscapeDataString(orderId)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadPayloadAsync<RetrieveOrderResponse>(response, cancellationToken);
        var order = payload.Order ?? throw new SquareIntegrationException("MISSING_ORDER");
        var total = order.TotalMoney ?? throw new SquareIntegrationException("MISSING_ORDER_TOTAL");
        return new SquareOrderResult(
            order.Id ?? orderId,
            order.State ?? "UNKNOWN",
            total.Amount,
            total.Currency,
            (order.Tenders ?? []).Select(value => value.PaymentId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToList());
    }

    public async Task<SquareRefundResult> RefundPaymentAsync(
        SquareRefundCommand command,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var body = new RefundPaymentRequest(
            command.IdempotencyKey,
            command.PaymentId,
            new SquareMoney(command.AmountMinor, command.Currency),
            command.Reason);
        using var request = Request(HttpMethod.Post, "v2/refunds");
        request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadPayloadAsync<RefundPaymentResponse>(response, cancellationToken);
        var refund = payload.Refund ?? throw new SquareIntegrationException("MISSING_REFUND");
        return new SquareRefundResult(
            refund.Id ?? throw new SquareIntegrationException("MISSING_REFUND_ID"),
            refund.Status ?? "UNKNOWN");
    }

    public async Task<SquarePaymentLinkDeleteResult> DeletePaymentLinkAsync(
        string paymentLinkId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = Request(
            HttpMethod.Delete,
            $"v2/online-checkout/payment-links/{Uri.EscapeDataString(paymentLinkId)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadPayloadAsync<DeletePaymentLinkResponse>(response, cancellationToken);
        return new SquarePaymentLinkDeleteResult(
            payload.Id ?? paymentLinkId,
            payload.CanceledOrderId);
    }

    public async Task<SquareRefundStatusResult> RetrieveRefundAsync(
        string refundId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = Request(HttpMethod.Get, $"v2/refunds/{Uri.EscapeDataString(refundId)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadPayloadAsync<RefundPaymentResponse>(response, cancellationToken);
        var refund = payload.Refund ?? throw new SquareIntegrationException("MISSING_REFUND");
        return new SquareRefundStatusResult(
            refund.Id ?? refundId,
            refund.Status ?? "UNKNOWN");
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.AccessToken);
        request.Headers.TryAddWithoutValidation("Square-Version", settings.ApiVersion);
        request.Headers.Accept.ParseAdd("application/json");
        return request;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(settings.AccessToken) ||
            string.IsNullOrWhiteSpace(settings.LocationId))
        {
            throw new SquareIntegrationException("NOT_CONFIGURED");
        }
    }

    private static async Task<T> ReadPayloadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var failure = await ReadSafeErrorAsync(response, cancellationToken);
            throw new SquareIntegrationException(failure.Code, (int)response.StatusCode, failure.Field);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
               ?? throw new SquareIntegrationException("EMPTY_RESPONSE");
    }

    private static async Task<SquareSafeError> ReadSafeErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0 &&
                errors[0].TryGetProperty("code", out var code))
            {
                var safeCode = SafeProviderToken(code.GetString(), "SQUARE_REQUEST_FAILED")
                    ?? "SQUARE_REQUEST_FAILED";
                var field = errors[0].TryGetProperty("field", out var fieldElement)
                    ? SafeProviderToken(fieldElement.GetString(), null)
                    : null;
                return new SquareSafeError(safeCode, field);
            }
        }
        catch (JsonException)
        {
            // Only a stable provider code is retained; raw response details are discarded.
        }

        return new SquareSafeError("SQUARE_REQUEST_FAILED", null);
    }

    private static string? SafeProviderToken(string? value, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100) return fallback;
        return value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '[' or ']')
            ? value
            : fallback;
    }

    private sealed record SquareSafeError(string Code, string? Field);

    private sealed record CreatePaymentLinkRequest(
        [property: JsonPropertyName("idempotency_key")] string IdempotencyKey,
        [property: JsonPropertyName("order")] SquareOrder Order,
        [property: JsonPropertyName("checkout_options")] SquareCheckoutOptions CheckoutOptions,
        [property: JsonPropertyName("pre_populated_data")] SquarePrePopulatedData PrePopulatedData);

    private sealed record SquareOrder(
        [property: JsonPropertyName("location_id")] string LocationId,
        [property: JsonPropertyName("reference_id")] string ReferenceId,
        [property: JsonPropertyName("line_items")] IReadOnlyList<SquareLineItem> LineItems,
        [property: JsonPropertyName("pricing_options")] SquarePricingOptions PricingOptions);

    private sealed record SquarePricingOptions(
        [property: JsonPropertyName("auto_apply_taxes")] bool AutoApplyTaxes);

    private sealed record SquareLineItem(
        [property: JsonPropertyName("quantity")] string Quantity,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("variation_name")] string VariationName,
        [property: JsonPropertyName("base_price_money")] SquareMoney BasePriceMoney,
        [property: JsonPropertyName("modifiers")] IReadOnlyList<SquareModifier> Modifiers);

    private sealed record SquareModifier(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("base_price_money")] SquareMoney BasePriceMoney);

    private sealed record SquareMoney(
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("currency")] string Currency);

    private sealed record SquareCheckoutOptions(
        [property: JsonPropertyName("redirect_url")] string RedirectUrl,
        [property: JsonPropertyName("allow_tipping")] bool AllowTipping,
        [property: JsonPropertyName("ask_for_shipping_address")] bool AskForShippingAddress,
        [property: JsonPropertyName("enable_coupon")] bool EnableCoupon);

    private sealed record SquarePrePopulatedData(
        [property: JsonPropertyName("buyer_email")] string BuyerEmail,
        [property: JsonPropertyName("buyer_phone_number")] string BuyerPhoneNumber);

    private sealed record CreatePaymentLinkResponse(
        [property: JsonPropertyName("payment_link")] PaymentLink? PaymentLink,
        [property: JsonPropertyName("related_resources")] RelatedResources? RelatedResources);

    private sealed record RelatedResources(
        [property: JsonPropertyName("orders")] IReadOnlyList<RelatedOrder>? Orders);

    private sealed record RelatedOrder(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("total_tax_money")] SquareMoney? TotalTaxMoney,
        [property: JsonPropertyName("total_money")] SquareMoney? TotalMoney);

    private sealed record PaymentLink(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("order_id")] string? OrderId,
        [property: JsonPropertyName("url")] string? Url);

    private sealed record RetrievePaymentResponse(
        [property: JsonPropertyName("payment")] Payment? Payment);

    private sealed record RetrieveOrderResponse(
        [property: JsonPropertyName("order")] RetrievedOrder? Order);

    private sealed record RetrievedOrder(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("state")] string? State,
        [property: JsonPropertyName("total_money")] SquareMoney? TotalMoney,
        [property: JsonPropertyName("tenders")] IReadOnlyList<SquareTender>? Tenders);

    private sealed record SquareTender(
        [property: JsonPropertyName("payment_id")] string? PaymentId);

    private sealed record Payment(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("order_id")] string? OrderId,
        [property: JsonPropertyName("amount_money")] SquareMoney? AmountMoney);

    private sealed record RefundPaymentRequest(
        [property: JsonPropertyName("idempotency_key")] string IdempotencyKey,
        [property: JsonPropertyName("payment_id")] string PaymentId,
        [property: JsonPropertyName("amount_money")] SquareMoney AmountMoney,
        [property: JsonPropertyName("reason")] string Reason);

    private sealed record RefundPaymentResponse(
        [property: JsonPropertyName("refund")] Refund? Refund);

    private sealed record DeletePaymentLinkResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("cancelled_order_id")] string? CanceledOrderId);

    private sealed record Refund(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string? Status);

}

public sealed class SquareIntegrationException(
    string safeCode,
    int? statusCode = null,
    string? safeField = null) : Exception($"Square integration failed with code {safeCode}.")
{
    public string SafeCode { get; } = safeCode;
    public int? StatusCode { get; } = statusCode;
    public string? SafeField { get; } = safeField;
    public bool IsDeterministicClientFailure => StatusCode is >= 400 and < 500;
}
