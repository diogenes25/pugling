namespace Pugling.Api.Services.Shared;

/// <summary>
/// Grades the answers submitted by the child against the stored solution – the one shared
/// comparison rule for the final tests (vocabulary/matching/cloze) and the Leitner practice loop
/// (<c>/review</c>). Stateless and without DB access; text comparisons go through
/// <see cref="StageMechanics.Normalize"/> (case and repeated whitespace do not matter).
/// </summary>
public class AnswerGrader
{
    /// <summary>Text answer (vocabulary/matching) against the expected solution. An empty input is never considered correct.</summary>
    public bool Matches(string? given, string expected)
    {
        var g = StageMechanics.Normalize(given);
        return g.Length > 0 && g == StageMechanics.Normalize(expected);
    }

    /// <summary>A gap: not empty and matches the solution or a stored alternative.</summary>
    public bool MatchesGap(Gap gap, string? given)
    {
        var g = StageMechanics.Normalize(given);
        return g.Length > 0 && (g == StageMechanics.Normalize(gap.Answer)
            || (gap.Alternatives?.Any(a => StageMechanics.Normalize(a) == g) ?? false));
    }
}
