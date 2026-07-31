using Pugling.Contracts.Creator;

namespace Pugling.Contracts.Supervisor;

// Vertrag der Klassenarbeiten (Route api/v1/supervisor/class-tests; intern weiterhin „Klassenarbeit"):
// planen, Übungen zuweisen (direkt oder über Tags), Note nachtragen, gezielt üben und wiederholen.

/// <summary>A tag in short form, as attached to a class test.</summary>
public record TagRef(int Id, string Name, string? Color);

/// <summary>Class test in list/summary view.</summary>
public record KlassenarbeitResponse(int Id, int ChildId, int? SubjectId, string? SubjectName,
    string Title, string? Topic, DateOnly ScheduledDate, KlassenarbeitStatus Status,
    decimal? Grade, string? GradeComment, int DirectExerciseCount, IReadOnlyList<TagRef> Tags, DateTime CreatedAt);

/// <summary>Class test with the directly assigned exercises.</summary>
public record KlassenarbeitDetail(KlassenarbeitResponse Klassenarbeit, IReadOnlyList<ExerciseBrief> AssignedExercises);

/// <summary>Input for scheduling a class test (or recording one already written).</summary>
public record CreateClassTestDto(int ChildId, string Title, string? Topic, int? SubjectId, DateOnly ScheduledDate,
    KlassenarbeitStatus? Status, decimal? Grade, string? GradeComment, List<int>? ExerciseIds, List<int>? TagIds);

/// <summary>Partial change – among other things, recording a grade and setting status. <c>ClearGrade</c> deletes the grade.</summary>
public record UpdateClassTestDto(string? Title, string? Topic, int? SubjectId, DateOnly? ScheduledDate,
    KlassenarbeitStatus? Status, decimal? Grade, bool ClearGrade, string? GradeComment);

/// <summary>Input for directly assigning exercises to a class test.</summary>
public record AssignExercisesDto(List<int> ExerciseIds);

/// <summary>Exercises relevant to a class test for targeted practice (days until the date, inclusive).</summary>
public record PracticeResponse(int KlassenarbeitId, string Title, DateOnly ScheduledDate, int DaysUntil,
    IReadOnlyList<ExerciseBrief> Exercises);

/// <summary>Exercises that should be repeated because of poorly graded class tests.</summary>
public record RepeatResponse(decimal MinBadGrade, IReadOnlyList<KlassenarbeitResponse> Sources,
    IReadOnlyList<ExerciseBrief> Exercises);
