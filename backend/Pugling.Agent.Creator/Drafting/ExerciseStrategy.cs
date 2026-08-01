using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// An exercise type the agent can generate. The implementations inherit from
/// <see cref="ExerciseStrategy{TDraft,TConfig}"/> - the flow is the same for all of them, only the
/// prompt, rules, config mapping and expected answers are type-specific.
/// </summary>
public interface IExerciseStrategy
{
    /// <summary>Exercise-type key as in the manifest (<c>Vocabulary</c>, <c>Cloze</c>, …).</summary>
    string TypeKey { get; }

    /// <summary>Generates (and publishes) an exercise for this briefing.</summary>
    Task<GenerationOutcome> RunAsync(CreatorBriefing briefing, GenerationRequest request, CancellationToken ct = default);
}

/// <summary>
/// The shared flow of all types: draft → validate → (repair) → create → self-test. Deliberately a
/// template: this gives <b>one</b> place where repair rounds, dry run and rollback are decided, and
/// keeps the types small.
/// </summary>
/// <typeparam name="TDraft">The draft shape the language model fills.</typeparam>
/// <typeparam name="TConfig">The contract config that ends up in the API.</typeparam>
public abstract class ExerciseStrategy<TDraft, TConfig>(
    IChatClient chat,
    CreatorApi creator,
    IOptions<AgentOptions> options,
    ILogger logger) : IExerciseStrategy
    where TDraft : class
{
    private static readonly JsonSerializerOptions PrintOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>The catalog access - the derivations need it too (vocabulary lookup, items).</summary>
    protected CreatorApi Creator { get; } = creator;

    /// <inheritdoc/>
    public abstract string TypeKey { get; }

    /// <summary>The type-specific task description in the prompt.</summary>
    protected abstract string TaskInstruction(CreatorBriefing briefing, GenerationRequest request);

    /// <summary>The deterministic rules of this type.</summary>
    protected abstract IReadOnlyList<string> Validate(TDraft draft, CreatorBriefing briefing, GenerationRequest request);

    /// <summary>Translates the draft into the API payload (may query the vocabulary store for this).</summary>
    protected abstract Task<ExercisePayload<TConfig>> ToPayloadAsync(TDraft draft, CreatorBriefing briefing,
        GenerationRequest request, CancellationToken ct);

    /// <summary>The expected answers in task order - the basis of the self-test.</summary>
    protected abstract IReadOnlyList<string> ExpectedAnswers(TDraft draft);

    /// <summary>The draft's title (for output and the result).</summary>
    protected abstract string TitleOf(TDraft draft);

    /// <summary>
    /// Wraps a finished config into the API payload, setting the metadata from the briefing along the
    /// way - grade, school type and source are what the supervisor later rediscovers the exercise by.
    /// Individually it holds the child's grade, generally the profile's range: a catalog exercise should
    /// be discoverable for its whole target audience, not for exactly one child.
    /// </summary>
    protected ExercisePayload<TConfig> Payload(string title, TConfig config, CreatorBriefing briefing,
        GenerationRequest request) =>
        new(title.Trim(), briefing.ExistingExerciseTitles.Count + 1, request.RewardPoints, config,
            GradeMin: briefing.GradeMin, GradeMax: briefing.GradeMax, SchoolTypes: briefing.SchoolType,
            Source: briefing.Source, Description: DescriptionFor(briefing));

    /// <summary>Short provenance note on the exercise - makes generated content recognizable in the catalog.</summary>
    private static string DescriptionFor(CreatorBriefing briefing)
    {
        var origin = briefing.Profile is { } profile ? $"Vom KI-Creator (Profil „{profile.Name}“)" : "Vom KI-Creator";
        return briefing.Individual
            ? briefing.Interests.Count > 0
                ? $"{origin} für {briefing.Audience} erzeugt (eingekleidet in: {string.Join(", ", briefing.Interests)})."
                : $"{origin} für {briefing.Audience} erzeugt."
            : $"{origin} für den gemeinsamen Katalog erzeugt.";
    }

    /// <inheritdoc/>
    public async Task<GenerationOutcome> RunAsync(CreatorBriefing briefing, GenerationRequest request,
        CancellationToken ct = default)
    {
        var route = await ResolveAuthoringRouteAsync(ct);

        TDraft draft;
        IReadOnlyList<string> violations = [];
        int attempt = 0;
        while (true)
        {
            draft = await DraftAsync(briefing, request, violations, ct);
            violations = Validate(draft, briefing, request);
            if (violations.Count == 0 || attempt++ >= options.Value.RepairAttempts) break;

            logger.LogWarning("Entwurf {Attempt} verletzt {Count} Regel(n) – Reparatur-Runde: {Violations}",
                attempt, violations.Count, string.Join(" | ", violations));
        }

        var json = JsonSerializer.Serialize(draft, PrintOptions);
        var title = TitleOf(draft);

        if (violations.Count > 0)
            return new GenerationOutcome(TypeKey, title, json, violations, null, null, false);

        if (request.DryRun)
            return new GenerationOutcome(TypeKey, title, json, [], null, null, false);

        var payload = await ToPayloadAsync(draft, briefing, request, ct);
        var created = await Creator.CreateExerciseAsync<TConfig>(request.SubjectId, request.ChapterId, route, payload, ct);

        var percent = await SelfTestAsync(created.Id, draft, ct);
        var rolledBack = false;
        if (percent != 100 && request.Strict)
        {
            // An exercise that contradicts its own solutions must not stay in the catalog.
            await Creator.DeleteExerciseAsync(request.SubjectId, request.ChapterId, route, created.Id, ct);
            rolledBack = true;
        }

        return new GenerationOutcome(TypeKey, title, json, [], created.Id, percent, rolledBack);
    }

    /// <summary>Fetches a draft from the language model; <paramref name="violations"/> triggers the repair round.</summary>
    private async Task<TDraft> DraftAsync(CreatorBriefing briefing, GenerationRequest request,
        IReadOnlyList<string> violations, CancellationToken ct)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, DraftPrompts.SystemFor(briefing)),
            new(ChatRole.User, DraftPrompts.User(briefing, request, TaskInstruction(briefing, request))),
        ];
        if (violations.Count > 0) messages.Add(new ChatMessage(ChatRole.User, DraftPrompts.Repair(violations)));

        var chatOptions = new ChatOptions { Temperature = (float)options.Value.Temperature };

        // The strict JSON schema first; if the model (or the Ollama version) cannot do that, JSON mode with
        // the schema embedded in the prompt remains as the fallback.
        try
        {
            var response = await chat.GetResponseAsync<TDraft>(messages, chatOptions,
                useJsonSchemaResponseFormat: true, cancellationToken: ct);
            if (response.TryGetResult(out var result) && result is not null) return result;

            logger.LogWarning("Das Modell lieferte kein schema-konformes JSON – Rückfall auf den JSON-Modus.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Strukturierte Ausgabe per JSON-Schema fehlgeschlagen – Rückfall auf den JSON-Modus.");
        }

        var fallback = await chat.GetResponseAsync<TDraft>(messages, chatOptions,
            useJsonSchemaResponseFormat: false, cancellationToken: ct);
        return fallback.TryGetResult(out var value) && value is not null
            ? value
            : throw new AgentException(
                $"Das Modell '{options.Value.Model}' hat keinen verwertbaren JSON-Entwurf geliefert. " +
                "Nutze ein Instruct-Modell mit verlässlicher JSON-Ausgabe (z. B. qwen2.5:14b-instruct).");
    }

    /// <summary>
    /// Plays through the freshly created exercise in side-effect-free test mode with its own expected
    /// answers. Only this way does it show up when answer and task do not match. A <b>typed</b> answer
    /// mode is preferred - the test would be worthless with self-assessment.
    /// </summary>
    private async Task<int> SelfTestAsync(int exerciseId, TDraft draft, CancellationToken ct)
    {
        var data = await Creator.PreviewExerciseAsync(exerciseId, ct: ct);
        int? stage = null;
        if (!data.Typed)
        {
            foreach (var option in data.Stages)
            {
                var candidate = await Creator.PreviewExerciseAsync(exerciseId, option.Value, ct);
                if (!candidate.Typed) continue;
                (data, stage) = (candidate, option.Value);
                break;
            }
        }

        var expected = ExpectedAnswers(draft);
        var answers = data.Items
            .Select(item => new PreviewAnswer(item.ItemIndex,
                data.Typed ? expected.ElementAtOrDefault(item.ItemIndex) : null,
                data.Typed ? null : true))
            .ToList();

        var result = await Creator.CheckPreviewAsync(exerciseId, new PreviewCheckDto(answers, stage), ct);
        return result.ScorePercent;
    }

    /// <summary>The route segment always comes from the manifest - never guessed.</summary>
    private async Task<string> ResolveAuthoringRouteAsync(CancellationToken ct)
    {
        var manifest = await Creator.GetExerciseTypesAsync(ct);
        return manifest.FirstOrDefault(m => string.Equals(m.Type, TypeKey, StringComparison.OrdinalIgnoreCase))?.AuthoringRoute
               ?? throw new AgentUsageException($"Der Server kennt keinen Übungstyp '{TypeKey}'.");
    }
}
