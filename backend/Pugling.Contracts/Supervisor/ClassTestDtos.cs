using Pugling.Contracts.Creator;

namespace Pugling.Contracts.Supervisor;

// Contract of the class tests (route api/v1/supervisor/class-tests; internally still "Klassenarbeit"):
// plan them, assign exercises (directly or through tags), enter the grade, practice and repeat targeted.

/// <summary>A tag in short form, as attached to a class test.</summary>
public record TagRef(int Id, string Name, string? Color);

/// <summary>
/// Class test in list/summary view. The preparation fields (B-752/B-757): <c>PrepWindowDays</c> is the test's own window
/// (<c>null</c> = the default <c>PrepWindowDefault</c>, 0 = no preparation duty), <c>PrepCardsPerDay</c> the server's "about N
/// cards a day" of the duty (<c>null</c> when none arises), <c>PrepBlockedReason</c> why none arises for a planned test
/// (<see cref="ClassTestPrepBlockReasons"/>), <c>PrepNotExecutableCount</c> how many exercises of its bundle the duty leaves
/// out because no supervisor of the child may assign them (withdrawn material, B-941; 0 when no duty runs).
/// </summary>
public record KlassenarbeitResponse(int Id, int ChildId, int? SubjectId, string? SubjectName,
    string Title, string? Topic, DateOnly ScheduledDate, KlassenarbeitStatus Status,
    decimal? Grade, string? GradeComment, int DirectExerciseCount, IReadOnlyList<TagRef> Tags, DateTime CreatedAt,
    int? PrepWindowDays, int PrepWindowDefault, int? PrepCardsPerDay, string? PrepBlockedReason, int PrepNotExecutableCount);

/// <summary>Why no preparation duty arises for a planned class test (B-752, decision 4) - stable codes, the UI words them.</summary>
public static class ClassTestPrepBlockReasons
{
    /// <summary>The class test has no subject, so there is no plan to put the duty in.</summary>
    public const string NoSubject = "no_subject";
    /// <summary>The child has no active plan in the test's subject; the app does not create one.</summary>
    public const string NoActivePlan = "no_active_plan";
}

/// <summary>Class test with the directly assigned exercises.</summary>
public record KlassenarbeitDetail(KlassenarbeitResponse Klassenarbeit, IReadOnlyList<ExerciseBrief> AssignedExercises);

/// <summary>Input for scheduling a class test (or recording one already written).</summary>
public record CreateClassTestDto(int ChildId, string Title, string? Topic, int? SubjectId, DateOnly ScheduledDate,
    KlassenarbeitStatus? Status, decimal? Grade, string? GradeComment, List<int>? ExerciseIds, List<int>? TagIds,
    int? PrepWindowDays = null);

/// <summary>Partial change – among other things, recording a grade and setting status. <c>ClearGrade</c> deletes the grade.</summary>
public record UpdateClassTestDto(string? Title, string? Topic, int? SubjectId, DateOnly? ScheduledDate,
    // ClearGrade has a default like every other clear switch (UpdateChildDto and friends): omitting it must
    // mean "do not clear". Without one the contract declares it REQUIRED, and a form that only sends a grade
    // would have to send `clearGrade: false` as well.
    KlassenarbeitStatus? Status, decimal? Grade, bool ClearGrade = false, string? GradeComment = null,
    // B-752/B-757: the preparation window; ClearPrepWindow puts it back to the default (null), 0 switches the duty off.
    int? PrepWindowDays = null, bool ClearPrepWindow = false);

/// <summary>Input for directly assigning exercises to a class test.</summary>
public record AssignExercisesDto(List<int> ExerciseIds);

/// <summary>Exercises relevant to a class test for targeted practice (days until the date, inclusive).</summary>
public record PracticeResponse(int KlassenarbeitId, string Title, DateOnly ScheduledDate, int DaysUntil,
    IReadOnlyList<ExerciseBrief> Exercises);

/// <summary>Exercises that should be repeated because of poorly graded class tests.</summary>
public record RepeatResponse(decimal MinBadGrade, IReadOnlyList<KlassenarbeitResponse> Sources,
    IReadOnlyList<ExerciseBrief> Exercises);
