using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// "Big goals" (objectives, the child-friendly OKR core) of a child: a time-boxed, motivating frame
/// around several measurable key results (<see cref="KeyResult"/>s, a separate controller below). Evaluated live against
/// the learning progress + the class test grades (status open/achieved/overdue). Distinction: the
/// plan-bound mandatory goal of the position (day/week, with penalty) and activity-based missions are something
/// else; an objective measures outcome progress and is rewarded without a penalty, but with key-result chunks instead.
/// Ownership via <see cref="ChildOwnershipFilter"/>; reading allowed for father <b>and</b> child, writing only by the father.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/objectives")]
[Tags("Supervisor – Objectives")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ObjectivesController(ObjectiveService objectives) : ControllerBase
{
    /// <summary>
    /// All big goals of the child, evaluated live. Filter: <paramref name="status"/>
    /// (<c>open</c>/<c>achieved</c>/<c>overdue</c>), <paramref name="kind"/> (<c>Committed</c>/<c>Stretch</c>).
    /// Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ObjectiveResponse>>> List(
        int childId, [FromQuery] string? status = null, [FromQuery] ObjectiveKind? kind = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        (await objectives.ListAsync(childId, status, kind, ct)).ToPagedList(Response, skip, take);

    /// <summary>A single big goal, evaluated live (404 if it does not exist for this child).</summary>
    [HttpGet("{objectiveId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveResponse>> Get(int childId, int objectiveId, CancellationToken ct = default) =>
        await objectives.GetAsync(childId, objectiveId, ct) is { } o ? o : NotFound();

    /// <summary>
    /// Creates a big goal (supervisor only); key results can be supplied inline. 400 on an invalid
    /// scope/target value, 409 if two inline key results share the same scope and metric.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ObjectiveResponse>> Create(
        int childId, [FromBody] CreateObjectiveRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.CreateAsync(childId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return CreatedAtAction(nameof(Get), new { childId, objectiveId = value!.Id }, value);
    }

    /// <summary>Changes header fields of a goal (title/motivation/kind/period/reward/active); father only.</summary>
    [HttpPatch("{objectiveId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveResponse>> Update(
        int childId, int objectiveId, [FromBody] UpdateObjectiveRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.UpdateAsync(childId, objectiveId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null ? value : NotFound();
    }

    /// <summary>Deletes a big goal together with its key results (supervisor only).</summary>
    [HttpDelete("{objectiveId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int objectiveId, CancellationToken ct = default) =>
        await objectives.DeleteAsync(childId, objectiveId, ct) ? NoContent() : NotFound();
}
