using System.Globalization;
using System.Text;

namespace Pugling.Contracts;

/// <summary>
/// Generates stable, unique vocabulary keys following the pattern <c>{src}_{word}_{tgt}_{translation}</c>.
/// Central, so that the seed and the vocabulary store use the same slug (the "simple" input works without a
/// hand-typed key – the server generates it). The key is only a stable reference: its separator is ambiguous, so the
/// server's notion of "the same word" is <see cref="Identity"/>, and a client that checks a draft beforehand (the
/// creator agent) folds with <see cref="Part"/> (B-336, B-346). Both live in the contract for that reason.
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

    /// <summary>
    /// The server's notion of "the same word": the four parts folded one by one like <see cref="Generate"/> folds them
    /// (<see cref="Slug"/>, underscores and runs of whitespace as one space). Not the joined key, because its separator
    /// is ambiguous: "pomme de terre" → "Kartoffel" and "pomme" → "terre de Kartoffel" join to the same key (B-346).
    /// </summary>
    public static VocabIdentity Identity(string sourceLanguage, string word, string targetLanguage, string translation) =>
        new(Part(sourceLanguage), Part(word), Part(targetLanguage), Part(translation));

    /// <summary>One part of <see cref="Identity"/>, e.g. a word on its own.</summary>
    public static string Part(string s) =>
        string.Join(' ', Slug(s).Replace('_', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>The folded parts of a word/translation pair; equal values name the same word (<see cref="VocabKey.Identity"/>).</summary>
public readonly record struct VocabIdentity(string SourceLanguage, string Word, string TargetLanguage, string Translation);
