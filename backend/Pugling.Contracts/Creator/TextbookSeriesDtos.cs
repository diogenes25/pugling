namespace Pugling.Contracts.Creator;

// Vertrag der Lehrwerk-Reihen (Route api/v1/creator/textbook-series): die Reihe selbst und ihre Units.
// Kindneutral wie der übrige Katalog – gelesen von jedem Creator, geändert nur vom Owner.

/// <summary>
/// Eine Lehrwerk-Reihe („Access") samt eigener Rechte-Sicht. <c>Slug</c> ist der normalisierte, global
/// eindeutige und unveränderliche Schlüssel der Reihe, <c>IsOwn</c> sagt, ob das aufrufende Konto sie
/// ändern darf, <c>UnitCount</c> zählt die hinterlegten Units über alle Bände.
/// </summary>
public record TextbookSeriesResponse(int Id, string Name, string Slug, string? Publisher,
    string? SubjectName, int? SubjectId, SchoolTypes SchoolTypes, string? SourceLanguage,
    string? TargetLanguage, string? Notes, int? OwnerAdultId, bool IsOwn, int UnitCount, DateTime CreatedAt);

/// <summary>
/// Eingabe zum Anlegen einer Reihe. Der Slug entsteht aus dem Namen; eine bereits vorhandene Reihe
/// wird zurückgegeben statt gedoppelt (idempotent).
/// </summary>
public record CreateTextbookSeriesDto(string Name, string? Publisher, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, string? SourceLanguage, string? TargetLanguage, string? Notes);

/// <summary>Partielle Änderung einer Reihe; weggelassene Felder bleiben unverändert. Der Slug bleibt fest.</summary>
public record UpdateTextbookSeriesDto(string? Name, string? Publisher, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, string? SourceLanguage, string? TargetLanguage, string? Notes);

/// <summary>
/// Eine Unit der Reihe inklusive Band. <c>Topics</c>, <c>Grammar</c> und <c>VocabularyNotes</c> sind der
/// Stoff, den ein KI-Creator kennen muss, um die Unit nicht zu erfinden.
/// </summary>
public record SeriesUnitResponse(int Id, int SeriesId, int? Grade, int OrderIndex, string Label,
    string? Topics, string? Grammar, string? VocabularyNotes, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen einer Unit; ohne <c>OrderIndex</c> wird hinten angehängt.</summary>
public record CreateSeriesUnitDto(string Label, int? Grade, int? OrderIndex,
    string? Topics, string? Grammar, string? VocabularyNotes);

/// <summary>Partielle Änderung einer Unit; weggelassene Felder bleiben unverändert.</summary>
public record UpdateSeriesUnitDto(string? Label, int? Grade, int? OrderIndex,
    string? Topics, string? Grammar, string? VocabularyNotes);
