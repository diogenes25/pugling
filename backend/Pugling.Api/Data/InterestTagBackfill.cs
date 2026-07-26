using Microsoft.EntityFrameworkCore;
using Pugling.Api.Models;
using Pugling.Api.Services.Shared;

namespace Pugling.Api.Data;

/// <summary>
/// Überführt die bestehenden <b>Freitext</b>-Interessen der Kinder einmalig in die referenzierte
/// Taxonomie (<see cref="ChildInterest"/>), damit die Bildauswahl auch für Bestandskinder sofort etwas
/// zu rechnen hat. Verlustfrei: <c>Child.Interests</c> bleibt unangetastet – es ist weiterhin die
/// Sprache des KI-Creators, der Freitext braucht.
/// <para>
/// Idempotent über „hat das Kind schon Einträge?": ein Kind, dessen Interessen der Vater bereits
/// gepflegt hat, wird übersprungen. Sonst würde ein Neustart bewusst gelöschte Einträge wiederbeleben
/// oder Gewichte überschreiben (analog <see cref="AccountBackfill"/>/<see cref="ExerciseItemBackfill"/>).
/// </para>
/// </summary>
public static class InterestTagBackfill
{
    /// <summary>Startgewicht der übernommenen Interessen: eine klare, aber nicht dominante Vorliebe.</summary>
    private const int DefaultWeight = 2;

    public static async Task RunAsync(PuglingDbContext db, InterestTagService tags, CancellationToken ct = default)
    {
        var childrenWithEntries = await db.ChildInterests.Select(i => i.ChildId).Distinct().ToListAsync(ct);
        var pending = await db.Children
            .Where(c => !childrenWithEntries.Contains(c.Id))
            .Select(c => new { c.Id, c.Interests })
            .ToListAsync(ct);

        foreach (var child in pending)
        {
            if (child.Interests.Count == 0) continue;

            // Über den geteilten Service, damit „Pokémon" hier denselben Tag trifft wie später im UI.
            foreach (var tag in await tags.EnsureManyAsync(child.Interests, ct: ct))
            {
                // Neu angelegte Tags haben noch keine Id – erst speichern, dann darauf verweisen.
                if (tag.Id == 0) await db.SaveChangesAsync(ct);
                db.ChildInterests.Add(new ChildInterest
                {
                    ChildId = child.Id,
                    InterestTagId = tag.Id,
                    Weight = DefaultWeight,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
