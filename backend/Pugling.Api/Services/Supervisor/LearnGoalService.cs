using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Verwaltet und wertet <see cref="LearnGoal"/>s aus: Ergebnis-/Beherrschungsziele je Kind auf einem
/// Katalog-Scope (Fach/Kapitel/Übung). Der Zielstatus wird <b>live</b> aus dem aggregierten Lernstand
/// berechnet (Reuse von <see cref="ChildLearnProgressService"/>), es gibt keinen materialisierten Zustand
/// und in v1 keine Belohnung. Validiert Scope (Katalog-Referenzen + Hierarchie) und Zielwert-Bereich.
/// </summary>
public class LearnGoalService(PuglingDbContext db, ChildLearnProgressService progress)
{
    /// <summary>Ausgewertetes Lernziel inkl. aktuellem Wert und Status (<c>open</c>/<c>achieved</c>/<c>overdue</c>).</summary>
    public record LearnGoalResponse(int Id, int ChildId, int SubjectId, int? ChapterId, int? ExerciseId,
        string Scope, string Metric, int TargetValue, int CurrentValue, int ProgressPercent,
        DateOnly? DueDate, string Status, string? Title, DateTime CreatedAt);

    /// <summary>Anlage-Request (Scope + Metrik + Zielwert + optionaler Stichtag/Titel).</summary>
    public record CreateLearnGoalRequest(int SubjectId, int? ChapterId, int? ExerciseId,
        LearnGoalMetric Metric, int TargetValue, DateOnly? DueDate, string? Title);

    /// <summary>Teil-Update: nur gesetzte Felder ändern sich (Scope bleibt fix – zum Umhängen neu anlegen).</summary>
    public record UpdateLearnGoalRequest(LearnGoalMetric? Metric, int? TargetValue, DateOnly? DueDate, string? Title);

    /// <summary>Ergebnis mit optionalem Fehler-Code; <c>Value</c> und <c>Error</c> beide <c>null</c> = nicht gefunden.</summary>
    public record Result(LearnGoalResponse? Value, ApiError? Error);

    private static string ScopeOf(LearnGoal g) =>
        g.ExerciseId is not null ? "exercise" : g.ChapterId is not null ? "chapter" : "subject";

    private static int Pct(int part, int whole) => whole == 0 ? 0 : (int)Math.Round(100.0 * part / whole);

    private static int CurrentOf(LearnGoalMetric metric, ChildLearnProgressService.MasteryRollup r) => metric switch
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

    private static LearnGoalResponse Map(LearnGoal g, ChildLearnProgressService.MasteryRollup r, DateOnly today)
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
            if (!await db.Exercises.AsNoTracking().AnyAsync(e => e.Id == exId && e.ChapterId == chapterId && e.Type == ExerciseType.Vocabulary, ct))
                return ApiErrors.InvalidReference; // nur Vokabelübungen sind item-getrackt
        }

        return null;
    }

    /// <summary>Alle Lernziele des Kindes, live ausgewertet; optional nach Fach und Status gefiltert.</summary>
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

    /// <summary>Ein Lernziel live ausgewertet; <c>null</c>, wenn es (für dieses Kind) nicht existiert.</summary>
    public async Task<LearnGoalResponse?> GetAsync(int childId, int goalId, CancellationToken ct = default)
    {
        var g = await db.LearnGoals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == goalId && x.ChildId == childId, ct);
        if (g is null) return null;
        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        return Map(g, eval.For(g.SubjectId, g.ChapterId, g.ExerciseId), Today);
    }

    /// <summary>Legt ein Lernziel an (nach Scope-/Zielwert-Validierung) und liefert es ausgewertet zurück.</summary>
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

    /// <summary>Ändert Metrik/Zielwert/Stichtag/Titel eines Ziels (Scope bleibt fix). Not-found = beide null.</summary>
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

    /// <summary>Löscht ein Lernziel; <c>false</c>, wenn es (für dieses Kind) nicht existiert.</summary>
    public async Task<bool> DeleteAsync(int childId, int goalId, CancellationToken ct = default)
    {
        var goal = await db.LearnGoals.FirstOrDefaultAsync(x => x.Id == goalId && x.ChildId == childId, ct);
        if (goal is null) return false;
        db.LearnGoals.Remove(goal);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
