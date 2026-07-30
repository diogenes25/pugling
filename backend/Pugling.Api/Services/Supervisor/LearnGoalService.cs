using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Manages and evaluates <see cref="LearnGoal"/>s: result/mastery goals per child on a
/// catalog scope (subject/chapter/exercise). The goal status is computed <b>live</b> from the aggregated
/// learning progress (reuse of <see cref="ChildLearnProgressService"/>), there is no materialized state
/// and no reward in v1. Validates scope (catalog references + hierarchy) and target value range.
/// </summary>
public class LearnGoalService(PuglingDbContext db, ChildLearnProgressService progress, ExerciseTypeRegistry registry)
{
    // LearnGoalResponse/Create-/UpdateLearnGoalRequest leben im Vertrags-Projekt (Pugling.Contracts.Supervisor);
    // das Result-Paar bleibt hier, weil es den API-internen ApiError trägt.

    /// <summary>Result with optional error code; <c>Value</c> and <c>Error</c> both <c>null</c> = not found.</summary>
    public record Result(LearnGoalResponse? Value, ApiError? Error);

    private static string ScopeOf(LearnGoal g) =>
        g.ExerciseId is not null ? "exercise" : g.ChapterId is not null ? "chapter" : "subject";

    private static int Pct(int part, int whole) => whole == 0 ? 0 : (int)Math.Round(100.0 * part / whole);

    private static int CurrentOf(LearnGoalMetric metric, MasteryRollup r) => metric switch
    {
        LearnGoalMetric.AvgMastery => r.AvgMasteryPercent,
        LearnGoalMetric.Coverage => Pct(r.IntroducedItems, r.TotalItems),
        LearnGoalMetric.MasteredPercent => Pct(r.MasteredItems, r.TotalItems),
        LearnGoalMetric.MaxWeakItems => r.WeakItems,
        _ => 0,
    };

    // MaxWeakItems ist ein „nicht mehr als"-Ziel (kleiner = besser), alle anderen sind „mindestens".
    private static bool IsAchieved(LearnGoalMetric metric, int current, int target) =>
        metric == LearnGoalMetric.MaxWeakItems ? current <= target : current >= target;

    private static int ProgressPercent(LearnGoalMetric metric, int current, int target, bool achieved)
    {
        if (achieved) return 100;
        if (metric == LearnGoalMetric.MaxWeakItems) return 0; // Untergrenzen-Ziel: bis erreicht bewusst 0
        return target <= 0 ? 100 : Math.Clamp((int)Math.Round(100.0 * current / target), 0, 99);
    }

