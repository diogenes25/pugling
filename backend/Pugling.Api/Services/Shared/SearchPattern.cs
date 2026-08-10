namespace Pugling.Api.Services.Shared;

/// <summary>
/// Builds the <c>LIKE</c> pattern behind every free-text catalog search.
/// <para>
/// <b>Why not <c>string.Contains</c>.</b> EF maps it to SQLite's <c>instr()</c>, and that function is
/// byte-exact: it ignores the column collation completely. Measured in B-128 - with <c>NOCASE</c> on the
/// column in place, three of four search cases still failed. A collation fixes <em>equality</em>
/// comparisons (that is what it is there for - see the duplicate check on <c>TextbookSeries.Name</c>) and
/// makes <c>ORDER BY</c> on the column case-insensitive, but never a substring search.
/// SQLite's <c>LIKE</c> on the other hand folds case by default, which is exactly what a search box needs.
/// </para>
/// <para>
/// <b>The limit that comes with it, so nobody rediscovers it.</b> The built-in <c>LIKE</c> folds ASCII
/// only: "STRASSE" still does not find "Straße", and "Ä" does not find "ä". Closing that would mean
/// shipping the ICU extension - too much for a catalog of publisher and series names, and a decision the
/// next person should make knowingly rather than by accident.
/// </para>
/// </summary>
public static class SearchPattern
{
    /// <summary>Escape character for wildcards a user may legitimately type into a search box.</summary>
    public const string Escape = "\\";

    /// <summary>
    /// Wraps the term in wildcards and neutralizes the ones inside it - otherwise a search for "50%"
    /// would silently match everything.
    /// </summary>
    public static string Contains(string term) =>
        "%" + term
            .Replace(Escape, Escape + Escape, StringComparison.Ordinal)
            .Replace("%", Escape + "%", StringComparison.Ordinal)
            .Replace("_", Escape + "_", StringComparison.Ordinal)
        + "%";
}
