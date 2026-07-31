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
        // Bereits gebuchte Anlässe je Objective einmal laden; verhindert doppelte Auszahlung schon vor dem Insert
        // (der Unique-Index ist die harte Absicherung gegen parallele Läufe).
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

            // Etappen-Häppchen je frisch erreichter Etappe.
            if (o.RewardPerKeyResult > 0)
                foreach (var kr in e.KeyResults.Where(k => k.Achieved))
                {
                    if (!booked.Add((o.Id, kr.KeyResult.Id))) continue;
                    Award(childId, o.Id, kr.KeyResult.Id, o.RewardPerKeyResult, kind,
                        $"[{o.Title}] Etappe geschafft: {Label(kr.KeyResult)}");
                    awarded += o.RewardPerKeyResult;
                }

            // Voll-Abschluss, sobald ALLE Etappen erreicht sind – die Buchung ohne Etappe (null).
            if (o.RewardOnComplete > 0 && e.TotalCount > 0 && e.AchievedCount == e.TotalCount
                && booked.Add((o.Id, null)))
            {
                Award(childId, o.Id, null, o.RewardOnComplete, kind, $"[{o.Title}] Großes Ziel erreicht 🎉");
                awarded += o.RewardOnComplete;
            }
        }

        // Award() erhöht awarded synchron mit jedem Insert-Paar; awarded == 0 heißt also, wir haben nichts
        // hinzugefügt – dann kein SaveChanges (und insbesondere kein Flush fremder, getrackter Änderungen).
        if (awarded == 0) return 0;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Paralleler Lauf traf den Unique-Index – gutartig: die Belohnung liegt bereits bzw. wird beim
            // nächsten Lauf idempotent nachgeholt. Nur additive Buchungen (kein Wallet-Concurrency-Bump nötig,
            // da nichts abgebucht wird und die Deckung eines Kaufs davon nicht abhängt).
            db.ChangeTracker.Clear();
            return 0;
        }
        return awarded;
    }

    // paidKeyResultId: die bezahlte Etappe, oder null für den Voll-Abschluss (der Diskriminator, s. Entity).
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
