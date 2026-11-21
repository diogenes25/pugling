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
}

/// <summary>The number a content atom expects and the leeway it grants (B-639); set only on number tasks.</summary>
public record NumericTarget(decimal Value, decimal Tolerance);
