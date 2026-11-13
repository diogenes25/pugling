namespace Pugling.Api.Services.Shared;

/// <summary>
/// Grades the answers submitted by the child against the stored solution – the one shared
/// comparison rule for the final tests (vocabulary/matching/cloze) and the Leitner practice loop
/// (<c>/review</c>). Stateless and without DB access; text comparisons go through
/// <see cref="StageMechanics.Normalize"/> (what it forgives is listed there).
/// </summary>
public class AnswerGrader
{
    /// <summary>
    /// A typed answer against a content atom: its solution or any accepted alternative, each with the atom's article
    /// tolerance (<see cref="ContentItem.ArticleOptionalIn"/>) and, for a word-list entry, each single meaning of it
    /// (<see cref="ContentItem.MeaningsSeparable"/>). The one entry point for every place that grades an atom
    /// (practice, test, preview, set matching), so the rule cannot differ between practice and test.
    /// </summary>
    public bool Matches(string? given, ContentItem item) =>
        item.GradedAnswers().Any(a => Matches(given, a, item.ArticleOptionalIn));

    /// <summary>
    /// Text answer against one expected solution. An empty input is never correct. With
    /// <paramref name="articleOptionalIn"/> (the language of the expected answer, B-421) a leading article of that
    /// language may be left out - the meter is the word, not typing discipline. Only leaving it out is forgiven: an
    /// added or a different article is still wrong.
    /// </summary>
    public bool Matches(string? given, string expected, string? articleOptionalIn)
    {
        var g = StageMechanics.Normalize(given);
        if (g.Length == 0) return false;
        var e = StageMechanics.Normalize(expected);
        return g == e
            || (articleOptionalIn is { } language && StageMechanics.WithoutLeadingArticle(e, language) == g);
    }

    /// <summary>
    /// Is a wrong typed answer the right word, misspelled (B-524)? Display only: the grading stays binary, because a
    /// misspelled word is not known yet - the category tells the supervisor whether the child lacks words or spelling.
    /// <para>
    /// Close enough means an edit distance (adjacent swaps count as one) of at most 1 for solutions up to five
    /// letters and at most 2 above. And no solution of another atom of the same exercise may be at least as close:
    /// "racist" in the gap for "racism" is one letter off, but it is the neighbouring word, not a spelling slip.
    /// </para>
    /// </summary>
    /// <param name="given">The child's answer.</param>
    /// <param name="item">The atom the answer was given for.</param>
    /// <param name="siblings">All atoms of the exercise; <paramref name="item"/> may be among them.</param>
    public bool IsMisspelled(string? given, ContentItem item, IReadOnlyList<ContentItem> siblings)
    {
        var g = StageMechanics.Normalize(given);
        if (g.Length == 0 || Matches(given, item)) return false;
        var own = Spellings(item, item).ToHashSet();
        if (own.Count == 0) return false;
        var nearest = own.Min(a => Distance(g, a));
        if (!own.Any(a => Distance(g, a) == nearest && nearest <= (a.Length <= 5 ? 1 : 2))) return false;
        // Another atom's solution that is just as close makes the answer a different word, not a spelling slip.
        // Solutions shared with the asked atom are its own and do not compete.
        return !siblings.Where(s => s.Index != item.Index)
            .SelectMany(s => Spellings(s, item))
            .Where(a => !own.Contains(a))
            .Any(a => Distance(g, a) <= nearest);
    }

    // The normalized solutions of an atom, each also without its leading article where the asked atom forgives one -
    // the same tolerance Matches grants: "chein" for "le chien" is measured against "chien", not against the article.
    private static IEnumerable<string> Spellings(ContentItem atom, ContentItem asked)
    {
        foreach (var a in atom.GradedAnswers().Select(StageMechanics.Normalize).Where(a => a.Length > 0))
        {
            yield return a;
            if (asked.ArticleOptionalIn is { } language && StageMechanics.WithoutLeadingArticle(a, language) is { } bare)
                yield return bare;
        }
    }

    // Optimal string alignment distance: insertions, deletions, substitutions and swaps of two adjacent letters each
    // cost one - "heiht"/"height" is one, and so is the classic swap "recieve"/"receive".
    private static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }
        return d[a.Length, b.Length];
    }

    /// <summary>A gap: not empty and matches the solution or a stored alternative.</summary>
    public bool MatchesGap(Gap gap, string? given)
    {
        var g = StageMechanics.Normalize(given);
        return g.Length > 0 && (g == StageMechanics.Normalize(ClozeGaps.SolutionOf(gap, gap.Answer))
            || (gap.Alternatives?.Any(a => StageMechanics.Normalize(a) == g) ?? false));
    }
}
