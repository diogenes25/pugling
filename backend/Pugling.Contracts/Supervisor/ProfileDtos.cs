namespace Pugling.Contracts.Supervisor;

// Contract of the master and profile data the supervisor maintains: their own adult record, the child's
// textbooks and its timetable (both exercise-independent profile).

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
/// <para>
/// <c>Publisher</c> and <c>Grade</c> come from the catalog once the book is linked: the series' publisher
/// while <c>SeriesId</c> is set, the unit's grade while <c>CurrentUnitId</c> is set. Without a catalog
/// value (series without publisher, unit without grade) or after the link is cleared, the free text
/// stored on the book shows again – it is kept, never overwritten by the catalog.
/// </para>
/// <para>
/// <c>PublisherFreeText</c>/<c>GradeFreeText</c> carry that stored free text as it is, whether or not the catalog
/// currently overrides it (B-267). A form needs it: when the father unlinks the book, the free-text field
/// reappears and must show what is stored – showing the catalog value instead would display something that is
/// not saved, and clearing the field would delete a value the father never saw.
/// </para>
/// </summary>
public record TextbookResponse(int Id, string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter, DateTime CreatedAt,
    int? SeriesId = null, string? SeriesName = null, int? CurrentUnitId = null, string? CurrentUnitLabel = null,
    string? PublisherFreeText = null, int? GradeFreeText = null);

/// <summary>
/// Input for creating a textbook.
///
/// With a <c>SubjectId</c> the server derives <c>SubjectName</c> from the catalog; a name sent
/// alongside an id is ignored (B-142).
/// </summary>
public record CreateTextbookDto(string Title, string? SubjectName, int? SubjectId, int? Grade,
    string? Publisher, string? Isbn, string? CurrentChapter,
    int? SeriesId = null, int? CurrentUnitId = null);

/// <summary>
/// Partial change to a textbook; omitted fields stay unchanged.
///
/// In a PATCH, <c>null</c> means "not specified" and therefore cannot <b>clear</b> anything – that is
/// what the <c>Clear…</c> switches are for (cf. <c>ClearGrade</c> on the class test): <c>ClearSeries</c>
/// detaches the book from the catalog ("not cataloged") and takes the current unit with it, because the
/// unit means nothing without its series; <c>ClearUnit</c> only resets the unit; <c>ClearSubject</c>
/// removes the subject id and name; <c>ClearGrade</c> the grade level of the book.
///
/// While the book is linked to a unit with a grade (or a series with a publisher), <c>Grade</c>,
/// <c>ClearGrade</c> and <c>Publisher</c> change only the stored fallback: the response keeps showing the
/// catalog value until the link is cleared (see <c>TextbookResponse</c>).
///
/// Sending <c>SubjectId</c> alone is enough: the server derives <c>SubjectName</c> from it, so the two
/// halves cannot contradict each other (B-142). A <c>SubjectName</c> sent alongside an id is therefore
/// ignored – the free text only carries meaning while <em>no</em> catalog subject is bound.
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
