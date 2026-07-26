using Pugling.Agent.Creator.Briefing;
using Pugling.Agent.Creator.Drafting;

namespace Pugling.Agent.Creator;

/// <summary>
/// Der Ablauf des Agenten in einem Satz: erst das Kind verstehen, dann den passenden Übungstyp
/// arbeiten lassen. Die Reihenfolge ist bewusst hier verdrahtet und nicht dem Sprachmodell überlassen –
/// das Modell liefert Inhalt, die Steuerung bleibt deterministisch und damit prüfbar.
/// </summary>
public sealed class CreatorPipeline(BriefingBuilder briefings, IEnumerable<IExerciseStrategy> strategies)
{
    private readonly IReadOnlyList<IExerciseStrategy> _strategies = [.. strategies];

    /// <summary>Die Übungstypen, die der Agent erzeugen kann.</summary>
    public IReadOnlyList<string> SupportedTypes => [.. _strategies.Select(s => s.TypeKey)];

    /// <summary>Nur das Briefing bauen (Verb <c>briefing</c>) – ohne Sprachmodell, ohne Schreibzugriff.</summary>
    public Task<CreatorBriefing> BriefAsync(GenerationRequest request, CancellationToken ct = default) =>
        briefings.BuildAsync(request, ct);

    /// <summary>Briefing bauen und die Übung erzeugen.</summary>
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
