using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Briefing;
using Pugling.Client;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Vocabulary exercise. The vocabulary is the actual learning material - here the interests only affect
/// the hint and the word selection when no required vocabulary is prescribed. Before creation, a
/// <b>lookup against the vocabulary store</b> runs: an exactly matching pair is reused instead of
/// duplicated, everything else the server creates anew and links when saving.
/// </summary>
public sealed class VocabularyStrategy(IChatClient chat, CreatorApi creator,
    IOptions<AgentOptions> options, ILogger<VocabularyStrategy> logger)
    : ExerciseStrategy<VocabularyDraft, VocabularyConfig>(chat, creator, options, logger)
{
    /// <inheritdoc/>
    public override string TypeKey => "Vocabulary";

    /// <inheritdoc/>
    protected override string TitleOf(VocabularyDraft draft) => draft.Title;

    /// <inheritdoc/>
    protected override VocabularyDraft WithTitle(VocabularyDraft draft, string title) => draft with { Title = title };

    /// <summary>
    /// The pairs this exercise consists of when the unit has a word list and nothing more specific was asked for
    /// (<c>--words</c>, weak words): then the program decides the words and their translations, the model only the
    /// title and the hints. Empty means the model picks the pairs itself, as before - also when the list is too
    /// short for an exercise at all: a fixed list of two words would fail the same way in every repair round.
    /// </summary>
    private static IReadOnlyList<VocabPair> FixedPairs(CreatorBriefing briefing) =>
        briefing.RequiredWords.Count == 0 && briefing.WordPairs.Count >= DraftRules.MinItems ? briefing.WordPairs : [];

    /// <inheritdoc/>
    protected override string TaskInstruction(CreatorBriefing briefing, GenerationRequest request) =>
        FixedPairs(briefing).Count > 0
            ? $"""
              Entwirf eine Vokabelübung aus den Wortpaaren im Abschnitt „Wortschatz dieser Übung“: übernimm sie
              unverändert und in dieser Reihenfolge – 'front' = Wort, 'back' = Übersetzung. Deine Aufgabe ist nur
              'hint': ein sehr kurzer deutscher Merkhinweis je Wort (Situation oder Eselsbrücke, gern aus der
              Interessenwelt des Kindes), der die Übersetzung NICHT enthält. Der Titel nennt den Stoff (Unit,
              Wortfeld), nicht die Interessen.
              """
            : $"""
              Entwirf eine Vokabelübung: Wortpaare {briefing.SourceLang} → {briefing.TargetLang}.
              Für jedes Paar: 'front' = Wort in der Lernsprache, 'back' = Übersetzung, 'hint' = optionaler,
              sehr kurzer Merkhinweis (Kontext oder Eselsbrücke – gern aus der Interessenwelt des Kindes).
              Verwende Grundformen (Nomen mit Artikel, Verben im Infinitiv) und keine ganzen Sätze.
              """;

    /// <inheritdoc/>
    protected override VocabularyDraft Normalize(VocabularyDraft draft, CreatorBriefing briefing, GenerationRequest request)
    {
        var fixedPairs = FixedPairs(briefing);
        if (fixedPairs.Count == 0)
            return draft with { Items = Take(draft.Items, request.ItemCount)?.Select(DropRevealingHint).ToList()! };

        // The words are set: whatever the model wrote for front and back is replaced by the unit's pair, and only a
        // hint it attached to that very word survives. A model that reorders, drops or "corrects" a word can no
        // longer cost a repair round or put its own translation into the catalog.
        var hints = (draft.Items ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i?.Front) && !string.IsNullOrWhiteSpace(i?.Hint))
            .GroupBy(i => i.Front.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Hint, StringComparer.OrdinalIgnoreCase);
        return draft with
        {
            // The model's hint first (it is the one dressed for the child), the book's when the model gave none or
            // one that named the answer.
            Items = [.. fixedPairs.Select(p =>
            {
                var item = DropRevealingHint(new VocabularyDraftItem(p.Word, p.Translation, hints.GetValueOrDefault(p.Word)));
                return item.Hint is null ? item with { Hint = p.Hint } : item;
            })],
        };
    }

    /// <summary>
    /// Drops a hint that names the translation ("die Kathedrale in London" for <i>cathedral</i>): the card would ask
    /// for a word it shows. Dropping is the whole repair - a card without a hint is still a good card.
    /// </summary>
    private static VocabularyDraftItem DropRevealingHint(VocabularyDraftItem item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Hint) || string.IsNullOrWhiteSpace(item.Back)) return item!;

        var revealing = item.Back.Split([' ', ',', ';', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            // Articles and short function words ("der", "sich", "etw.") say nothing about the answer.
            .Where(w => w.Trim('.').Length > 3)
            // The word start is enough: "Kathedralen" in the hint gives "Kathedrale" away just as well.
            .Any(w => ClozeMarkup.ContainsWordStart(item.Hint, w.Trim('.')));
        return revealing ? item with { Hint = null } : item;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> Validate(VocabularyDraft draft, CreatorBriefing briefing,
        GenerationRequest request)
    {
        var violations = new Violations();
        // If the model omits 'items' this is null. Turned into an empty list it becomes the rule violation
        // "too few tasks" - and thus a repair round instead of a crash.
        var items = draft.Items ?? [];
        var fixedPairs = FixedPairs(briefing);
        DraftRules.Title(violations, draft.Title, briefing);
        // A word list shorter than the count yields fewer cards than asked for; that is the book's limit, not a
        // fault of the draft.
        if (fixedPairs.Count == 0) DraftRules.Count(violations, items.Count, request);
        // Folded like the server folds a word: it refuses a second item of the same word, and that 400 would end the
        // run instead of a repair round (B-336).
        DraftRules.NoDuplicates(violations, items.Select(i => i?.Front), "Vokabeln", VocabKey.Part);
        DraftRules.CoversRequiredWords(violations, briefing, items.Select(i => i?.Front), exact: true);

        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            DraftRules.NotBlank(violations, item?.Front, $"Vokabel {index + 1}: die Vorderseite");
            DraftRules.NotBlank(violations, item?.Back, $"Vokabel {index + 1}: die Rückseite");
            // "test = Test" is right when the book says so; the rule is for pairs a model made up.
            if (fixedPairs.Count == 0) DraftRules.PromptDiffersFromAnswer(violations, item?.Front, item?.Back, index);
        }

        return violations.Messages;
    }

    /// <summary>
    /// The front side: the self-test plays the type's preview stage, free text, and on every writing stage the child writes
    /// the learnt word whatever the direction (B-597). The exercise sets no stage of its own, so that is what is asked.
    /// </summary>
    protected override IReadOnlyList<string> ExpectedAnswers(VocabularyDraft draft) =>
        [.. draft.Items.Select(i => i.Front.Trim())];

    /// <inheritdoc/>
    protected override async Task<ExercisePayload<VocabularyConfig>> ToPayloadAsync(VocabularyDraft draft,
        CreatorBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var known = await LookupKnownPairsAsync(draft, briefing, ct);
        // The unit's own pairs are known by id already - no lookup could match them better.
        foreach (var pair in FixedPairs(briefing)) known[Key(pair.Word, pair.Translation)] = pair.VocabularyId;

        var config = new VocabularyConfig
        {
            Direction = "front-to-back",
            SourceLang = briefing.SourceLang,
            TargetLang = briefing.TargetLang,
            Items =
            [
                .. draft.Items.Select(item => known.TryGetValue(Key(item.Front, item.Back), out var vocabularyId)
                    // Known pair: just link it - front/back then come from the store.
                    ? new VocabItem(Hint: Blank(item.Hint), VocabularyId: vocabularyId)
                    : new VocabItem(item.Front.Trim(), item.Back.Trim(), Blank(item.Hint))),
            ],
        };

        return Payload(draft.Title, config, briefing, request);
    }

    /// <summary>
    /// Queries the store for the draft's words. Reuse happens only for an <b>exactly matching pair</b>
    /// (word and translation): an identically spelled word with a different meaning would be a different
    /// vocabulary item - and the self-test would rightly fail.
    /// </summary>
    private async Task<Dictionary<string, int>> LookupKnownPairsAsync(VocabularyDraft draft,
        CreatorBriefing briefing, CancellationToken ct)
    {
        var words = draft.Items.Select(i => i.Front.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var lookup = await Creator.LookupVocabularyAsync(
            new LookupRequest(briefing.SourceLang, briefing.TargetLang, words, null), ct);

        var known = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var result in lookup.Words.Where(w => w.Exists))
            foreach (var match in result.Matches)
                known.TryAdd(Key(match.Word, match.Translation), match.Id);

        return known;
    }

    private static string Key(string front, string back) => $"{front.Trim()}\0{back.Trim()}";

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
