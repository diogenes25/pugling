using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Books the <b>one-time</b> reward for reached <see cref="Objective"/>s idempotently – the objective
/// counterpart to the plan position's goal reward. A small chunk per reached key result
/// (<see cref="Objective.RewardPerKeyResult"/>, booked onto <see cref="ObjectiveReward.PaidKeyResultId"/>),
/// and the big payout on full completion (<see cref="Objective.RewardOnComplete"/>, booked without a key
/// result – that is, with <c>null</c>). Mandatory goals pay 🪙 coins, stretch goals pay 💎 gems. There is no
/// scheduler; this method is called at POST seams (child login, student-facing view of goals) and is
/// idempotent via the unique index (<see cref="ObjectiveReward"/>). Deliberately <b>no penalty</b> and
/// <b>no clawback</b>: a key result once earned stays paid, even if the learning progress later regresses.
/// </summary>
public class ObjectiveRewardService(PuglingDbContext db, ObjectiveEvaluationService evaluation)
{
    /// <summary>
    /// Recomputes all of a child's active objectives and books any outstanding key-result/completion
    /// rewards once.
    /// </summary>
    /// <returns>Sum of the points credited in this run (0 = nothing due).</returns>
    public async Task<int> SettleAsync(int childId, DateOnly today, CancellationToken ct = default)
    {
        var evals = await evaluation.EvaluateAllAsync(childId, today, activeOnly: true, ct);
        if (evals.Count == 0) return 0;

        var objectiveIds = evals.Select(e => e.Objective.Id).ToList();
        // Load the already booked occasions per objective once; that prevents a double payout before the insert
        // (the unique index is the hard safeguard against parallel runs).
        var booked = (await db.ObjectiveRewards.AsNoTracking()
            .Where(r => objectiveIds.Contains(r.ObjectiveId))
            .Select(r => new { r.ObjectiveId, r.PaidKeyResultId })
            .ToListAsync(ct))
            .Select(x => (x.ObjectiveId, x.PaidKeyResultId)).ToHashSet();

        var awarded = 0;
        foreach (var e in evals)
        {
            var o = e.Objective;
            var kind = o.Kind == ObjectiveKind.Committed ? PointKind.ObjectiveCoins : PointKind.ObjectiveGems;

            // The milestone bite per freshly reached milestone.
            if (o.RewardPerKeyResult > 0)
                foreach (var kr in e.KeyResults.Where(k => k.Achieved))
                {
                    if (!booked.Add((o.Id, kr.KeyResult.Id))) continue;
                    Award(childId, o.Id, kr.KeyResult.Id, o.RewardPerKeyResult, kind,
                        $"[{o.Title}] Etappe geschafft: {Label(kr.KeyResult)}");
                    awarded += o.RewardPerKeyResult;
                }

            // Full completion as soon as ALL milestones are reached - the entry without a milestone (null).
            if (o.RewardOnComplete > 0 && e.TotalCount > 0 && e.AchievedCount == e.TotalCount
                && booked.Add((o.Id, null)))
            {
                Award(childId, o.Id, null, o.RewardOnComplete, kind, $"[{o.Title}] Großes Ziel erreicht 🎉");
                awarded += o.RewardOnComplete;
            }
        }

        // Award() increments awarded in step with every insert pair; awarded == 0 therefore means we added
        // nothing - then no SaveChanges (and in particular no flush of other tracked changes).
        if (awarded == 0) return 0;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A parallel run hit the unique index - benign: the reward is already there (or is picked up
            // idempotently on the next run). Additive entries only (no wallet concurrency bump needed, since
            // nothing is debited and no purchase's funds check depends on it).
            db.ChangeTracker.Clear();
            return 0;
        }
        return awarded;
    }

    // paidKeyResultId: the milestone paid for, or null for the full completion (the discriminator, see the entity).
    private void Award(int childId, int objectiveId, int? paidKeyResultId, int points, PointKind kind, string reason)
    {
        db.ObjectiveRewards.Add(new ObjectiveReward
        {
            ObjectiveId = objectiveId,
            PaidKeyResultId = paidKeyResultId,
            Points = points,
        });
        db.ChildPointsEntries.Add(new ChildPointsEntry { ChildId = childId, Kind = kind, Amount = points, Reason = reason });
    }

    private static string Label(KeyResult kr) =>
        string.IsNullOrWhiteSpace(kr.Title) ? kr.Metric.ToString() : kr.Title!;
}
