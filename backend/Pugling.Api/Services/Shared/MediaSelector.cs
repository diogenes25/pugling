using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>The image chosen for a child for a given carrier – ready to serve.</summary>
/// <param name="MediaAssetId">The chosen asset (for "reshuffle" and diagnostics).</param>
/// <param name="Url">URL of the variant for the requested purpose (or the next best one).</param>
/// <param name="Alt">Description of the asset – alt text for accessibility.</param>
public record SelectedMedia(int MediaAssetId, string Url, string Alt);

/// <summary>
/// Selects, for <b>a specific child</b>, the matching one from a motif's renditions – the place where
/// the media store and the child's profile meet and "many images" become one image.
/// <para>
/// The flow is deliberately in this order:
/// <list type="number">
/// <item><b>Candidates</b> – item links beat store links (specificity cascade). If the item has its own
/// images, <i>only</i> that set counts.</item>
/// <item><b>Hard filter</b> – eligibility against the child's content rating, dislike (negatively
/// weighted tag), or already rejected: out. A dislike does not rank lower, it excludes.</item>
/// <item><b>Frozen pick</b> – if there is a valid, not-rejected pick, it <i>always</i> wins. Image
/// constancy is the retention effect in vocabulary learning; newly added images must not upset it.</item>
/// <item><b>Score</b> – sum of interest weights over the tag intersection; the link's editorial rank
/// breaks ties, then a <b>deterministic</b> hash.</item>
/// <item><b>Freeze</b> – so step 3 applies next time.</item>
/// </list>
/// No match means <b>no image</b> – never a stopgap. An unillustrated card is better than a misleadingly
/// illustrated one.
/// </para>
/// </summary>
public class MediaSelector(PuglingDbContext db, ILogger<MediaSelector> logger)
{
    /// <summary>Theme tags weigh twice as much as style tags: <i>what</i> is shown binds more strongly than <i>how</i>.</summary>
    private const int ThemeFactor = 2;
    private const int StyleFactor = 1;

    /// <summary>
    /// Fallback order when the requested purpose has no variant: better an oversized image than none
    /// at all. The client can scale it down; it cannot replace a missing image.
    /// </summary>
    private static readonly MediaPurpose[] PurposeFallback =
        [MediaPurpose.Card, MediaPurpose.Full, MediaPurpose.Thumb, MediaPurpose.Hero];

    /// <summary>
    /// Selects for multiple carriers at once (one exercise = many items). Deliberately a batch: the
    /// alternative would be an N+1 over links, pick, and interests per card.
    /// </summary>
    /// <param name="childId">The child for whom the selection is made.</param>
    /// <param name="carriers">For each item, the stable item id and the store vocabulary entry behind it.</param>
    /// <param name="purpose">Desired delivery slot (exercise card = <see cref="MediaPurpose.Card"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The chosen image per item id; items with no match are missing from the map.</returns>
    public async Task<IReadOnlyDictionary<int, SelectedMedia>> SelectForItemsAsync(
        int childId, IReadOnlyList<(int ItemId, int VocabularyId)> carriers,
        MediaPurpose purpose = MediaPurpose.Card, CancellationToken ct = default)
    {
        var result = new Dictionary<int, SelectedMedia>();
        if (carriers.Count == 0) return result;

        var context = await LoadContextAsync(childId,
            [.. carriers.Select(c => c.ItemId)], [.. carriers.Select(c => c.VocabularyId)], ct);
        if (context is null) return result;

        var frozen = new List<ChildMediaPick>();
        foreach (var (itemId, vocabularyId) in carriers)
        {
            // Genauigkeits-Kaskade: hat das Item eigene Bilder, zählt ausschließlich diese Menge.
            var itemLinks = context.LinksByItem.GetValueOrDefault(itemId);
            var (links, carrier, carrierId) = itemLinks is { Count: > 0 }
                ? (itemLinks, Carrier.Item, itemId)
                : (context.LinksByVocabulary.GetValueOrDefault(vocabularyId) ?? [], Carrier.Vocabulary, vocabularyId);

            var chosen = Choose(context, links, carrier, carrierId, purpose, out var isNew);
            if (chosen is null) continue;

            result[itemId] = chosen.Value.Media;
            if (isNew) frozen.Add(NewPick(childId, carrier, carrierId, chosen.Value.Media.MediaAssetId));
        }

        // Zwei Items derselben Übung dürfen auf dieselbe Vokabel zeigen – dann fiele die Wahl zweimal
        // auf denselben Träger und der Unique-Index risse. Je Träger nur einmal einfrieren.
        if (frozen.Count > 0)
            db.ChildMediaPicks.AddRange(frozen
                .GroupBy(p => (p.VocabularyId, p.ExerciseItemId, p.MediaAssetId))
                .Select(g => g.First()));
        if (context.Superseded.Count > 0) db.ChildMediaPicks.RemoveRange(context.Superseded);

        if (frozen.Count > 0 || context.Superseded.Count > 0) await SaveFreezeAsync(ct);
        return result;
    }

