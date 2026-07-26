using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Ein Übungstyp, den der Agent erzeugen kann. Die Implementierungen erben von
/// <see cref="ExerciseStrategy{TDraft,TConfig}"/> – der Ablauf ist für alle gleich, typ-spezifisch
/// sind nur Prompt, Regeln, Config-Abbildung und Soll-Antworten.
/// </summary>
public interface IExerciseStrategy
{
    /// <summary>Übungstyp-Schlüssel wie im Manifest (<c>Vocabulary</c>, <c>Cloze</c>, …).</summary>
    string TypeKey { get; }

    /// <summary>Erzeugt (und veröffentlicht) eine Übung für dieses Briefing.</summary>
    Task<GenerationOutcome> RunAsync(CreatorBriefing briefing, GenerationRequest request, CancellationToken ct = default);
}

/// <summary>
/// Der gemeinsame Ablauf aller Typen: entwerfen → prüfen → (reparieren) → anlegen → selbst testen.
/// Bewusst als Schablone: so gibt es <b>eine</b> Stelle, an der über Reparatur-Runden, Trockenlauf
/// und Rücknahme entschieden wird, und die Typen bleiben klein.
/// </summary>
/// <typeparam name="TDraft">Die Entwurfsform, die das Sprachmodell füllt.</typeparam>
/// <typeparam name="TConfig">Die Vertrags-Config, die am Ende in der API landet.</typeparam>
public abstract class ExerciseStrategy<TDraft, TConfig>(
    IChatClient chat,
    CreatorApi creator,
    IOptions<AgentOptions> options,
    ILogger logger) : IExerciseStrategy
    where TDraft : class
{
    private static readonly JsonSerializerOptions PrintOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Der Katalog-Zugriff – auch die Ableitungen brauchen ihn (Vokabel-Lookup, Items).</summary>
    protected CreatorApi Creator { get; } = creator;

    /// <inheritdoc/>
    public abstract string TypeKey { get; }

    /// <summary>Die typ-spezifische Auftragsbeschreibung im Prompt.</summary>
    protected abstract string TaskInstruction(CreatorBriefing briefing, GenerationRequest request);

    /// <summary>Die deterministischen Regeln dieses Typs.</summary>
    protected abstract IReadOnlyList<string> Validate(TDraft draft, CreatorBriefing briefing, GenerationRequest request);

    /// <summary>Übersetzt den Entwurf in die Nutzlast der API (darf dafür den Vokabelspeicher befragen).</summary>
    protected abstract Task<ExercisePayload<TConfig>> ToPayloadAsync(TDraft draft, CreatorBriefing briefing,
        GenerationRequest request, CancellationToken ct);

    /// <summary>Die Soll-Antworten in Aufgabenreihenfolge – Grundlage des Selbsttests.</summary>
    protected abstract IReadOnlyList<string> ExpectedAnswers(TDraft draft);

    /// <summary>Der Titel des Entwurfs (für Ausgabe und Ergebnis).</summary>
    protected abstract string TitleOf(TDraft draft);

    /// <summary>
    /// Hüllt eine fertige Config in die API-Nutzlast und setzt dabei die Metadaten aus dem Briefing –
    /// Klassenstufe, Schulart und Quelle sind das, woran der Supervisor die Übung später wiederfindet.
    /// Individuell steht dort die Stufe des Kindes, allgemein der Bereich des Profils: eine Katalog-Übung
    /// soll für ihre ganze Zielgruppe auffindbar sein, nicht für genau ein Kind.
    /// </summary>
    protected ExercisePayload<TConfig> Payload(string title, TConfig config, CreatorBriefing briefing,
        GenerationRequest request) =>
        new(title.Trim(), briefing.ExistingExerciseTitles.Count + 1, request.RewardPoints, config,
            GradeMin: briefing.GradeMin, GradeMax: briefing.GradeMax, SchoolTypes: briefing.SchoolType,
            Source: briefing.Source, Description: DescriptionFor(briefing));

    /// <summary>Kurze Herkunftsnotiz an der Übung – macht generierte Inhalte im Katalog erkennbar.</summary>
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
            // Eine Übung, die ihren eigenen Lösungen widerspricht, darf nicht im Katalog stehen bleiben.
            await Creator.DeleteExerciseAsync(request.SubjectId, request.ChapterId, route, created.Id, ct);
            rolledBack = true;
        }

        return new GenerationOutcome(TypeKey, title, json, [], created.Id, percent, rolledBack);
    }

    /// <summary>Holt einen Entwurf vom Sprachmodell; <paramref name="violations"/> löst die Reparatur-Runde aus.</summary>
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

        // Erst das strenge JSON-Schema; kann das Modell (oder die Ollama-Version) das nicht, bleibt der
        // JSON-Modus mit ins Prompt eingebettetem Schema als Rückfallebene.
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
    /// Spielt die frisch angelegte Übung im nebenwirkungsfreien Testmodus mit den eigenen Soll-Antworten
    /// durch. Nur so fällt auf, wenn Lösung und Aufgabe nicht zusammenpassen. Bevorzugt wird eine
    /// <b>getippte</b> Abfrageform – bei Selbsteinschätzung wäre der Test wertlos.
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

    /// <summary>Das Routen-Segment kommt immer aus dem Manifest – nie geraten.</summary>
    private async Task<string> ResolveAuthoringRouteAsync(CancellationToken ct)
    {
        var manifest = await Creator.GetExerciseTypesAsync(ct);
        return manifest.FirstOrDefault(m => string.Equals(m.Type, TypeKey, StringComparison.OrdinalIgnoreCase))?.AuthoringRoute
               ?? throw new AgentUsageException($"Der Server kennt keinen Übungstyp '{TypeKey}'.");
    }
}
