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

        // The ChangeTracker FIRST, then the DB: a tag created within the same call is not saved yet and would
        // be invisible to every query. Two inputs falling onto the same slug ("Fußball"/"Fussball" - ß becomes
        // ss) would otherwise create two rows and saving would violate the unique index on Slug. That is
        // exactly what could take startup down, because the interest backfill pushes the free-text interests
        // of the existing children through here.
        if (Pending(slug) is { } pending) return pending;

        var bySlug = await db.InterestTags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        if (bySlug is not null) return bySlug;

        // The slug is new - before a duplicate arises, check it against the synonyms of the existing tags.
        // Loaded tracked and searched through the local view, so that the not yet saved tags of this call count
        // here as well (synonyms sit in a JSON column and are not queryable).
        await db.InterestTags.LoadAsync(ct);
        var bySynonym = db.InterestTags.Local
            .FirstOrDefault(t => t.Synonyms.Any(s => InterestSlug.From(s) == slug));
        if (bySynonym is not null) return bySynonym;

        // Create it. Careful: the tag only hangs in the ChangeTracker here - the caller saves.
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
            // The hit's slug counts (not the input's): a synonym hit carries a different one.
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
