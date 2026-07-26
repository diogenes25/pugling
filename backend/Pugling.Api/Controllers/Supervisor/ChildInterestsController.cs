using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Die gewichteten Interessen eines Kindes – <b>referenziert</b> auf die geteilte Taxonomie und damit
/// maschinell auswertbar, anders als das freie <c>Child.Interests</c> (das bleibt: es ist die Sprache
/// des KI-Creators, der den Stoff sprachlich einkleidet).
/// <para>
/// Das Vorzeichen trägt die Hauptaussage: <b>negative Gewichte sind Abneigungen</b>. Sie sind für ein
/// gutes Ergebnis wichtiger als die Vorlieben – ein abstoßendes Bild kehrt den Lerneffekt um –, deshalb
/// schließen sie passende Bilder später hart aus, statt nur schlechter zu ranken.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/interests")]
[Tags("Supervisor – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildInterestsController(PuglingDbContext db, InterestTagService tags) : ControllerBase
{
    /// <summary>Alle Interessen des Kindes – stärkste Vorlieben zuerst, Abneigungen zuletzt.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IEnumerable<ChildInterestResponse>> List(int childId) =>
        await db.ChildInterests.AsNoTracking()
            .Where(i => i.ChildId == childId)
            .OrderByDescending(i => i.Weight).ThenBy(i => i.InterestTag!.Slug)
            .Select(i => new ChildInterestResponse(i.InterestTagId, i.InterestTag!.Slug, i.InterestTag.Label,
                i.InterestTag.Facet, i.Weight, i.CreatedAt))
            .ToListAsync();

    /// <summary>
    /// Ersetzt die Interessen des Kindes vollständig (leere Liste = alle entfernen). Bewusst ersetzend:
    /// das UI bearbeitet die Menge als Ganzes, und nur so lässt sich ein Eintrag auch wieder loswerden.
    /// Unbekannte Schlagworte werden angelegt (create-if-missing), damit der Vater frei tippen kann,
    /// ohne vorher den Katalog zu pflegen.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildInterestResponse>>> Replace(int childId, SetChildInterestsDto dto)
    {
        var inputs = dto.Interests ?? [];
        foreach (var input in inputs)
            if (Weight(input.Weight) is null)
                return this.ProblemWithCode(ApiErrors.ValidationError,
                    $"Weight must be between {ChildInterest.MinWeight} and {ChildInterest.MaxWeight}.");

        // Erst alle Schlagworte auflösen: schlägt eines fehl, wird der Bestand nicht angetastet.
        var resolved = new List<(InterestTag Tag, int Weight)>();
        foreach (var input in inputs)
        {
            var tag = await ResolveAsync(input);
            if (tag is null)
                return this.ProblemWithCode(ApiErrors.InvalidReference,
                    "Each interest needs an existing tagId or a slug/label to create one from.");
            resolved.Add((tag, input.Weight));
        }
        // Neu angelegte Tags haben noch keine Id – speichern, bevor die Gewichte darauf verweisen.
        await db.SaveChangesAsync();

        db.ChildInterests.RemoveRange(await db.ChildInterests.Where(i => i.ChildId == childId).ToListAsync());
        // Dubletten innerhalb der Eingabe (zwei Schreibweisen desselben Tags) würden den Unique-Index
        // reißen – der letzte Eintrag gewinnt, wie bei einer Zuweisung.
        foreach (var (tag, weight) in resolved.GroupBy(r => r.Tag.Id).Select(g => g.Last()))
            db.ChildInterests.Add(new ChildInterest { ChildId = childId, InterestTagId = tag.Id, Weight = weight });

        await db.SaveChangesAsync();
        return Ok(await List(childId));
    }

    /// <summary>Setzt oder ändert das Gewicht eines einzelnen Schlagworts (Upsert).</summary>
    [HttpPut("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildInterestResponse>> SetWeight(int childId, int tagId, SetChildInterestWeightDto dto)
    {
        if (Weight(dto.Weight) is not { } weight)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"Weight must be between {ChildInterest.MinWeight} and {ChildInterest.MaxWeight}.");

        var tag = await db.InterestTags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId);
        if (tag is null) return NotFound();

        var entry = await db.ChildInterests.FirstOrDefaultAsync(i => i.ChildId == childId && i.InterestTagId == tagId);
        if (entry is null)
        {
            entry = new ChildInterest { ChildId = childId, InterestTagId = tagId, Weight = weight };
            db.ChildInterests.Add(entry);
        }
        else
        {
            entry.Weight = weight;
        }

        await db.SaveChangesAsync();
        return new ChildInterestResponse(tag.Id, tag.Slug, tag.Label, tag.Facet, entry.Weight, entry.CreatedAt);
    }

    /// <summary>Entfernt ein Interesse (das Schlagwort selbst bleibt im Katalog).</summary>
    [HttpDelete("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int childId, int tagId)
    {
        var entry = await db.ChildInterests.FirstOrDefaultAsync(i => i.ChildId == childId && i.InterestTagId == tagId);
        if (entry is null) return NotFound();

        db.ChildInterests.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Helfer -------------------------------------------------------------------------------------

    /// <summary>Löst die Eingabe zu einem Tag auf: bevorzugt per Id, sonst per Slug/Label (create-if-missing).</summary>
    private async Task<InterestTag?> ResolveAsync(ChildInterestInput input)
    {
        if (input.TagId is { } id)
            return await db.InterestTags.FirstOrDefaultAsync(t => t.Id == id);

        var text = input.Slug ?? input.Label;
        return string.IsNullOrWhiteSpace(text) ? null : await tags.EnsureAsync(text, input.Label, input.Facet);
    }

    /// <summary>Prüft das Gewicht gegen die Skala; <c>null</c> = außerhalb (der Aufrufer meldet 400).</summary>
    private static int? Weight(int value) =>
        value is >= ChildInterest.MinWeight and <= ChildInterest.MaxWeight ? value : null;
}
