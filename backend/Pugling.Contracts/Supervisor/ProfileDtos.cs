namespace Pugling.Contracts.Supervisor;

// Vertrag der Stamm- und Profildaten, die der Supervisor pflegt: der eigene Vater-Datensatz,
// die Lehrbücher des Kindes und sein Stundenplan (beides übungsunabhängiges Profil).

/// <summary>Vater ohne PIN (wird nie ausgeliefert).</summary>
public record FatherResponse(int Id, string Name, string? Email, DateTime CreatedAt, int ChildrenCount);

/// <summary>Eingabe der Vater-Registrierung.</summary>
public record CreateFatherDto(string Name, string? Email, string? Pin);

/// <summary>Nur gesetzte Felder werden geändert.</summary>
public record UpdateFatherDto(string? Name, string? Email, string? Pin);

/// <summary>Ein vom Kind verwendetes Lehrbuch samt aktuellem Kapitel.</summary>
public record TextbookResponse(int Id, string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen eines Lehrbuchs.</summary>
public record CreateTextbookDto(string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter);

/// <summary>Partielle Änderung eines Lehrbuchs; weggelassene Felder bleiben unverändert.</summary>
public record UpdateTextbookDto(string? Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter);

/// <summary>Ein Stundenplan-Eintrag: Fach an einem Wochentag.</summary>
public record EntryResponse(int Id, int ChildId, int SubjectId, string SubjectName, DayOfWeek DayOfWeek, string? TimeOfDay);

/// <summary>Eingabe zum Eintragen eines Fachs an einem Wochentag.</summary>
public record CreateEntryDto(int SubjectId, DayOfWeek DayOfWeek, string? TimeOfDay);
