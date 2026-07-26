using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Grammatikübung: Umformungs- und Regelaufgaben mit genau einer richtigen Lösung. Der Regelhinweis
/// ist bewusst Teil des Entwurfs – er macht aus einer Abfrage eine Übung, aus der man etwas lernt.
/// </summary>
public sealed class GrammarStrategy(IChatClient chat, CreatorApi creator,
    IOptions<AgentOptions> options, ILogger<GrammarStrategy> logger)
    : ExerciseStrategy<GrammarDraft, GrammarConfig>(chat, creator, options, logger)
{
    /// <inheritdoc/>
    public override string TypeKey => "Grammar";

    /// <inheritdoc/>
    protected override string TitleOf(GrammarDraft draft) => draft.Title;

    /// <inheritdoc/>
    protected override string TaskInstruction(ChildBriefing briefing, GenerationRequest request) =>
        $"""
        Entwirf eine Grammatikübung zu {request.SourceLang}. 'instruction' ist die deutsche
        Arbeitsanweisung für das ganze Blatt. Jede Aufgabe hat in 'prompt' den Ausgangssatz bzw. die
        Aufgabenstellung, in 'answer' die einzig richtige Lösung (nur das, was das Kind schreiben soll)
        und in 'ruleHint' einen kurzen deutschen Hinweis auf die zugrunde liegende Regel.
        Die Lösung ist eindeutig – keine Aufgabe, bei der mehrere Formen richtig wären.
        """;

    /// <inheritdoc/>
    protected override IReadOnlyList<string> Validate(GrammarDraft draft, ChildBriefing briefing,
        GenerationRequest request)
    {
        var violations = new Violations();
        DraftRules.Title(violations, draft.Title, briefing);
        DraftRules.NotBlank(violations, draft.Instruction, "Die Arbeitsanweisung");
        DraftRules.Count(violations, draft.Tasks.Count, request);
        DraftRules.NoDuplicates(violations, draft.Tasks.Select(t => t.Prompt), "Aufgabenstellungen");
        DraftRules.CoversRequiredWords(violations, briefing,
            draft.Tasks.Select(t => $"{t.Prompt} {t.Answer}"), exact: false);

        foreach (var (task, index) in draft.Tasks.Select((task, index) => (task, index)))
        {
            DraftRules.NotBlank(violations, task.Prompt, $"Aufgabe {index + 1}: die Aufgabenstellung");
            DraftRules.NotBlank(violations, task.Answer, $"Aufgabe {index + 1}: die Lösung");
            DraftRules.PromptDiffersFromAnswer(violations, task.Prompt, task.Answer, index);
        }

        return violations.Messages;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExpectedAnswers(GrammarDraft draft) =>
        [.. draft.Tasks.Select(t => t.Answer.Trim())];

    /// <inheritdoc/>
    protected override Task<ExercisePayload<GrammarConfig>> ToPayloadAsync(GrammarDraft draft,
        ChildBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var config = new GrammarConfig
        {
            Instruction = draft.Instruction.Trim(),
            Tasks =
            [
                .. draft.Tasks.Select(t => new GrammarTask(t.Prompt.Trim(), t.Answer.Trim(),
                    string.IsNullOrWhiteSpace(t.RuleHint) ? null : t.RuleHint.Trim())),
            ],
        };

        return Task.FromResult(Payload(draft.Title, config, briefing, request));
    }
}
