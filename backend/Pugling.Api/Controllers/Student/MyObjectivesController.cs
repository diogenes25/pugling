using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Services;
using Pugling.Api.Services.Supervisor;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Die „großen Ziele" des Sohns aus seiner eigenen Sicht (reine Lesesicht): das große Ziel, seine Etappen mit
/// Fortschritt und die Belohnungs-Vorschau. Der Fortschritt wird live berechnet; das <c>rewarded</c>-Flag zeigt,
/// ob der Abschluss-Batzen schon geflossen ist. Verdiente Belohnungen werden am <b>Kind-Login</b> idempotent
/// gutgeschrieben (<c>AuthController</c> → <c>ObjectiveRewardService</c>, es gibt keinen Scheduler) – dieser
/// Endpunkt bleibt bewusst nebenwirkungsfrei (GET). Nur <b>aktive</b> Ziele werden gezeigt. Die Verwaltung liegt
/// beim Vater (<c>supervisor/children/{childId}/objectives</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/me/objectives")]
[Tags("Student – Objectives")]
[Produces("application/json")]
[Authorize]
public class MyObjectivesController(ObjectiveService objectives) : ControllerBase
{
    /// <summary>
    /// Eigene aktive große Ziele mit Etappen-Fortschritt (offene/überfällige zuerst), seitenweise.
    /// Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ObjectiveService.ObjectiveResponse>>> List(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var all = (await objectives.ListAsync(cid.Value, status: null, kind: null, ct)).Where(o => o.Active).ToList();
        return all.ToPagedList(Response, skip, take);
    }

    /// <summary>Ein einzelnes eigenes großes Ziel (Einzelansicht zur Liste). 404, wenn es (für dich) nicht existiert.</summary>
    [HttpGet("{objectiveId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveService.ObjectiveResponse>> Get(int objectiveId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        // Nur aktive Ziele sind für den Sohn sichtbar (deckungsgleich zur Liste); ein deaktiviertes → 404.
        return await objectives.GetAsync(cid.Value, objectiveId, ct) is { Active: true } o ? o : NotFound();
    }
}
