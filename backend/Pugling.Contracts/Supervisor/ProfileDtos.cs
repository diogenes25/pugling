namespace Pugling.Contracts.Supervisor;

// Vertrag der Stamm- und Profildaten, die der Supervisor pflegt: der eigene Erwachsenen-Datensatz,
// die Lehrbücher des Kindes und sein Stundenplan (beides übungsunabhängiges Profil).

/// <summary>Adult without PIN (never delivered).</summary>
public record AdultResponse(int Id, string Name, string? Email, DateTime CreatedAt, int ChildrenCount);

/// <summary>Input for registering an adult.</summary>
public record CreateAdultDto(string Name, string? Email, string? Pin);

/// <summary>Only fields that are set are changed.</summary>
public record UpdateAdultDto(string? Name, string? Email, string? Pin);

/// <summary>
/// A textbook used by the child, together with its current chapter. <c>SeriesId</c>/<c>CurrentUnitId</c>
/// are the cataloged form of title and chapter: only through them does the profile matching find the
/// creator who knows this work.
/// </summary>
public record TextbookResponse(int Id, string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter, DateTime CreatedAt,
    int? SeriesId = null, string? SeriesName = null, int? CurrentUnitId = null, string? CurrentUnitLabel = null);

/// <summary>Input for creating a textbook.</summary>
public record CreateTextbookDto(string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter,
    int? SeriesId = null, int? CurrentUnitId = null);

/// <summary>Partial change to a textbook; omitted fields stay unchanged.</summary>
/// <summary>
/// Partial change to a textbook; omitted fields stay unchanged.
/// <para>
/// In a PATCH, <c>null</c> means "not specified" and therefore cannot <b>clear</b> anything – that is
/// what the <c>Clear…</c> switches are for (cf. <c>ClearGrade</c> on the class test): <c>ClearSeries</c>
/// detaches the book from the catalog ("not cataloged") and takes the current unit with it, because the
/// unit means nothing without its series; <c>ClearUnit</c> only resets the unit; <c>ClearSubject</c>
/// removes the subject id and name; <c>ClearGrade</c> the grade level of the book.
/// </para>
/// </summary>
public record UpdateTextbookDto(string? Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter,
    int? SeriesId = null, int? CurrentUnitId = null,
    bool ClearSeries = false, bool ClearUnit = false,
    bool ClearSubject = false, bool ClearGrade = false);

/// <summary>A timetable entry: subject on a weekday.</summary>
public record EntryResponse(int Id, int ChildId, int SubjectId, string SubjectName, DayOfWeek DayOfWeek, string? TimeOfDay);

/// <summary>Input for entering a subject on a weekday.</summary>
public record CreateEntryDto(int SubjectId, DayOfWeek DayOfWeek, string? TimeOfDay);
