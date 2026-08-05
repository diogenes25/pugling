using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Permission management of an exercise (RWX grants). Exercise-global (not chapter-bound), hence top-level under
/// <c>api/v1/creator/exercises/{exerciseId}/grants</c>. Only an <see cref="GrantPermission.Owner"/> may view
/// the permissions and grant/revoke them. The catalog stays readable for everyone – Read is deliberately not a permission.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises/{exerciseId:int}/grants")]
[Tags("Creator – Exercise Grants")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseGrantsController(PuglingDbContext db, ExercisePermissionService perms) : ControllerBase
{
    // Checks that the exercise exists and that the requesting creator may manage it (owner). 404 before 403,
    // so other people's exercise ids cannot be enumerated through the status code.
    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles out.
    private async Task<ObjectResult?> EnsureOwnerAsync(int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Exercise not found.");
        return await perms.CanAdministerAsync(User, exerciseId, ct)
            ? null
            : this.ProblemWithCode(ApiErrors.NotOwner, "Only an owner can view or manage the permissions of this exercise.");
    }

    /// <summary>All permissions granted for the exercise (visible only to owners).</summary>
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
    /// Grants a creator a permission (Owner/Write/Execute). Only an owner may grant; the beneficiary
    /// creator must exist. Idempotent: an already existing (creator, permission) pair is not duplicated -
    /// then answers <c>200</c> with the stored grant (not the caller's values), <c>201</c> only on a real insert.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrantResponse>> Add(int exerciseId, AddGrantDto dto, CancellationToken ct = default)
    {
        if (await EnsureOwnerAsync(exerciseId, ct) is { } forbidden) return forbidden;
        var creator = await db.Adults.FirstOrDefaultAsync(f => f.Id == dto.CreatorId, ct);
        if (creator is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Creator not found.");

        var existing = await db.ExerciseGrants.AsNoTracking().FirstOrDefaultAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == dto.CreatorId && g.Permission == dto.Permission, ct);
        if (existing is not null)
            return Ok(new GrantResponse(creator.Id, creator.Name, dto.Permission, existing.GrantedByAdultId, existing.CreatedAt));

        var grant = new ExerciseGrant
        {
            ExerciseId = exerciseId,
            CreatorId = dto.CreatorId,
            Permission = dto.Permission,
            GrantedByAdultId = User.AdultId(),
        };
        db.ExerciseGrants.Add(grant);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { exerciseId },
            new GrantResponse(creator.Id, creator.Name, dto.Permission, grant.GrantedByAdultId, grant.CreatedAt));
    }

    /// <summary>
    /// Revokes a permission from a creator. Only an owner may revoke. The <b>last owner</b> cannot
    /// be removed – otherwise the exercise would be orphaned (only seeded system exercises are ownerless).
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
