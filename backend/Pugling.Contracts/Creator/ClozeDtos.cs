namespace Pugling.Contracts.Creator;

// Contract of the cloze building blocks (ClozeText) in the authoring catalog. The gaps themselves (Gap)
// are a shared base type and live in the root namespace of the contract project.

/// <summary>A cloze building block of the catalog.</summary>
public record ClozeResponse(int Id, string Key, string Title, string SourceLanguage, string TargetLanguage,
    string Text, string? Translation, IReadOnlyList<Gap> Gaps, IReadOnlyList<string>? WordBank, DateTime CreatedAt);

/// <summary>Input for creating a cloze. <c>Key</c> must be unique; at least one gap.</summary>
public record CreateClozeDto(string Key, string Title, string SourceLanguage, string TargetLanguage,
    string Text, List<Gap> Gaps, string? Translation = null, List<string>? WordBank = null);

/// <summary>
/// Partial change to a cloze: <c>null</c> means "not specified" (the value remains).
/// The two optional contents are therefore cleared via <see cref="ClearTranslation"/> resp.
/// <see cref="ClearWordBank"/> – a field cleared in a form would arrive as <c>null</c> and would
/// otherwise be indistinguishable from "unchanged".
/// </summary>
public record UpdateClozeDto(string? Title, string? Text, string? Translation, List<Gap>? Gaps,
    List<string>? WordBank, bool ClearTranslation = false, bool ClearWordBank = false);
