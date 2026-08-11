using System.Net;
using System.Text;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Commerce;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class SquareClientTests
{
    [Fact]
    public async Task CreatePaymentLink_UsesAdHocLinesAndSquareManagedTaxes()
    {
        var handler = new RecordingHandler(
            """{"payment_link":{"id":"link-1","order_id":"square-order-1","url":"https://sandbox.square.link/u/test"},"related_resources":{"orders":[{"id":"square-order-1","total_tax_money":{"amount":330,"currency":"USD"},"total_money":{"amount":5830,"currency":"USD"}}]}}""");
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(SquareSettings.SandboxBaseUrl)
        };
        var client = new SquareClient(httpClient, new SquareSettings
        {
            AccessToken = "test-token",
            LocationId = "location-1",
            ApiVersion = "2026-07-15"
        });

        var result = await client.CreatePaymentLinkAsync(
            new SquarePaymentLinkCommand(
                "idempotency-key",
                "ESA-ORDER-1",
                "https://web.example.invalid/shop/order-confirmation",
                "USD",
                "buyer@example.com",
                "+14155550100",
                [
                    new SquareCheckoutLineItem(
                        "Team hoodie",
                        "Large / Black",
                        1,
                        5000,
                        [new SquareCheckoutModifier("White logo", 500)])
                ]),
            CancellationToken.None);

        Assert.Equal("link-1", result.PaymentLinkId);
        Assert.Equal(330, result.TaxMinor);
        Assert.Equal(5830, result.TotalMinor);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/v2/online-checkout/payment-links", handler.Request.RequestUri!.AbsolutePath);
        Assert.Contains("\"auto_apply_taxes\":true", handler.Body);
        Assert.Contains("\"reference_id\":\"ESA-ORDER-1\"", handler.Body);
        Assert.Contains("\"buyer_email\":\"buyer@example.com\"", handler.Body);
        Assert.Contains("\"ask_for_shipping_address\":false", handler.Body);
        Assert.Contains("\"base_price_money\":{\"amount\":5000,\"currency\":\"USD\"}", handler.Body);
        Assert.DoesNotContain("test-token", handler.Body);
    }

    [Fact]
    public async Task GetCatalogSnapshot_NormalizesOptionsImagesVariantsAndLocationInventory()
    {
        var handler = new SequenceHandler([
            """
            {
              "objects": [
                {
                  "type": "ITEM_OPTION", "id": "OPTION-SIZE",
                  "item_option_data": { "name": "Size", "values": [
                    { "id": "VALUE-M", "item_option_value_data": { "name": "Medium", "ordinal": 1 } }
                  ] }
                },
                { "type": "IMAGE", "id": "IMAGE-1", "image_data": { "url": "https://images.squareup.com/item.png", "caption": "Front" } },
                { "type": "CATEGORY", "id": "CATEGORY-1", "category_data": { "name": "Apparel" } },
                {
                  "type": "ITEM", "id": "ITEM-1", "version": 14,
                  "item_data": {
                    "name": "Team hoodie", "description_plaintext": "Warm team hoodie",
                    "category_id": "CATEGORY-1", "image_ids": ["IMAGE-1"],
                    "item_options": [{ "item_option_id": "OPTION-SIZE" }],
                    "variations": [{
                      "type": "ITEM_VARIATION", "id": "VARIATION-1", "version": 9,
                      "item_variation_data": {
                        "name": "Medium", "sku": "HOOD-M",
                        "price_money": { "amount": 5000, "currency": "USD" },
                        "item_option_values": [{
                          "item_option_id": "OPTION-SIZE", "item_option_value_id": "VALUE-M"
                        }]
                      }
                    }]
                  }
                }
              ]
            }
            """,
            """
            {
              "counts": [{
                "catalog_object_id": "VARIATION-1",
                "location_id": "location-1",
                "state": "IN_STOCK",
                "quantity": "7"
              }]
            }
            """
        ]);
        var client = new SquareClient(
            new HttpClient(handler) { BaseAddress = new Uri(SquareSettings.SandboxBaseUrl) },
            new SquareSettings { AccessToken = "token", LocationId = "location-1", ApiVersion = "2026-07-15" });

        var snapshot = await client.GetCatalogSnapshotAsync(CancellationToken.None);

        var product = Assert.Single(snapshot.Products);
        Assert.Equal("Team hoodie", product.Name);
        Assert.Equal("Apparel", product.CategoryName);
        Assert.Single(product.Images);
        var option = Assert.Single(product.Options);
        Assert.Equal("Size", option.Name);
        var variant = Assert.Single(product.Variants);
        Assert.Equal("HOOD-M", variant.Sku);
        Assert.Equal(5000, variant.PriceMinor);
        Assert.Equal(7, variant.OnHandQuantity);
        Assert.Contains("VALUE-M", variant.OptionValueCatalogObjectIds);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/v2/catalog/search", handler.Requests[0].Path);
        Assert.Equal("/v2/inventory/counts/batch-retrieve", handler.Requests[1].Path);
        Assert.Contains("\"location_ids\":[\"location-1\"]", handler.Requests[1].Body);
        Assert.All(handler.Requests, request => Assert.Equal("2026-07-15", request.SquareVersion));
    }

    [Fact]
    public async Task PaymentLinkDeletionAndRefundLookup_UseSquareManagementEndpoints()
    {
        var handler = new SequenceHandler([
            """{"id":"link-1","cancelled_order_id":"order-1"}""",
            """{"refund":{"id":"refund-1","status":"COMPLETED"}}"""
        ]);
        var client = new SquareClient(
            new HttpClient(handler) { BaseAddress = new Uri(SquareSettings.SandboxBaseUrl) },
            new SquareSettings { AccessToken = "token", LocationId = "location-1", ApiVersion = "2026-07-15" });

        var deleted = await client.DeletePaymentLinkAsync("link-1", CancellationToken.None);
        var refund = await client.RetrieveRefundAsync("refund-1", CancellationToken.None);

        Assert.Equal("order-1", deleted.CanceledOrderId);
        Assert.Equal("COMPLETED", refund.Status);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("/v2/online-checkout/payment-links/link-1", handler.Requests[0].Path);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("/v2/refunds/refund-1", handler.Requests[1].Path);
    }

    [Fact]
    public async Task CreatePaymentLink_RetainsOnlySafeProviderCodeAndField()
    {
        var handler = new ErrorHandler(
            HttpStatusCode.BadRequest,
            """{"errors":[{"code":"INVALID_PHONE_NUMBER","field":"pre_populated_data.buyer_phone_number","detail":"must not be retained"}]}""");
        var client = new SquareClient(
            new HttpClient(handler) { BaseAddress = new Uri(SquareSettings.SandboxBaseUrl) },
            new SquareSettings { AccessToken = "token", LocationId = "location-1", ApiVersion = "2026-07-15" });

        var exception = await Assert.ThrowsAsync<SquareIntegrationException>(() =>
            client.CreatePaymentLinkAsync(new SquarePaymentLinkCommand(
                "attempt", "ESA-ORDER-1", "https://example.test/return", "USD",
                "buyer@example.com", "+14125550100",
                [new SquareCheckoutLineItem("Hoodie", "Large / Red", 1, 5000, [])]),
                CancellationToken.None));

        Assert.Equal("INVALID_PHONE_NUMBER", exception.SafeCode);
        Assert.Equal("pre_populated_data.buyer_phone_number", exception.SafeField);
        Assert.True(exception.IsDeterministicClientFailure);
        Assert.DoesNotContain("must not be retained", exception.Message);
    }

    private sealed class RecordingHandler(string responseJson) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class SequenceHandler(IReadOnlyList<string> responses) : HttpMessageHandler
    {
        private int _index;
        public List<(HttpMethod Method, string Path, string Body, string? SquareVersion)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add((
                request.Method,
                request.RequestUri!.AbsolutePath,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.TryGetValues("Square-Version", out var values) ? values.Single() : null));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responses[_index++], Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class ErrorHandler(HttpStatusCode statusCode, string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });
    }
}
