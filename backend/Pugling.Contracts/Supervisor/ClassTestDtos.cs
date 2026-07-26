using Pugling.Contracts.Creator;

namespace Pugling.Contracts.Supervisor;

// Vertrag der Klassenarbeiten (Route api/v1/supervisor/class-tests; intern weiterhin „Klassenarbeit"):
// planen, Übungen zuweisen (direkt oder über Tags), Note nachtragen, gezielt üben und wiederholen.

/// <summary>Ein Tag in Kurzform, wie er an einer Klassenarbeit hängt.</summary>
public record TagRef(int Id, string Name, string? Color);

/// <summary>Klassenarbeit in Listen-/Zusammenfassungssicht.</summary>
public record KlassenarbeitResponse(int Id, int ChildId, int? SubjectId, string? SubjectName,
    string Title, string? Topic, DateOnly ScheduledDate, KlassenarbeitStatus Status,
    decimal? Grade, string? GradeComment, int DirectExerciseCount, IReadOnlyList<TagRef> Tags, DateTime CreatedAt);

/// <summary>Klassenarbeit mit den direkt zugewiesenen Übungen.</summary>
public record KlassenarbeitDetail(KlassenarbeitResponse Klassenarbeit, IReadOnlyList<ExerciseBrief> AssignedExercises);

/// <summary>Eingabe zum Planen einer Klassenarbeit (oder zum Nachtragen einer bereits geschriebenen).</summary>
public record CreateClassTestDto(int ChildId, string Title, string? Topic, int? SubjectId, DateOnly ScheduledDate,
    KlassenarbeitStatus? Status, decimal? Grade, string? GradeComment, List<int>? ExerciseIds, List<int>? TagIds);

/// <summary>Partielle Änderung – u. a. Note nachtragen und Status setzen. <c>ClearGrade</c> löscht die Note.</summary>
public record UpdateClassTestDto(string? Title, string? Topic, int? SubjectId, DateOnly? ScheduledDate,
    KlassenarbeitStatus? Status, decimal? Grade, bool ClearGrade, string? GradeComment);

/// <summary>Eingabe zum direkten Zuweisen von Übungen zu einer Klassenarbeit.</summary>
public record AssignExercisesDto(List<int> ExerciseIds);

/// <summary>Relevante Übungen einer Klassenarbeit zum gezielten Üben (Tage bis zum Termin inklusive).</summary>
public record PracticeResponse(int KlassenarbeitId, string Title, DateOnly ScheduledDate, int DaysUntil,
    IReadOnlyList<ExerciseBrief> Exercises);

/// <summary>Übungen, die wegen schlecht benoteter Klassenarbeiten wiederholt werden sollten.</summary>
public record RepeatResponse(decimal MinBadGrade, IReadOnlyList<KlassenarbeitResponse> Sources,
    IReadOnlyList<ExerciseBrief> Exercises);
