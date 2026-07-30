using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Images on an exercise – the two cases that the store assignment
/// (<see cref="VocabularyMediaController"/>) does not cover:
/// <list type="bullet">
/// <item><b>Item override</b> (<c>items/{itemId}/media</c>): in <i>this</i> exercise, a
/// different image should appear than the one hanging off the word in the store – without bending the store. The resolver
/// later reads bottom-up: item beats vocabulary entry.</item>
/// <item><b>Title image</b> (<c>media</c>): the header image of a text/reading/sentence exercise that has no
/// single word to illustrate at all.</item>
/// </list>
/// Both change the content of an exercise and therefore require <b>write permission</b> – unlike the
/// child-neutral store, which every creator may maintain.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises/{exerciseId:int}")]
[Tags("Creator – Media Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseMediaController(PuglingDbContext db, MediaLinkService links, ExercisePermissionService perms)
    : ControllerBase
{
    // ---- Titelbild der Übung -------------------------------------------------------------------------

    /// <summary>The title images of the exercise, best editorial rank first.</summary>
    [HttpGet("media")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaLinkResponse>>> ListForExercise(int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct)) return NotFound();
        return (await links.ListAsync(MediaLinkService.Carrier.Exercise, exerciseId, ct)).Select(MediaLinkService.Map).ToList();
    }

    /// <summary>Assigns a title image to the exercise (write permission required).</summary>
    [HttpPost("media")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaLinkResponse>> LinkExercise(int exerciseId, AddMediaLinkDto dto, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct)) return NotFound();
        if (await EnsureCanWriteAsync(exerciseId, ct) is { } forbidden) return forbidden;

        var (link, error, detail) = await links.LinkAsync(MediaLinkService.Carrier.Exercise, exerciseId, dto, ct);
        if (error is { } failure) return this.ProblemWithCode(failure, detail);

        return CreatedAtAction(nameof(ListForExercise), new { exerciseId }, MediaLinkService.Map(link!));
    }

    /// <summary>Removes a title image from the exercise (write permission required).</summary>
    [HttpDelete("media/{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkExercise(int exerciseId, int linkId, CancellationToken ct)
    {
        if (await EnsureCanWriteAsync(exerciseId, ct) is { } forbidden) return forbidden;

        var link = await links.FindAsync(MediaLinkService.Carrier.Exercise, exerciseId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this exercise.");

        await links.RemoveAsync(link, ct);
        return NoContent();
    }

    // ---- Übungslokale Übersteuerung je Item ----------------------------------------------------------

    /// <summary>The images that override the store assignment for this item.</summary>
    [HttpGet("items/{itemId:int}/media")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaLinkResponse>>> ListForItem(int exerciseId, int itemId, CancellationToken ct)
    {
        if (!await ItemBelongsAsync(exerciseId, itemId, ct))
            return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");

        return (await links.ListAsync(MediaLinkService.Carrier.ExerciseItem, itemId, ct)).Select(MediaLinkService.Map).ToList();
    }

    /// <summary>Assigns an image to the item – applies only in this exercise (write permission required).</summary>
    [HttpPost("items/{itemId:int}/media")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaLinkResponse>> LinkItem(int exerciseId, int itemId, AddMediaLinkDto dto, CancellationToken ct)
    {
        if (!await ItemBelongsAsync(exerciseId, itemId, ct))
            return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");
        if (await EnsureCanWriteAsync(exerciseId, ct) is { } forbidden) return forbidden;

        var (link, error, detail) = await links.LinkAsync(MediaLinkService.Carrier.ExerciseItem, itemId, dto, ct);
        if (error is { } failure) return this.ProblemWithCode(failure, detail);

        return CreatedAtAction(nameof(ListForItem), new { exerciseId, itemId }, MediaLinkService.Map(link!));
    }

    /// <summary>Removes the override – after this, the image from the store applies again (write permission required).</summary>
    [HttpDelete("items/{itemId:int}/media/{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkItem(int exerciseId, int itemId, int linkId, CancellationToken ct)
    {
        if (!await ItemBelongsAsync(exerciseId, itemId, ct))
            return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");
        if (await EnsureCanWriteAsync(exerciseId, ct) is { } forbidden) return forbidden;

        var link = await links.FindAsync(MediaLinkService.Carrier.ExerciseItem, itemId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this exercise item.");

        await links.RemoveAsync(link, ct);
        return NoContent();
    }

    // ---- Helfer --------------------------------------------------------------------------------------

    /// <summary>Prevents a foreign item from being addressed via an arbitrary exercise route.</summary>
    private Task<bool> ItemBelongsAsync(int exerciseId, int itemId, CancellationToken ct) =>
        db.ExerciseItems.AnyAsync(i => i.Id == itemId && i.ExerciseId == exerciseId, ct);

    /// <summary><c>null</c> = may write; otherwise the ready-made 403 response (same pattern as in the exercise CRUD).</summary>
    private async Task<ActionResult?> EnsureCanWriteAsync(int exerciseId, CancellationToken ct) =>
        await perms.CanWriteAsync(User, exerciseId, ct)
            ? null
            : this.ProblemWithCode(ApiErrors.NotAuthor, "You need write permission on this exercise.");
}
