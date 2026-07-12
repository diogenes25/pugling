using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Controllers.Supervisor;
using Pugling.Api.Data;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Plan-Discovery für den angemeldeten Sohn: findet hier seinen einen aktuell spielbaren Lehrplan,
/// ohne die planId raten zu müssen – der Einstieg vor Overview/Practice/Test. Namensraum-treuer
/// Alias zur <see cref="StudyPlansController"/>-Liste (die den Student-Fall zwar mitliest, aber
/// unter <c>supervisor/</c> liegt); gibt dieselbe <see cref="StudyPlansController.PlanResponse"/> zurück.
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
    /// Die spielbaren Lehrpläne des angemeldeten Sohns (praktisch genau einer): aktiv <b>und</b> heute
    /// in Laufzeit. Inaktive/abgelaufene bleiben bewusst verborgen (Anti-Cheat: kein leichter
    /// Punkte-Plan zum Aussuchen). Aus dem Ergebnis nimmt der Client die <c>id</c> für die weiteren Schritte.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<StudyPlansController.PlanResponse>>> List()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == cid.Value && p.Active && p.StartDate <= today && p.EndDate >= today)
            .OrderByDescending(p => p.CreatedAt)
            .Select(StudyPlansController.ToResponse(today))
            .ToListAsync());
    }
}
