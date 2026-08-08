using System.Security.Cryptography;
using System.Text;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class PrintifySignatureVerifier(PrintifySettings settings) : IPrintifySignatureVerifier
{
    public bool IsValid(string rawBody, string? suppliedSignature)
    {
        if (!settings.HasWebhookCredentials ||
            string.IsNullOrWhiteSpace(suppliedSignature) ||
            !suppliedSignature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suppliedHex = suppliedSignature[7..];
        if (suppliedHex.Length != 64)
        {
            return false;
        }

        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(suppliedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(settings.WebhookSecret!),
            Encoding.UTF8.GetBytes(rawBody));
        return CryptographicOperations.FixedTimeEquals(supplied, expected);
    }
}
