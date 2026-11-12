using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The work list of the image generator (B-485): which vocabulary entries still lack a neutral image or an image for
/// a wanted interest tag. Read-only – the generator uploads and links through the regular media endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/media/demand")]
[Tags("Creator – Media")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class MediaDemandController(PuglingDbContext db, MediaDemandService demand, AuthAccess access) : ControllerBase
{
    /// <summary>Default and upper bound of <c>take</c> – one generator run should stay a reviewable batch.</summary>
    private const int DefaultTake = 50;
    private const int MaxTake = 500;

    /// <summary>
    /// Vocabulary entries that still need images, alphabetically by word. Give exactly one of two scopes.
    /// <para>
    /// With <c>childId</c> the list covers the vocabulary of the child's playable plans, and wanted are a neutral image
    /// and the child's two strongest theme interests (weight ≥ 2). The endpoint reads child data and is therefore bound
    /// to <b>supervision</b>: a creator who does not supervise the child gets <c>404</c> like for an unknown child
    /// (B-310). The answer itself carries no child data.
    /// </para>
    /// <para>
    /// With <c>seriesUnitId</c> the list covers the vocabulary of a unit, independent of any child, and wanted are a
    /// neutral image and, when <c>tag</c> (a slug) is given, an image for that tag.
    /// </para>
    /// A tag in the answer without an <c>imagePrompt</c> must not be illustrated yet; the generator reports it.
    /// </summary>
    /// <param name="childId">Scope: the child whose plans are illustrated.</param>
    /// <param name="seriesUnitId">Scope: the unit whose vocabulary is illustrated.</param>
    /// <param name="tag">Only with <c>seriesUnitId</c>: the slug of the interest tag to draw for.</param>
    /// <param name="take">Maximum number of entries (1..500, default 50).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MediaDemandEntry>>> Get(
        [FromQuery] int? childId = null,
        [FromQuery] int? seriesUnitId = null,
        [FromQuery] string? tag = null,
        [FromQuery] int take = DefaultTake,
        CancellationToken ct = default)
    {
        if ((childId is null) == (seriesUnitId is null))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Give exactly one of childId and seriesUnitId.");
        if (take is < 1 or > MaxTake)
            return this.ProblemWithCode(ApiErrors.ValidationError, $"take must be between 1 and {MaxTake}.");

        if (childId is { } child)
        {
            if (tag is not null)
                return this.ProblemWithCode(ApiErrors.ValidationError,
                    "tag only applies with seriesUnitId – for a child the tags come from its interests.");
            // One check for "unknown" and "not yours": a supervision link only exists for an existing child.
            if (!await access.SupervisorOwnsChildAsync(User, child, ct))
                return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
            return Ok(await demand.ForChildAsync(child, take, ct));
        }

        var unitId = seriesUnitId!.Value;
        if (!await db.SeriesUnits.AnyAsync(u => u.Id == unitId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Series unit not found.");
        var interestTag = tag is null ? null : await db.InterestTags.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == tag, ct);
        if (tag is not null && interestTag is null)
            return this.ProblemWithCode(ApiErrors.NotFound, $"Interest tag '{tag}' not found.");
        return Ok(await demand.ForUnitAsync(unitId, interestTag, take, ct));
    }
}
