namespace Pugling.Api.Models;

/// <summary>
/// Builds the HATEOAS self link to a vocabulary store entry. One place for the path, so that all exercise
/// types return the same <c>_self</c>. The path is stable until publication (v1); deliberately a string
/// (no <c>LinkGenerator</c>) because the link is derivable from the ID alone.
/// </summary>
public static class VocabLink
{
    /// <summary>Base path of the vocabulary store entry.</summary>
    public const string Path = "/api/v1/creator/vocabulary/";

    /// <summary>Self link for the ID; <c>null</c> for missing/unknown IDs (0 = legacy reference without a resolved ID).</summary>
    public static string? Self(int? id) => id is null or 0 ? null : Path + id;
}
