using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child's day across all of its active plans (B-606): every duty of the day in one list, whichever plan it comes
/// from, with the streak and the daily chest. The arcade's start page reads it; a position still leads into the path of
/// its own plan (<c>PositionStatus.PlanId</c>). The supervisor may read it for his child, like the other student views.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/today")]
[Tags("Student – Today")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildTodayController(PositionProgressService progress, DailyBoxService dailyBox, LearningDay learningDay)
    : ControllerBase
{
    /// <summary>
    /// Today's duties of all active, running plans of the child: goals met and open, whether the day's rewards are
    /// unlocked (the chest is earned once the daily goals of all plans are met, or every goal when no plan has a daily
    /// one) with their count and kind, the child's streak and today's chest.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodayResponse>> Get(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var day = await progress.ComputeChildDayAsync(childId, today, ct);
        var streak = await progress.ChildStreakAsync(childId, today, int.MaxValue, ct);
        var box = await dailyBox.StatusAsync(childId, today, ct);
        return new TodayResponse(childId, today, day.DutyDone, day.GoalsTotal, day.GoalsMet, day.Outstanding,
            day.RewardsUnlocked, streak, day.Positions, box, day.RewardGoalsTotal, day.RewardGoalsMet, day.RewardCadence);
    }
}
