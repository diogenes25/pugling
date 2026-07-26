using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>Das für ein Kind gewählte Bild eines Trägers – fertig zum Ausliefern.</summary>
/// <param name="MediaAssetId">Das gewählte Asset (für „anderes Bild" und Diagnose).</param>
/// <param name="Url">URL der Variante im angefragten Zweck (oder der nächstbesten).</param>
/// <param name="Alt">Beschreibung des Assets – Alt-Text für die Barrierefreiheit.</param>
public record SelectedMedia(int MediaAssetId, string Url, string Alt);

/// <summary>
/// Wählt für <b>ein bestimmtes Kind</b> aus den Darstellungen eines Motivs die passende aus – die Stelle,
/// an der Medien-Store und Kind-Profil zusammenkommen und aus „viele Bilder" ein Bild wird.
/// <para>
/// Der Ablauf ist bewusst in dieser Reihenfolge:
/// <list type="number">
/// <item><b>Kandidaten</b> – Item-Zuordnungen schlagen Store-Zuordnungen (Genauigkeits-Kaskade). Hat das
/// Item eigene Bilder, gilt <i>nur</i> diese Menge.</item>
/// <item><b>Hart filtern</b> – Eignung über der Freigabe des Kindes, Abneigung (negativ gewichteter Tag)
/// oder bereits abgelehnt: raus. Eine Abneigung rankt nicht schlechter, sie schließt aus.</item>
/// <item><b>Eingefrorene Wahl</b> – gibt es eine gültige, nicht abgelehnte Wahl, gewinnt sie <i>immer</i>.
/// Bildkonstanz ist beim Vokabellernen der Merkeffekt; neu hinzugefügte Bilder dürfen ihn nicht kippen.</item>
/// <item><b>Bewerten</b> – Summe der Interessens-Gewichte über die Tag-Schnittmenge; der redaktionelle
/// Rang der Zuordnung bricht Gleichstände, danach ein <b>deterministischer</b> Hash.</item>
/// <item><b>Einfrieren</b> – damit Schritt 3 beim nächsten Mal greift.</item>
/// </list>
/// Kein Treffer heißt <b>kein Bild</b> – nie ein Notnagel. Eine unbebilderte Karte ist besser als eine
/// irreführend bebilderte.
/// </para>
/// </summary>
public class MediaSelector(PuglingDbContext db, ILogger<MediaSelector> logger)
{
    /// <summary>Themen-Tags wiegen doppelt so schwer wie Stil-Tags: <i>was</i> zu sehen ist, bindet stärker als <i>wie</i>.</summary>
    private const int ThemeFactor = 2;
    private const int StyleFactor = 1;

    /// <summary>
    /// Fallback-Reihenfolge, wenn der gefragte Zweck keine Variante hat: lieber ein zu großes Bild als
    /// gar keines. Der Client skaliert; ein fehlendes Bild kann er nicht ersetzen.
    /// </summary>
    private static readonly MediaPurpose[] PurposeFallback =
        [MediaPurpose.Card, MediaPurpose.Full, MediaPurpose.Thumb, MediaPurpose.Hero];

    /// <summary>
    /// Wählt für mehrere Träger auf einmal (eine Übung = viele Items). Bewusst als Batch: die
    /// Alternative wäre ein N+1 über Zuordnungen, Wahl und Interessen pro Karte.
    /// </summary>
    /// <param name="childId">Das Kind, für das gewählt wird.</param>
    /// <param name="carriers">Je Item die stabile Item-Id und die dahinterliegende Store-Vokabel.</param>
    /// <param name="purpose">Gewünschter Auslieferungs-Slot (Übungskarte = <see cref="MediaPurpose.Card"/>).</param>
    /// <param name="ct">Abbruch-Token.</param>
    /// <returns>Je Item-Id das gewählte Bild; Items ohne Treffer fehlen in der Map.</returns>
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
    /// „Anderes Bild": lehnt die aktuelle Wahl ab und zieht neu. Gibt es keine Alternative, bleibt der
    /// Bestand <b>unverändert</b> – lieber dasselbe Bild behalten als das Kind bildlos zurücklassen.
    /// </summary>
    /// <returns>Das neue Bild, oder <c>null</c>, wenn es keine Alternative gab.</returns>
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
    /// Speichert Einfrierung und Rückzug – und verschluckt <b>genau</b> den nebenläufigen Konflikt.
    /// <para>
    /// Zwei gleichzeitige Abrufe für dasselbe Kind (React-StrictMode-Doppelaufruf, Doppeltipp auf
    /// „neu laden", zwei offene Tabs) frieren denselben Träger ein bzw. ziehen dieselbe veraltete Wahl
    /// zurück. Der Verlierer läuft in den gefilterten Unique-Index oder löscht eine schon gelöschte Zeile.
    /// Beides ist <b>harmlos</b>, und zwar nicht zufällig: die Auswahl ist deterministisch (gleiche
    /// Eingaben, stabiler Tiebreak), der Gewinner hat also genau dieselbe Zeile geschrieben. Der Konflikt
    /// heißt hier immer „schon erledigt". Das Einfrieren ist ein Cache-Auffüllen, das Ergebnis steht
    /// bereits fest – ein 500 wäre die einzige Wirkung eines durchgereichten Fehlers.
    /// </para>
    /// Die betroffenen Einträge werden abgehängt, damit ein späteres <c>SaveChanges</c> desselben
    /// Requests (z. B. das Buchen einer Wiederholung) nicht erneut darüber stolpert; den Rückzug
    /// wiederholt der nächste Abruf.
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
    /// „Anderes Bild" aus Sicht einer <b>Übungskarte</b>. Der Client kann das nicht selbst adressieren:
    /// ob die Wahl am Item (Übersteuerung) oder an der Vokabel hängt, ergibt sich erst aus der
    /// Genauigkeits-Kaskade – und die kennt nur der Server. Deshalb nimmt diese Überladung beide Ids und
    /// bestimmt den Träger genauso wie die Ausspielung.
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

