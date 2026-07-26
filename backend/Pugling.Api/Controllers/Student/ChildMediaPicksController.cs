using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// „Anderes Bild" – der einzige Weg, die eingefrorene Bildwahl eines Kindes zu ändern.
/// <para>
/// Warum überhaupt eingefroren? Weil beim Vokabellernen <b>Bildkonstanz der Merkeffekt ist</b>: dasselbe
/// Motiv bei jeder Wiederholung baut die Verknüpfung auf, ein wechselndes Bild zerstört sie. Deshalb
/// rechnet die Auswahl nicht bei jedem Abruf neu – und deshalb braucht das Umwählen eine ausdrückliche
/// Handlung statt eines Automatismus.
/// </para>
/// Die Ablehnung ist dauerhaft: das abgelehnte Bild wird für diesen Träger nie wieder gezogen. Damit ist
/// dieser Endpunkt zugleich das billigste Feedback-Signal, das wir über den Geschmack des Kindes bekommen.
/// Wie die übrigen Student-Endpunkte nur <c>[Authorize]</c> – der Supervisor darf für sein Kind mitwählen.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/media-picks")]
[Tags("Student – Media Picks")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildMediaPicksController(MediaSelector selector) : ControllerBase
{
    /// <summary>
    /// Lehnt das aktuelle Bild ab und zieht ein neues. Gibt es keine zulässige Alternative, bleibt der
    /// Bestand <b>unverändert</b> (<c>409 media_no_alternative</c>) – lieber dasselbe Bild behalten, als
    /// den letzten Kandidaten zu verbrennen und die Karte dauerhaft bildlos zu machen.
    /// </summary>
    [HttpPost("reshuffle")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SelectedMediaResponse>> Reshuffle(int childId, ReshuffleMediaDto dto,
        CancellationToken ct)
    {
        // Genau ein Träger – dieselbe Regel wie an Zuordnung und Wahl selbst (dort als DB-Constraint).
        if ((dto.VocabularyId is null) == (dto.ExerciseItemId is null))
            return this.ProblemWithCode(ApiErrors.ValidationError,
                "Provide exactly one of vocabularyId or exerciseItemId.");

        var picked = await selector.ReshuffleAsync(childId, dto.VocabularyId, dto.ExerciseItemId, ct: ct);
        if (picked is null)
            return this.ProblemWithCode(ApiErrors.MediaNoAlternative,
                "There is no other image available for this object.");

        return new SelectedMediaResponse(picked.MediaAssetId, picked.Url, picked.Alt);
    }
}
