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
        // Fehlt 'gaps' im Modell-JSON, steht hier null – als leere Liste wird daraus ein Regelverstoß.
        var gaps = draft.Gaps ?? [];
        DraftRules.Title(violations, draft.Title, briefing);
        DraftRules.NotBlank(violations, draft.Text, "Der Lückentext");
        DraftRules.Count(violations, gaps.Count, request);
        DraftRules.CoversRequiredWords(violations, briefing, gaps.Select(g => g?.Answer), exact: true);

        // Platzhalter im Text und Lücken müssen sich eins zu eins entsprechen – sonst zeigt die Übung
        // eine Lücke ohne Lösung (oder eine Lösung ohne Lücke). Verglichen wird die ANZAHL je Nummer,
        // nicht die Mengendifferenz: ein Text mit zweimal {{3}} bestand die Except-Prüfung in beiden
        // Richtungen, der Server rendert daraus aber ein Feld mehr als es Lösungen gibt – ein Kästchen,
        // das das Kind nie richtig beantworten kann.
        var inText = Placeholder().Matches(draft.Text ?? "")
            .GroupBy(m => int.Parse(m.Groups[1].Value))
            .ToDictionary(g => g.Key, g => g.Count());
        var asGap = gaps.Where(g => g is not null)
            .GroupBy(g => g.Index)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var index in inText.Keys.Union(asGap.Keys).Order())
        {
            var (texts, entries) = (inText.GetValueOrDefault(index), asGap.GetValueOrDefault(index));
            // Genau einmal im Text und genau eine Lücke – Zahlengleichheit allein genügt nicht, sonst
            // ginge „zweimal {{3}} plus zwei Lücken mit index 3" als in Ordnung durch.
            if (texts == 1 && entries == 1) continue;

            violations.Add(
                entries == 0 ? $"Zum Platzhalter {{{{{index}}}}} im Text fehlt die passende Lücke."
                : texts == 0 ? $"Die Lücke mit index {index} hat keinen Platzhalter {{{{{index}}}}} im Text."
                : $"Der Platzhalter {{{{{index}}}}} steht {texts}× im Text, dazu gibt es {entries} Lücke(n) – "
                  + "jeder Platzhalter darf nur einmal vorkommen und braucht genau eine Lücke.");
        }

        foreach (var gap in gaps.Where(g => g is not null))
        {
            DraftRules.NotBlank(violations, gap.Answer, $"Lücke {gap.Index}: die Lösung");
            if (!string.IsNullOrWhiteSpace(gap.Answer)
                && draft.Text?.Contains(gap.Answer.Trim(), StringComparison.OrdinalIgnoreCase) == true)
                violations.Add($"Lücke {gap.Index}: die Lösung '{gap.Answer}' steht im Text und ist damit verraten.");
        }

        // Die Standard-Abfrageform des Typs ist die Wortbank – ohne sie hätte das Kind keine Auswahl.
        var bank = draft.WordBank ?? [];
        violations.Require(bank.Count > 0, "Die Wortbank fehlt.");
        foreach (var answer in gaps.Select(g => g?.Answer).Where(a => !string.IsNullOrWhiteSpace(a)))
            if (!bank.Any(w => string.Equals(w?.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase)))
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