    /// <summary>
    /// "Reshuffle": rejects the current pick and draws a new one. If there is no alternative, the
    /// existing pick stays <b>unchanged</b> – better to keep the same image than leave the child without one.
    /// </summary>
    /// <returns>The new image, or <c>null</c> if there was no alternative.</returns>
    public async Task<SelectedMedia?> ReshuffleAsync(int childId, int? vocabularyId, int? exerciseItemId,
        MediaPurpose purpose = MediaPurpose.Card, CancellationToken ct = default)
    {
        var carrier = exerciseItemId is not null ? Carrier.Item : Carrier.Vocabulary;
        var carrierId = exerciseItemId ?? vocabularyId!.Value;

        var context = await LoadContextAsync(childId,
            exerciseItemId is { } i ? [i] : [], vocabularyId is { } v ? [v] : [], ct);
        if (context is null) return null;

        var links = (carrier == Carrier.Item
            ? context.LinksByItem.GetValueOrDefault(carrierId)
            : context.LinksByVocabulary.GetValueOrDefault(carrierId)) ?? [];

        var current = context.ActivePick(carrier, carrierId);
        // Erst prüfen, ob es überhaupt eine Alternative gibt – sonst würde die Ablehnung den einzigen
        // Kandidaten verbrennen und die Karte dauerhaft bildlos machen.
        // Der Guard ist heute *nicht* die einzige Schranke: fällt er weg, findet die Neuwahl unten nichts
        // mehr und steigt vor SaveFreezeAsync aus – die Ablehnung bleibt ungespeichert, das Verhalten also
        // gleich. Genau darum blieb seine Entfernung in der Defektinjektion grün (docs/testplan.md, B02):
        // API-seitig ist er nicht beobachtbar und darum auch nicht testbar. Er bleibt trotzdem stehen und
        // ist Voraussetzung, nicht Zierde: wer unten ein SaveChanges vorzieht, verbrennt ohne ihn sofort
        // den letzten Kandidaten – und dann führt kein API-Weg zurück.
        var alternatives = links
            .Where(l => l.MediaAssetId != current?.MediaAssetId)
            .Where(l => Eligible(context, l, purpose))
            .ToList();
        if (alternatives.Count == 0) return null;

        if (current is not null)
        {
            current.Rejected = true;
            // Auch im Ausschluss-Set vermerken: sonst zöge die gleich folgende Auswahl genau das Bild
            // wieder, das eben abgelehnt wurde (das Set wird aus den Picks von vorhin gebaut).
            context.RejectedAssets.Add((current.VocabularyId, current.ExerciseItemId, current.MediaAssetId));
        }

        var chosen = Choose(context, links, carrier, carrierId, purpose, out var isNew);
        if (chosen is null) return null;

        if (isNew) db.ChildMediaPicks.Add(NewPick(childId, carrier, carrierId, chosen.Value.Media.MediaAssetId));
        if (context.Superseded.Count > 0) db.ChildMediaPicks.RemoveRange(context.Superseded);
        await SaveFreezeAsync(ct);
        return chosen.Value.Media;
    }

