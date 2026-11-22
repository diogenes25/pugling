using System.Text.RegularExpressions;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The single meanings a word-list entry stands for (B-462). A textbook word list packs several meanings and
/// optional parts into one entry - "sobald, sowie", "antworten (auf); erwidern, entgegnen", "Aha! / Verstehe.",
/// "the best thing about ..." - and the store keeps the entry as the book prints it. A child who types one of
/// the meanings knows the word; asking for the whole entry with its punctuation measures copying, not vocabulary.
/// Only applied where <see cref="ContentItem.MeaningsSeparable"/> says so: in a sentence (translation, cloze) a
/// comma is part of the one answer.
/// </summary>
public static partial class AnswerVariants
{
    // Past this many forms of one meaning only "all kept" and "all dropped" are built - they multiply per bracket.
    private const int MaxForms = 16;

    // A closing dot after one of these is an abbreviation of the word list, not the end of a sentence.
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "sb", "sth", "so", "jd", "jdm", "jdn", "jds", "etw", "usw", "etc",
    };

    // B-626: the word list's slots for a person or thing ("sth.", "qc", "jdm."). Not the list above: "so" is an
    // abbreviation there but a real word here, and a slot may be dropped from an answer only as a whole word.
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "sth", "sb", "qn", "qc", "jdn", "jdm", "etw",
    };

    /// <summary>
    /// Whether a whitespace-delimited token of a word-list entry is only slots: "sth.", "qc", "qn/qc", "jdn./jdm.".
    /// </summary>
    public static bool IsPlaceholder(string token)
    {
        var parts = token.TrimEnd(',', ';').Split('/');
        return parts.All(p => Placeholders.Contains(p.TrimEnd('.')));
    }

    /// <summary>
    /// Which characters of an entry are accessories the child need not type (B-626): bracketed parts including their
    /// brackets, and slot words outside brackets. The letter-box mask and the comparison share this knowledge, so a
    /// card never shows a fixed "sth." that the grader demands, nor a box for one it forgives.
    /// </summary>
    public static bool[] AccessoryMask(string entry)
    {
        var mask = new bool[entry.Length];
        var depth = 0;
        for (var i = 0; i < entry.Length; i++)
        {
            if (entry[i] == '(') depth++;
            if (depth > 0) mask[i] = true;
            if (entry[i] == ')' && depth > 0) depth--;
        }
        for (var i = 0; i < entry.Length;)
        {
            if (char.IsWhiteSpace(entry[i])) { i++; continue; }
            var start = i;
            while (i < entry.Length && !char.IsWhiteSpace(entry[i])) i++;
            if (!mask[start..i].Any(m => m) && IsPlaceholder(entry[start..i]))
                Array.Fill(mask, true, start, i - start);
        }
        return mask;
    }

    /// <summary>
    /// Every answer that counts for <paramref name="expected"/>, raw (the grader normalizes): the entry itself,
    /// each meaning between <c>;</c>, a spaced <c>/</c> and <c>,</c>, each with its bracketed parts kept or left
    /// out, without ellipsis and closing punctuation.
    /// </summary>
    public static IReadOnlyList<string> Of(string expected)
    {
        var variants = new List<string> { expected };
        foreach (var meaning in Meanings(expected))
        {
            foreach (var form in OptionalParts(meaning))
            {
                // The comma goes too: what is left of "Und überhaupt, ..." is the phrase, not the phrase plus a comma.
                var bare = Collapse(form.Replace("...", " ").Replace("…", " ")).TrimEnd('.', '!', '?', ',', ' ');
                if (bare.Length > 0 && !variants.Contains(bare)) variants.Add(bare);
                // B-626: the slots may be left out as a whole ("marcher sur qc" -> "marcher sur").
                var core = Collapse(string.Join(' ', bare.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !IsPlaceholder(t))));
                if (core.Length > 0 && !variants.Contains(core)) variants.Add(core);
            }
        }
        return variants;
    }

    private static IEnumerable<string> Meanings(string expected)
    {
        yield return expected;
        foreach (var segment in SplitOutsideBrackets(expected, IsSegmentSeparator))
        {
            yield return segment;
            if (IsOnePhrase(segment)) continue;
            foreach (var meaning in SplitOutsideBrackets(segment, (s, i) => s[i] == ',')) yield return meaning;
        }
    }

    // Where a comma separates nothing: a sentence ("Ja, bitte."), and a pattern with a slot ("Es war schade,
    // dass ...") - half of it is no meaning, "dass" must not count for it.
    private static bool IsOnePhrase(string segment)
    {
        if (segment.Contains("...") || segment.Contains('…')) return true;
        var closing = segment.TrimEnd();
        if (closing.EndsWith('!') || closing.EndsWith('?')) return true;
        if (!closing.EndsWith('.')) return false;
        // Any whitespace, like the separator below (B-521): "Gebäude" + NBSP + "usw." ends on the abbreviation too.
        var lastWord = Regex.Split(closing[..^1], @"[\s,()]").LastOrDefault() ?? "";
        return !Abbreviations.TryGetValue(lastWord, out _);
    }

    // Any whitespace around the slash (char.IsWhiteSpace - a U+FEFF does not count, unlike in Normalize): "Aha!" + NBSP + "/ Verstehe." is two meanings too (B-501).
    private static bool IsSegmentSeparator(string s, int i) =>
        s[i] == ';' || (s[i] == '/' && i > 0 && i < s.Length - 1 && char.IsWhiteSpace(s[i - 1]) && char.IsWhiteSpace(s[i + 1]));

    // A separator inside brackets belongs to the bracket: "an sein (Radio, Licht usw.)" is one meaning.
    private static List<string> SplitOutsideBrackets(string s, Func<string, int, bool> isSeparator)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')' && depth > 0) depth--;
            else if (depth == 0 && isSeparator(s, i))
            {
                parts.Add(s[start..i]);
                start = i + 1;
            }
        }
        parts.Add(s[start..]);
        return parts;
    }

    // "(to) reply (to)" -> to reply to, reply to, to reply, reply. A slash inside the bracket lists choices:
    // "stay (at/with)" -> stay, stay at, stay with.
    private static IEnumerable<string> OptionalParts(string meaning)
    {
        var groups = Bracketed().Matches(meaning);
        if (groups.Count == 0) return [meaning];

        // Per bracket: left out, kept whole, or one of its slash-separated choices.
        var choices = groups.Select(g => (string[])[" ", g.Groups[1].Value, .. g.Groups[1].Value.Split('/')])
            .Select(c => c.Distinct().ToArray()).ToList();
        if (choices.Aggregate(1L, (n, c) => n * c.Length) > MaxForms)
            return [Bracketed().Replace(meaning, "$1"), Bracketed().Replace(meaning, " ")];

        IEnumerable<string[]> picks = [[]];
        foreach (var options in choices)
            picks = [.. picks.SelectMany(p => options.Select(o => (string[])[.. p, o]))];
        return picks.Select(pick =>
        {
            var index = 0;
            return Bracketed().Replace(meaning, _ => pick[index++]);
        });
    }

    private static string Collapse(string s) =>
        string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"\(([^()]*)\)")]
    private static partial Regex Bracketed();
}
