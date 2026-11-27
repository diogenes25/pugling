using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The day planner's plans of a child (B-734), read by the supervisor: what the planner set for each learning day and why,
/// and the days the server never ran. Supervisor-only - a creator-only account gets 403, a foreign supervisor 404, and no
/// student route returns these types: in shadow mode the child must not see a plan that has no effect.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/daily-plans")]
[Tags("Supervisor – Daily plans")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildDailyPlansController(DailyPlanner planner, LearningDay learningDay) : ControllerBase
{
    /// <summary>Default length of the period: the last 14 days up to today, the shadow week and the one before.</summary>
    private const int DefaultDays = 14;

    /// <summary>
    /// The child's daily plans from <paramref name="from"/> to <paramref name="to"/> (default: the last 14 days up to today),
    /// newest first, with their tasks and reasons. Today's plan is made first if it is due (from 6 a.m., planner on, a duty
    /// day) - the supervisor's look counts as the first access like the child's. A stored plan never changes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DailyPlanResponse>>> List(int childId, [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var (start, end, problem) = Range(from, to);
        if (problem is not null) return problem;
        await planner.EnsureTodayAsync(childId, ct);
        var plans = await planner.ListAsync(childId, start, end, ct);
        return plans.Select(Map).ToPagedList(Response, skip, take);
    }

    /// <summary>
    /// The closed duty days from <paramref name="from"/> to <paramref name="to"/> (default: the last 14 days) on which the
    /// planner was on but no plan was made - the server did not run (B-644, decision 19). Such a day counts like a pause: no
    /// penalty, the streak stands. Newest first; today, days before the planner was switched on and days without duty (Sunday,
    /// pause) never appear.
    /// </summary>
    [HttpGet("missing")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DateOnly>>> Missing(int childId, [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null, CancellationToken ct = default)
    {
        var (start, end, problem) = Range(from, to);
        if (problem is not null) return problem;
        return await planner.MissingDaysAsync(childId, start, end, ct);
    }

    private (DateOnly From, DateOnly To, ActionResult? Problem) Range(DateOnly? from, DateOnly? to)
    {
        var end = to ?? learningDay.Today;
        var start = from ?? end.AddDays(-(DefaultDays - 1));
        return start > end
            ? (start, end, this.ProblemWithCode(ApiErrors.ValidationError, "'from' must not be after 'to'."))
            : (start, end, null);
    }

    private static DailyPlanResponse Map(DailyPlan p) =>
        new(p.Day, p.CreatedAt, p.Mode, p.NewWordsBudget, p.Warnings,
            [.. p.Tasks.OrderBy(t => t.Order).Select(t => new DailyTaskResponse(t.PlanPositionId, t.ExerciseId,
                t.ExerciseTitle, t.SubjectId, t.Subject?.Name ?? "", t.Order, t.DueReviews, t.NewWords, t.Stage, t.Reason))]);
}
