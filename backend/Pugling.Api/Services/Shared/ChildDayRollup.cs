using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>One active plan of the child with its day rollup (<see cref="PositionProgressService.ComputeDayAsync"/>).</summary>
public sealed record ChildPlanDay(StudyPlan Plan, DayOverview Overview);

/// <summary>
/// The child's day across all of its active, running plans (B-606) - computed by
/// <see cref="PositionProgressService.ComputeChildDayAsync"/>, read by the daily box, the bonus lock, the supervisor's
/// dashboard and <c>student/children/{id}/today</c>. A service result, not a contract: the response is
/// <see cref="TodayResponse"/>.
/// </summary>
/// <param name="Day">The day.</param>
/// <param name="Plans">The active plans running on that day, each with its own rollup.</param>
/// <param name="DutyDone">Every goal of every plan is met, and there is at least one.</param>
/// <param name="GoalsTotal">Goals over all plans.</param>
/// <param name="GoalsMet">Goals met over all plans.</param>
/// <param name="Outstanding">Child-facing lines of every open goal.</param>
/// <param name="RewardOutstanding">Child-facing lines of the open reward goals (what still locks chest and bonus).</param>
/// <param name="RewardsUnlocked">There are reward goals and all of them are met: the daily chest may be earned.</param>
/// <param name="Positions">The positions of all plans, plan by plan in the server's order.</param>
public sealed record ChildDayRollup(
    DateOnly Day, IReadOnlyList<ChildPlanDay> Plans, bool DutyDone, int GoalsTotal, int GoalsMet,
    IReadOnlyList<string> Outstanding, IReadOnlyList<string> RewardOutstanding, bool RewardsUnlocked,
    IReadOnlyList<PositionStatus> Positions);
