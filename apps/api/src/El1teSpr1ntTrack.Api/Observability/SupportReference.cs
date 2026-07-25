using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace El1teSpr1ntTrack.Api.Observability;

public static partial class SupportReference
{
    public const string HeaderName = "X-Reference-Id";

    public static string Create() => $"ESA-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && ReferencePattern().IsMatch(value);

    [GeneratedRegex("^ESA-[0-9A-F]{16}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReferencePattern();
}
