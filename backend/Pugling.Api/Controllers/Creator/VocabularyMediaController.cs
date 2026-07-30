using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Bilder einer Store-Vokabel – die <b>Regelzuordnung</b>: einmal gepflegt, wirkt sie in jeder Übung,
/// die das Wort nutzt. Mehrere Zuordnungen sind der Normalfall und der Sinn der Sache: erst die Auswahl
/// mehrerer Darstellungen macht die Individualisierung je Kind möglich. Eine einzelne Übung kann davon
/// abweichen – siehe die Item-Zuordnung im <see cref="ExerciseMediaController"/>.
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

    /// <summary>Die Bilder dieser Vokabel, bester redaktioneller Rang zuerst.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaLinkResponse>>> List(int vocabularyId, CancellationToken ct)
    {
        if (!await db.Vocabularies.AnyAsync(v => v.Id == vocabularyId, ct)) return NotFound();
        return (await links.ListAsync(Carrier, vocabularyId, ct)).Select(MediaLinkService.Map).ToList();
    }

    /// <summary>Ordnet der Vokabel ein Bild zu (per Id oder Key). Dasselbe Bild nur einmal je Vokabel.</summary>
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

    /// <summary>Ändert den redaktionellen Rang einer Zuordnung.</summary>
    [HttpPatch("{linkId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaLinkResponse>> Update(int vocabularyId, int linkId,
        UpdateMediaLinkDto dto, CancellationToken ct)
    {
        var link = await links.FindAsync(Carrier, vocabularyId, linkId, ct);
        if (link is null) return this.ProblemWithCode(ApiErrors.MediaLinkNotFound, "The link does not belong to this vocabulary item.");

        link.Weight = dto.Weight;
        await db.SaveChangesAsync(ct);

        // Nach dem Speichern neu projizieren, damit die Antwort das Asset samt Varianten/Tags trägt.
        var refreshed = (await links.ListAsync(Carrier, vocabularyId, ct)).First(l => l.Id == linkId);
        return MediaLinkService.Map(refreshed);
    }

    /// <summary>Löst die Zuordnung. Das Bild bleibt im Store (es hängt womöglich an anderen Wörtern).</summary>
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
