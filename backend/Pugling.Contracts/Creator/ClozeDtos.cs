namespace Pugling.Contracts.Creator;

// Vertrag der Lückentext-Bausteine (ClozeText) im Autoren-Katalog. Die Lücken selbst (Gap)
// sind ein geteilter Basistyp und liegen im Wurzel-Namespace des Vertrags-Projekts.

/// <summary>Ein Lückentext-Baustein des Katalogs.</summary>
public record ClozeResponse(int Id, string Key, string Title, string SourceLanguage, string TargetLanguage,
    string Text, string? Translation, IReadOnlyList<Gap> Gaps, IReadOnlyList<string>? WordBank, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen eines Lückentexts. <c>Key</c> muss eindeutig sein; mind. eine Lücke.</summary>
public record CreateClozeDto(string Key, string Title, string SourceLanguage, string TargetLanguage,
    string Text, List<Gap> Gaps, string? Translation = null, List<string>? WordBank = null);

/// <summary>
/// Partielle Änderung eines Lückentexts: <c>null</c> heißt „nicht angegeben" (der Wert bleibt).
/// Die beiden optionalen Inhalte leert man darum über <see cref="ClearTranslation"/> bzw.
/// <see cref="ClearWordBank"/> – ein im Formular geräumtes Feld käme als <c>null</c> an und wäre
/// sonst nicht von „unverändert" zu unterscheiden.
/// </summary>
public record UpdateClozeDto(string? Title, string? Text, string? Translation, List<Gap>? Gaps,
    List<string>? WordBank, bool ClearTranslation = false, bool ClearWordBank = false);
