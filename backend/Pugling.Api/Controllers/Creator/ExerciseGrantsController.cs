using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Rechteverwaltung einer Übung (RWX-Grants). Übungs-global (nicht kapitelgebunden), daher top-level unter
/// <c>api/v1/creator/exercises/{exerciseId}/grants</c>. Nur ein <see cref="GrantPermission.Owner"/> darf die
/// Rechte einsehen und vergeben/entziehen. Der Katalog bleibt für alle lesbar – Read ist bewusst kein Recht.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises/{exerciseId:int}/grants")]
[Tags("Creator – Exercise Grants")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseGrantsController(PuglingDbContext db, ExercisePermissionService perms) : ControllerBase
{
    // Prüft, dass die Übung existiert und der anfragende Creator sie verwalten darf (Owner). 404 vor 403,
    // damit fremde Übungs-Ids nicht über den Statuscode enumerierbar sind.
    // Kein Vorgabewert für `ct`: er ließe die Aufrufstelle korrekt aussehen, während der Abbruch des
    // Clients verpufft.
    private async Task<ObjectResult?> EnsureOwnerAsync(int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Exercise not found.");
        return await perms.CanAdministerAsync(User, exerciseId, ct)
            ? null
            : this.ProblemWithCode(ApiErrors.NotOwner, "Only an owner can view or manage the permissions of this exercise.");
    }

    /// <summary>Alle vergebenen Rechte der Übung (nur für Owner sichtbar).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GrantResponse>>> List(int exerciseId, CancellationToken ct = default)
    {
        if (await EnsureOwnerAsync(exerciseId, ct) is { } forbidden) return forbidden;
        return await db.ExerciseGrants.AsNoTracking()
            .Where(g => g.ExerciseId == exerciseId)
            .OrderBy(g => g.CreatedAt).ThenBy(g => g.Id)
            .Select(g => new GrantResponse(g.CreatorId, g.Creator!.Name, g.Permission, g.GrantedByAdultId, g.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Vergibt einem Creator ein Recht (Owner/Write/Execute). Nur ein Owner darf vergeben; der begünstigte
    /// Creator muss existieren. Idempotent: ein bereits vorhandenes (Creator, Recht) wird nicht dupliziert.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrantResponse>> Add(int exerciseId, AddGrantDto dto, CancellationToken ct = default)
    {
        if (await EnsureOwnerAsync(exerciseId, ct) is { } forbidden) return forbidden;
        var creator = await db.Adults.FirstOrDefaultAsync(f => f.Id == dto.CreatorId, ct);
        if (creator is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Creator not found.");

        if (!await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == dto.CreatorId && g.Permission == dto.Permission, ct))
        {
            db.ExerciseGrants.Add(new ExerciseGrant
            {
                ExerciseId = exerciseId,
                CreatorId = dto.CreatorId,
                Permission = dto.Permission,
                GrantedByAdultId = User.AdultId(),
            });
            await db.SaveChangesAsync(ct);
        }
        return CreatedAtAction(nameof(List), new { exerciseId },
            new GrantResponse(creator.Id, creator.Name, dto.Permission, User.AdultId(), DateTime.UtcNow));
    }

    /// <summary>
    /// Entzieht einem Creator ein Recht. Nur ein Owner darf entziehen. Der <b>letzte Owner</b> kann nicht
    /// entfernt werden – sonst wäre die Übung verwaist (nur noch geseedete System-Übungen sind ownerlos).
    /// </summary>
    [HttpDelete("{creatorId:int}/{permission}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(int exerciseId, int creatorId, GrantPermission permission, CancellationToken ct = default)
    {
        if (await EnsureOwnerAsync(exerciseId, ct) is { } forbidden) return forbidden;
        var grant = await db.ExerciseGrants.FirstOrDefaultAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == creatorId && g.Permission == permission, ct);
        if (grant is null) return NotFound();

        if (permission == GrantPermission.Owner
            && await db.ExerciseGrants.CountAsync(g => g.ExerciseId == exerciseId && g.Permission == GrantPermission.Owner, ct) <= 1)
            return this.ProblemWithCode(ApiErrors.LastOwner, "Cannot remove the last owner of an exercise.");

        db.ExerciseGrants.Remove(grant);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