    /// <summary>
    /// Saves the freeze and the withdrawal – and swallows <b>exactly</b> the concurrent conflict.
    /// <para>
    /// Two simultaneous requests for the same child (React StrictMode double-invoke, a double-tap on
    /// "reload", two open tabs) freeze the same carrier or withdraw the same stale pick. The loser hits
    /// the filtered unique index or deletes an already-deleted row. Both are <b>harmless</b>, and not
    /// by accident: the selection is deterministic (same inputs, stable tiebreak), so the winner wrote
    /// exactly the same row. The conflict here always means "already done". Freezing is a cache fill,
    /// the result is already fixed – a 500 would be the only effect of a propagated error.
    /// </para>
    /// The affected entries are detached so that a later <c>SaveChanges</c> of the same request
    /// (e.g. recording a repetition) does not stumble over it again; the next request repeats the
    /// withdrawal.
    /// </summary>
    private async Task SaveFreezeAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            logger.LogDebug(e, "Bildwahl war nebenläufig schon eingefroren – die bestehende Wahl gilt.");
            foreach (var entry in db.ChangeTracker.Entries<ChildMediaPick>().ToList())
                entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// "Reshuffle" from the perspective of an <b>exercise card</b>. The client cannot address this
    /// itself: whether the pick hangs off the item (override) or the vocabulary entry only follows from
    /// the specificity cascade – and only the server knows that. That is why this overload takes both ids
    /// and determines the carrier the same way delivery does.
    /// </summary>
    public async Task<SelectedMedia?> ReshuffleForItemAsync(int childId, int itemId, int vocabularyId,
        MediaPurpose purpose = MediaPurpose.Card, CancellationToken ct = default)
    {
        var hasItemLinks = await db.MediaLinks.AsNoTracking().AnyAsync(l => l.ExerciseItemId == itemId, ct);
        return hasItemLinks
            ? await ReshuffleAsync(childId, null, itemId, purpose, ct)
            : await ReshuffleAsync(childId, vocabularyId, null, purpose, ct);
    }

    // ---- Auswahl ------------------------------------------------------------------------------------

    private enum Carrier { Vocabulary, Item }

    /// <summary>Chooses from a carrier's candidates; <paramref name="isNew"/> indicates whether a freeze is needed.</summary>
    private static (SelectedMedia Media, MediaLink Link)? Choose(SelectionContext ctx, IReadOnlyList<MediaLink> links,
        Carrier carrier, int carrierId, MediaPurpose purpose, out bool isNew)
    {
        isNew = false;
        var eligible = links.Where(l => Eligible(ctx, l, purpose)).ToList();
        if (eligible.Count == 0) return null;

        // Eine Einfrierung, die nicht mehr ausspielbar ist (Freigabe gesenkt, Abneigung ergänzt,
        // Zuordnung oder Variante gelöscht), wird ZURÜCKGEZOGEN und nicht bloß übergangen: sonst bliebe
        // sie die aktive Wahl, die Neuwahl fiele bei jedem Abruf erneut und das zweite Einfrieren risse
        // den Unique-Index – die Karte wäre dauerhaft nicht mehr abrufbar. Gelöscht statt abgelehnt:
        // „abgelehnt" heißt „nie wieder", der Grund hier ist aber nur vorübergehend (der Vater kann die
        // Freigabe wieder heben).
        foreach (var stale in ctx.ActivePicks(carrier, carrierId)
            .Where(p => eligible.All(l => l.MediaAssetId != p.MediaAssetId)).ToList())
            ctx.Supersede(stale);

        // Die eingefrorene Wahl gewinnt, solange sie noch zulässig ist – daran hängt der Merkeffekt.
        if (ctx.ActivePick(carrier, carrierId) is { } pick
            && eligible.FirstOrDefault(l => l.MediaAssetId == pick.MediaAssetId) is { } kept)
            return (Media(ctx, kept, purpose), kept);

        var best = eligible
            .OrderByDescending(l => Score(ctx, l))
            .ThenByDescending(l => l.Weight)
            .ThenBy(l => StableTiebreak(ctx.ChildId, carrierId, l.MediaAssetId))
            .First();

        isNew = true;
        return (Media(ctx, best, purpose), best);
    }

