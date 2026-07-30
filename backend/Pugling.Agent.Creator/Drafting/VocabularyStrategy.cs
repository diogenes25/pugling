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
    protected override string TaskInstruction(CreatorBriefing briefing, GenerationRequest request) =>
        $"""
        Entwirf eine Vokabelübung: Wortpaare {briefing.SourceLang} → {briefing.TargetLang}.
        Für jedes Paar: 'front' = Wort in der Lernsprache, 'back' = Übersetzung, 'hint' = optionaler,
        sehr kurzer Merkhinweis (Kontext oder Eselsbrücke – gern aus der Interessenwelt des Kindes).
        Verwende Grundformen (Nomen mit Artikel, Verben im Infinitiv) und keine ganzen Sätze.
        """;

    /// <inheritdoc/>
    protected override IReadOnlyList<string> Validate(VocabularyDraft draft, CreatorBriefing briefing,
        GenerationRequest request)
    {
        var violations = new Violations();
        // Lässt das Modell 'items' weg, steht hier null. Zur leeren Liste gemacht wird daraus der
        // Regelverstoß „zu wenige Aufgaben" – und damit eine Reparatur-Runde statt eines Absturzes.
        var items = draft.Items ?? [];
        DraftRules.Title(violations, draft.Title, briefing);
        DraftRules.Count(violations, items.Count, request);
        DraftRules.NoDuplicates(violations, items.Select(i => i?.Front), "Vokabeln");
        DraftRules.CoversRequiredWords(violations, briefing, items.Select(i => i?.Front), exact: true);

        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            DraftRules.NotBlank(violations, item?.Front, $"Vokabel {index + 1}: die Vorderseite");
            DraftRules.NotBlank(violations, item?.Back, $"Vokabel {index + 1}: die Rückseite");
            DraftRules.PromptDiffersFromAnswer(violations, item?.Front, item?.Back, index);
        }

        return violations.Messages;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExpectedAnswers(VocabularyDraft draft) =>
        [.. draft.Items.Select(i => i.Back.Trim())];

    /// <inheritdoc/>
    protected override async Task<ExercisePayload<VocabularyConfig>> ToPayloadAsync(VocabularyDraft draft,
        CreatorBriefing briefing, GenerationRequest request, CancellationToken ct)
    {
        var known = await LookupKnownPairsAsync(draft, briefing, ct);

        var config = new VocabularyConfig
        {
            Direction = "front-to-back",
            SourceLang = briefing.SourceLang,
            TargetLang = briefing.TargetLang,
            Items =
            [
                .. draft.Items.Select(item => known.TryGetValue(Key(item.Front, item.Back), out var vocabularyId)
                    // Bekanntes Paar: nur verlinken – Front/Back kommen dann aus dem Speicher.
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
