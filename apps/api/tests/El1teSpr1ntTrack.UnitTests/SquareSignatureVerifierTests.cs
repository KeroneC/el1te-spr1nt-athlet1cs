using System.Security.Cryptography;
using System.Text;
using El1teSpr1ntTrack.Infrastructure.Commerce;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class SquareSignatureVerifierTests
{
    [Fact]
    public void IsValid_RequiresExactNotificationUrlAndRawBody()
    {
        var settings = new SquareSettings
        {
            WebhookNotificationUrl = "https://api.example.invalid/api/webhooks/square",
            WebhookSignatureKey = "test-signature-key"
        };
        var rawBody = """{"event_id":"event-1","type":"payment.updated"}""";
        var signature = Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(settings.WebhookSignatureKey),
            Encoding.UTF8.GetBytes(settings.WebhookNotificationUrl + rawBody)));
        var verifier = new SquareSignatureVerifier(settings);

        Assert.True(verifier.IsValid(rawBody, signature));
        Assert.False(verifier.IsValid(rawBody + " ", signature));
        Assert.False(verifier.IsValid(rawBody, "not-base64"));
        Assert.False(verifier.IsValid(rawBody, null));
    }
}
