using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Learning goals of a child: outcome/mastery goals set by the father on a catalog scope
/// (subject/chapter/exercise), evaluated live against the aggregated learning progress (status open/achieved/overdue).
/// Distinction: the plan-bound mandatory goal of the position (day/week) and activity-based missions are
/// something else – see <see cref="ChildLearnProgressController"/> for the underlying evaluation.
/// Ownership via <see cref="ChildOwnershipFilter"/>; reading allowed for father <b>and</b> child, writing only by the father.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/learn-goals")]
[Tags("Supervisor – Learning Goals")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class LearnGoalsController(LearnGoalService goals) : ControllerBase
{
    /// <summary>
    /// All learning goals of the child, evaluated live. Filter: <paramref name="subjectId"/> (a single subject only),
    /// <paramref name="status"/> (<c>open</c>/<c>achieved</c>/<c>overdue</c>). Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<LearnGoalResponse>>> List(
        int childId, [FromQuery] int? subjectId = null, [FromQuery] string? status = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        (await goals.ListAsync(childId, subjectId, status, ct)).ToPagedList(Response, skip, take);

    /// <summary>A single learning goal, evaluated live (404 if it does not exist for this child).</summary>
    [HttpGet("{goalId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnGoalResponse>> Get(int childId, int goalId, CancellationToken ct = default) =>
        await goals.GetAsync(childId, goalId, ct) is { } g ? g : NotFound();

    /// <summary>Creates a learning goal (supervisor only). 400 on an invalid scope/target value.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnGoalResponse>> Create(
        int childId, [FromBody] CreateLearnGoalRequest request, CancellationToken ct = default)
    {
        var (value, error) = await goals.CreateAsync(childId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return CreatedAtAction(nameof(Get), new { childId, goalId = value!.Id }, value);
    }

    /// <summary>Changes metric/target value/due date/title of a goal (supervisor only); the scope stays fixed.</summary>
    [HttpPatch("{goalId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnGoalResponse>> Update(
        int childId, int goalId, [FromBody] UpdateLearnGoalRequest request, CancellationToken ct = default)
    {
        var (value, error) = await goals.UpdateAsync(childId, goalId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null ? value : NotFound();
    }

    /// <summary>Deletes a learning goal (supervisor only).</summary>
    [HttpDelete("{goalId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int goalId, CancellationToken ct = default) =>
        await goals.DeleteAsync(childId, goalId, ct) ? NoContent() : NotFound();
}
