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
        var pending = new List<(int Index, Vocabulary Vocab)>();
        for (var i = 0; i < config.Items.Count; i++)
        {
            var item = config.Items[i];
            if (item.VocabularyId is { } id)
                desired[i] = new DesiredItem(id, item.Hint);
            else
                // Front/back are guaranteed to be set here: ValidateConfigAsync rejects items without a
                // VocabularyId and without both (front + back) up front; the fallback only silences the nullable warning.
                pending.Add((i, await store.GetOrCreateAsync(config.SourceLang, item.Front ?? "", config.TargetLang, item.Back ?? "", ct: ct)));
        }
        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            foreach (var (i, vocab) in pending)
                desired[i] = new DesiredItem(vocab.Id, config.Items[i].Hint);
        }
        return desired.ToList();
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
