using Pugling.Agent.Creator.Drafting;

namespace Pugling.Agent.Creator;

/// <summary>
/// One generated exercise inside a bundle - a part of a class test (<see cref="ExamPlanner"/>) or a
/// position of a study plan (<see cref="PlanPlanner"/>).
/// <para>
/// Shared by both planners on purpose: both report the same five outcomes per exercise (failed, rule
/// violation, rolled back, dry run, created), and two copies of that list would drift apart the first
/// time a sixth one appears.
/// </para>
/// </summary>
/// <param name="TypeKey">The exercise type this part was generated for.</param>
/// <param name="Outcome">The result of the run, or <c>null</c> if it failed before producing one.</param>
/// <param name="Error">The error message of the failed run; <c>null</c> if it ran.</param>
public sealed record GeneratedPart(string TypeKey, GenerationOutcome? Outcome, string? Error);
