using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Die eine Stelle, an der aus Text ein <see cref="InterestTag"/> wird. Drei Wege münden hier
/// (Creator taggt ein Bild, Supervisor tippt ein Interesse, Backfill übernimmt Freitext-Interessen) –
/// liefen sie getrennt, zerfiele die <b>geteilte</b> Taxonomie in Dubletten und das Matching
/// „Bild ↔ Kind" ginge genau dort ins Leere, wo es gebraucht wird.
/// </summary>
public class InterestTagService(PuglingDbContext db)
{
    /// <summary>
    /// Findet das Schlagwort zu einem Text oder legt es an. Die Suche läuft in zwei Stufen: erst der
    /// indizierte Slug-Treffer, sonst ein Synonym-Abgleich (Synonyme liegen als JSON-Spalte und sind
    /// nicht abfragbar – der Scan lohnt nur, weil er ausschließlich beim Fehlschlag greift).
    /// </summary>
    /// <param name="text">Slug oder Anzeigename („pokemon", „Pokémon", „Poke").</param>
    /// <param name="label">Anzeigename für einen ggf. neu angelegten Tag; sonst wird <paramref name="text"/> genommen.</param>
    /// <param name="facet">Facette für einen ggf. neu angelegten Tag.</param>
    /// <param name="ct">Abbruch-Token.</param>
    /// <returns>Der gefundene/angelegte Tag – oder <c>null</c>, wenn der Text keinen gültigen Slug ergibt.</returns>
    public async Task<InterestTag?> EnsureAsync(string text, string? label = null,
        InterestFacet? facet = null, CancellationToken ct = default)
    {
        var slug = InterestSlug.From(text);
        if (slug.Length == 0) return null;

        var bySlug = await db.InterestTags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        if (bySlug is not null) return bySlug;

        // Der Slug ist neu – bevor eine Dublette entsteht, gegen die Synonyme der bestehenden Tags prüfen.
        var bySynonym = (await db.InterestTags.ToListAsync(ct))
            .FirstOrDefault(t => t.Synonyms.Any(s => InterestSlug.From(s) == slug));
        if (bySynonym is not null) return bySynonym;

        // Neu anlegen. Achtung: der Tag hängt hier nur im ChangeTracker – der Aufrufer speichert.
        var created = new InterestTag
        {
            Slug = slug,
            Label = label?.Trim() is { Length: > 0 } l ? l : text.Trim(),
            Facet = facet ?? InterestFacet.Other,
        };
        db.InterestTags.Add(created);
        return created;
    }

    /// <summary>
    /// Wie <see cref="EnsureAsync"/> für mehrere Texte, aber ohne Dubletten innerhalb desselben Aufrufs:
    /// zwei Eingaben, die auf denselben Slug fallen („Pokémon" und „pokemon"), liefern denselben Tag.
    /// </summary>
    public async Task<IReadOnlyList<InterestTag>> EnsureManyAsync(IEnumerable<string> texts,
        InterestFacet? facet = null, CancellationToken ct = default)
    {
        var result = new List<InterestTag>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var text in texts)
        {
            var tag = await EnsureAsync(text, facet: facet, ct: ct);
            // Der Slug des Treffers zählt (nicht der der Eingabe): ein Synonym-Treffer trägt einen anderen.
            if (tag is not null && seen.Add(tag.Slug)) result.Add(tag);
        }

        return result;
    }
}
