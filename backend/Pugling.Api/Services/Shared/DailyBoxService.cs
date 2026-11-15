using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Grants the daily reward box (a chest the child opens, B-527) - the positive counterpart to
/// <see cref="PositionProgressService.SettleClosedPeriodsAsync"/> (the "stick"). Awarded once per
/// (child, day) when the day's reward goals are met (<see cref="DayOverview.RewardsUnlocked"/>, B-535), with a random
/// coins/gems draw that scales with the child's current streak (<see cref="PositionProgressService.Streak"/>,
/// reused rather than a separate login counter - see B-105, decision 2).
/// <para>
/// Deliberately evaluated only at the same write seams as the position goal reward (practice-session end,
/// test submit) - never on a GET, and never with an expiry: the box waits for the next completed duty day
/// instead of nagging or lapsing (B-105, decision 7 - the ethical guardrail against dark patterns).
/// </para>
/// </summary>
public class DailyBoxService(PuglingDbContext db, PositionProgressService progress, IOptions<DailyBoxOptions> options,
    TimeProvider time)
{
    private readonly DailyBoxOptions opts = options.Value;

    /// <summary>
    /// Earns the box for <paramref name="day"/> (draw fixed, nothing booked yet) if <paramref name="dayOverview"/> reports the reward goals
    /// met (<see cref="DayOverview.RewardsUnlocked"/>) and no box has been claimed yet that day. The idempotency pre-check (a single indexed lookup)
    /// runs BEFORE the streak recomputation, so it only ever runs once per calendar day, not on every
    /// practice/test completion; the streak itself uses the bounded, short-circuiting
    /// <see cref="PositionProgressService.StreakBoundedAsync"/> rather than a full-runtime scan, since this
    /// runs on the write path (unlike the supervisor's on-demand <c>overview/progress</c> read).
    /// </summary>
    public async Task EvaluateAndAwardAsync(StudyPlan plan, DateOnly day, DayOverview dayOverview, CancellationToken ct = default)
    {
        if (!dayOverview.RewardsUnlocked) return;
        if (await db.DailyBoxClaims.AnyAsync(c => c.ChildId == plan.ChildId && c.Day == day, ct)) return;

        var streak = await progress.StreakBoundedAsync(plan, day, ct);
        var multiplier = opts.StreakTiers
            .Where(t => streak >= t.FromStreak)
            .OrderByDescending(t => t.FromStreak)
            .Select(t => t.Multiplier)
            .FirstOrDefault(1.0);

        var coins = (int)Math.Round(Random.Shared.Next(opts.MinCoins, opts.MaxCoins + 1) * multiplier);
        var gems = (int)Math.Round(Random.Shared.Next(opts.MinGems, opts.MaxGems + 1) * multiplier);

        // Drawn now, booked on opening (B-527): the streak of the earning day scales the chest, so saving chests up for
        // a longer streak gains nothing. No ledger row here - the balance moves only when the child opens it.
        db.DailyBoxClaims.Add(new DailyBoxClaim
        {
            ChildId = plan.ChildId,
            Day = day,
            CoinsAwarded = coins,
            GemsAwarded = gems,
            StreakAtClaim = streak,
            AwardedAt = time.GetUtcNow().UtcDateTime,
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Same benign race as PositionProgressService.EvaluateAndAwardAsync: a concurrent completion
            // (double submit, two open tabs) already earned the box for this day via the unique index -
            // nothing is open in domain terms, so this must not surface as a 500.
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>Outcome of opening a chest: either the opened chest or the error to answer with.</summary>
    public record OpenOutcome(DailyBoxOpened? Value, ApiError? Error = null, string? Detail = null);

    /// <summary>
    /// Opens the child's oldest waiting chest and books its draw to the ledger (B-527). Two guards keep a double tap from
    /// booking twice: the claim is marked opened by a conditional update (<c>OpenedAt IS NULL</c>) inside the same
    /// transaction as the booking, and the child's concurrency stamp serializes the booking against every other wallet write.
    /// </summary>
    public async Task<OpenOutcome> OpenAsync(int childId, CancellationToken ct)
    {
        // The child first: its stamp only protects what is read after it.
        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);
        var claim = await db.DailyBoxClaims.AsNoTracking()
            .Where(c => c.ChildId == childId && c.OpenedAt == null)
            .OrderBy(c => c.Day)
            .FirstOrDefaultAsync(ct);
        if (claim is null) return new OpenOutcome(null, ApiErrors.DailyBoxNone, "No reward chest is waiting to be opened.");

        var now = time.GetUtcNow().UtcDateTime;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var marked = await db.DailyBoxClaims
            .Where(c => c.Id == claim.Id && c.OpenedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.OpenedAt, now), ct);
        // A parallel open of the same chest won the update - it books, this one must not. The text names only that lock:
        // another chest may still be waiting, and the next call opens it.
        if (marked == 0)
            return new OpenOutcome(null, ApiErrors.DailyBoxNone, "Another request opened this reward chest at the same moment.");

        var reason = $"Tägliche Belohnungsbox (Streak {claim.StreakAtClaim})";
        if (claim.CoinsAwarded > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.DailyBoxCoins,
                Amount = claim.CoinsAwarded,
                Reason = reason,
                CreatedAt = now,
            });
        if (claim.GemsAwarded > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.DailyBoxGems,
                Amount = claim.GemsAwarded,
                Reason = reason,
                CreatedAt = now,
            });

        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The disposed transaction rolls the opened mark back: the chest waits again, nothing was booked.
            db.ChangeTracker.Clear();
            return new OpenOutcome(null, ApiErrors.ConcurrencyConflict,
                "Your coins were changed at the same time. Please try again.");
        }
        await tx.CommitAsync(ct);

        var waitingAfter = await db.DailyBoxClaims.CountAsync(c => c.ChildId == childId && c.OpenedAt == null, ct);
        return new OpenOutcome(new DailyBoxOpened(claim.CoinsAwarded, claim.GemsAwarded, claim.StreakAtClaim, claim.Day,
            waitingAfter));
    }

    /// <summary>
    /// Read-only status for the overview: today's chest (earned, opened) and the number of waiting chests. The content
    /// is withheld until today's chest is opened - otherwise the surprise would sit in the network tab.
    /// </summary>
    public async Task<DailyBoxStatus> StatusAsync(int childId, DateOnly day, CancellationToken ct = default)
    {
        var claim = await db.DailyBoxClaims.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChildId == childId && c.Day == day, ct);
        var waiting = await db.DailyBoxClaims.CountAsync(c => c.ChildId == childId && c.OpenedAt == null, ct);
        if (claim is null) return new DailyBoxStatus(false, false, waiting, null, null, 0);
        var opened = claim.OpenedAt is not null;
        return new DailyBoxStatus(true, opened, waiting, opened ? claim.CoinsAwarded : null,
            opened ? claim.GemsAwarded : null, claim.StreakAtClaim);
    }
}
