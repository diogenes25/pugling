using Pugling.Agent.Creator.Briefing;
using Pugling.Agent.Creator.Drafting;

namespace Pugling.Agent.Creator;

/// <summary>
/// The agent's flow in one sentence: first understand the child, then let the matching exercise type do
/// its work. The order is deliberately wired here and not left to the language model - the model delivers
/// content, control stays deterministic and thus verifiable.
/// </summary>
public sealed class CreatorPipeline(BriefingBuilder briefings, IEnumerable<IExerciseStrategy> strategies)
{
    private readonly IReadOnlyList<IExerciseStrategy> _strategies = [.. strategies];

    /// <summary>The exercise types the agent can generate.</summary>
    public IReadOnlyList<string> SupportedTypes => [.. _strategies.Select(s => s.TypeKey)];

    /// <summary>Build only the briefing (verb <c>briefing</c>) - without a language model, without write access.</summary>
    public Task<CreatorBriefing> BriefAsync(GenerationRequest request, CancellationToken ct = default) =>
        briefings.BuildAsync(request, ct);

    /// <summary>Build the briefing and generate the exercise.</summary>
    public async Task<(CreatorBriefing Briefing, GenerationOutcome Outcome)> CreateAsync(
        GenerationRequest request, CancellationToken ct = default)
    {
        var strategy = _strategies.FirstOrDefault(s =>
                           string.Equals(s.TypeKey, request.TypeKey, StringComparison.OrdinalIgnoreCase))
                       ?? throw new AgentUsageException(
                           $"Übungstyp '{request.TypeKey}' wird vom Agenten (noch) nicht erzeugt. " +
                           $"Möglich sind: {string.Join(", ", SupportedTypes)}.");

        var briefing = await briefings.BuildAsync(request, ct);
        var outcome = await strategy.RunAsync(briefing, request, ct);
        return (briefing, outcome);
    }
}
