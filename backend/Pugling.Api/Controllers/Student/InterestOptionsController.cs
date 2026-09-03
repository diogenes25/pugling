using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The shared interest taxonomy as the child may read it: the tiles the guided self-description
/// (<see cref="MyInterestsController"/>) offers. Maintaining the taxonomy stays with the creator –
/// this is a pure picking list.
/// <para>
/// Deliberately without the usage counts of the creator's view: they would tell one child how many other
/// children picked a keyword, which is nobody's business and no help in choosing.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/interest-tags")]
[Tags("Student – Interests")]
[Produces("application/json")]
[Authorize(Roles = Roles.Student)]
public class InterestOptionsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// The selectable keywords, alphabetically by slug. Total count (before paging) in the
    /// <c>X-Total-Count</c> header.
    /// </summary>
    /// <param name="facet">Only keywords of this facet – the guided process shows topics and styles apart.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of entries (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<InterestTagOptionResponse>>> List(
        [FromQuery] InterestFacet? facet = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.InterestTags.AsNoTracking().AsQueryable();
        if (facet is not null) query = query.Where(t => t.Facet == facet);

        // By slug, not by label - the same order the creator's view uses. The label column carries no
        // NOCASE collation, so SQLite compares it byte-wise: every capital sorts before every lowercase
        // letter and umlauts land behind all of ASCII ("Zebra" before "apfel"). The slug is lowercase and
        // diacritic-free by derivation, so ordering by it is the one that is actually alphabetical.
        // The id is the tiebreaker: offset paging is only sound over an order that cannot move.
        return await query
            .OrderBy(t => t.Slug).ThenBy(t => t.Id)
            .Select(t => new InterestTagOptionResponse(t.Id, t.Slug, t.Label, t.Facet))
            .ToPagedListAsync(Response, skip, take, ct);
    }
}
