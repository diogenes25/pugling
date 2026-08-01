namespace Pugling.Contracts.Creator;

// Contract of the textbook series (route api/v1/creator/textbook-series): the series itself and its units.
// Child-neutral like the rest of the catalog - read by every creator, changed only by the owner.

/// <summary>
/// A textbook series ("Access") including its own permission view. <c>Slug</c> is the normalized, globally
/// unique and immutable key of the series, <c>IsOwn</c> says whether the calling account may
/// change it, <c>UnitCount</c> counts the stored units across all volumes.
/// </summary>
public record TextbookSeriesResponse(int Id, string Name, string Slug, string? Publisher,
    string? SubjectName, int? SubjectId, SchoolTypes SchoolTypes, string? SourceLanguage,
    string? TargetLanguage, string? Notes, int? OwnerAdultId, bool IsOwn, int UnitCount, DateTime CreatedAt);

/// <summary>
/// Input for creating a series. The slug is derived from the name; an already existing series
/// is returned instead of duplicated (idempotent).
/// </summary>
public record CreateTextbookSeriesDto(string Name, string? Publisher, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, string? SourceLanguage, string? TargetLanguage, string? Notes);

/// <summary>Partial change to a series; omitted fields remain unchanged. The slug stays fixed.</summary>
public record UpdateTextbookSeriesDto(string? Name, string? Publisher, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, string? SourceLanguage, string? TargetLanguage, string? Notes);

/// <summary>
/// A unit of the series including volume. <c>Topics</c>, <c>Grammar</c> and <c>VocabularyNotes</c> are the
/// material an AI creator must know so as not to invent the unit.
/// </summary>
public record SeriesUnitResponse(int Id, int SeriesId, int? Grade, int OrderIndex, string Label,
    string? Topics, string? Grammar, string? VocabularyNotes, DateTime CreatedAt);

/// <summary>Input for creating a unit; without <c>OrderIndex</c> it is appended at the end.</summary>
public record CreateSeriesUnitDto(string Label, int? Grade, int? OrderIndex,
    string? Topics, string? Grammar, string? VocabularyNotes);

/// <summary>Partial change to a unit; omitted fields remain unchanged.</summary>
public record UpdateSeriesUnitDto(string? Label, int? Grade, int? OrderIndex,
    string? Topics, string? Grammar, string? VocabularyNotes);