    /// <summary>
    /// Hard exclusions – <b>before</b> any scoring. Eligibility is the load-bearing axis of audience
    /// separation, a dislike must not be overridden by strong interests, and an asset without a
    /// deliverable file is of no use to anyone.
    /// </summary>
    private static bool Eligible(SelectionContext ctx, MediaLink link, MediaPurpose purpose)
    {
        var asset = link.MediaAsset!;
        if (asset.Rating > ctx.AllowedRating) return false;
        if (ctx.RejectedAssets.Contains((link.VocabularyId, link.ExerciseItemId, asset.Id))) return false;
        if (Tags(asset).Any(t => ctx.Weights.GetValueOrDefault(t.InterestTagId) < 0)) return false;
        return Variant(asset, purpose) is not null;
    }

    /// <summary>Sum of interest weights over the tag intersection (theme counts double).</summary>
    private static int Score(SelectionContext ctx, MediaLink link) =>
        Tags(link.MediaAsset!).Sum(t => ctx.Weights.GetValueOrDefault(t.InterestTagId)
            * (t.InterestTag!.Facet == InterestFacet.Style ? StyleFactor : ThemeFactor));

    private static IEnumerable<MediaTagLink> Tags(MediaAsset asset) =>
        asset.TagLinks.Where(t => t.InterestTag is not null);

    private static SelectedMedia Media(SelectionContext ctx, MediaLink link, MediaPurpose purpose)
    {
        var asset = link.MediaAsset!;
        return new SelectedMedia(asset.Id, Variant(asset, purpose)!.Url, asset.Description);
    }

    /// <summary>Variant for the requested purpose, otherwise the next best one; format tiebreak for reproducibility.</summary>
    private static MediaVariant? Variant(MediaAsset asset, MediaPurpose purpose)
    {
        if (asset.Variants.Count == 0) return null;
        var order = new[] { purpose }.Concat(PurposeFallback).Distinct().ToList();
        return asset.Variants
            .OrderBy(v => order.IndexOf(v.Purpose) is var i && i >= 0 ? i : int.MaxValue)
            .ThenBy(v => v.Format, StringComparer.Ordinal)
            .First();
    }

