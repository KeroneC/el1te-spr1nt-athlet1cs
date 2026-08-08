using System.Net.Http.Headers;
using System.Text.Json;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class PrintifyClient(HttpClient httpClient, PrintifySettings settings) : IPrintifyClient
{
    public async Task<PrintifyShopSnapshot> GetShopAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await SendAsync(HttpMethod.Get, "shops.json", cancellationToken);
        using var document = await ReadAsync(response, cancellationToken);
        foreach (var shop in document.RootElement.EnumerateArray())
        {
            if (shop.GetProperty("id").GetInt64() == settings.ShopId)
            {
                return new PrintifyShopSnapshot(
                    settings.ShopId!.Value,
                    String(shop, "title") ?? "Printify shop",
                    String(shop, "sales_channel") ?? "unknown");
            }
        }

        throw new PrintifyIntegrationException("SHOP_NOT_FOUND", 404);
    }

    public async Task<IReadOnlyList<PrintifyProductSnapshot>> GetProductsAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var products = new List<PrintifyProductSnapshot>();
        var page = 1;
        while (true)
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                $"shops/{settings.ShopId}/products.json?page={page}&limit=100",
                cancellationToken);
            using var document = await ReadAsync(response, cancellationToken);
            var root = document.RootElement;
            foreach (var product in root.GetProperty("data").EnumerateArray())
            {
                products.Add(ParseProduct(product));
            }

            var lastPage = root.TryGetProperty("last_page", out var lastPageElement)
                ? lastPageElement.GetInt32()
                : page;
            if (page >= lastPage)
            {
                break;
            }

            page++;
        }

        return products;
    }

    public async Task<PrintifyProductSnapshot> GetProductAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(productId) || productId.Length > 100)
        {
            throw new PrintifyIntegrationException("INVALID_PRODUCT_ID");
        }

        using var response = await SendAsync(
            HttpMethod.Get,
            $"shops/{settings.ShopId}/products/{Uri.EscapeDataString(productId)}.json",
            cancellationToken);
        using var document = await ReadAsync(response, cancellationToken);
        return ParseProduct(document.RootElement);
    }

    public async Task<IReadOnlyList<PrintifyWebhookSubscription>> GetWebhooksAsync(
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await SendAsync(
            HttpMethod.Get,
            $"shops/{settings.ShopId}/webhooks.json",
            cancellationToken);
        using var document = await ReadAsync(response, cancellationToken);
        return document.RootElement.EnumerateArray()
            .Select(value => new PrintifyWebhookSubscription(
                String(value, "id") ?? string.Empty,
                String(value, "topic") ?? string.Empty,
                String(value, "url") ?? string.Empty))
            .Where(value => value.Id.Length > 0)
            .ToList();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("El1teSpr1ntAthlet1cs/1.0");
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PrintifyIntegrationException("TIMEOUT");
        }
        catch (HttpRequestException)
        {
            throw new PrintifyIntegrationException("CONNECTION_FAILED");
        }
    }

    private static async Task<JsonDocument> ReadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new PrintifyIntegrationException("HTTP_ERROR", (int)response.StatusCode);
        }

        try
        {
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            throw new PrintifyIntegrationException("INVALID_RESPONSE", (int)response.StatusCode);
        }
    }

    private static PrintifyProductSnapshot ParseProduct(JsonElement product)
    {
        var options = new List<PrintifyOptionSnapshot>();
        if (product.TryGetProperty("options", out var optionsElement))
        {
            var optionOrder = 0;
            foreach (var option in optionsElement.EnumerateArray())
            {
                var values = new List<PrintifyOptionValueSnapshot>();
                var valueOrder = 0;
                foreach (var value in option.GetProperty("values").EnumerateArray())
                {
                    values.Add(new PrintifyOptionValueSnapshot(
                        value.GetProperty("id").GetInt32(),
                        String(value, "title") ?? $"Option {valueOrder + 1}",
                        FirstColor(value),
                        valueOrder++));
                }

                options.Add(new PrintifyOptionSnapshot(
                    String(option, "name") ?? String(option, "type") ?? $"Option {optionOrder + 1}",
                    optionOrder++,
                    values));
            }
        }

        var variants = new List<PrintifyVariantSnapshot>();
        if (product.TryGetProperty("variants", out var variantsElement))
        {
            foreach (var variant in variantsElement.EnumerateArray())
            {
                variants.Add(new PrintifyVariantSnapshot(
                    variant.GetProperty("id").GetInt32(),
                    String(variant, "title") ?? "Variant",
                    String(variant, "sku"),
                    variant.TryGetProperty("cost", out var cost) ? cost.GetInt64() : 0,
                    Boolean(variant, "is_enabled"),
                    !variant.TryGetProperty("is_available", out _) || Boolean(variant, "is_available"),
                    variant.TryGetProperty("options", out var selectedOptions)
                        ? selectedOptions.EnumerateArray().Select(value => value.GetInt32()).ToList()
                        : []));
            }
        }

        var images = new List<PrintifyImageSnapshot>();
        if (product.TryGetProperty("images", out var imagesElement))
        {
            foreach (var image in imagesElement.EnumerateArray())
            {
                var source = String(image, "src");
                if (string.IsNullOrWhiteSpace(source))
                {
                    continue;
                }

                images.Add(new PrintifyImageSnapshot(
                    source,
                    image.TryGetProperty("variant_ids", out var variantIds)
                        ? variantIds.EnumerateArray().Select(value => value.GetInt32()).ToList()
                        : [],
                    Boolean(image, "is_default")));
            }
        }

        return new PrintifyProductSnapshot(
            String(product, "id") ?? throw new PrintifyIntegrationException("INVALID_RESPONSE"),
            String(product, "title") ?? "Untitled Printify product",
            String(product, "description"),
            product.TryGetProperty("blueprint_id", out var blueprint) ? blueprint.GetInt32() : 0,
            product.TryGetProperty("print_provider_id", out var provider) ? provider.GetInt32() : 0,
            options,
            variants,
            images);
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool Boolean(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? FirstColor(JsonElement value)
    {
        if (!value.TryGetProperty("colors", out var colors) || colors.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return colors.EnumerateArray().FirstOrDefault().ValueKind == JsonValueKind.String
            ? colors.EnumerateArray().First().GetString()
            : null;
    }

    private void EnsureConfigured()
    {
        if (!settings.HasCatalogCredentials)
        {
            throw new PrintifyIntegrationException("NOT_CONFIGURED");
        }
    }
}

public sealed class PrintifyIntegrationException(
    string safeCode,
    int? statusCode = null) : Exception($"Printify integration failed with code {safeCode}.")
{
    public string SafeCode { get; } = safeCode;
    public int? StatusCode { get; } = statusCode;
}
