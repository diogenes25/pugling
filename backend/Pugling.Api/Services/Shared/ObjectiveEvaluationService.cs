using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Evaluates <see cref="Objective"/>s (big goals) and their <see cref="KeyResult"/>s (milestones) <b>live</b>.
/// The mastery metrics come from a learning-progress snapshot loaded once
/// (<see cref="ChildLearnProgressService.ScopeEvaluator"/>), the grade metric from the recorded
/// class test grades. Shared foundation for the adult CRUD view (<c>ObjectiveService</c>) and the
/// idempotent reward (<c>ObjectiveRewardService</c>); there is no materialized state.
/// </summary>
public class ObjectiveEvaluationService(PuglingDbContext db, ChildLearnProgressService progress)
{
    /// <summary>Evaluated milestone: current value, achieved?, progress (0..100), and status.</summary>
    public record KeyResultEval(KeyResult KeyResult, int Current, bool Achieved, int ProgressPercent, string Status);

    /// <summary>Evaluated objective incl. milestones and roll-up (how many milestones achieved + overall status).</summary>
    public record ObjectiveEval(Objective Objective, IReadOnlyList<KeyResultEval> KeyResults,
        int AchievedCount, int TotalCount, int ProgressPercent, string Status);

    // A graded class test, reduced to what the grade metric needs.
    private record GradeRow(int SubjectId, decimal Grade, DateOnly ScheduledDate);

    private static int Pct(int part, int whole) => whole == 0 ? 0 : (int)Math.Round(100.0 * part / whole);

    // Current value of a mastery metric from the scope rollup (ClassTestGrade is handled separately).
    private static int MasteryCurrent(KeyResultMetric metric, MasteryRollup r) => metric switch
    {
        KeyResultMetric.AvgMastery => r.AvgMasteryPercent,
        KeyResultMetric.MasteredPercent => Pct(r.MasteredItems, r.TotalItems),
        KeyResultMetric.MaxWeakItems => r.WeakItems,
        _ => 0,
    };

    // MaxWeakItems and ClassTestGrade are "no more than" goals (smaller = better), the others "at least".
    private static bool IsAchieved(KeyResultMetric metric, int current, int target) => metric switch
    {
        KeyResultMetric.MaxWeakItems => current <= target,
        // The grade counts as reached only if there is one at all (current > 0) AND it is at least as good.
        KeyResultMetric.ClassTestGrade => current > 0 && current <= target,
        _ => current >= target,
    };

    private static int ProgressPercent(KeyResultMetric metric, int current, int target, bool achieved)
    {
        if (achieved) return 100;
        // "No more than" goals are effectively binary (reached/open) - until then deliberately 0.
        if (metric is KeyResultMetric.MaxWeakItems or KeyResultMetric.ClassTestGrade) return 0;
        return target <= 0 ? 100 : Math.Clamp((int)Math.Round(100.0 * current / target), 0, 99);
    }

    private static string StatusOf(bool achieved, DateOnly? dueDate, DateOnly today) =>
        achieved ? "achieved" : dueDate is { } due && due < today ? "overdue" : "open";

    /// <summary>
    /// Evaluates all objectives of a child (empty list if none exist). With
    /// <paramref name="activeOnly"/>, inactive goals are already filtered out in the DB – this way the
    /// reward settlement saves the (more expensive) evaluation of goals it would not settle anyway.
    /// </summary>
    public async Task<List<ObjectiveEval>> EvaluateAllAsync(int childId, DateOnly today, bool activeOnly = false, CancellationToken ct = default)
    {
        var query = db.Objectives.AsNoTracking().Include(o => o.KeyResults).Where(o => o.ChildId == childId);
        if (activeOnly) query = query.Where(o => o.Active);
        var objectives = await query
            .OrderBy(o => o.DueDate == null).ThenBy(o => o.DueDate).ThenBy(o => o.Id)
            .ToListAsync(ct);
        return await EvaluateAsync(childId, objectives, today, ct);
    }

    /// <summary>Evaluates a single objective; <c>null</c> if it does not exist (for this child).</summary>
    public async Task<ObjectiveEval?> EvaluateOneAsync(int childId, int objectiveId, DateOnly today, CancellationToken ct = default)
    {
        var objective = await db.Objectives.AsNoTracking().Include(o => o.KeyResults)
            .FirstOrDefaultAsync(o => o.Id == objectiveId && o.ChildId == childId, ct);
        if (objective is null) return null;
        return (await EvaluateAsync(childId, [objective], today, ct))[0];
    }

    // The core: loads the learning-state snapshot + the class test grades once and evaluates all objectives passed in.
    private async Task<List<ObjectiveEval>> EvaluateAsync(int childId, List<Objective> objectives, DateOnly today, CancellationToken ct)
    {
        if (objectives.Count == 0) return [];

        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        // Load all graded class tests of the child once; the best (smallest) grade per subject is formed per
        // objective in memory (respecting its start date). The set per child is small.
        var grades = (await db.Klassenarbeiten.AsNoTracking()
            .Where(k => k.ChildId == childId && k.Status == KlassenarbeitStatus.Written
                && k.Grade != null && k.SubjectId != null)
            .Select(k => new { SubjectId = k.SubjectId!.Value, Grade = k.Grade!.Value, k.ScheduledDate })
            .ToListAsync(ct))
            .Select(g => new GradeRow(g.SubjectId, g.Grade, g.ScheduledDate))
            .ToList();

        return objectives.Select(o => EvalObjective(o, eval, grades, today)).ToList();
    }

    private static ObjectiveEval EvalObjective(Objective o, ChildLearnProgressService.ScopeEvaluator eval,
        List<GradeRow> grades, DateOnly today)
    {
        var krs = o.KeyResults.OrderBy(k => k.Id).Select(kr =>
        {
            var current = kr.Metric == KeyResultMetric.ClassTestGrade
                ? BestGradeTimesTen(grades, kr.SubjectId, o.Start)
                : MasteryCurrent(kr.Metric, eval.For(kr.SubjectId, kr.SeriesUnitId, kr.ExerciseId));
            var achieved = IsAchieved(kr.Metric, current, kr.TargetValue);
            return new KeyResultEval(kr, current, achieved,
                ProgressPercent(kr.Metric, current, kr.TargetValue, achieved),
                StatusOf(achieved, o.DueDate, today));
        }).ToList();

        var total = krs.Count;
        var achievedCount = krs.Count(k => k.Achieved);
        // The objective is reached as soon as ALL milestones are reached (and there are any at all).
        var objectiveAchieved = total > 0 && achievedCount == total;
        return new ObjectiveEval(o, krs, achievedCount, total, Pct(achievedCount, total),
            StatusOf(objectiveAchieved, o.DueDate, today));
    }

    // Best (smallest) grade in the subject from the start date on, as grade×10 (e.g. 2.3 → 23). 0 = no grade yet.
    private static int BestGradeTimesTen(List<GradeRow> grades, int subjectId, DateOnly? start)
    {
        var relevant = grades.Where(g => g.SubjectId == subjectId && (start is null || g.ScheduledDate >= start)).ToList();
        return relevant.Count > 0 ? (int)Math.Round(relevant.Min(g => g.Grade) * 10) : 0;
    }
}
