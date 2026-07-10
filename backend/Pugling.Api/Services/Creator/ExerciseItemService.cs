using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>Ein gewünschtes Item beim Abgleich: welche Store-Vokabel, mit optionalem übungslokalem Hinweis.</summary>
public readonly record struct DesiredItem(int VocabularyId, string? Hint);

/// <summary>
/// Pflegt die stabil identifizierten <see cref="ExerciseItem"/>-Zeilen einer Vokabelübung. Übersetzt die
/// Authoring-Form (<see cref="VocabularyConfig"/> mit inline <c>Items</c> bzw. ID-<c>Refs</c>) in Item-Zeilen
/// und gleicht sie <b>ID-erhaltend</b> ab: überlebende Wörter behalten ihre <see cref="ExerciseItem.Id"/> (die
/// „ItemId") und damit den je Kind daran hängenden Lernfortschritt; nur wegfallende Wörter verschwinden, neue
/// kommen hinzu. Zentrale Stelle für POST/PUT der Übung, den Tag-Snapshot und den einmaligen Backfill.
/// </summary>
public class ExerciseItemService(PuglingDbContext db, VocabularyStoreService store)
{
    /// <summary>Baut die gewünschte Item-Liste aus einer Vokabel-Config und legt inline genutzte Wörter im Store an.</summary>
    public async Task<List<DesiredItem>> DesiredFromConfigAsync(VocabularyConfig config, CancellationToken ct = default)
    {
        // Refs haben Vorrang (spiegelt die Auflösungspräzedenz des ExerciseContentResolver: Übung spielt aus
        // Refs ODER inline Items, nicht beides gemischt). Refs tragen keinen eigenen Hinweis (fällt auf Store).
        // Alt-Daten tragen die Referenz nur als Key (VocabularyId == 0) – diese Keys zu Ids auflösen, damit
        // key-basierte Refs beim Materialisieren nicht verlorengehen (der frühere Resolver löste sie per Key auf).
        if (config.Refs is { Count: > 0 } refs)
        {
            var keys = refs.Where(r => r.VocabularyId <= 0 && !string.IsNullOrEmpty(r.Key))
                .Select(r => r.Key!).Distinct().ToList();
            var idByKey = keys.Count == 0
                ? new Dictionary<string, int>()
                : await db.Vocabulary.Where(v => keys.Contains(v.Key)).ToDictionaryAsync(v => v.Key, v => v.Id, ct);
            var fromRefs = new List<DesiredItem>(refs.Count);
            foreach (var r in refs)
            {
                var id = r.VocabularyId > 0 ? r.VocabularyId
                    : r.Key is not null && idByKey.TryGetValue(r.Key, out var byKey) ? byKey : 0;
                if (id > 0) fromRefs.Add(new DesiredItem(id, null));
            }
            return fromRefs;
        }

        // Inline-Items: vorhandene Store-Id direkt übernehmen, fehlende anlegen (Id materialisiert erst nach Save).
        var desired = new DesiredItem[config.Items.Count];
        var pending = new List<(int Index, Vocabulary Vocab)>();
        for (var i = 0; i < config.Items.Count; i++)
        {
            var item = config.Items[i];
            if (item.VocabularyId is { } id)
                desired[i] = new DesiredItem(id, item.Hint);
            else
                // Front/Back sind hier garantiert gesetzt: ValidateConfigAsync lehnt Items ohne VocabularyId
                // und ohne beides (Front + Back) vorab ab; der Fallback verhindert nur die Nullable-Warnung.
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

    /// <summary>Materialisiert die Items einer Übung aus ihrer Config (Convenience über <see cref="ReconcileAsync"/>).</summary>
    public async Task SyncFromConfigAsync(int exerciseId, VocabularyConfig config, CancellationToken ct = default) =>
        await ReconcileAsync(exerciseId, await DesiredFromConfigAsync(config, ct), ct);

    /// <summary>
    /// Gleicht die Item-Zeilen einer Übung auf die gewünschte, geordnete Liste ab. Überlebende Vokabeln
    /// (per <see cref="ExerciseItem.VocabularyId"/> zugeordnet) behalten ihre Zeile und Id; wegfallende werden
    /// gelöscht, neue angelegt. <see cref="ExerciseItem.OrderIndex"/> spiegelt die Zielreihenfolge (0-basiert).
    /// </summary>
    public async Task ReconcileAsync(int exerciseId, IReadOnlyList<DesiredItem> desired, CancellationToken ct = default)
    {
        var pool = await db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).ToListAsync(ct);
        for (var i = 0; i < desired.Count; i++)
        {
            var d = desired[i];
            // Erste noch freie Zeile mit gleicher Vokabel wiederverwenden (bewahrt ItemId + Fortschritt).
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
        if (pool.Count > 0) db.ExerciseItems.RemoveRange(pool); // nicht mehr enthaltene Wörter
        await db.SaveChangesAsync(ct);
    }
}
