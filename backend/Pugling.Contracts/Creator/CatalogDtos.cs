namespace Pugling.Contracts.Creator;

// Vertrag des Autoren-Katalogs: Fach → Kapitel → (Übungs-)Kategorie.
// Reine Transportformen ohne Entity-Bezug; die Projektion aus den Entities bleibt in der API.

/// <summary>Ein Schulfach im gemeinsamen Katalog.</summary>
public record SubjectResponse(int Id, string Name, DateTime CreatedAt, int ChaptersCount);

/// <summary>Eingabe zum Anlegen eines Fachs.</summary>
public record CreateSubjectDto(string Name);

/// <summary>Partielle Änderung eines Fachs; leere Felder bleiben unverändert.</summary>
public record UpdateSubjectDto(string? Name);

/// <summary>Ein Kapitel innerhalb eines Fachs.</summary>
public record ChapterResponse(int Id, int SubjectId, string Name, int OrderIndex, int ExercisesCount);

/// <summary>Eingabe zum Anlegen eines Kapitels.</summary>
public record CreateChapterDto(string Name, int OrderIndex);

/// <summary>Partielle Änderung eines Kapitels; leere Felder bleiben unverändert.</summary>
public record UpdateChapterDto(string? Name, int? OrderIndex);

/// <summary>Eine Übungs-Kategorie innerhalb eines Fachs.</summary>
public record CategoryResponse(int Id, int SubjectId, string Name, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen einer Kategorie.</summary>
public record CreateCategoryDto(string Name);

/// <summary>Partielle Änderung einer Kategorie; leere Felder bleiben unverändert.</summary>
public record UpdateCategoryDto(string? Name);