    /// <summary>
    /// Deterministic tiebreak (FNV-1a). Deliberately <b>no</b> <c>Random</c> and no
    /// <c>string.GetHashCode</c>: the latter is randomized per process, a restart would shift the pick
    /// for a carrier not yet frozen.
    /// </summary>
    private static uint StableTiebreak(int childId, int carrierId, int assetId)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var value in new[] { childId, carrierId, assetId })
                foreach (var b in BitConverter.GetBytes(value))
                {
                    hash ^= b;
                    hash *= 16777619u;
                }
            return hash;
        }
    }

    private static ChildMediaPick NewPick(int childId, Carrier carrier, int carrierId, int assetId) => new()
    {
        ChildId = childId,
        VocabularyId = carrier == Carrier.Vocabulary ? carrierId : null,
        ExerciseItemId = carrier == Carrier.Item ? carrierId : null,
        MediaAssetId = assetId,
    };

    // ---- Kontext-Ladung (ein Satz Queries statt N+1) --------------------------------------------------

    /// <summary>Everything the selection needs – loaded in a single pass.</summary>
    private sealed class SelectionContext
    {
        public required int ChildId { get; init; }
        public required ContentRating AllowedRating { get; init; }
        /// <summary>Interest weight per tag id (negative = dislike).</summary>
        public required Dictionary<int, int> Weights { get; init; }
        public required Dictionary<int, List<MediaLink>> LinksByVocabulary { get; init; }
        public required Dictionary<int, List<MediaLink>> LinksByItem { get; init; }
        public required List<ChildMediaPick> Picks { get; init; }
        /// <summary>Rejected combinations (carrier + asset) – quick to look up.</summary>
        public required HashSet<(int? VocabularyId, int? ItemId, int AssetId)> RejectedAssets { get; init; }

        /// <summary>
        /// Withdrawn freezes – to be deleted by the caller. Deliberately collected instead of removed
        /// immediately: the <see cref="PuglingDbContext"/> does not know this class, and the deletion
        /// belongs in the same <c>SaveChanges</c> as the new pick that replaces it.
        /// </summary>
        public List<ChildMediaPick> Superseded { get; } = [];

        public IEnumerable<ChildMediaPick> ActivePicks(Carrier carrier, int carrierId) => Picks.Where(p =>
            !p.Rejected && (carrier == Carrier.Item ? p.ExerciseItemId == carrierId : p.VocabularyId == carrierId));

        public ChildMediaPick? ActivePick(Carrier carrier, int carrierId) =>
            ActivePicks(carrier, carrierId).FirstOrDefault();

        /// <summary>Withdraws a freeze – immediately in the context too, so the new pick no longer sees it.</summary>
        public void Supersede(ChildMediaPick pick)
        {
            Picks.Remove(pick);
            Superseded.Add(pick);
        }
    }

    private async Task<SelectionContext?> LoadContextAsync(int childId, List<int> itemIds, List<int> vocabIds,
        CancellationToken ct)
    {
        var child = await db.Children.AsNoTracking()
            .Where(c => c.Id == childId)
            .Select(c => new { c.AllowedContentRating })
            .FirstOrDefaultAsync(ct);
        if (child is null) return null;

        var weights = await db.ChildInterests.AsNoTracking()
            .Where(i => i.ChildId == childId)
            .ToDictionaryAsync(i => i.InterestTagId, i => i.Weight, ct);

        // Die Zuordnungen samt Asset-Graph (Varianten + Tags) – ohne sie könnte weder gefiltert
        // (Rating/Abneigung/Variante) noch bewertet werden.
        var links = await db.MediaLinks.AsNoTracking()
            .Include(l => l.MediaAsset!).ThenInclude(a => a.Variants)
            .Include(l => l.MediaAsset!).ThenInclude(a => a.TagLinks).ThenInclude(t => t.InterestTag)
            .Where(l => (l.VocabularyId != null && vocabIds.Contains(l.VocabularyId.Value))
                || (l.ExerciseItemId != null && itemIds.Contains(l.ExerciseItemId.Value)))
            .ToListAsync(ct);

        // Getrackt laden: das Reshuffle setzt Rejected auf einer dieser Zeilen. Nach Id sortiert, damit
        // „die aktive Wahl" auch dann eindeutig dieselbe bleibt, wenn ein Träger (aus Altdaten) mehr als
        // eine trägt – sonst entschiede die Laune der Abfrage über das Bild.
        var picks = await db.ChildMediaPicks
            .Where(p => p.ChildId == childId
                && ((p.VocabularyId != null && vocabIds.Contains(p.VocabularyId.Value))
                    || (p.ExerciseItemId != null && itemIds.Contains(p.ExerciseItemId.Value))))
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        return new SelectionContext
        {
            ChildId = childId,
            AllowedRating = child.AllowedContentRating,
            Weights = weights,
            LinksByVocabulary = links.Where(l => l.VocabularyId is not null)
                .GroupBy(l => l.VocabularyId!.Value).ToDictionary(g => g.Key, g => g.ToList()),
            LinksByItem = links.Where(l => l.ExerciseItemId is not null)
                .GroupBy(l => l.ExerciseItemId!.Value).ToDictionary(g => g.Key, g => g.ToList()),
            Picks = picks,
            RejectedAssets = [.. picks.Where(p => p.Rejected)
                .Select(p => (p.VocabularyId, p.ExerciseItemId, p.MediaAssetId))],
        };
    }
}
