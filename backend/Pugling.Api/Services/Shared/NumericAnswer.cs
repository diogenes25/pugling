using System.Globalization;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The one rule for reading a typed number (B-639), shared by the catalog check and by playing in a position so that a
/// task right in the father's test mode is right for the child too. Point and comma are both decimal separators and
/// there is no thousands separator: school answers never use one, and "2,5" must not read as 25.
/// </summary>
public static class NumericAnswer
{
    private const NumberStyles Style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>Reads a typed number; <c>false</c> for anything that is not exactly one number.</summary>
    public static bool TryParse(string? given, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(given)) return false;
        // A tablet keyboard may type the typographic minus (U+2212); it means the same sign.
        // A trailing full stop ends the sentence, not the number - the text comparison forgave it too (Normalize).
        var text = given.Trim().TrimEnd('.').Replace('−', '-').Replace(',', '.');
        return decimal.TryParse(text, Style, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Is the typed answer the expected number, within the task's tolerance (inclusive)?</summary>
    public static bool Matches(string? given, decimal expected, decimal tolerance) =>
        TryParse(given, out var value) && Math.Abs(value - expected) <= tolerance;

    /// <summary>The only separator between the numbers of a set: the comma is a decimal separator (B-639), so "2,4" is one number.</summary>
    public const char SetSeparator = ';';

    /// <summary>The fixed text for the empty set (B-710). Product text, compared like every typed answer.</summary>
    public const string NoSolution = "keine Lösung";

    /// <summary>
    /// Is the typed answer the expected solution set (B-710)? Complete and nothing wrong: the numbers are separated by
    /// <see cref="SetSeparator"/>, may come in any order, and are compared pairwise after sorting, each within the
    /// tolerance. A doubled number is a second element, not an emphasis ("3;3" is not {3}). The empty set is only
    /// <see cref="NoSolution"/> - an empty input is never right.
    /// </summary>
    public static bool MatchesSet(string? given, IReadOnlyList<decimal> expected, decimal tolerance)
    {
        if (string.IsNullOrWhiteSpace(given)) return false;
        if (StageMechanics.Normalize(given) == StageMechanics.Normalize(NoSolution)) return expected.Count == 0;
        var parts = given.Split(SetSeparator);
        if (parts.Length != expected.Count) return false;
        var values = new List<decimal>(parts.Length);
        foreach (var part in parts)
        {
            if (!TryParse(part, out var value)) return false;
            values.Add(value);
        }
        values.Sort();
        var sorted = expected.Order().ToList();
        return values.Zip(sorted).All(p => Math.Abs(p.First - p.Second) <= tolerance);
    }
}

/// <summary>The number a content atom expects and the leeway it grants (B-639); set only on number tasks.</summary>
public record NumericTarget(decimal Value, decimal Tolerance);

/// <summary>
/// The solution set a content atom expects and the leeway per element (B-710); set only on equation tasks. Empty
/// <paramref name="Values"/> is the empty set, answered with <see cref="NumericAnswer.NoSolution"/>.
/// </summary>
public record NumericSetTarget(IReadOnlyList<decimal> Values, decimal Tolerance);
