using System.Net.Http.Json;
using System.Globalization;
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

    public async Task<SquareCatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var catalogObjects = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        string? cursor = null;

        do
        {
            using var request = Request(HttpMethod.Post, "v2/catalog/search");
            request.Content = JsonContent.Create(new SearchCatalogRequest(
                ["ITEM", "CATEGORY", "ITEM_OPTION", "IMAGE"],
                false,
                true,
                cursor));
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await ReadPayloadAsync<SearchCatalogResponse>(response, cancellationToken);

            foreach (var catalogObject in (payload.Objects ?? []).Concat(payload.RelatedObjects ?? []))
            {
                var id = PropertyString(catalogObject, "id");
                if (!string.IsNullOrWhiteSpace(id))
                {
                    catalogObjects[id] = catalogObject.Clone();
                }
            }

            cursor = string.IsNullOrWhiteSpace(payload.Cursor) ? null : payload.Cursor;
        }
        while (cursor is not null);

        var images = catalogObjects.Values
            .Where(value => PropertyString(value, "type") == "IMAGE")
            .Select(value => new
            {
                Id = PropertyString(value, "id"),
                Url = NestedString(value, "image_data", "url"),
                Caption = NestedString(value, "image_data", "caption") ??
                          NestedString(value, "image_data", "name")
            })
            .Where(value => !string.IsNullOrWhiteSpace(value.Id) && !string.IsNullOrWhiteSpace(value.Url))
            .ToDictionary(
                value => value.Id!,
                value => new SquareCatalogImage(value.Id!, value.Url!, value.Caption),
                StringComparer.Ordinal);

        var categories = catalogObjects.Values
            .Where(value => PropertyString(value, "type") == "CATEGORY")
            .Select(value => new
            {
                Id = PropertyString(value, "id"),
                Name = NestedString(value, "category_data", "name")
            })
            .Where(value => !string.IsNullOrWhiteSpace(value.Id))
            .ToDictionary(value => value.Id!, value => value.Name, StringComparer.Ordinal);

        var optionDefinitions = catalogObjects.Values
            .Where(value => PropertyString(value, "type") == "ITEM_OPTION")
            .Select(ParseOption)
            .Where(value => value is not null)
            .Cast<SquareCatalogOption>()
            .ToDictionary(value => value.CatalogObjectId, StringComparer.Ordinal);

        var itemObjects = catalogObjects.Values
            .Where(value => PropertyString(value, "type") == "ITEM")
            .ToList();
        var variationIds = itemObjects
            .SelectMany(ItemVariations)
            .Select(value => PropertyString(value, "id"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var inventory = await GetInventoryCountsAsync(variationIds, cancellationToken);

        var products = new List<SquareCatalogProduct>();
        foreach (var item in itemObjects)
        {
            var id = PropertyString(item, "id");
            var name = NestedString(item, "item_data", "name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var itemData = item.GetProperty("item_data");
            var categoryId = NestedString(item, "item_data", "category_id") ??
                             FirstNestedId(itemData, "categories");
            categories.TryGetValue(categoryId ?? string.Empty, out var categoryName);

            var optionIds = NestedIds(itemData, "item_options", "item_option_id");
            var variationElements = ItemVariations(item).ToList();
            var referencedOptionIds = variationElements
                .SelectMany(value => NestedObjects(value, "item_variation_data", "item_option_values"))
                .Select(value => PropertyString(value, "item_option_id"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>();
            var productOptions = optionIds
                .Concat(referencedOptionIds)
                .Distinct(StringComparer.Ordinal)
                .Select(optionId => optionDefinitions.GetValueOrDefault(optionId))
                .Where(value => value is not null)
                .Cast<SquareCatalogOption>()
                .Select((value, index) => value with { DisplayOrder = index })
                .ToList();

            var imageIds = NestedStringArray(itemData, "image_ids")
                .Concat(variationElements.SelectMany(value =>
                    NestedStringArray(value.GetProperty("item_variation_data"), "image_ids")))
                .Distinct(StringComparer.Ordinal);
            var productImages = imageIds
                .Select(imageId => images.GetValueOrDefault(imageId))
                .Where(value => value is not null)
                .Cast<SquareCatalogImage>()
                .ToList();

            var variants = variationElements
                .Select((value, index) => ParseVariant(value, inventory, index))
                .Where(value => value is not null)
                .Cast<SquareCatalogVariant>()
                .ToList();
            if (variants.Count == 0)
            {
                continue;
            }

            products.Add(new SquareCatalogProduct(
                id,
                PropertyLong(item, "version"),
                name.Trim(),
                NestedString(item, "item_data", "description_plaintext") ??
                NestedString(item, "item_data", "description"),
                categoryId,
                categoryName,
                productImages,
                productOptions,
                variants));
        }

        return new SquareCatalogSnapshot(products.OrderBy(value => value.Name).ToList());
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

    private async Task<Dictionary<string, int>> GetInventoryCountsAsync(
        IReadOnlyList<string> variationIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var batch in variationIds.Chunk(1000))
        {
            string? cursor = null;
            do
            {
                using var request = Request(HttpMethod.Post, "v2/inventory/counts/batch-retrieve");
                request.Content = JsonContent.Create(new BatchInventoryRequest(
                    batch,
                    [settings.LocationId!],
                    cursor));
                using var response = await httpClient.SendAsync(request, cancellationToken);
                var payload = await ReadPayloadAsync<BatchInventoryResponse>(response, cancellationToken);
                foreach (var count in payload.Counts ?? [])
                {
                    var catalogObjectId = PropertyString(count, "catalog_object_id");
                    var state = PropertyString(count, "state");
                    var locationId = PropertyString(count, "location_id");
                    if (string.IsNullOrWhiteSpace(catalogObjectId) ||
                        state != "IN_STOCK" ||
                        locationId != settings.LocationId)
                    {
                        continue;
                    }

                    var quantity = PropertyString(count, "quantity");
                    if (decimal.TryParse(quantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    {
                        result[catalogObjectId] = Math.Max(0, (int)decimal.Truncate(parsed));
                    }
                }

                cursor = string.IsNullOrWhiteSpace(payload.Cursor) ? null : payload.Cursor;
            }
            while (cursor is not null);
        }

        return result;
    }

    private static SquareCatalogOption? ParseOption(JsonElement catalogObject)
    {
        var id = PropertyString(catalogObject, "id");
        var name = NestedString(catalogObject, "item_option_data", "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var values = NestedObjects(catalogObject, "item_option_data", "values")
            .Select(value =>
            {
                var valueId = PropertyString(value, "id");
                var data = value.TryGetProperty("item_option_value_data", out var nestedData)
                    ? nestedData
                    : value;
                var valueName = PropertyString(data, "name");
                return string.IsNullOrWhiteSpace(valueId) || string.IsNullOrWhiteSpace(valueName)
                    ? null
                    : new SquareCatalogOptionValue(
                        valueId,
                        valueName.Trim(),
                        PropertyString(data, "color"),
                        PropertyInt(data, "ordinal"));
            })
            .Where(value => value is not null)
            .Cast<SquareCatalogOptionValue>()
            .OrderBy(value => value.DisplayOrder)
            .ToList();

        return new SquareCatalogOption(id, name.Trim(), 0, values);
    }

    private static SquareCatalogVariant? ParseVariant(
        JsonElement catalogObject,
        IReadOnlyDictionary<string, int> inventory,
        int displayOrder)
    {
        var id = PropertyString(catalogObject, "id");
        if (string.IsNullOrWhiteSpace(id) ||
            !catalogObject.TryGetProperty("item_variation_data", out var data))
        {
            return null;
        }

        var name = PropertyString(data, "name");
        var amount = data.TryGetProperty("price_money", out var money)
            ? PropertyLong(money, "amount")
            : 0;
        var currency = data.TryGetProperty("price_money", out money)
            ? PropertyString(money, "currency") ?? "USD"
            : "USD";
        var optionValueIds = NestedObjects(data, "item_option_values")
            .Select(value => PropertyString(value, "item_option_value_id"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        return new SquareCatalogVariant(
            id,
            PropertyLong(catalogObject, "version"),
            string.IsNullOrWhiteSpace(name) ? $"Variation {displayOrder + 1}" : name.Trim(),
            PropertyString(data, "sku"),
            Math.Max(0, amount),
            currency,
            inventory.GetValueOrDefault(id),
            optionValueIds);
    }

    private static IEnumerable<JsonElement> ItemVariations(JsonElement item) =>
        NestedObjects(item, "item_data", "variations");

    private static IEnumerable<JsonElement> NestedObjects(
        JsonElement element,
        string parentProperty,
        string collectionProperty)
    {
        if (!element.TryGetProperty(parentProperty, out var parent))
        {
            return [];
        }

        return NestedObjects(parent, collectionProperty);
    }

    private static IEnumerable<JsonElement> NestedObjects(
        JsonElement element,
        string collectionProperty) =>
        element.TryGetProperty(collectionProperty, out var collection) &&
        collection.ValueKind == JsonValueKind.Array
            ? collection.EnumerateArray()
            : [];

    private static IReadOnlyList<string> NestedIds(
        JsonElement element,
        string collectionProperty,
        string idProperty) =>
        NestedObjects(element, collectionProperty)
            .Select(value => PropertyString(value, idProperty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

    private static IReadOnlyList<string> NestedStringArray(
        JsonElement element,
        string propertyName) =>
        element.TryGetProperty(propertyName, out var collection) &&
        collection.ValueKind == JsonValueKind.Array
            ? collection.EnumerateArray()
                .Select(value => value.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList()
            : [];

    private static string? FirstNestedId(JsonElement element, string collectionProperty) =>
        NestedObjects(element, collectionProperty)
            .Select(value => PropertyString(value, "id"))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? NestedString(
        JsonElement element,
        string parentProperty,
        string propertyName) =>
        element.TryGetProperty(parentProperty, out var parent)
            ? PropertyString(parent, propertyName)
            : null;

    private static string? PropertyString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static long PropertyLong(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) &&
        value.TryGetInt64(out var result)
            ? result
            : 0;

    private static int PropertyInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) &&
        value.TryGetInt32(out var result)
            ? result
            : 0;

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

    private sealed record SearchCatalogRequest(
        [property: JsonPropertyName("object_types")] IReadOnlyList<string> ObjectTypes,
        [property: JsonPropertyName("include_deleted_objects")] bool IncludeDeletedObjects,
        [property: JsonPropertyName("include_related_objects")] bool IncludeRelatedObjects,
        [property: JsonPropertyName("cursor")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Cursor);

    private sealed record SearchCatalogResponse(
        [property: JsonPropertyName("objects")] IReadOnlyList<JsonElement>? Objects,
        [property: JsonPropertyName("related_objects")] IReadOnlyList<JsonElement>? RelatedObjects,
        [property: JsonPropertyName("cursor")] string? Cursor);

    private sealed record BatchInventoryRequest(
        [property: JsonPropertyName("catalog_object_ids")] IReadOnlyList<string> CatalogObjectIds,
        [property: JsonPropertyName("location_ids")] IReadOnlyList<string> LocationIds,
        [property: JsonPropertyName("cursor")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Cursor);

    private sealed record BatchInventoryResponse(
        [property: JsonPropertyName("counts")] IReadOnlyList<JsonElement>? Counts,
        [property: JsonPropertyName("cursor")] string? Cursor);
}

public sealed class SquareIntegrationException(
    string safeCode,
    int? statusCode = null) : Exception($"Square integration failed with code {safeCode}.")
{
    public string SafeCode { get; } = safeCode;
    public int? StatusCode { get; } = statusCode;
}
