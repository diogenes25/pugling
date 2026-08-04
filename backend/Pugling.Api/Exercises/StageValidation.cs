namespace Pugling.Api.Exercises;

/// <summary>
/// Checks a stage value against the stages its exercise type actually has.
/// <para>
/// This exists because an invalid stage does not break a card - it <b>gives it away</b>:
/// <see cref="IExerciseType.IsTypedStage"/> matches the int against the type's stage enum, an unknown value
/// falls through to <c>false</c>, and a non-typed card carries the answer in <c>Reveal</c>
/// (<c>PositionPlayService.CardFacets</c>). A typo therefore hands the child the solutions instead of asking
/// for them, silently and with a 200.
/// </para>
/// <para>
/// Shared rather than private to one controller because <b>two</b> write paths reach the same playback:
/// the position (<c>PlanPosition.Stage</c>/<c>StageSchedule</c>, supervisor) and the exercise default
/// (<c>Exercise.DefaultStage</c>, creator) - <c>PositionPlayService.StageForDay</c> falls back to the latter
/// whenever the position names no stage, which is the normal case for most types. Closing only one door would
/// have left the same defect reachable from the other.
/// </para>
/// </summary>
public static class StageValidation
{
    /// <summary>
    /// Returns an error text if any of the <paramref name="stages"/> is outside the type's
    /// <see cref="IExerciseType.StageOptions"/>, otherwise <c>null</c>. An <b>empty</b> option list means
    /// "this type has no stage selection" (matching, essay, reading): there the value has no effect at all,
    /// so validating it would reject requests that are fine. Where the list exists it is complete - a guard
    /// test pins that (<c>ExerciseTypeManifestTests</c>), which is what makes it usable as the permitted set.
    /// </summary>
    public static string? ProblemText(IExerciseType? type, params IEnumerable<int?> stages)
    {
        if (type is null || type.StageOptions.Count == 0) return null;

        foreach (var stage in stages)
        {
            if (stage is null || type.StageOptions.Any(o => o.Value == stage.Value)) continue;
            return $"Stage {stage} does not exist for exercise type '{type.Key}'; allowed: "
                + string.Join(", ", type.StageOptions.Select(o => $"{o.Value} ({o.Label})")) + ".";
        }
        return null;
    }
}
