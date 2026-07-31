namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// What came out of a generation run - success and failure in the same shape, so the output (and a
/// test) can treat both the same way.
/// </summary>
/// <param name="TypeKey">The exercise type generated.</param>
/// <param name="Title">Title of the draft (also filled in during a dry run).</param>
/// <param name="DraftJson">The draft as JSON - the output of <c>--dry-run</c> and the evidence in case of failure.</param>
/// <param name="Violations">Rule violations that even the repair round could not fix (empty = clean).</param>
/// <param name="ExerciseId">Id of the created exercise; <c>null</c> during a dry run or on violations.</param>
/// <param name="SelfTestPercent">Result of the side-effect-free self-test; 100 % is expected.</param>
/// <param name="RolledBack">Whether the exercise was deleted again due to a failed self-test.</param>
public sealed record GenerationOutcome(
    string TypeKey,
    string Title,
    string DraftJson,
    IReadOnlyList<string> Violations,
    int? ExerciseId,
    int? SelfTestPercent,
    bool RolledBack)
{
    /// <summary>The draft passed all rules.</summary>
    public bool DraftAccepted => Violations.Count == 0;

    /// <summary>A playable, self-tested exercise is now in the catalog.</summary>
    public bool Published => ExerciseId is not null && !RolledBack && SelfTestPercent == 100;
}
