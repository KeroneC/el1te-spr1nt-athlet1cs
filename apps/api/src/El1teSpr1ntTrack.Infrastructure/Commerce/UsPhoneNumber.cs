namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public static class UsPhoneNumber
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        Span<char> digits = stackalloc char[11];
        var length = 0;
        foreach (var character in value)
        {
            if (char.IsAsciiDigit(character))
            {
                if (length == digits.Length) return false;
                digits[length++] = character;
                continue;
            }

            if (character is not (' ' or '+' or '-' or '(' or ')' or '.')) return false;
        }

        var offset = length == 11 && digits[0] == '1' ? 1 : 0;
        if (length - offset != 10) return false;

        // NANP area codes and exchanges cannot begin with 0 or 1.
        if (digits[offset] is '0' or '1' || digits[offset + 3] is '0' or '1') return false;

        normalized = $"+1{new string(digits.Slice(offset, 10))}";
        return true;
    }
}
