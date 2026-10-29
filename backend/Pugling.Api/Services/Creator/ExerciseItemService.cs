using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>A desired item for reconciliation: which store vocabulary entry, with an optional exercise-local hint.</summary>
public readonly record struct DesiredItem(int VocabularyId, string? Hint);

/// <summary>
/// Maintains the stably identified <see cref="ExerciseItem"/> rows of a vocabulary exercise. Translates the
/// authoring form (<see cref="VocabularyConfig"/> with inline <c>Items</c> or ID <c>Refs</c>) into item rows
/// and reconciles them <b>ID-preservingly</b>: surviving words keep their <see cref="ExerciseItem.Id"/> (the
/// "ItemId") and thus the per-child learning progress attached to it; only dropped words disappear, new
/// ones are added. Central place for POST/PUT of the exercise, the tag snapshot, and the one-time backfill.
/// </summary>
public class ExerciseItemService(PuglingDbContext db, VocabularyStoreService store)
{
    /// <summary>Builds the desired item list from a vocabulary config and creates inline-used words in the store.</summary>
    public async Task<List<DesiredItem>> DesiredFromConfigAsync(VocabularyConfig config, CancellationToken ct = default)
    {
        // Refs take precedence (mirrors the resolution precedence of ExerciseContentResolver: an exercise plays
        // refs OR inline items, never a mix). Refs carry no hint of their own (it falls back to the store).
        // Legacy data carries the reference as a key only (VocabularyId == 0) - resolve those keys to ids so
        // that key-based refs are not lost while materializing (the former resolver resolved them by key).
        if (config.Refs is { Count: > 0 } refs)
        {
            var keys = refs.Where(r => r.VocabularyId <= 0 && !string.IsNullOrEmpty(r.Key))
                .Select(r => r.Key!).Distinct().ToList();
            var idByKey = keys.Count == 0
                ? new Dictionary<string, int>()
                : await db.Vocabularies.Where(v => keys.Contains(v.Key)).ToDictionaryAsync(v => v.Key, v => v.Id, ct);
            var fromRefs = new List<DesiredItem>(refs.Count);
            foreach (var r in refs)
            {
                var id = r.VocabularyId > 0 ? r.VocabularyId
                    : r.Key is not null && idByKey.TryGetValue(r.Key, out var byKey) ? byKey : 0;
                if (id > 0) fromRefs.Add(new DesiredItem(id, null));
            }
            return fromRefs;
        }

        // Inline items: take an existing store id as is, create the missing ones (the id materializes only after save).
        var desired = new DesiredItem[config.Items.Count];
        // Front/back are guaranteed to be set here: ValidateConfigAsync rejects items without a VocabularyId and
        // without both (front + back) up front; the fallback only silences the nullable warning.
        var inline = InlineItems(config);
        for (var i = 0; i < config.Items.Count; i++)
            if (config.Items[i].VocabularyId is { } id)
                desired[i] = new DesiredItem(id, config.Items[i].Hint);
        if (inline.Count > 0)
        {
            var vocabs = await store.GetOrCreateManyAsync(config.SourceLang, config.TargetLang,
                inline.Select(x => x.Pair).ToList(), ct: ct);
            await db.SaveChangesAsync(ct);
            for (var n = 0; n < inline.Count; n++)
                desired[inline[n].Index] = new DesiredItem(vocabs[n].Id, config.Items[inline[n].Index].Hint);
        }
        return desired.ToList();
    }

    /// <summary>
    /// The store ids a config would reconcile to, without creating anything: an inline word that is not in the
    /// store yet yields <c>null</c> (it would become a new entry). For the lock check before a PUT writes.
    /// </summary>
    public async Task<List<int?>> PeekDesiredIdsAsync(VocabularyConfig config, CancellationToken ct)
    {
        // Refs first, like DesiredFromConfigAsync. Key-only legacy refs are not resolved here: ValidateConfigAsync
        // rejects VocabularyId <= 0 before this runs, and if that ever changed the result would be an extra 409, not a bypass.
        if (config.Refs is { Count: > 0 } refs) return refs.Select(r => (int?)r.VocabularyId).ToList();
        // The same content-based lookup as the reconcile, so the lock and duplicate checks judge the entry it will link.
        var found = await store.FindManyAsync(config.SourceLang, config.TargetLang, InlineItems(config).Select(x => x.Pair), ct);
        return config.Items.Select(item => item.VocabularyId is { } id ? id
            : found.TryGetValue(VocabKey.Generate(config.SourceLang, item.Front ?? "", config.TargetLang, item.Back ?? ""), out var v)
              && v.Id > 0 ? v.Id : (int?)null).ToList();
    }

    private static List<(int Index, (string Word, string Translation) Pair)> InlineItems(VocabularyConfig config) =>
        config.Items.Select((item, i) => (Index: i, Item: item))
            .Where(x => x.Item.VocabularyId is null)
            .Select(x => (x.Index, (x.Item.Front ?? "", x.Item.Back ?? "")))
            .ToList();

    /// <summary>
    /// Whether reconciling to <paramref name="desiredVocabularyIds"/> would move a word the exercise already has.
    /// Plan progress hangs on the item's position (<see cref="PositionItemProgress.ItemIndex"/>), so the current
    /// items have to stay the leading part of the new list in the same order - only appending is shift-free.
    /// </summary>
    public async Task<bool> WouldShiftAsync(int exerciseId, IReadOnlyList<int?> desiredVocabularyIds, CancellationToken ct)
    {
        var current = await db.ExerciseItems.Where(i => i.ExerciseId == exerciseId)
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.Id)
            .Select(i => (int?)i.VocabularyId)
            .ToListAsync(ct);
        return current.Count > desiredVocabularyIds.Count || !current.SequenceEqual(desiredVocabularyIds.Take(current.Count));
    }

    /// <summary>Materializes the items of an exercise from its config (convenience wrapper over <see cref="ReconcileAsync"/>).</summary>
    public async Task SyncFromConfigAsync(int exerciseId, VocabularyConfig config, CancellationToken ct = default) =>
        await ReconcileAsync(exerciseId, await DesiredFromConfigAsync(config, ct), ct);

    /// <summary>
    /// Reconciles the item rows of an exercise against the desired, ordered list. Surviving vocabulary
    /// entries (matched by <see cref="ExerciseItem.VocabularyId"/>) keep their row and id; dropped ones are
    /// deleted, new ones created. <see cref="ExerciseItem.OrderIndex"/> reflects the target order (0-based).
    /// </summary>
    public async Task ReconcileAsync(int exerciseId, IReadOnlyList<DesiredItem> desired, CancellationToken ct = default)
    {
        var pool = await db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).ToListAsync(ct);
        for (var i = 0; i < desired.Count; i++)
        {
            var d = desired[i];
            // Reuse the first still-free row with the same vocabulary entry (preserves ItemId + progress).
            var match = pool.FirstOrDefault(r => r.VocabularyId == d.VocabularyId);
            if (match is not null)
            {
                pool.Remove(match);
                match.OrderIndex = i;
                match.Hint = d.Hint;
            }
            else
            {
                db.ExerciseItems.Add(new ExerciseItem
                {
                    ExerciseId = exerciseId,
                    VocabularyId = d.VocabularyId,
                    Hint = d.Hint,
                    OrderIndex = i,
                });
            }
        }
        if (pool.Count > 0) db.ExerciseItems.RemoveRange(pool); // words no longer contained
        await db.SaveChangesAsync(ct);
    }
}
