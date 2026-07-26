using System.Globalization;
using System.Text;

namespace Pugling.Api.Data;

/// <summary>
/// Bildet Freitext („Pokémon", „Brawl Stars") auf den stabilen Slug eines <see cref="Models.InterestTag"/>
/// ab. Zentral, weil drei Wege denselben Slug treffen müssen, sonst zerfällt die geteilte Taxonomie in
/// Dubletten: der Creator beim Anlegen, der Supervisor beim Tippen eines Interesses und der Backfill der
/// bestehenden Freitext-Interessen.
/// </summary>
public static class InterestSlug
{
    /// <summary>
    /// Kleinschreibung, ß→ss, Diakritika entfernt, alles Nicht-Alphanumerische zu einem Bindestrich
    /// verdichtet („Brawl Stars!" → "brawl-stars"). Leerer/rein symbolischer Text ergibt <c>""</c> –
    /// der Aufrufer entscheidet, ob das ein Validierungsfehler ist.
    /// </summary>
    public static string From(string text)
    {
        var normalized = text.ToLowerInvariant().Replace("ß", "ss").Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            // Trennzeichen nur anhängen, wenn schon Inhalt da ist – verhindert führende Bindestriche
            // und Doppelungen ohne teure Nachbearbeitung.
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }

        return sb.ToString().Trim('-');
    }
}
