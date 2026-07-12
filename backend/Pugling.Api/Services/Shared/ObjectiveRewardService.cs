using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Bucht die <b>einmalige</b> Belohnung erreichter <see cref="Objective"/>s idempotent – das Objective-Gegenstück
/// zur Ziel-Belohnung der Position. Je erreichter Etappe ein Häppchen (<see cref="Objective.RewardPerKeyResult"/>,
/// PeriodKey <c>kr:{id}</c>), beim Voll-Abschluss der große Batzen (<see cref="Objective.RewardOnComplete"/>,
/// PeriodKey <c>done</c>). Verbindliche Ziele zahlen 🪙 Münzen, Dehnungsziele 💎 Gems. Es gibt keinen Scheduler;
/// diese Methode wird an POST-Nahtstellen (Kind-Login, Sohn-Sicht der Ziele) aufgerufen und ist über den
/// Unique-Index (<see cref="ObjectiveReward"/>) idempotent. Bewusst <b>kein Malus</b> und <b>kein Clawback</b>:
/// eine einmal verdiente Etappe bleibt bezahlt, auch wenn der Lernstand später zurückfällt.
/// </summary>
public class ObjectiveRewardService(PuglingDbContext db, ObjectiveEvaluationService evaluation)
{
    private const string DoneKey = "done";

    /// <summary>
    /// Rechnet alle aktiven Ziele des Kindes nach und bucht offene Etappen-/Abschluss-Belohnungen einmalig.
    /// </summary>
    /// <returns>Summe der in diesem Lauf gutgeschriebenen Punkte (0 = nichts fällig).</returns>
    public async Task<int> SettleAsync(int childId, DateOnly today)
    {
        var evals = await evaluation.EvaluateAllAsync(childId, today, activeOnly: true);
        if (evals.Count == 0) return 0;

        var objectiveIds = evals.Select(e => e.Objective.Id).ToList();
        // Bereits gebuchte Anlässe je Objective einmal laden; verhindert doppelte Auszahlung schon vor dem Insert
        // (der Unique-Index ist die harte Absicherung gegen parallele Läufe).
        var booked = (await db.ObjectiveRewards.AsNoTracking()
            .Where(r => objectiveIds.Contains(r.ObjectiveId))
            .Select(r => new { r.ObjectiveId, r.PeriodKey })
            .ToListAsync())
            .Select(x => (x.ObjectiveId, x.PeriodKey)).ToHashSet();

        var awarded = 0;
        foreach (var e in evals)
        {
            var o = e.Objective;
            var kind = o.Kind == ObjectiveKind.Committed ? PointKind.ObjectiveCoins : PointKind.ObjectiveGems;

            // Etappen-Häppchen je frisch erreichter Etappe.
            if (o.RewardPerKeyResult > 0)
                foreach (var kr in e.KeyResults.Where(k => k.Achieved))
                {
                    var key = $"kr:{kr.KeyResult.Id}";
                    if (!booked.Add((o.Id, key))) continue;
                    Award(childId, o.Id, key, o.RewardPerKeyResult, kind,
                        $"[{o.Title}] Etappe geschafft: {Label(kr.KeyResult)}");
                    awarded += o.RewardPerKeyResult;
                }

            // Voll-Abschluss, sobald ALLE Etappen erreicht sind.
            if (o.RewardOnComplete > 0 && e.TotalCount > 0 && e.AchievedCount == e.TotalCount
                && booked.Add((o.Id, DoneKey)))
            {
                Award(childId, o.Id, DoneKey, o.RewardOnComplete, kind, $"[{o.Title}] Großes Ziel erreicht 🎉");
                awarded += o.RewardOnComplete;
            }
        }

        // Award() erhöht awarded synchron mit jedem Insert-Paar; awarded == 0 heißt also, wir haben nichts
        // hinzugefügt – dann kein SaveChanges (und insbesondere kein Flush fremder, getrackter Änderungen).
        if (awarded == 0) return 0;

        try
        {
            await db.SaveChangesAsync();
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

    private void Award(int childId, int objectiveId, string periodKey, int points, PointKind kind, string reason)
    {
        db.ObjectiveRewards.Add(new ObjectiveReward { ObjectiveId = objectiveId, PeriodKey = periodKey, Points = points });
        db.ChildPoints.Add(new ChildPointsEntry { ChildId = childId, Kind = kind, Amount = points, Reason = reason });
    }

    private static string Label(KeyResult kr) =>
        string.IsNullOrWhiteSpace(kr.Title) ? kr.Metric.ToString() : kr.Title!;
}
