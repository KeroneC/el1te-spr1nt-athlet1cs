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
            """{"payment_link":{"id":"link-1","order_id":"square-order-1","url":"https://sandbox.square.link/u/test"}}""");
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
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/v2/online-checkout/payment-links", handler.Request.RequestUri!.AbsolutePath);
        Assert.Contains("\"auto_apply_taxes\":true", handler.Body);
        Assert.Contains("\"reference_id\":\"ESA-ORDER-1\"", handler.Body);
        Assert.Contains("\"base_price_money\":{\"amount\":5000,\"currency\":\"USD\"}", handler.Body);
        Assert.DoesNotContain("test-token", handler.Body);
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
}
