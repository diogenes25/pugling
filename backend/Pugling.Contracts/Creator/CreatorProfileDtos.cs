namespace Pugling.Contracts.Creator;

// Contract of the creator profiles (route api/v1/creator/profiles): the "subject teacher" - subject,
// school branch, grades, optionally a textbook series, plus persona and didactics for the AI creator.

/// <summary>
/// A creator profile including its own permission view. <c>IsOwn</c> says whether the calling account
/// may change the profile; <c>DefaultTypes</c> are the preferred exercise types (keys from the type manifest).
/// </summary>
public record CreatorProfileResponse(int Id, string Name, int? OwnerAdultId, bool IsOwn,
    string? SubjectName, int? SubjectId, SchoolTypes SchoolTypes, int? GradeMin, int? GradeMax,
    int? SeriesId, string? SeriesName, string SourceLang, string TargetLang,
    string? Persona, string? Didactics, IReadOnlyList<string> DefaultTypes, bool Active, DateTime CreatedAt);

/// <summary>
/// Input for creating a profile. Only <c>Name</c> is required – everything else narrows the fit.
/// <para>With a <c>SubjectId</c> the server derives <c>SubjectName</c> from the catalog; a name sent
/// alongside an id is ignored (B-142).</para>
/// </summary>
public record CreateCreatorProfileDto(string Name, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, int? GradeMin, int? GradeMax, int? SeriesId,
    string? SourceLang, string? TargetLang, string? Persona, string? Didactics,
    List<string>? DefaultTypes, bool? Active);

/// <summary>
/// Partial change to a profile; omitted fields remain unchanged.
/// <para>
/// A <c>null</c> means "not specified" in a PATCH – so it cannot <b>clear</b> a field.
/// The <c>Clear…</c> switches (like <c>ClearGrade</c> on the class test) exist for that: only with them
/// can a profile become subject-neutral, series-independent, or grade-open again.
/// </para>
/// <para>
/// <c>ClearSubject</c> makes the profile subject-neutral (subject id and name drop away), <c>ClearSeries</c>
/// makes it series-independent, <c>ClearGradeMin</c>/<c>ClearGradeMax</c> lift the respective grade-level limit.
/// </para>
/// <para>
/// Sending <c>SubjectId</c> alone is enough: the server derives <c>SubjectName</c> from it, so the two
/// halves cannot contradict each other (B-142). A <c>SubjectName</c> sent alongside an id is therefore
/// ignored – the free text only carries meaning while <em>no</em> catalog subject is bound.
/// </para>
/// </summary>
public record UpdateCreatorProfileDto(string? Name, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, int? GradeMin, int? GradeMax, int? SeriesId,
    string? SourceLang, string? TargetLang, string? Persona, string? Didactics,
    List<string>? DefaultTypes, bool? Active,
    bool ClearSubject = false, bool ClearSeries = false,
    bool ClearGradeMin = false, bool ClearGradeMax = false);

/// <summary>
/// A match of the profile search for a child. <paramref name="Score"/> is computed deterministically
/// (a series match weighs heaviest), <paramref name="Reasons"/> names the reasons in plain text –
/// so a console or a UI can explain the choice instead of showing a bare number.
/// </summary>
public record CreatorProfileMatch(CreatorProfileResponse Profile, int Score, IReadOnlyList<string> Reasons);
