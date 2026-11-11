namespace Pugling.Api.Services.Shared;

/// <summary>
/// Grades the answers submitted by the child against the stored solution – the one shared
/// comparison rule for the final tests (vocabulary/matching/cloze) and the Leitner practice loop
/// (<c>/review</c>). Stateless and without DB access; text comparisons go through
/// <see cref="StageMechanics.Normalize"/> (case and repeated whitespace do not matter).
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

    /// <summary>A gap: not empty and matches the solution or a stored alternative.</summary>
    public bool MatchesGap(Gap gap, string? given)
    {
        var g = StageMechanics.Normalize(given);
        return g.Length > 0 && (g == StageMechanics.Normalize(gap.Answer)
            || (gap.Alternatives?.Any(a => StageMechanics.Normalize(a) == g) ?? false));
    }
}
