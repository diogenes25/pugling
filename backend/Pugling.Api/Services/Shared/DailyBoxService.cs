using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Grants the daily reward box - the positive counterpart to
/// <see cref="PositionProgressService.SettleClosedPeriodsAsync"/> (the "stick"). Awarded once per
/// (child, day) when the day's duty is fully met (<see cref="DayOverview.DutyDone"/>), with a random
/// coins/gems draw that scales with the child's current streak (<see cref="PositionProgressService.Streak"/>,
/// reused rather than a separate login counter - see B-105, decision 2).
/// <para>
/// Deliberately evaluated only at the same write seams as the position goal reward (practice-session end,
/// test submit) - never on a GET, and never with an expiry: the box waits for the next completed duty day
/// instead of nagging or lapsing (B-105, decision 7 - the ethical guardrail against dark patterns).
/// </para>
/// </summary>
public class DailyBoxService(PuglingDbContext db, PositionProgressService progress, IOptions<DailyBoxOptions> options)
{
    private readonly DailyBoxOptions opts = options.Value;

    /// <summary>
    /// Awards the box for <paramref name="day"/> if <paramref name="dayOverview"/> reports the duty fully
    /// met and no box has been claimed yet that day. The idempotency pre-check (a single indexed lookup)
    /// runs BEFORE the streak recomputation, so it only ever runs once per calendar day, not on every
    /// practice/test completion; the streak itself uses the bounded, short-circuiting
    /// <see cref="PositionProgressService.StreakBoundedAsync"/> rather than a full-runtime scan, since this
    /// runs on the write path (unlike the supervisor's on-demand <c>overview/progress</c> read).
    /// </summary>
    public async Task EvaluateAndAwardAsync(StudyPlan plan, DateOnly day, DayOverview dayOverview, CancellationToken ct = default)
    {
        if (!dayOverview.DutyDone) return;
        if (await db.DailyBoxClaims.AnyAsync(c => c.ChildId == plan.ChildId && c.Day == day, ct)) return;

        var streak = await progress.StreakBoundedAsync(plan, day, ct);
        var multiplier = opts.StreakTiers
            .Where(t => streak >= t.FromStreak)
            .OrderByDescending(t => t.FromStreak)
            .Select(t => t.Multiplier)
            .FirstOrDefault(1.0);

        var coins = (int)Math.Round(Random.Shared.Next(opts.MinCoins, opts.MaxCoins + 1) * multiplier);
        var gems = (int)Math.Round(Random.Shared.Next(opts.MinGems, opts.MaxGems + 1) * multiplier);

        db.DailyBoxClaims.Add(new DailyBoxClaim
        {
            ChildId = plan.ChildId,
            Day = day,
            CoinsAwarded = coins,
            GemsAwarded = gems,
            StreakAtClaim = streak,
        });
        if (coins > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = PointKind.DailyBoxCoins,
                Amount = coins,
                Reason = $"Tägliche Belohnungsbox (Streak {streak})",
            });
        if (gems > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = PointKind.DailyBoxGems,
                Amount = gems,
                Reason = $"Tägliche Belohnungsbox (Streak {streak})",
            });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Same benign race as PositionProgressService.EvaluateAndAwardAsync: a concurrent completion
            // (double submit, two open tabs) already booked the box for this day via the unique index -
            // nothing is open in domain terms, so this must not surface as a 500.
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>Read-only status for the overview: whether today's box was already claimed and, if so, what it held.</summary>
    public async Task<DailyBoxStatus> StatusAsync(int childId, DateOnly day, CancellationToken ct = default)
    {
        var claim = await db.DailyBoxClaims.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChildId == childId && c.Day == day, ct);
        return claim is null
            ? new DailyBoxStatus(false, null, null, 0)
            : new DailyBoxStatus(true, claim.CoinsAwarded, claim.GemsAwarded, claim.StreakAtClaim);
    }
}
