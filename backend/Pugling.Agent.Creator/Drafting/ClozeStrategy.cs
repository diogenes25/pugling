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
        request.SchoolTest ? SchoolTestInstruction(briefing, request) : $"""
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

    // B-524: the school's format - single sentences, a word of the unit's list per gap, German distractors.
    private static string SchoolTestInstruction(CreatorBriefing briefing, GenerationRequest request) =>
        $"""
        Entwirf einen Vokabeltest wie in der Schule: {request.ItemCount} einzelne, voneinander unabhängige Sätze in
        {briefing.SourceLang} in 'text', jeder in einer eigenen Zeile, nummeriert ("1. ", "2. " …), jeder mit genau
        einer Lücke. Jede Lücke ist ein anderes Wort aus der Wortliste oben – nimm KEINE anderen Wörter.
        Setze das Wort in eckige Klammern, und zwar in der Form, die der Satz verlangt, und schreibe dahinter in
        runden Klammern das Wort genau so, wie es in der Wortliste steht:
          The doctor sees forty [patients] (patient) a day.
          It is important for your [health] (health) to sleep well.
        Die runde Klammer sieht das Kind nicht; sie ordnet die Lücke der Vokabel zu.
        'distractors': zwei bis vier DEUTSCHE Wörter für den Wortkasten, die zu keiner Lücke gehören – am besten aus
        derselben Wortfamilie wie eine Lösung (zu "Rassismus" etwa "rassistisch"). Keine Übersetzung einer Lösung.
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
        if (request.SchoolTest)
        {
            // B-524: a school-test gap names its word of the list in the round bracket, in the base form a required
            // word is given in - the sentence form ("patients") would never equal it. And only words of the list can
            // be asked: a required word outside it (a weak word of an earlier unit) has no store entry in the slice,
            // so demanding it would contradict SchoolTestRules in every repair round.
            var listed = briefing.RequiredWords.Where(w => PairOf(briefing, w) is not null).ToList();
            DraftRules.CoversRequiredWords(violations, briefing with { RequiredWords = listed },
                ClozeMarkup.FormHints(parsed.Text).Select(h => h.Hint), exact: true);
        }
        else
        {
            DraftRules.CoversRequiredWords(violations, briefing, parsed.Gaps.Select(g => g.Answer), exact: true);
        }

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

        if (request.SchoolTest) SchoolTestRules(violations, parsed, briefing, draft);

        var distractors = Clean(draft.Distractors);
        violations.Require(distractors.Count >= MinDistractors,
            $"Es fehlen Ablenker: {distractors.Count} falsche Wörter in 'distractors', mindestens {MinDistractors}. " +
            "Ablenker sind Wörter, die NICHT in eine Lücke gehören – die Lösungen trägt die Wortbank von selbst.");

        return violations.Messages;
    }

    // B-524: every gap names its word of the list (the round bracket) - that is the link to the store entry the cue
    // box reads its meaning from. A word outside the list would leave the box without a meaning for that gap. And a
    // distractor that is the meaning of a gap would be a second right entry in the box.
    private static void SchoolTestRules(Violations violations, ClozeMarkupResult parsed, CreatorBriefing briefing,
        ClozeDraft draft)
    {
        var hints = ClozeMarkup.FormHints(parsed.Text).ToDictionary(h => h.Gap, h => h.Hint);
        for (var gap = 1; gap <= parsed.Gaps.Count; gap++)
        {
            if (!hints.TryGetValue(gap, out var word))
                violations.Add($"Lücke {gap}: es fehlt das Wort aus der Wortliste in runden Klammern hinter der Lücke.");
            else if (PairOf(briefing, word) is null)
                violations.Add($"Lücke {gap}: '{word}' steht nicht in der Wortliste – nimm nur Wörter von dort.");
        }

        var meanings = briefing.WordPairs.Select(p => p.Translation.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var distractor in Clean(draft.Distractors).Where(meanings.Contains))
            violations.Add($"Der Ablenker '{distractor}' ist die Bedeutung eines Wortes der Liste – nimm ein Wort, das zu keiner Lücke gehört.");
    }

    private static VocabPair? PairOf(CreatorBriefing briefing, string word) =>
        briefing.WordPairs.FirstOrDefault(p => string.Equals(p.Word.Trim(), word.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExpectedAnswers(ClozeDraft draft) =>
        // The task index is the gap's position in the text - that is how the server reads it.
        [.. ClozeMarkup.Parse(draft.Text).Gaps.Select(g => g.Answer)];

    /// <inheritdoc/>
    protected override async Task<ExercisePayload<ClozeConfig>> ToPayloadAsync(ClozeDraft draft,
        CreatorBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var parsed = ClozeMarkup.Parse(draft.Text);
        if (request.SchoolTest) return Payload(draft.Title, await SchoolTestConfigAsync(parsed, draft, briefing, ct), briefing, request);

        var config = new ClozeConfig
        {
            Text = parsed.Text.Trim(),
            Gaps = [.. parsed.Gaps.Select((g, i) => new Gap(i + 1, g.Answer, g.Alternatives.Count > 0 ? [.. g.Alternatives] : null))],
            // Sorted, not in text order: a bank listing the answers first, gap by gap, would be the answer key.
            WordBank = [.. parsed.Gaps.Select(g => g.Answer).Concat(Clean(draft.Distractors))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)],
        };

        return Payload(draft.Title, config, briefing, request);
    }

    /// <summary>
    /// The school test's config (B-524): each gap links its store entry (meaning for the cue box, learning progress)
    /// and carries the sentence form only where it differs from the stored word; the round brackets leave the text,
    /// the child never sees them. No English word bank - the cue box shows the meanings plus the German distractors.
    /// Validation has made sure every gap names a word of the list.
    /// </summary>
    private async Task<ClozeConfig> SchoolTestConfigAsync(ClozeMarkupResult parsed, ClozeDraft draft,
        CreatorBriefing briefing, CancellationToken ct)
    {
        var hints = ClozeMarkup.FormHints(parsed.Text).ToDictionary(h => h.Gap, h => h.Hint);
        var gaps = new List<Gap>(parsed.Gaps.Count);
        foreach (var (g, i) in parsed.Gaps.Select((g, i) => (g, i)))
        {
            var pair = PairOf(briefing, hints[i + 1])!;
            // The server addresses a store entry by key; the briefing carries its id.
            var key = (await Creator.GetVocabularyAsync(pair.VocabularyId, ct)).Key;
            var form = string.Equals(g.Answer.Trim(), pair.Word.Trim(), StringComparison.OrdinalIgnoreCase) ? null : g.Answer.Trim();
            gaps.Add(new Gap(i + 1, "", g.Alternatives.Count > 0 ? [.. g.Alternatives] : null, key, form));
        }
        return new ClozeConfig
        {
            Text = ClozeMarkup.WithoutFormHints(parsed.Text).Trim(),
            Gaps = gaps,
            CueDistractors = Clean(draft.Distractors),
        };
    }

    /// <summary>Strips empty entries and duplicates - a model in JSON mode is not held to the item type.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(v => v?.Trim() ?? "").Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
