namespace Pugling.Contracts.Creator;

// Vertrag der beiden Tag-Ebenen:
//   * kind-skopierte Tags an Übungen UND Vokabeln (Tag/TagResponse) – Vater und Sohn dürfen taggen,
//   * kindneutrale Vokabel-Tags am Store (VocabTagResponse).

/// <summary>Tag in der Antwort inkl. Anzahl markierter Übungen und Vokabeln.</summary>
public record TagResponse(int Id, int ChildId, string Name, string? Color, TaggedBy CreatedBy,
    int ExerciseCount, int VocabularyCount, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen eines kind-skopierten Tags.</summary>
public record CreateTagDto(int ChildId, string Name, string? Color);

/// <summary>Partielle Änderung eines Tags; leere Felder bleiben unverändert.</summary>
public record UpdateTagDto(string? Name, string? Color);

/// <summary>Eingabe zum Markieren von Übungen mit einem Tag.</summary>
public record TagExercisesDto(List<int> ExerciseIds);

/// <summary>Schlanke Vokabel-Sicht für die Tag-Zuordnung (ohne die kindneutralen Store-Details).</summary>
public record TaggedVocabularyDto(int Id, string Key, string Word, string Translation);

/// <summary>Eingabe zum Markieren von Vokabeln mit einem kind-skopierten Tag.</summary>
public record TagVocabularyDto(List<int> VocabularyIds);

/// <summary>Kindneutraler Vokabel-Tag inkl. Anzahl verknüpfter Vokabeln.</summary>
public record VocabTagResponse(int Id, string Name, string? Color, int VocabCount, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen eines kindneutralen Vokabel-Tags.</summary>
public record CreateVocabTagDto(string Name, string? Color);

/// <summary>Partielle Änderung eines Vokabel-Tags; leere Felder bleiben unverändert.</summary>
public record UpdateVocabTagDto(string? Name, string? Color);

/// <summary>Eingabe zum Verknüpfen einer Vokabel mit Tags (create-if-missing über die Namen).</summary>
public record TagVocabDto(List<string> Tags);
