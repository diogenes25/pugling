using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The weighted interests of a child – <b>referenced</b> against the shared taxonomy and thereby
/// machine-evaluable, unlike the free-form <c>Child.Interests</c> (that stays: it is the language
/// of the AI creator, which clothes the material in language).
/// <para>
/// The sign carries the main message: <b>negative weights are dislikes</b>. They matter more for a
/// good result than the preferences do – a repellent image reverses the learning effect –, which is why
/// they later hard-exclude matching images instead of merely ranking them lower.
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
    /// <summary>All interests of the child – strongest preferences first, dislikes last.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IEnumerable<ChildInterestResponse>> List(int childId, CancellationToken ct = default) =>
        await db.ChildInterests.AsNoTracking()
            .Where(i => i.ChildId == childId)
            .OrderByDescending(i => i.Weight).ThenBy(i => i.InterestTag!.Slug)
            .Select(i => new ChildInterestResponse(i.InterestTagId, i.InterestTag!.Slug, i.InterestTag.Label,
                i.InterestTag.Facet, i.Weight, i.CreatedAt))
            .ToListAsync(ct);

    /// <summary>
    /// Replaces the child's interests completely (empty list = remove all). Deliberately a replacement:
    /// the UI edits the set as a whole, and that's the only way to get rid of an entry again.
    /// Unknown tags are created (create-if-missing), so the father can type freely
    /// without maintaining the catalog beforehand.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildInterestResponse>>> Replace(int childId, SetChildInterestsDto dto, CancellationToken ct = default)
    {
        var inputs = dto.Interests ?? [];
        foreach (var input in inputs)
            if (Weight(input.Weight) is null)
                return this.ProblemWithCode(ApiErrors.ValidationError,
                    $"Weight must be between {ChildInterest.MinWeight} and {ChildInterest.MaxWeight}.");

        // Resolve all keywords first: if one fails, the existing set is not touched.
        var resolved = new List<(InterestTag Tag, int Weight)>();
        foreach (var input in inputs)
        {
            var tag = await ResolveAsync(input, ct);
            if (tag is null)
                return this.ProblemWithCode(ApiErrors.InvalidReference,
                    "Each interest needs an existing tagId or a slug/label to create one from.");
            resolved.Add((tag, input.Weight));
        }
        // Newly created tags have no id yet - save before the weights reference them.
        await db.SaveChangesAsync(ct);

        db.ChildInterests.RemoveRange(await db.ChildInterests.Where(i => i.ChildId == childId).ToListAsync(ct));
        // Duplicates within the input (two spellings of the same tag) would violate the unique index -
        // the last entry wins, as with an assignment.
        foreach (var (tag, weight) in resolved.GroupBy(r => r.Tag.Id).Select(g => g.Last()))
            db.ChildInterests.Add(new ChildInterest { ChildId = childId, InterestTagId = tag.Id, Weight = weight });

        await db.SaveChangesAsync(ct);
        return Ok(await List(childId, ct));
    }

    /// <summary>Sets or changes the weight of a single tag (upsert).</summary>
    [HttpPut("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildInterestResponse>> SetWeight(int childId, int tagId, SetChildInterestWeightDto dto, CancellationToken ct = default)
    {
        if (Weight(dto.Weight) is not { } weight)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"Weight must be between {ChildInterest.MinWeight} and {ChildInterest.MaxWeight}.");

        var tag = await db.InterestTags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId, ct);
        if (tag is null) return NotFound();

        var entry = await db.ChildInterests.FirstOrDefaultAsync(i => i.ChildId == childId && i.InterestTagId == tagId, ct);
        if (entry is null)
        {
            entry = new ChildInterest { ChildId = childId, InterestTagId = tagId, Weight = weight };
            db.ChildInterests.Add(entry);
        }
        else
        {
            entry.Weight = weight;
        }

        await db.SaveChangesAsync(ct);
        return new ChildInterestResponse(tag.Id, tag.Slug, tag.Label, tag.Facet, entry.Weight, entry.CreatedAt);
    }

    /// <summary>Removes an interest (the tag itself stays in the catalog).</summary>
    [HttpDelete("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int childId, int tagId, CancellationToken ct = default)
    {
        var entry = await db.ChildInterests.FirstOrDefaultAsync(i => i.ChildId == childId && i.InterestTagId == tagId, ct);
        if (entry is null) return NotFound();

        db.ChildInterests.Remove(entry);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Helpers ------------------------------------------------------------------------------------

    /// <summary>Resolves the input to a tag: preferably by id, otherwise by slug/label (create-if-missing).</summary>
    private async Task<InterestTag?> ResolveAsync(ChildInterestInput input, CancellationToken ct)
    {
        if (input.TagId is { } id)
            return await db.InterestTags.FirstOrDefaultAsync(t => t.Id == id, ct);

        var text = input.Slug ?? input.Label;
        return string.IsNullOrWhiteSpace(text) ? null : await tags.EnsureAsync(text, input.Label, input.Facet, ct);
    }

    /// <summary>Checks the weight against the scale; <c>null</c> = out of range (the caller reports 400).</summary>
    private static int? Weight(int value) =>
        value is >= ChildInterest.MinWeight and <= ChildInterest.MaxWeight ? value : null;
}
