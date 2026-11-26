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
    /// The child's duty days, with its pauses loaded once - never one query per day inside a loop over days.
    /// </summary>
    public async Task<ChildDutyDays> ForChildAsync(int childId, CancellationToken ct) =>
        new(options.Value.SundayFree, await db.ChildPauses.AsNoTracking().Where(p => p.ChildId == childId)
            .Select(p => new PauseSpan(p.From, p.Until)).ToListAsync(ct));
}

/// <summary>One pause, both days included.</summary>
public readonly record struct PauseSpan(DateOnly From, DateOnly Until);

/// <summary>The duty days of one child: a value to ask per day, loaded by <see cref="DutyCalendar.ForChildAsync"/>.</summary>
/// <param name="sundayFree">Sunday owes nothing (decision 1); off only where a test suite must not depend on the weekday.</param>
/// <param name="pauses">The child's pauses.</param>
public sealed class ChildDutyDays(bool sundayFree, IReadOnlyList<PauseSpan> pauses)
{
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