    /// <summary>Wählt aus den Kandidaten eines Trägers; <paramref name="isNew"/> zeigt an, ob eingefroren werden muss.</summary>
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
    /// Harte Ausschlüsse – <b>vor</b> jeder Bewertung. Die Eignung ist die tragende Achse der
    /// Zielgruppen-Trennung, eine Abneigung darf nicht durch starke Interessen überstimmt werden, und ein
    /// Asset ohne ausspielbare Datei nützt niemandem.
    /// </summary>
    private static bool Eligible(SelectionContext ctx, MediaLink link, MediaPurpose purpose)
    {
        var asset = link.MediaAsset!;
        if (asset.Rating > ctx.AllowedRating) return false;
        if (ctx.RejectedAssets.Contains((link.VocabularyId, link.ExerciseItemId, asset.Id))) return false;
        if (Tags(asset).Any(t => ctx.Weights.GetValueOrDefault(t.InterestTagId) < 0)) return false;
        return Variant(asset, purpose) is not null;
    }

    /// <summary>Summe der Interessens-Gewichte über die Tag-Schnittmenge (Thema zählt doppelt).</summary>
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

    /// <summary>Variante im gefragten Zweck, sonst die nächstbeste; Format-Tiebreak für Reproduzierbarkeit.</summary>
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
    /// Deterministischer Tiebreak (FNV-1a). Bewusst <b>kein</b> <c>Random</c> und kein
    /// <c>string.GetHashCode</c>: Letzterer ist pro Prozess randomisiert, ein Neustart würde die Wahl
    /// eines noch nicht eingefrorenen Trägers verschieben.
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

    /// <summary>Alles, was die Auswahl braucht – in einem Rutsch geladen.</summary>
    private sealed class SelectionContext
    {
        public required int ChildId { get; init; }
        public required ContentRating AllowedRating { get; init; }
        /// <summary>Interessens-Gewicht je Tag-Id (negativ = Abneigung).</summary>
        public required Dictionary<int, int> Weights { get; init; }
        public required Dictionary<int, List<MediaLink>> LinksByVocabulary { get; init; }
        public required Dictionary<int, List<MediaLink>> LinksByItem { get; init; }
        public required List<ChildMediaPick> Picks { get; init; }
        /// <summary>Abgelehnte Kombinationen (Träger + Asset) – schnell nachschlagbar.</summary>
        public required HashSet<(int? VocabularyId, int? ItemId, int AssetId)> RejectedAssets { get; init; }

        /// <summary>
        /// Zurückgezogene Einfrierungen – vom Aufrufer zu löschen. Bewusst gesammelt statt sofort
        /// entfernt: das <see cref="PuglingDbContext"/> kennt diese Klasse nicht, und die Löschung
        /// gehört in dasselbe <c>SaveChanges</c> wie die Neuwahl, die sie ersetzt.
        /// </summary>
        public List<ChildMediaPick> Superseded { get; } = [];

        public IEnumerable<ChildMediaPick> ActivePicks(Carrier carrier, int carrierId) => Picks.Where(p =>
            !p.Rejected && (carrier == Carrier.Item ? p.ExerciseItemId == carrierId : p.VocabularyId == carrierId));

        public ChildMediaPick? ActivePick(Carrier carrier, int carrierId) =>
            ActivePicks(carrier, carrierId).FirstOrDefault();

        /// <summary>Zieht eine Einfrierung zurück – sofort auch im Kontext, damit die Neuwahl sie nicht mehr sieht.</summary>
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
