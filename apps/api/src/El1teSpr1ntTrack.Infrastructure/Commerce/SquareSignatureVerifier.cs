using System.Security.Cryptography;
using System.Text;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class SquareSignatureVerifier(SquareSettings settings) : ISquareSignatureVerifier
{
    public bool IsValid(string rawBody, string? suppliedSignature)
    {
        if (string.IsNullOrWhiteSpace(suppliedSignature) ||
            string.IsNullOrWhiteSpace(settings.WebhookSignatureKey) ||
            string.IsNullOrWhiteSpace(settings.WebhookNotificationUrl))
        {
            return false;
        }

        byte[] supplied;
        try
        {
            supplied = Convert.FromBase64String(suppliedSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        var message = Encoding.UTF8.GetBytes(settings.WebhookNotificationUrl + rawBody);
        var key = Encoding.UTF8.GetBytes(settings.WebhookSignatureKey);
        var expected = HMACSHA256.HashData(key, message);
        return supplied.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(supplied, expected);
    }
}
