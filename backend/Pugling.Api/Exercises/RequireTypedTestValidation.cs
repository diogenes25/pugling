namespace Pugling.Api.Exercises;

/// <summary>
/// Checks a <c>requireTypedTest</c> setting against whether its exercise type has a typed stage at all.
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
}
