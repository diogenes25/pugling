using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Cloze text - the type where interests take effect most strongly: the same gap words, but sentences
/// from the child's world. The model marks each answer in square brackets; placeholders, gaps and word bank
/// are derived in C# (<see cref="ClozeMarkup"/>). Accepted spellings come only from inside a bracket, the word
/// bank's wrong words only from <see cref="ClozeDraft.Distractors"/> - in the old shape a model put its
/// distractors into a gap's alternatives, and the server then graded the wrong word as correct.
/// </summary>
public sealed class ClozeStrategy(IChatClient chat, CreatorApi creator,
    IOptions<AgentOptions> options, ILogger<ClozeStrategy> logger)
    : ExerciseStrategy<ClozeDraft, ClozeConfig>(chat, creator, options, logger)
{
    /// <summary>Fewest wrong words in the word bank - below that, the bank gives the answers away by elimination.</summary>
    public const int MinDistractors = 2;

    /// <inheritdoc/>
    public override string TypeKey => "Cloze";

    /// <inheritdoc/>
    protected override string TitleOf(ClozeDraft draft) => draft.Title;

    /// <inheritdoc/>
    protected override ClozeDraft WithTitle(ClozeDraft draft, string title) => draft with { Title = title };

    /// <inheritdoc/>
    protected override string TaskInstruction(CreatorBriefing briefing, GenerationRequest request) =>
        $"""
        Entwirf einen Lückentext in {briefing.SourceLang}: einen kurzen, zusammenhängenden Text in 'text'.
        Setze jede Lösung in eckige Klammern – genau das, was das Kind einsetzen soll, sonst nichts:
          Yesterday we [went] (go) to the Tower and [took] (take) a lot of photos.
        Fragst du eine Form ab (Zeitform, Frageanhängsel, Steigerung), schreibe die Grundform in runden Klammern
        dahinter – so hat jede Lücke genau eine richtige Lösung. Ist eine zweite Schreibweise ebenso richtig
        (Kurzform), trenne sie mit |: [didn't|did not]. In eine Klammer kommt NIE ein falsches Wort.
        Eine Lösung darf außerhalb der Klammern nicht im Text stehen.
        'distractors': zwei bis vier falsche, aber plausible Wörter derselben Art. Die Lösungen gehören NICHT hinein –
        die Wortbank enthält sie von selbst. In die runde Klammer hinter einer Lücke kommt genau ein Wort (die
        Grundform), nie eine Auswahlliste.
        """;

    /// <inheritdoc/>
    protected override ClozeDraft Normalize(ClozeDraft draft, CreatorBriefing briefing, GenerationRequest request)
    {
        var text = ClozeMarkup.Limit(draft.Text, request.ItemCount);
        // A distractor that is also an answer is only a duplicate in the word bank, which lists the answers anyway -
        // harmless, so it is dropped rather than sent back. Models read 'distractors' as "the word bank" and copy
        // every answer in (eight of ten in the second live run). The danger the old shape had, a wrong word ACCEPTED
        // as correct, cannot arise here: accepted spellings come only from inside a bracket.
        var accepted = ClozeMarkup.Parse(text).Gaps.SelectMany(g => g.Alternatives.Prepend(g.Answer))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return draft with
        {
            Text = text,
            Distractors = draft.Distractors?.Where(d => d is null || !accepted.Contains(d.Trim())).ToList(),
        };
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> Validate(ClozeDraft draft, CreatorBriefing briefing,
        GenerationRequest request)
    {
        var violations = new Violations();
        DraftRules.Title(violations, draft.Title, briefing);
        DraftRules.NotBlank(violations, draft.Text, "Der Lückentext");

        var parsed = ClozeMarkup.Parse(draft.Text);
        foreach (var error in parsed.Errors) violations.Add(error);
        DraftRules.Count(violations, parsed.Gaps.Count, request);
        DraftRules.CoversRequiredWords(violations, briefing, parsed.Gaps.Select(g => g.Answer), exact: true);

        // A form hint is one base form. A list in the brackets ("(Pokémon, friend, match)", second live run) puts the
        // answer right beside its gap and turns the gap into multiple choice without a word bank's distractors.
        foreach (var (gap, hint) in ClozeMarkup.FormHints(parsed.Text))
            if (hint.IndexOfAny([',', '/', ';']) >= 0 || hint.Contains(" or ", StringComparison.OrdinalIgnoreCase))
                violations.Add($"Lücke {gap}: in die runde Klammer hinter einer Lücke gehört genau eine Grundform, " +
                               $"keine Auswahl ('({hint})').");

        // Revealed means: standing as a word of its own outside the brackets. What remains of the text is what the
        // child reads - minus the base-form hints (part of the task) and the placeholders (a "3" is no "{{3}}").
        var readable = ClozeMarkup.ReadableText(parsed.Text);
        foreach (var (gap, index) in parsed.Gaps.Select((gap, index) => (gap, index)))
            if (ClozeMarkup.ContainsWord(readable, gap.Answer))
                violations.Add($"Lücke {index + 1}: die Lösung '{gap.Answer}' steht außerhalb der Klammern im Text " +
                               "und ist damit verraten.");

        var distractors = Clean(draft.Distractors);
        violations.Require(distractors.Count >= MinDistractors,
            $"Es fehlen Ablenker: {distractors.Count} falsche Wörter in 'distractors', mindestens {MinDistractors}. " +
            "Ablenker sind Wörter, die NICHT in eine Lücke gehören – die Lösungen trägt die Wortbank von selbst.");

        return violations.Messages;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExpectedAnswers(ClozeDraft draft) =>
        // The task index is the gap's position in the text - that is how the server reads it.
        [.. ClozeMarkup.Parse(draft.Text).Gaps.Select(g => g.Answer)];

    /// <inheritdoc/>
    protected override Task<ExercisePayload<ClozeConfig>> ToPayloadAsync(ClozeDraft draft,
        CreatorBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var parsed = ClozeMarkup.Parse(draft.Text);
        var config = new ClozeConfig
        {
            Text = parsed.Text.Trim(),
            Gaps = [.. parsed.Gaps.Select((g, i) => new Gap(i + 1, g.Answer, g.Alternatives.Count > 0 ? [.. g.Alternatives] : null))],
            // Sorted, not in text order: a bank listing the answers first, gap by gap, would be the answer key.
            WordBank = [.. parsed.Gaps.Select(g => g.Answer).Concat(Clean(draft.Distractors))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)],
        };

        return Task.FromResult(Payload(draft.Title, config, briefing, request));
    }

    /// <summary>Strips empty entries and duplicates - a model in JSON mode is not held to the item type.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(v => v?.Trim() ?? "").Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