    private static LearnGoalResponse Map(LearnGoal g, MasteryRollup r, DateOnly today)
    {
        var current = CurrentOf(g.Metric, r);
        var achieved = IsAchieved(g.Metric, current, g.TargetValue);
        var status = achieved ? "achieved" : g.DueDate is { } due && due < today ? "overdue" : "open";
        return new LearnGoalResponse(g.Id, g.ChildId, g.SubjectId, g.ChapterId, g.ExerciseId,
            ScopeOf(g), g.Metric.ToString(), g.TargetValue, current,
            ProgressPercent(g.Metric, current, g.TargetValue, achieved),
            g.DueDate, status, g.Title, g.CreatedAt);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    // Validiert Scope (Katalog + Hierarchie) und Zielwert-Bereich; null = ok.
    private async Task<ApiError?> ValidateAsync(int subjectId, int? chapterId, int? exerciseId,
        LearnGoalMetric metric, int targetValue, CancellationToken ct)
    {
        if (!Enum.IsDefined(metric))
            return ApiErrors.ValidationError;

        var max = metric == LearnGoalMetric.MaxWeakItems ? int.MaxValue : 100;
        if (targetValue < 0 || targetValue > max)
            return ApiErrors.ValidationError;

        if (!await db.Subjects.AsNoTracking().AnyAsync(s => s.Id == subjectId, ct))
            return ApiErrors.InvalidReference;

        if (chapterId is { } chId && !await db.Chapters.AsNoTracking().AnyAsync(c => c.Id == chId && c.SubjectId == subjectId, ct))
            return ApiErrors.InvalidReference;

        if (exerciseId is { } exId)
        {
            if (chapterId is null)
                return ApiErrors.ValidationError; // Übungs-Scope setzt ein Kapitel voraus
            var type = await db.Exercises.AsNoTracking()
                .Where(e => e.Id == exId && e.ChapterId == chapterId).Select(e => e.Type).FirstOrDefaultAsync(ct);
            if (type is null || registry.ByKey(type)?.SupportsLearnGoals != true)
                return ApiErrors.InvalidReference; // nur item-getrackte Typen (heute Vokabeln)
        }

        return null;
    }

    /// <summary>All learn goals of the child, evaluated live; optionally filtered by subject and status.</summary>
    public async Task<List<LearnGoalResponse>> ListAsync(int childId, int? subjectId, string? status, CancellationToken ct = default)
    {
        var q = db.LearnGoals.AsNoTracking().Where(g => g.ChildId == childId);
        if (subjectId is { } sid) q = q.Where(g => g.SubjectId == sid);
        var goals = await q.OrderBy(g => g.DueDate == null).ThenBy(g => g.DueDate).ThenBy(g => g.Id).ToListAsync(ct);

        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        var today = Today;
        var mapped = goals.Select(g => Map(g, eval.For(g.SubjectId, g.ChapterId, g.ExerciseId), today));
        if (!string.IsNullOrWhiteSpace(status))
            mapped = mapped.Where(r => r.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        return mapped.ToList();
    }

    /// <summary>A learn goal evaluated live; <c>null</c> if it (for this child) does not exist.</summary>
    public async Task<LearnGoalResponse?> GetAsync(int childId, int goalId, CancellationToken ct = default)
    {
        var g = await db.LearnGoals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == goalId && x.ChildId == childId, ct);
        if (g is null) return null;
        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        return Map(g, eval.For(g.SubjectId, g.ChapterId, g.ExerciseId), Today);
    }

    /// <summary>Creates a learn goal (after scope/target value validation) and returns it evaluated.</summary>
    public async Task<Result> CreateAsync(int childId, CreateLearnGoalRequest req, CancellationToken ct = default)
    {
        if (await ValidateAsync(req.SubjectId, req.ChapterId, req.ExerciseId, req.Metric, req.TargetValue, ct) is { } err)
            return new Result(null, err);

        var goal = new LearnGoal
        {
            ChildId = childId,
            SubjectId = req.SubjectId,
            ChapterId = req.ChapterId,
            ExerciseId = req.ExerciseId,
            Metric = req.Metric,
            TargetValue = req.TargetValue,
            DueDate = req.DueDate,
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
        };
        db.LearnGoals.Add(goal);
        await db.SaveChangesAsync(ct);

        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        return new Result(Map(goal, eval.For(goal.SubjectId, goal.ChapterId, goal.ExerciseId), Today), null);
    }

    /// <summary>Changes metric/target value/due date/title of a goal (scope stays fixed). Not-found = both null.</summary>
    public async Task<Result> UpdateAsync(int childId, int goalId, UpdateLearnGoalRequest req, CancellationToken ct = default)
    {
        var goal = await db.LearnGoals.FirstOrDefaultAsync(x => x.Id == goalId && x.ChildId == childId, ct);
        if (goal is null) return new Result(null, null);

        var metric = req.Metric ?? goal.Metric;
        var target = req.TargetValue ?? goal.TargetValue;
        // Nur Metrik/Zielwert (bereichsabhängig) neu prüfen – der Scope bleibt unverändert gültig.
        if (!Enum.IsDefined(metric) || target < 0 || (metric != LearnGoalMetric.MaxWeakItems && target > 100))
            return new Result(null, ApiErrors.ValidationError);

        goal.Metric = metric;
        goal.TargetValue = target;
        if (req.DueDate is not null) goal.DueDate = req.DueDate;
        if (req.Title is not null) goal.Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim();
        await db.SaveChangesAsync(ct);

        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        return new Result(Map(goal, eval.For(goal.SubjectId, goal.ChapterId, goal.ExerciseId), Today), null);
    }

    /// <summary>Deletes a learn goal; <c>false</c> if it (for this child) does not exist.</summary>
    public async Task<bool> DeleteAsync(int childId, int goalId, CancellationToken ct = default)
    {
        var goal = await db.LearnGoals.FirstOrDefaultAsync(x => x.Id == goalId && x.ChildId == childId, ct);
        if (goal is null) return false;
        db.LearnGoals.Remove(goal);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
