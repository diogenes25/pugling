using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Wertet <see cref="Objective"/>s (große Ziele) und ihre <see cref="KeyResult"/>s (Etappen) <b>live</b> aus.
/// Die Beherrschungs-Metriken kommen – wie beim <see cref="LearnGoal"/> – aus einem einmal geladenen Lernstand-
/// Snapshot (<see cref="ChildLearnProgressService.ScopeEvaluator"/>), die Noten-Metrik aus den nachgetragenen
/// Klassenarbeits-Noten. Geteilte Grundlage für die Vater-CRUD-Sicht (<c>ObjectiveService</c>) und die
/// idempotente Belohnung (<c>ObjectiveRewardService</c>); es gibt keinen materialisierten Zustand.
/// </summary>
public class ObjectiveEvaluationService(PuglingDbContext db, ChildLearnProgressService progress)
{
    /// <summary>Ausgewertete Etappe: aktueller Wert, erreicht?, Fortschritt (0..100) und Status.</summary>
    public record KeyResultEval(KeyResult KeyResult, int Current, bool Achieved, int ProgressPercent, string Status);

    /// <summary>Ausgewertetes Objective inkl. Etappen und Roll-up (wie viele Etappen erreicht + Gesamtstatus).</summary>
    public record ObjectiveEval(Objective Objective, IReadOnlyList<KeyResultEval> KeyResults,
        int AchievedCount, int TotalCount, int ProgressPercent, string Status);

    // Eine bewertete Klassenarbeit, auf das für die Noten-Metrik Nötige reduziert.
    private record GradeRow(int SubjectId, decimal Grade, DateOnly ScheduledDate);

    private static int Pct(int part, int whole) => whole == 0 ? 0 : (int)Math.Round(100.0 * part / whole);

    // Aktueller Wert einer Beherrschungs-Metrik aus dem Scope-Roll-up (ClassTestGrade wird getrennt behandelt).
    private static int MasteryCurrent(KeyResultMetric metric, ChildLearnProgressService.MasteryRollup r) => metric switch
    {
        KeyResultMetric.AvgMastery => r.AvgMasteryPercent,
        KeyResultMetric.MasteredPercent => Pct(r.MasteredItems, r.TotalItems),
        KeyResultMetric.MaxWeakItems => r.WeakItems,
        _ => 0,
    };

    // MaxWeakItems und ClassTestGrade sind „nicht mehr als"-Ziele (kleiner = besser), die anderen „mindestens".
    private static bool IsAchieved(KeyResultMetric metric, int current, int target) => metric switch
    {
        KeyResultMetric.MaxWeakItems => current <= target,
        // Note nur erreicht, wenn überhaupt eine vorliegt (current > 0) UND sie mindestens so gut ist.
        KeyResultMetric.ClassTestGrade => current > 0 && current <= target,
        _ => current >= target,
    };

    private static int ProgressPercent(KeyResultMetric metric, int current, int target, bool achieved)
    {
        if (achieved) return 100;
        // „Nicht mehr als"-Ziele sind faktisch binär (erreicht/offen) – bis dahin bewusst 0.
        if (metric is KeyResultMetric.MaxWeakItems or KeyResultMetric.ClassTestGrade) return 0;
        return target <= 0 ? 100 : Math.Clamp((int)Math.Round(100.0 * current / target), 0, 99);
    }

    private static string StatusOf(bool achieved, DateOnly? dueDate, DateOnly today) =>
        achieved ? "achieved" : dueDate is { } due && due < today ? "overdue" : "open";

    /// <summary>
    /// Wertet alle Objectives eines Kindes aus (leere Liste, wenn keine existieren). Mit
    /// <paramref name="activeOnly"/> werden inaktive Ziele schon in der DB weggefiltert – so spart das
    /// Belohnungs-Settlement die (teurere) Auswertung von Zielen, die es ohnehin nicht abrechnet.
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

    /// <summary>Wertet ein einzelnes Objective aus; <c>null</c>, wenn es (für dieses Kind) nicht existiert.</summary>
    public async Task<ObjectiveEval?> EvaluateOneAsync(int childId, int objectiveId, DateOnly today, CancellationToken ct = default)
    {
        var objective = await db.Objectives.AsNoTracking().Include(o => o.KeyResults)
            .FirstOrDefaultAsync(o => o.Id == objectiveId && o.ChildId == childId, ct);
        if (objective is null) return null;
        return (await EvaluateAsync(childId, [objective], today, ct))[0];
    }

    // Kern: lädt den Lernstand-Snapshot + die Klassenarbeits-Noten einmal und wertet alle übergebenen Objectives aus.
    private async Task<List<ObjectiveEval>> EvaluateAsync(int childId, List<Objective> objectives, DateOnly today, CancellationToken ct)
    {
        if (objectives.Count == 0) return [];

        var eval = await progress.LoadScopeEvaluatorAsync(childId, ct);
        // Alle bewerteten Klassenarbeiten des Kindes einmal laden; die beste (kleinste) Note je Fach wird pro
        // Objective im Speicher gebildet (respektiert dessen Start-Datum). Die Menge je Kind ist klein.
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
                : MasteryCurrent(kr.Metric, eval.For(kr.SubjectId, kr.ChapterId, kr.ExerciseId));
            var achieved = IsAchieved(kr.Metric, current, kr.TargetValue);
            return new KeyResultEval(kr, current, achieved,
                ProgressPercent(kr.Metric, current, kr.TargetValue, achieved),
                StatusOf(achieved, o.DueDate, today));
        }).ToList();

        var total = krs.Count;
        var achievedCount = krs.Count(k => k.Achieved);
        // Objective erreicht, sobald ALLE Etappen erreicht sind (und es überhaupt welche gibt).
        var objectiveAchieved = total > 0 && achievedCount == total;
        return new ObjectiveEval(o, krs, achievedCount, total, Pct(achievedCount, total),
            StatusOf(objectiveAchieved, o.DueDate, today));
    }

    // Beste (kleinste) Note im Fach ab dem Start-Datum, als Note×10 (z. B. 2,3 → 23). 0 = noch keine Note.
    private static int BestGradeTimesTen(List<GradeRow> grades, int subjectId, DateOnly? start)
    {
        var relevant = grades.Where(g => g.SubjectId == subjectId && (start is null || g.ScheduledDate >= start)).ToList();
        return relevant.Count > 0 ? (int)Math.Round(relevant.Min(g => g.Grade) * 10) : 0;
    }
}
