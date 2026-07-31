using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// DB-backed resolution of an exercise's content into <see cref="ContentItem"/>s. For most types it
/// delegates to the stateless <see cref="ExerciseContentProvider"/>; for vocabulary exercises that
/// reference the store by key (<see cref="VocabularyConfig.Refs"/>), it loads the store vocabulary
/// entries (complex type) and builds the items from them. This keeps the same vocabulary item linked
/// across multiple exercises and centrally maintainable. Legacy vocabulary (inline
/// <see cref="VocabularyConfig.Items"/> only) still runs through the provider.
/// </summary>
public class ExerciseContentResolver(PuglingDbContext db, ExerciseContentProvider provider,
    ExerciseTypeRegistry registry, MediaSelector media)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The content of an exercise as a method-neutral item list (with store resolution for vocabulary/cloze).
    /// <paramref name="childId"/> is the <b>only</b> point where images come into play: only with a child can
    /// the matching one be picked from several renditions. Child-neutral callers (preview, evaluation, goal
    /// computation) omit it and get the same items without an image – deliberately explicit rather than
    /// implicit via a loaded navigation, otherwise the image would hinge on a forgotten <c>Include</c>.
    /// </summary>
    public async Task<IReadOnlyList<ContentItem>> ItemsOfAsync(Exercise exercise, int? childId = null,
        CancellationToken ct = default)
    {
        // Die Verzweigung folgt der StoreResolution-Fähigkeit des Typs (enum-frei); die DB-Logik bleibt hier.
        switch (registry.ByKey(exercise.Type)?.StoreResolution)
        {
            case StoreResolution.ItemTable:
                var vocab = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                    ? new VocabularyConfig()
                    : JsonSerializer.Deserialize<VocabularyConfig>(exercise.ConfigJson, JsonOptions) ?? new VocabularyConfig();
                return await ResolveVocabularyItemsAsync(exercise, vocab.Direction, childId, ct);

            case StoreResolution.VocabRefs:
                var cloze = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                    ? new ClozeConfig()
                    : JsonSerializer.Deserialize<ClozeConfig>(exercise.ConfigJson, JsonOptions) ?? new ClozeConfig();
                // Nur wenn mindestens eine Lücke den Store referenziert – sonst reicht die Inline-Projektion.
                if (cloze.Gaps.Any(g => !string.IsNullOrWhiteSpace(g.VocabKey)))
                    return await ResolveClozeRefsAsync(cloze, ct);
                break;
        }

        // Inline-Typen (inkl. Legacy-Vokabeln/Lückentexte ohne Store-Bezug): zustandslose Projektion aus der Config.
        return provider.ItemsOf(exercise);
    }

    /// <summary>
    /// Resolves the items of a vocabulary exercise from the <see cref="ExerciseItem"/> table: each row carries
    /// the stable <c>ItemId</c> and references the store via <c>VocabularyId</c> (word/translation/audio come
    /// live from there, centrally maintainable). The item index results from the list position (sorted by
    /// <see cref="ExerciseItem.OrderIndex"/>, Id) – this keeps it gapless/stable, independent of the sort key.
    /// An optional row hint overrides the derived store hint (e.g. article). If the store entry is missing,
    /// a placeholder stays at the same index (Leitner/test progress does not shift).
    /// <b>Without item rows the exercise is empty</b> – the item table is the only content source. There used
    /// to be a fallback onto the config projection here; it has been unreachable ever since the items are
    /// materialized (<c>VocabularyController.AfterSaveAsync</c> clears the config afterwards, and the seed
    /// runs it through <c>SeedExerciseItems</c>). Two content paths for the same type mean two truths: the
    /// fallback would have played a half-edited exercise with <i>old</i> items, without an ItemId and thus
    /// without learning progress. "Empty" is the right answer – and the preview endpoint says so
    /// (<c>exercise_empty</c>).
    /// <para>
    /// If a <paramref name="childId"/> is given, the <see cref="MediaSelector"/> additionally picks the image
    /// matching that child per item (one batch call for the whole exercise, no N+1 per card).
    /// </para>
    /// </summary>
    private async Task<IReadOnlyList<ContentItem>> ResolveVocabularyItemsAsync(Exercise exercise, string? direction,
        int? childId, CancellationToken ct)
    {
        var rows = await db.ExerciseItems.AsNoTracking()
            .Where(i => i.ExerciseId == exercise.Id)
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.VocabularyId, i.Hint })
            .ToListAsync(ct);
        if (rows.Count == 0) return [];

        var ids = rows.Select(r => r.VocabularyId).Distinct().ToList();
        var byId = await db.Vocabularies.AsNoTracking().Where(v => ids.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);

        var images = childId is { } cid
            ? await media.SelectForItemsAsync(cid, [.. rows.Select(r => (r.Id, r.VocabularyId))], ct: ct)
            : new Dictionary<int, SelectedMedia>();

        // Die Aussprache-Audioquelle gehört zum Wort und wird richtungsunabhängig mitgetragen (die Hör-Stufe
        // liest sie); WithDirection dreht Wort ↔ Übersetzung und bewahrt dabei ItemId/VocabularyId.
        return rows.Select((r, i) =>
        {
            if (!byId.TryGetValue(r.VocabularyId, out var v))
                return new ContentItem(i, $"(Vokabel #{r.VocabularyId} fehlt)", "", [""], ItemId: r.Id, VocabularyId: r.VocabularyId);
            var picked = images.GetValueOrDefault(r.Id);
            var item = new ContentItem(i, v.Word, v.Translation, [v.Translation],
                r.Hint ?? v.Noun?.Article, AudioUrl: v.PronunciationAudioUrl, ItemId: r.Id, VocabularyId: r.VocabularyId,
                ImageUrl: picked?.Url, ImageAlt: picked?.Alt);
            return ExerciseContentProvider.WithDirection(item, direction);
        }).ToList();
    }

    /// <summary>
    /// Builds the cloze items like the provider does, but pulls the solution for each gap from the vocabulary
    /// store when <see cref="Gap.VocabKey"/> is set (store word = missing word in the text; translation as the
    /// hint). Gaps without a key use the inline <see cref="Gap.Answer"/>. The item index stays the gap order –
    /// a missing key becomes a placeholder but does not shift any indices (Leitner/test progress stays stable).
    /// </summary>
    private async Task<IReadOnlyList<ContentItem>> ResolveClozeRefsAsync(ClozeConfig config, CancellationToken ct)
    {
        var keys = config.Gaps.Where(g => !string.IsNullOrWhiteSpace(g.VocabKey))
            .Select(g => g.VocabKey!).Distinct().ToList();
        var byKey = await db.Vocabularies.AsNoTracking()
            .Where(v => keys.Contains(v.Key))
            .ToDictionaryAsync(v => v.Key, ct);

        return config.Gaps.Select((g, i) =>
        {
            if (string.IsNullOrWhiteSpace(g.VocabKey))
                return new ContentItem(i, config.Text, g.Answer, Accepted(g.Answer, g.Alternatives), Hint: null, GapIndex: g.Index);
            if (byKey.TryGetValue(g.VocabKey, out var v))
                return new ContentItem(i, config.Text, v.Word, Accepted(v.Word, g.Alternatives), v.Translation, g.Index);
            // Fehlender Store-Key: Platzhalter auf gleichem Index (keine Lösung), damit sich nichts verschiebt.
            return new ContentItem(i, config.Text, "", [""], $"(Vokabel '{g.VocabKey}' fehlt)", g.Index);
        }).ToList();
    }

    // Lösung + Alternativen, roh (Normalisierung macht erst der AnswerGrader) – wie im Provider.
    private static IReadOnlyList<string> Accepted(string answer, IEnumerable<string>? alternatives) =>
        alternatives is null ? [answer] : [answer, .. alternatives];
}
