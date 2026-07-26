namespace Pugling.Contracts.Supervisor;

// Vertrag der Stamm- und Profildaten, die der Supervisor pflegt: der eigene Vater-Datensatz,
// die Lehrbücher des Kindes und sein Stundenplan (beides übungsunabhängiges Profil).

/// <summary>Vater ohne PIN (wird nie ausgeliefert).</summary>
public record FatherResponse(int Id, string Name, string? Email, DateTime CreatedAt, int ChildrenCount);

/// <summary>Eingabe der Vater-Registrierung.</summary>
public record CreateFatherDto(string Name, string? Email, string? Pin);

/// <summary>Nur gesetzte Felder werden geändert.</summary>
public record UpdateFatherDto(string? Name, string? Email, string? Pin);

/// <summary>
/// Ein vom Kind verwendetes Lehrbuch samt aktuellem Kapitel. <c>SeriesId</c>/<c>CurrentUnitId</c> sind
/// die katalogisierte Form von Titel und Kapitel: erst über sie findet das Profil-Matching den Creator,
/// der dieses Werk kennt.
/// </summary>
public record TextbookResponse(int Id, string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter, DateTime CreatedAt,
    int? SeriesId = null, string? SeriesName = null, int? CurrentUnitId = null, string? CurrentUnitLabel = null);

/// <summary>Eingabe zum Anlegen eines Lehrbuchs.</summary>
public record CreateTextbookDto(string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter,
    int? SeriesId = null, int? CurrentUnitId = null);

/// <summary>Partielle Änderung eines Lehrbuchs; weggelassene Felder bleiben unverändert.</summary>
/// <param name="ClearSeries">
/// Löst das Buch aus dem Katalog („nicht katalogisiert"); die aktuelle Unit fällt mit weg, weil sie ohne
/// ihre Reihe nichts bezeichnet. Nötig, weil <c>null</c> im PATCH „nicht angegeben" heißt (vgl. <c>ClearGrade</c>).
/// </param>
/// <param name="ClearUnit">Setzt nur die aktuelle Unit zurück; die Reihe bleibt.</param>
/// <param name="ClearSubject">Entfernt die Fach-Zuordnung (Id und Name).</param>
/// <param name="ClearGrade">Entfernt die Klassenstufe des Buchs.</param>
public record UpdateTextbookDto(string? Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter,
    int? SeriesId = null, int? CurrentUnitId = null,
    bool ClearSeries = false, bool ClearUnit = false,
    bool ClearSubject = false, bool ClearGrade = false);

/// <summary>Ein Stundenplan-Eintrag: Fach an einem Wochentag.</summary>
public record EntryResponse(int Id, int ChildId, int SubjectId, string SubjectName, DayOfWeek DayOfWeek, string? TimeOfDay);

/// <summary>Eingabe zum Eintragen eines Fachs an einem Wochentag.</summary>
public record CreateEntryDto(int SubjectId, DayOfWeek DayOfWeek, string? TimeOfDay);
