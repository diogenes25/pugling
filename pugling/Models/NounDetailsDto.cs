using pugling.Models.Constants;

namespace pugling.Models;

/// <summary>
/// Contains specific details for nouns, including the grammatical gender,
/// the determined article, and the undetermined article.
/// </summary>
/// <example>
/// {
///    "genus": "maskulin",
///    "determinedArticle": "der",
///    "undeterminedArticle": "ein"
/// }
/// </example>
public record NounDetailsDto : INounDetails
{
    /// <summary>
    /// The grammatical gender of the noun (e.g., "maskulin", "feminin", "neutrum" in German; "maschile", "femminile" in Italian).
    /// </summary>
    /// <example>
    /// "feminin"
    /// </example>
    public EGenus Genus { get; init; }

    /// <summary>
    /// The determined article of the noun (e.g., "der", "die", "das"). Null if the language doesn't have determined articles or if not applicable.
    /// </summary>
    /// <example>
    /// "die"
    /// </example>
    public string? DeterminedArticle { get; init; }

    /// <summary>
    /// The undetermined article of the noun (e.g., "ein", "eine"). Null if the language doesn't have undetermined articles or if not applicable.
    /// </summary>
    /// <example>
    /// "eine"
    /// </example>
    public string? UndeterminedArticle { get; init; }
}