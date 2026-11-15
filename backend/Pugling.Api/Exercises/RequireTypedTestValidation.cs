namespace Pugling.Api.Exercises;

/// <summary>
/// Checks a <c>requireTypedTest</c> setting against whether its exercise type has a typed stage at all
/// (<see cref="ProblemText"/>), and against the stages the position is actually tested on - base stage and stage schedule
/// (<see cref="UnreachableProblemText"/>, B-550/B-552).
/// <para>
/// This exists because the setting does not break a card either - it silently disables scoring:
/// <c>PositionPracticeController</c> gates <c>scored</c> on <c>typed || !RequireTypedTest</c>, and a type
/// whose <see cref="IExerciseType.IsTypedStage"/> is constantly <c>false</c> (Birkenbihl) can never satisfy
/// that <c>typed</c> half. A position with this setting on such a type would never score, and the father
/// would only notice after weeks of an unmet goal (B-93).
/// </para>
/// <para>
/// Shared rather than private to one controller because <b>two</b> write paths reach the same effective
/// value - the position (<c>PlanPosition.RequireTypedTest</c>, supervisor) and the exercise default
/// (<c>Exercise.DefaultRequireTypedTest</c>, creator): <c>effectiveRequireTypedTest = dto.RequireTypedTest
/// ?? exercise.DefaultRequireTypedTest</c>. Closing only the position side left the creator's own default
/// unchecked, so the same problem surfaced at the wrong seat - the supervisor, not the creator who set it
/// (B-108).
/// </para>
/// </summary>
public static class RequireTypedTestValidation
{
    /// <summary>
    /// Returns an error text if <paramref name="requireTypedTest"/> is <c>true</c> for a type that has no
    /// typed stage at all (<see cref="IExerciseType.SupportsRequireTypedTest"/> is <c>false</c>), otherwise
    /// <c>null</c>.
    /// </summary>
    public static string? ProblemText(IExerciseType? type, bool? requireTypedTest) =>
        requireTypedTest == true && type?.SupportsRequireTypedTest == false
            ? $"requireTypedTest cannot be set: the exercise type \"{type.Key}\" has no typed stage at all, so this would never score."
            : null;

    /// <summary>
    /// Returns an error text if <paramref name="requireTypedTest"/> is <c>true</c> while a day of the position would be
    /// tested on a self-assessed stage (neither typed nor a free display stage), otherwise <c>null</c>. Such a day is the
    /// base stage until the schedule takes over (B-552) or a step of the schedule (B-550). A test on it is never graded,
    /// so the goal could not be met there - the malus would run against a child that did its part. Judged by
    /// <see cref="PositionPlayService.LearningCheckOf(IExerciseType, bool, int, IReadOnlyList{StageStep})"/>, the rule the
    /// father's plan page shows, so the refusal and the red "unreachable" line cannot disagree.
    /// </summary>
    /// <param name="type">The exercise type; an unknown type (<c>null</c>) is not judged here.</param>
    /// <param name="requireTypedTest">The effective "typed tests only" after the request.</param>
    /// <param name="baseStage">The effective base stage: position stage, else the exercise default, else the type default.</param>
    /// <param name="schedule">The effective stage schedule after the request; <c>null</c> or empty = none.</param>
    public static string? UnreachableProblemText(IExerciseType? type, bool requireTypedTest, int baseStage,
        IReadOnlyList<StageStep>? schedule) =>
        requireTypedTest && type is not null
            && PositionPlayService.LearningCheckOf(type, true, baseStage, schedule).Kind == LearningCheckKind.Unreachable
            ? "requireTypedTest cannot be combined with a self-assessed stage (the base stage or a step of the stage schedule): a test there is never graded, so the goal could not be met. Choose a typed stage or switch requireTypedTest off."
            : null;
}
