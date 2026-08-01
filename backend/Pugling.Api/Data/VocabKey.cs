using System.Globalization;
using System.Text;

namespace Pugling.Api.Data;

/// <summary>
/// Generates stable, unique vocabulary keys following the pattern <c>{src}_{word}_{tgt}_{translation}</c>.
/// Central, so that the seed and the vocabulary store use the same slug (the "simple" input works without a
/// hand-typed key – the server generates it).
/// </summary>
public static class VocabKey
{
    /// <summary>Lower case, ß→ss, diacritics removed, apostrophe→space, trimmed.</summary>
    public static string Slug(string s) =>
        s.ToLowerInvariant().Replace("ß", "ss").Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Aggregate(new StringBuilder(), (sb, ch) => sb.Append(ch)).ToString()
            .Replace("'", " ").Trim();

    /// <summary>Base key from the languages + word/translation (space→underscore, no double underscores).</summary>
    public static string Generate(string sourceLanguage, string word, string targetLanguage, string translation)
    {
        var src = Slug(sourceLanguage).Replace(' ', '_');
        var tgt = Slug(targetLanguage).Replace(' ', '_');
        var w = Slug(word).Replace(' ', '_');
        var t = Slug(translation).Replace(' ', '_');
        return $"{src}_{w}_{tgt}_{t}".Replace("__", "_").Trim('_');
    }
}
