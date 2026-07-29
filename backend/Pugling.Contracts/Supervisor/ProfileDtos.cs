namespace Pugling.Contracts.Supervisor;

// Vertrag der Stamm- und Profildaten, die der Supervisor pflegt: der eigene Erwachsenen-Datensatz,
// die Lehrbücher des Kindes und sein Stundenplan (beides übungsunabhängiges Profil).

/// <summary>Erwachsener ohne PIN (wird nie ausgeliefert).</summary>
public record AdultResponse(int Id, string Name, string? Email, DateTime CreatedAt, int ChildrenCount);

/// <summary>Eingabe der Registrierung eines Erwachsenen.</summary>
public record CreateAdultDto(string Name, string? Email, string? Pin);

/// <summary>Nur gesetzte Felder werden geändert.</summary>
public record UpdateAdultDto(string? Name, string? Email, string? Pin);

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
/// <summary>
/// Partielle Änderung eines Lehrbuchs; weggelassene Felder bleiben unverändert.
/// <para>
/// <c>null</c> heißt im PATCH „nicht angegeben" und kann darum nichts <b>leeren</b> – dafür stehen die
/// <c>Clear…</c>-Schalter (vgl. <c>ClearGrade</c> an der Klassenarbeit): <c>ClearSeries</c> löst das Buch aus
/// dem Katalog („nicht katalogisiert") und nimmt die aktuelle Unit mit, weil sie ohne ihre Reihe nichts
/// bezeichnet; <c>ClearUnit</c> setzt nur die Unit zurück; <c>ClearSubject</c> entfernt Fach-Id und -Name;
/// <c>ClearGrade</c> die Klassenstufe des Buchs.
/// </para>
/// </summary>
public record UpdateTextbookDto(string? Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter,
    int? SeriesId = null, int? CurrentUnitId = null,
    bool ClearSeries = false, bool ClearUnit = false,
    bool ClearSubject = false, bool ClearGrade = false);

/// <summary>Ein Stundenplan-Eintrag: Fach an einem Wochentag.</summary>
public record EntryResponse(int Id, int ChildId, int SubjectId, string SubjectName, DayOfWeek DayOfWeek, string? TimeOfDay);

/// <summary>Eingabe zum Eintragen eines Fachs an einem Wochentag.</summary>
public record CreateEntryDto(int SubjectId, DayOfWeek DayOfWeek, string? TimeOfDay);
