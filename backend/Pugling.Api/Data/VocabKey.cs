using System.Globalization;
using System.Text;

namespace Pugling.Api.Data;

/// <summary>
/// Erzeugt stabile, eindeutige Vokabel-Keys nach dem Muster <c>{src}_{wort}_{tgt}_{übersetzung}</c>.
/// Zentral, damit Seed und der Vokabel-Store denselben Slug verwenden (die „einfache" Eingabe kommt
/// ohne selbst getippten Key aus – der Server generiert ihn).
/// </summary>
public static class VocabKey
{
    /// <summary>Kleinschreibung, ß→ss, Diakritika entfernt, Apostroph→Leerzeichen, getrimmt.</summary>
    public static string Slug(string s) =>
        s.ToLowerInvariant().Replace("ß", "ss").Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Aggregate(new StringBuilder(), (sb, ch) => sb.Append(ch)).ToString()
            .Replace("'", " ").Trim();

    /// <summary>Basiskey aus Sprachen + Wort/Übersetzung (Leerzeichen→Unterstrich, keine Doppel-Unterstriche).</summary>
    public static string Generate(string sourceLanguage, string word, string targetLanguage, string translation)
    {
        var src = Slug(sourceLanguage).Replace(' ', '_');
        var tgt = Slug(targetLanguage).Replace(' ', '_');
        var w = Slug(word).Replace(' ', '_');
        var t = Slug(translation).Replace(' ', '_');
        return $"{src}_{w}_{tgt}_{t}".Replace("__", "_").Trim('_');
    }
}
