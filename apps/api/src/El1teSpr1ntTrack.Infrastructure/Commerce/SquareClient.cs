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
            new SquareCheckoutOptions(command.RedirectUrl, false));

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

        return new SquarePaymentLinkResult(link.Id, link.OrderId, link.Url);
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
            var safeCode = await ReadSafeErrorCodeAsync(response, cancellationToken);
            throw new SquareIntegrationException(safeCode, (int)response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
               ?? throw new SquareIntegrationException("EMPTY_RESPONSE");
    }

    private static async Task<string> ReadSafeErrorCodeAsync(
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
                return code.GetString() ?? "SQUARE_REQUEST_FAILED";
            }
        }
        catch (JsonException)
        {
            // Only a stable provider code is retained; raw response details are discarded.
        }

        return "SQUARE_REQUEST_FAILED";
    }

    private sealed record CreatePaymentLinkRequest(
        [property: JsonPropertyName("idempotency_key")] string IdempotencyKey,
        [property: JsonPropertyName("order")] SquareOrder Order,
        [property: JsonPropertyName("checkout_options")] SquareCheckoutOptions CheckoutOptions);

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
        [property: JsonPropertyName("allow_tipping")] bool AllowTipping);

    private sealed record CreatePaymentLinkResponse(
        [property: JsonPropertyName("payment_link")] PaymentLink? PaymentLink);

    private sealed record PaymentLink(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("order_id")] string? OrderId,
        [property: JsonPropertyName("url")] string? Url);

    private sealed record RetrievePaymentResponse(
        [property: JsonPropertyName("payment")] Payment? Payment);

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

    private sealed record Refund(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string? Status);
}

public sealed class SquareIntegrationException(
    string safeCode,
    int? statusCode = null) : Exception($"Square integration failed with code {safeCode}.")
{
    public string SafeCode { get; } = safeCode;
    public int? StatusCode { get; } = statusCode;
}
