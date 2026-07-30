using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Plan discovery for the logged-in child: finds its one currently playable study plan here,
/// without having to guess the planId – the entry point before overview/practice/test. Namespace-faithful
/// alias to the <see cref="StudyPlansController"/> list (which does read the student case too, but
/// lives under <c>supervisor/</c>); returns the same <see cref="PlanResponse"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans")]
[Tags("Student – Plans")]
[Produces("application/json")]
[Authorize(Roles = Roles.Student)]
public class StudentPlansController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// The playable study plans of the logged-in child (practically exactly one): active <b>and</b> today
    /// within its run time. Inactive/expired ones stay deliberately hidden (anti-cheat: no easy
    /// points plan to pick). From the result the client takes the <c>id</c> for the next steps.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PlanResponse>>> List(CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == cid.Value && p.Active && p.StartDate <= today && p.EndDate >= today)
            .OrderByDescending(p => p.CreatedAt)
            .Select(StudyPlansController.ToResponse(today))
            .ToListAsync(ct));
    }
}
