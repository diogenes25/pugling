using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Lückentext – der Typ, bei dem die Interessen am stärksten wirken: dieselben Lückenwörter, aber
/// Sätze aus der Lebenswelt des Kindes. Die riskanteste Stelle ist die Kopplung zwischen den
/// Platzhaltern im Text und den Lücken; genau die prüft der Validator vollständig nach.
/// </summary>
public sealed partial class ClozeStrategy(IChatClient chat, CreatorApi creator,
    IOptions<AgentOptions> options, ILogger<ClozeStrategy> logger)
    : ExerciseStrategy<ClozeDraft, ClozeConfig>(chat, creator, options, logger)
{
    /// <inheritdoc/>
    public override string TypeKey => "Cloze";

    /// <inheritdoc/>
    protected override string TitleOf(ClozeDraft draft) => draft.Title;

    /// <inheritdoc/>
    protected override string TaskInstruction(ChildBriefing briefing, GenerationRequest request) =>
        // Drei '$': so bleiben die Platzhalter {{1}} literal und nur {{{…}}} interpoliert.
        $$$"""
        Entwirf einen Lückentext in {{{request.SourceLang}}}: einen zusammenhängenden, kurzen Text, in dem
        an den Lernstellen Platzhalter stehen – {{1}}, {{2}}, {{3}} … in aufsteigender Reihenfolge.
        Zu jedem Platzhalter gehört genau ein Eintrag in 'gaps' mit derselben Nummer in 'index' und der
        richtigen Lösung in 'answer'. Die Lösung darf im Text sonst nirgends stehen.
        Fülle 'wordBank' mit allen Lösungen plus zwei bis vier plausiblen Ablenkern (gleiche Wortart).
        """;

    /// <inheritdoc/>
    protected override IReadOnlyList<string> Validate(ClozeDraft draft, ChildBriefing briefing,
        GenerationRequest request)
    {
        var violations = new Violations();
        DraftRules.Title(violations, draft.Title, briefing);
        DraftRules.NotBlank(violations, draft.Text, "Der Lückentext");
        DraftRules.Count(violations, draft.Gaps.Count, request);
        DraftRules.NoDuplicates(violations, draft.Gaps.Select(g => g.Index.ToString()), "Lücken-Nummern");
        DraftRules.CoversRequiredWords(violations, briefing, draft.Gaps.Select(g => g.Answer), exact: true);

        // Platzhalter im Text und Lücken müssen sich eins zu eins entsprechen – sonst zeigt die Übung
        // eine Lücke ohne Lösung (oder eine Lösung ohne Lücke).
        var placeholders = Placeholder().Matches(draft.Text ?? "")
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToList();
        var gapIndexes = draft.Gaps.Select(g => g.Index).ToList();

        foreach (var missing in placeholders.Except(gapIndexes))
            violations.Add($"Zum Platzhalter {{{{{missing}}}}} im Text fehlt die passende Lücke.");
        foreach (var orphan in gapIndexes.Except(placeholders))
            violations.Add($"Die Lücke mit index {orphan} hat keinen Platzhalter {{{{{orphan}}}}} im Text.");

        foreach (var gap in draft.Gaps)
        {
            DraftRules.NotBlank(violations, gap.Answer, $"Lücke {gap.Index}: die Lösung");
            if (!string.IsNullOrWhiteSpace(gap.Answer)
                && draft.Text?.Contains(gap.Answer.Trim(), StringComparison.OrdinalIgnoreCase) == true)
                violations.Add($"Lücke {gap.Index}: die Lösung '{gap.Answer}' steht im Text und ist damit verraten.");
        }

        // Die Standard-Abfrageform des Typs ist die Wortbank – ohne sie hätte das Kind keine Auswahl.
        var bank = draft.WordBank ?? [];
        violations.Require(bank.Count > 0, "Die Wortbank fehlt.");
        foreach (var answer in draft.Gaps.Select(g => g.Answer).Where(a => !string.IsNullOrWhiteSpace(a)))
            if (!bank.Any(w => string.Equals(w.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase)))
                violations.Add($"Die Wortbank enthält die Lösung '{answer}' nicht.");

        return violations.Messages;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExpectedAnswers(ClozeDraft draft) =>
        // Der Aufgaben-Index ist die Listenposition der Lücke (nicht ihre Nummer) – so liest sie der Server.
        [.. draft.Gaps.Select(g => g.Answer.Trim())];

    /// <inheritdoc/>
    protected override Task<ExercisePayload<ClozeConfig>> ToPayloadAsync(ClozeDraft draft,
        ChildBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var config = new ClozeConfig
        {
            Text = draft.Text.Trim(),
            Gaps = [.. draft.Gaps.Select(g => new Gap(g.Index, g.Answer.Trim(), Clean(g.Alternatives)))],
            WordBank = Clean(draft.WordBank),
        };

        return Task.FromResult(Payload(draft.Title, config, briefing, request));
    }

    /// <summary>Leere Einträge und Dubletten raus; komplett leere Listen werden zu <c>null</c>.</summary>
    private static List<string>? Clean(List<string>? values)
    {
        var cleaned = (values ?? [])
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return cleaned.Count > 0 ? cleaned : null;
    }

    [GeneratedRegex(@"\{\{(\d+)\}\}")]
    private static partial Regex Placeholder();
}
