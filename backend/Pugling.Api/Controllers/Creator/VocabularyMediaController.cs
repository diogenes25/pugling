using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Images of a store vocabulary entry – the <b>default assignment</b>: maintained once, it takes effect in every
/// exercise that uses the word. Multiple assignments are the normal case and the whole point: only the choice
/// among several representations makes per-child individualization possible. A single exercise can deviate
/// from this – see the item assignment in <see cref="ExerciseMediaController"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/vocabulary/{vocabularyId:int}/media")]
[Tags("Creator – Media Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class VocabularyMediaController(PuglingDbContext db, MediaLinkService links) : ControllerBase
{
    private const MediaLinkService.Carrier Carrier = MediaLinkService.Carrier.Vocabulary;

    /// <summary>The images of this vocabulary entry, best editorial rank first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaLinkResponse>>> List(int vocabularyId, CancellationToken ct)
    {
        if (!await db.Vocabularies.AnyAsync(v => v.Id == vocabularyId, ct)) return NotFound();
        return (await links.ListAsync(Carrier, vocabularyId, ct)).Select(MediaLinkService.Map).ToList();
    }

    /// <summary>Assigns an image to the vocabulary entry (by id or key). The same image only once per vocabulary entry.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaLinkResponse>> Link(int vocabularyId, AddMediaLinkDto dto, CancellationToken ct)
    {
        if (!await db.Vocabularies.AnyAsync(v => v.Id == vocabularyId, ct)) return NotFound();

        var (link, error, detail) = await links.LinkAsync(Carrier, vocabularyId, dto, ct);
        if (error is { } failure) return this.ProblemWithCode(failure, detail);

        return CreatedAtAction(nameof(List), new { vocabularyId }, MediaLinkService.Map(link!));
    }

    /// <summary>Changes the editorial rank of an assignment.</summary>
    [HttpPatch("{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaLinkResponse>> Update(int vocabularyId, int linkId,
        UpdateMediaLinkDto dto, CancellationToken ct)
    {
        var link = await links.FindAsync(Carrier, vocabularyId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this vocabulary item.");

        link.Weight = dto.Weight;
        await db.SaveChangesAsync(ct);

        // Re-project after saving so that the response carries the asset including variants/tags.
        var refreshed = (await links.ListAsync(Carrier, vocabularyId, ct)).First(l => l.Id == linkId);
        return MediaLinkService.Map(refreshed);
    }

    /// <summary>Removes the assignment. The image stays in the store (it may still hang off other words).</summary>
    [HttpDelete("{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlink(int vocabularyId, int linkId, CancellationToken ct)
    {
        var link = await links.FindAsync(Carrier, vocabularyId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this vocabulary item.");

        await links.RemoveAsync(link, ct);
        return NoContent();
    }
}
