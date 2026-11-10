using System.Text;
using System.Text.RegularExpressions;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>One gap read from the markup: the answer and the spellings accepted besides it.</summary>
/// <param name="Answer">The expected answer.</param>
/// <param name="Alternatives">Further correct spellings (<c>[didn't|did not]</c>); empty if there are none.</param>
public sealed record ClozeMarkupGap(string Answer, IReadOnlyList<string> Alternatives);

/// <summary>The result of reading cloze markup.</summary>
/// <param name="Text">The running text with <c>{{1}}</c>, <c>{{2}}</c> … in place of the brackets.</param>
/// <param name="Gaps">The gaps in text order; gap <c>n</c> belongs to placeholder <c>{{n+1}}</c>.</param>
/// <param name="Errors">Markup the reader could not turn into a gap (empty or nested brackets, …).</param>
public sealed record ClozeMarkupResult(string Text, IReadOnlyList<ClozeMarkupGap> Gaps, IReadOnlyList<string> Errors);

/// <summary>
/// Turns a cloze text the model wrote with the answers in square brackets (<c>We [went] to the Tower.</c>) into the
/// contract's placeholder form. The model used to number placeholders and gaps itself, and that coupling was where
/// local models failed: a placeholder without its gap, two gaps on one number. Numbering is bookkeeping, so C# does
/// it, and the model only has to mark the word it means.
/// </summary>
public static partial class ClozeMarkup
{
    /// <summary>
    /// Keeps the first <paramref name="maxGaps"/> filled brackets and dissolves the rest into plain text (first
    /// spelling): an over-delivering model keeps a complete, readable text instead of costing a repair round.
    /// </summary>
    public static string Limit(string? markup, int maxGaps)
    {
        var kept = 0;
        return BracketWithHint().Replace(markup ?? "", match =>
        {
            var first = match.Groups[1].Value.Split('|').Select(s => s.Trim()).FirstOrDefault(s => s.Length > 0);
            if (first is null) return match.Value;
            // A dissolved gap takes its base-form hint with it: "took (take)" in running text reads as a typo.
            return ++kept <= maxGaps ? match.Value : first;
        });
    }

    /// <summary>Reads the markup into placeholder text and gaps.</summary>
    public static ClozeMarkupResult Parse(string? markup)
    {
        var errors = new List<string>();
        var gaps = new List<ClozeMarkupGap>();
        var text = new StringBuilder();
        var source = markup ?? "";
        var position = 0;

        foreach (Match match in Bracket().Matches(source))
        {
            text.Append(source, position, match.Index - position);
            position = match.Index + match.Length;

            var spellings = match.Groups[1].Value.Split('|')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (spellings.Count == 0)
            {
                errors.Add($"Die Klammer '{match.Value}' ist leer – in jede Klammer gehört die Lösung.");
                continue;
            }

            gaps.Add(new ClozeMarkupGap(spellings[0], spellings.Skip(1).ToList()));
            text.Append("{{").Append(gaps.Count).Append("}}");
        }

        text.Append(source, position, source.Length - position);
        var rest = text.ToString();
        if (rest.Contains('[') || rest.Contains(']'))
            errors.Add("Der Text enthält eine eckige Klammer ohne Gegenstück – jede Lösung steht in genau einem [ … ].");
        // Placeholders the model wrote itself would survive the conversion unnumbered and render as dead fields.
        if (Bracket().Replace(source, "").Contains("{{"))
            errors.Add("Schreibe keine {{n}}-Platzhalter – markiere jede Lösung nur mit eckigen Klammern: [went].");

        return new ClozeMarkupResult(rest, gaps, errors);
    }

    /// <summary>
    /// Does <paramref name="answer"/> stand in <paramref name="text"/> as a word of its own? A substring check
    /// reported "is" as revealed by "This", and every false report costs a repair round.
    /// </summary>
    public static bool ContainsWord(string text, string answer) =>
        Regex.IsMatch(text, $@"(?<!\w){Regex.Escape(answer.Trim())}(?!\w)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Does a word in <paramref name="text"/> begin with <paramref name="word"/>? For hints, where an inflected form
    /// ("Kathedralen") gives the answer ("Kathedrale") away just as the bare word does.
    /// </summary>
    public static bool ContainsWordStart(string text, string word) =>
        Regex.IsMatch(text, $@"(?<!\w){Regex.Escape(word.Trim())}", RegexOptions.IgnoreCase);

    /// <summary>
    /// The placeholder text without the base-form hints right after a gap (<c>{{1}} (play)</c>). In
    /// <c>They [play] (play) football.</c> the hint equals the answer and the task is still real - the child has to
    /// know that no -s belongs there - so the hint must not count as giving the answer away.
    /// </summary>
    public static string WithoutFormHints(string placeholderText) => FormHint().Replace(placeholderText, "$1");

    /// <summary>
    /// What the child reads besides the gaps: the placeholder text without the base-form hints and without the
    /// placeholders themselves - an answer "3" must not count as revealed by the "{{3}}" of another gap.
    /// </summary>
    public static string ReadableText(string placeholderText) =>
        PlaceholderToken().Replace(WithoutFormHints(placeholderText), " ");

    /// <summary>The base-form hints right after a gap, as (gap number, hint text).</summary>
    public static IEnumerable<(int Gap, string Hint)> FormHints(string placeholderText) =>
        FormHint().Matches(placeholderText)
            .Select(m => (int.Parse(PlaceholderToken().Match(m.Groups[1].Value).Groups[1].Value), m.Groups[2].Value.Trim()));

    [GeneratedRegex(@"\[([^\[\]]*)\]")]
    private static partial Regex Bracket();

    [GeneratedRegex(@"(\{\{\d+\}\})\s*\(([^()]*)\)")]
    private static partial Regex FormHint();

    [GeneratedRegex(@"\{\{(\d+)\}\}")]
    private static partial Regex PlaceholderToken();

    // A bracket plus the base-form hint that may follow it, so that Limit removes both together.
    [GeneratedRegex(@"\[([^\[\]]*)\](?:\s*\([^()]*\))?")]
    private static partial Regex BracketWithHint();
}
