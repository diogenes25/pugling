using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The single place where text becomes an <see cref="InterestTag"/>. Three paths converge here
/// (the creator tags an image, the supervisor types an interest, backfill takes over free-text
/// interests) – if they ran separately, the <b>shared</b> taxonomy would fall apart into duplicates
/// and the "image ↔ child" matching would fail exactly where it is needed.
/// </summary>
public class InterestTagService(PuglingDbContext db)
{
    /// <summary>
    /// Finds the tag for a given text or creates it. The lookup runs in two stages: first the
    /// indexed slug match, otherwise a synonym comparison (synonyms live in a JSON column and are
    /// not queryable – the scan is only worthwhile because it only kicks in on a miss).
    /// </summary>
    /// <param name="text">Slug or display name ("pokemon", "Pokémon", "Poke").</param>
    /// <param name="label">Display name for a possibly newly created tag; otherwise <paramref name="text"/> is used.</param>
    /// <param name="facet">Facet for a possibly newly created tag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The found/created tag – or <c>null</c> if the text does not yield a valid slug.</returns>
    public async Task<InterestTag?> EnsureAsync(string text, string? label = null,
        InterestFacet? facet = null, CancellationToken ct = default)
    {
        var slug = InterestSlug.From(text);
        if (slug.Length == 0) return null;

        // ZUERST der ChangeTracker, dann die DB: ein im selben Aufruf angelegter Tag ist noch nicht
        // gespeichert und wäre für jede Abfrage unsichtbar. Zwei Eingaben, die auf denselben Slug fallen
        // („Fußball"/„Fussball" – ß wird zu ss), legten sonst zwei Zeilen an und das Speichern risse den
        // Unique-Index auf Slug. Genau darüber fiel im Zweifel der Start um, weil der
        // <see cref="InterestTagBackfill"/> die Freitext-Interessen der Bestandskinder hier durchschickt.
        if (Pending(slug) is { } pending) return pending;

        var bySlug = await db.InterestTags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        if (bySlug is not null) return bySlug;

        // Der Slug ist neu – bevor eine Dublette entsteht, gegen die Synonyme der bestehenden Tags prüfen.
        // Getrackt geladen und über die lokale Sicht gesucht, damit auch hier die noch nicht gespeicherten
        // Tags dieses Aufrufs mitzählen (Synonyme liegen als JSON-Spalte und sind nicht abfragbar).
        await db.InterestTags.LoadAsync(ct);
        var bySynonym = db.InterestTags.Local
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
    /// Like <see cref="EnsureAsync"/> for multiple texts, but without duplicates within the same call:
    /// two inputs that fall onto the same slug ("Pokémon" and "pokemon") yield the same tag.
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

    /// <summary>
    /// The tag for the slug, if it is already attached in this <see cref="PuglingDbContext"/> – including
    /// as a not-yet-saved new entry. The local view is the only place that sees both.
    /// </summary>
    private InterestTag? Pending(string slug) =>
        db.InterestTags.Local.FirstOrDefault(t => t.Slug == slug);
}
