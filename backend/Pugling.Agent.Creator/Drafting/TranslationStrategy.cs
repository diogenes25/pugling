using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Translation exercise: whole sentences instead of single words. The required vocabulary does not
/// need to appear here as its own entry, but <b>within</b> the sentences - checked accordingly for
/// occurrence, not equality.
/// </summary>
public sealed class TranslationStrategy(IChatClient chat, CreatorApi creator,
    IOptions<AgentOptions> options, ILogger<TranslationStrategy> logger)
    : ExerciseStrategy<TranslationDraft, TranslationConfig>(chat, creator, options, logger)
{
    /// <inheritdoc/>
    public override string TypeKey => "Translation";

    /// <inheritdoc/>
    protected override string TitleOf(TranslationDraft draft) => draft.Title;

    /// <inheritdoc/>
    protected override string TaskInstruction(CreatorBriefing briefing, GenerationRequest request) =>
        $"""
        Entwirf eine Übersetzungsübung: kurze, vollständige Sätze in {briefing.SourceLang} ('source'),
        dazu die erwartete Übersetzung in {briefing.TargetLang} ('target'). Die Sätze bauen aufeinander
        nicht auf und stehen für sich. Trage in 'alternatives' weitere korrekte Übersetzungen ein,
        wenn mehrere natürlich klingen – sonst lasse das Feld leer.
        """;

    /// <inheritdoc/>
    protected override IReadOnlyList<string> Validate(TranslationDraft draft, CreatorBriefing briefing,
        GenerationRequest request)
    {
        var violations = new Violations();
        // If 'items' is missing from the model JSON this is null - as an empty list it becomes a rule violation.
        var items = draft.Items ?? [];
        DraftRules.Title(violations, draft.Title, briefing);
        DraftRules.Count(violations, items.Count, request);
        DraftRules.NoDuplicates(violations, items.Select(i => i?.Source), "Ausgangssätze");
        DraftRules.CoversRequiredWords(violations, briefing, items.Select(i => i?.Source), exact: false);

        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            DraftRules.NotBlank(violations, item?.Source, $"Satz {index + 1}: der Ausgangssatz");
            DraftRules.NotBlank(violations, item?.Target, $"Satz {index + 1}: die Übersetzung");
            DraftRules.PromptDiffersFromAnswer(violations, item?.Source, item?.Target, index);
        }

        return violations.Messages;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExpectedAnswers(TranslationDraft draft) =>
        [.. draft.Items.Select(i => i.Target.Trim())];

    /// <inheritdoc/>
    protected override Task<ExercisePayload<TranslationConfig>> ToPayloadAsync(TranslationDraft draft,
        CreatorBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var config = new TranslationConfig
        {
            SourceLang = briefing.SourceLang,
            TargetLang = briefing.TargetLang,
            Items =
            [
                .. draft.Items.Select(i => new TranslationItem(i.Source.Trim(), i.Target.Trim(),
                    i.Alternatives?.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct().ToList() is { Count: > 0 } alts
                        ? alts
                        : null)),
            ],
        };

        return Task.FromResult(Payload(draft.Title, config, briefing, request));
    }
}
