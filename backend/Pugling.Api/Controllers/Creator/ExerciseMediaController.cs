using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Bilder an einer Übung – die beiden Fälle, die die Store-Zuordnung
/// (<see cref="VocabularyMediaController"/>) nicht abdeckt:
/// <list type="bullet">
/// <item><b>Item-Übersteuerung</b> (<c>items/{itemId}/media</c>): in <i>dieser</i> Übung soll ein
/// anderes Bild stehen als das, was am Wort im Store hängt – ohne den Store zu verbiegen. Der Resolver
/// liest später von unten nach oben: Item schlägt Vokabel.</item>
/// <item><b>Titelbild</b> (<c>media</c>): der Aufmacher einer Text-/Lese-/Satzübung, die gar kein
/// einzelnes Wort bebildert.</item>
/// </list>
/// Beides ändert den Inhalt einer Übung und verlangt daher <b>Schreibrecht</b> – anders als der
/// kindneutrale Store, den jeder Creator pflegen darf.
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

    /// <summary>Die Titelbilder der Übung, bester redaktioneller Rang zuerst.</summary>
    [HttpGet("media")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaLinkResponse>>> ListForExercise(int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct)) return NotFound();
        return (await links.ListAsync(MediaLinkService.Carrier.Exercise, exerciseId, ct)).Select(MediaLinkService.Map).ToList();
    }

    /// <summary>Ordnet der Übung ein Titelbild zu (Schreibrecht nötig).</summary>
    [HttpPost("media")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaLinkResponse>> LinkExercise(int exerciseId, AddMediaLinkDto dto, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct)) return NotFound();
        if (await EnsureCanWriteAsync(exerciseId) is { } forbidden) return forbidden;

        var (link, error, detail) = await links.LinkAsync(MediaLinkService.Carrier.Exercise, exerciseId, dto, ct);
        if (error is { } failure) return this.ProblemWithCode(failure, detail);

        return CreatedAtAction(nameof(ListForExercise), new { exerciseId }, MediaLinkService.Map(link!));
    }

    /// <summary>Löst ein Titelbild von der Übung (Schreibrecht nötig).</summary>
    [HttpDelete("media/{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkExercise(int exerciseId, int linkId, CancellationToken ct)
    {
        if (await EnsureCanWriteAsync(exerciseId) is { } forbidden) return forbidden;

        var link = await links.FindAsync(MediaLinkService.Carrier.Exercise, exerciseId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this exercise.");

        await links.RemoveAsync(link, ct);
        return NoContent();
    }

    // ---- Übungslokale Übersteuerung je Item ----------------------------------------------------------

    /// <summary>Die Bilder, die für dieses Item die Store-Zuordnung übersteuern.</summary>
    [HttpGet("items/{itemId:int}/media")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaLinkResponse>>> ListForItem(int exerciseId, int itemId, CancellationToken ct)
    {
        if (!await ItemBelongsAsync(exerciseId, itemId, ct))
            return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");

        return (await links.ListAsync(MediaLinkService.Carrier.ExerciseItem, itemId, ct)).Select(MediaLinkService.Map).ToList();
    }

    /// <summary>Ordnet dem Item ein Bild zu – gilt nur in dieser Übung (Schreibrecht nötig).</summary>
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
        if (await EnsureCanWriteAsync(exerciseId) is { } forbidden) return forbidden;

        var (link, error, detail) = await links.LinkAsync(MediaLinkService.Carrier.ExerciseItem, itemId, dto, ct);
        if (error is { } failure) return this.ProblemWithCode(failure, detail);

        return CreatedAtAction(nameof(ListForItem), new { exerciseId, itemId }, MediaLinkService.Map(link!));
    }

    /// <summary>Löst die Übersteuerung – danach greift wieder das Bild aus dem Store (Schreibrecht nötig).</summary>
    [HttpDelete("items/{itemId:int}/media/{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkItem(int exerciseId, int itemId, int linkId, CancellationToken ct)
    {
        if (!await ItemBelongsAsync(exerciseId, itemId, ct))
            return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");
        if (await EnsureCanWriteAsync(exerciseId) is { } forbidden) return forbidden;

        var link = await links.FindAsync(MediaLinkService.Carrier.ExerciseItem, itemId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this exercise item.");

        await links.RemoveAsync(link, ct);
        return NoContent();
    }

    // ---- Helfer --------------------------------------------------------------------------------------

    /// <summary>Verhindert, dass ein fremdes Item über eine beliebige Übungs-Route adressiert wird.</summary>
    private Task<bool> ItemBelongsAsync(int exerciseId, int itemId, CancellationToken ct) =>
        db.ExerciseItems.AnyAsync(i => i.Id == itemId && i.ExerciseId == exerciseId, ct);

    /// <summary><c>null</c> = darf schreiben; sonst die fertige 403-Antwort (Muster wie im Übungs-CRUD).</summary>
    private async Task<ActionResult?> EnsureCanWriteAsync(int exerciseId) =>
        await perms.CanWriteAsync(User, exerciseId)
            ? null
            : this.ProblemWithCode(ApiErrors.NotAuthor, "You need write permission on this exercise.");
}
