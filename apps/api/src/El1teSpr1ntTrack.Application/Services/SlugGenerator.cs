using System.Globalization;
using System.Text;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class SlugGenerator : ISlugGenerator
{
    public string Generate(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = value.Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(normalized.Length);
        var pendingHyphen = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (pendingHyphen && slug.Length > 0)
                {
                    slug.Append('-');
                }

                slug.Append(char.ToLowerInvariant(character));
                pendingHyphen = false;
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                pendingHyphen = slug.Length > 0;
            }
        }

        return slug.Length == 0 ? "item" : slug.ToString();
    }

    public async Task<string> GenerateUniqueAsync(
        string value,
        Func<string, CancellationToken, Task<bool>> slugExists,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slugExists);

        var baseSlug = Generate(value);
        var candidate = baseSlug;
        var suffix = 2;

        while (await slugExists(candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
