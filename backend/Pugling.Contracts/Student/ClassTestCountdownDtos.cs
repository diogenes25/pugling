namespace Pugling.Contracts.Student;

// The child's view on its class tests (B-750/B-751): when the next one is and how sure it already is in its vocabulary.
// Deliberately its own shape, not KlassenarbeitResponse - grade and grade comment are the supervisor's data.

/// <summary>
/// An upcoming class test as the child sees it: title, subject, date and the days until it (0 = today). No grade, no
/// grade comment.
/// </summary>
public record UpcomingClassTestResponse(int Id, string Title, int? SubjectId, string? SubjectName, DateOnly ScheduledDate,
    int DaysUntil);

/// <summary>
/// How sure the child already is in the vocabulary of a class test's bundle (B-751). Both counts are distinct store
/// words, not exercise items: a word in two bundle exercises counts once (B-945). <c>TotalItems</c> counts every word of
/// the bundle, practised or not; <c>SecureItems</c> the words whose average mastery over their bundle items (an
/// unpractised item counts as 0) is at least <c>WeakBelowPercent</c>. Both are null when the bundle holds no vocabulary
/// item - there is no measure for other exercise types, and none is estimated.
/// </summary>
public record ClassTestReadinessResponse(int ClassTestId, int ExerciseCount, int VocabularyExerciseCount,
    int? TotalItems, int? SecureItems, int WeakBelowPercent);
