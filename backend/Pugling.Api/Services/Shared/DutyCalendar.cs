using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Which days owe a duty (B-731): Monday to Saturday, outside the child's pauses. Sunday and a pause neither count for the
/// streak nor break it, and cost no penalty. The one place this rule lives, so the streak, the penalty and the supervisor's
/// timeline cannot disagree about a day. Which day "today" is still comes from <see cref="LearningDay"/> alone.
/// </summary>
public class DutyCalendar(PuglingDbContext db, IOptions<LearningDayOptions> options)
{
    /// <summary>
    /// The child's duty days, with its pauses and the tasks of its enforced daily plans (B-1036) loaded once - never one query
    /// per day inside a loop over days.
    /// </summary>
    public async Task<ChildDutyDays> ForChildAsync(int childId, CancellationToken ct)
    {
        var pauses = await db.ChildPauses.AsNoTracking().Where(p => p.ChildId == childId)
            .Select(p => new PauseSpan(p.From, p.Until)).ToListAsync(ct);
        var enforcedSince = await db.Children.AsNoTracking()
            .Where(c => c.Id == childId && c.PlannerMode == PlannerMode.Enforced)
            .Select(c => c.PlannerSince).FirstOrDefaultAsync(ct);
        // The plan's mode is a snapshot: a day planned enforced stays judged by its tasks after the planner is switched back.
        var plans = await db.DailyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Mode == PlannerMode.Enforced)
            .Select(p => new { p.Day, Positions = p.Tasks.Where(t => t.PlanPositionId != null).Select(t => t.PlanPositionId!.Value).ToList() })
            .ToListAsync(ct);
        return new(options.Value.SundayFree, pauses,
            new EnforcedDuty(enforcedSince, plans.ToDictionary(p => p.Day, p => (IReadOnlySet<int>)p.Positions.ToHashSet())));
    }
}

/// <summary>
/// The days judged by the day planner's tasks instead of the positions' cadence (B-1036): every day of an enforced plan, and
/// every day since the planner was switched to enforced - a day of that span without a plan holds no task.
/// </summary>
/// <param name="Since">The learning day the child's planner was switched to enforced; <c>null</c> while it is not.</param>
/// <param name="TasksByDay">The positions with a task, per day of an enforced plan.</param>
public sealed record EnforcedDuty(DateOnly? Since, IReadOnlyDictionary<DateOnly, IReadOnlySet<int>> TasksByDay)
{
    /// <summary>No enforced day at all: every day is judged by the positions.</summary>
    public static readonly EnforcedDuty None = new(null, new Dictionary<DateOnly, IReadOnlySet<int>>());
}

/// <summary>One pause, both days included.</summary>
public readonly record struct PauseSpan(DateOnly From, DateOnly Until);

/// <summary>The duty days of one child: a value to ask per day, loaded by <see cref="DutyCalendar.ForChildAsync"/>.</summary>
/// <param name="sundayFree">Sunday owes nothing (decision 1); off only where a test suite must not depend on the weekday.</param>
/// <param name="pauses">The child's pauses.</param>
/// <param name="enforced">The days judged by the planner's tasks (B-1036); <c>null</c> = none.</param>
public sealed class ChildDutyDays(bool sundayFree, IReadOnlyList<PauseSpan> pauses, EnforcedDuty? enforced = null)
{
    private static readonly IReadOnlySet<int> NoTasks = new HashSet<int>();
    private readonly EnforcedDuty enforced = enforced ?? EnforcedDuty.None;

    /// <summary>
    /// The positions holding a task on <paramref name="day"/> when the day is judged by the day planner's tasks (B-1036), an
    /// empty set on such a day without a plan; <c>null</c> on a day judged by the positions' own cadence.
    /// </summary>
    public IReadOnlySet<int>? TaskPositionsOn(DateOnly day) =>
        enforced.TasksByDay.TryGetValue(day, out var tasks) ? tasks
        : enforced.Since is { } since && since <= day ? NoTasks
        : null;

    /// <summary>Is any day of [<paramref name="from"/>, <paramref name="to"/>] judged by the planner's tasks?</summary>
    public bool JudgedByTasks(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            if (TaskPositionsOn(d) is not null) return true;
        return false;
    }

    /// <summary>
    /// Does <paramref name="day"/> owe the child's duty for streak and timeline: a learning day (<see cref="IsDutyDay"/>), and on
    /// a day judged by tasks one that holds a task - a day the server made no plan counts like a pause (B-644, decision 19).
    /// The planner itself asks <see cref="IsDutyDay"/>, or it would never plan a day it has not planned yet.
    /// </summary>
    public bool DayOwesDuty(DateOnly day) => IsDutyDay(day) && TaskPositionsOn(day) is not { Count: 0 };

    /// <summary>Is <paramref name="day"/> inside one of the child's pauses?</summary>
    public bool InPause(DateOnly day) => pauses.Any(p => p.From <= day && day <= p.Until);

    /// <summary>Does <paramref name="day"/> owe a duty: Monday to Saturday, outside every pause?</summary>
    public bool IsDutyDay(DateOnly day) => IsWeekday(day) && !InPause(day);

    /// <summary>
    /// Does the period [<paramref name="from"/>, <paramref name="to"/>] owe its duty: it holds a weekday and touches no
    /// pause. A single day is <see cref="IsDutyDay"/>. A week touched by a pause owes nothing (stand-in decision, B-731):
    /// its goal is one total that a sick day already cut short, and only the supervisor sets a pause, so the child cannot
    /// use it to escape. Sunday alone frees nothing - every week has one.
    /// </summary>
    public bool OwesDuty(DateOnly from, DateOnly to)
    {
        if (pauses.Any(p => p.From <= to && from <= p.Until)) return false;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (IsWeekday(d)) return true;
        return false;
    }

    private bool IsWeekday(DateOnly day) => !sundayFree || day.DayOfWeek != DayOfWeek.Sunday;
}
