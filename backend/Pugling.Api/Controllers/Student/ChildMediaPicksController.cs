using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// "Different image" – the only way to change a child's frozen image pick.
/// <para>
/// Why frozen at all? Because in vocabulary learning <b>image constancy is the memory effect</b>: the same
/// motif on every repetition builds the association, a changing image destroys it. That's why the
/// selection isn't recomputed on every retrieval – and why re-picking requires an explicit
/// action instead of an automatism.
/// </para>
/// The rejection is permanent: the rejected image is never drawn again for this carrier. This makes
/// this endpoint at the same time the cheapest feedback signal we get about the child's taste.
/// Like the other student endpoints, only <c>[Authorize]</c> – the supervisor may pick along for their child.
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
    /// Rejects the current image and draws a new one. If there is no eligible alternative, the current
    /// pick stays <b>unchanged</b> (<c>409 media_no_alternative</c>) – better to keep the same image than
    /// to burn the last candidate and leave the card permanently without an image.
    /// </summary>
    [HttpPost("reshuffle")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SelectedMediaResponse>> Reshuffle(int childId, ReshuffleMediaDto dto,
        CancellationToken ct)
    {
        // Exactly one carrier - the same rule as on the assignment and the choice itself (a DB constraint there).
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
