using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The measurable key results (<see cref="Pugling.Api.Models.KeyResult"/>s) of a big goal. Each is attached to a
/// catalog scope (subject/chapter/exercise) and a cheat-proof metric; the scope is fixed after creation (create a new
/// one to re-target). Evaluated live via the learning progress or the class test grade. Ownership via
/// <see cref="ChildOwnershipFilter"/> (the chain child → objective → key result is checked in the service); writing only by the father.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/objectives/{objectiveId:int}/key-results")]
[Tags("Supervisor – Objectives")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class KeyResultsController(ObjectiveService objectives) : ControllerBase
{
    /// <summary>
    /// Adds a key result to the goal (supervisor only). 400 on an invalid scope/target value, 404 if the
    /// goal is missing, 409 if the goal already has a key result with this scope and metric.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KeyResultResponse>> Create(
        int childId, int objectiveId, [FromBody] CreateKeyResultRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.AddKeyResultAsync(childId, objectiveId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null
            ? CreatedAtAction(nameof(ObjectivesController.Get), "Objectives", new { childId, objectiveId }, value)
            : NotFound();
    }

    /// <summary>
    /// Changes metric/target value/title of a key result (supervisor only); the scope stays fixed. 409 if
    /// the new metric collides with another key result of the same scope.
    /// </summary>
    [HttpPatch("{keyResultId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KeyResultResponse>> Update(
        int childId, int objectiveId, int keyResultId, [FromBody] UpdateKeyResultRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.UpdateKeyResultAsync(childId, objectiveId, keyResultId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null ? value : NotFound();
    }

    /// <summary>Deletes a key result of the goal (supervisor only).</summary>
    [HttpDelete("{keyResultId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int objectiveId, int keyResultId, CancellationToken ct = default) =>
        await objectives.DeleteKeyResultAsync(childId, objectiveId, keyResultId, ct) ? NoContent() : NotFound();
}
