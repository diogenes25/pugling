using System.Globalization;
using System.Text;

namespace Pugling.Api.Data;

/// <summary>
/// Maps free text ("Pokémon", "Brawl Stars") onto the stable slug of an <see cref="Models.InterestTag"/>.
/// Central, because three paths have to hit the same slug or the shared taxonomy falls apart into
/// duplicates: the creator when creating one, the supervisor when typing an interest, and the backfill of
/// the existing free-text interests.
/// </summary>
public static class InterestSlug
{
    /// <summary>
    /// Lower case, ß→ss, diacritics removed, everything non-alphanumeric condensed into a single hyphen
    /// ("Brawl Stars!" → "brawl-stars"). Empty/purely symbolic text yields <c>""</c> – the caller decides
    /// whether that is a validation error.
    /// </summary>
    public static string From(string text)
    {
        var normalized = text.ToLowerInvariant().Replace("ß", "ss").Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            // Only append a separator if there is content already - prevents leading hyphens and doubles
            // without expensive post-processing.
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }

        return sb.ToString().Trim('-');
    }
}
