using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Ziel-/Punkte-Engine des Positions-Modells (Etappe 4): entscheidet je <see cref="PlanPosition"/>,
/// ob ihr Ziel in der laufenden Periode <em>erledigt</em> ist (Regel nach <see cref="ExerciseCheckMode"/>
/// der referenzierten Übung), schreibt die Ziel-Punkte <b>idempotent</b> gut (<see cref="PositionGoalReward"/>)
/// und rollt den Tages-/Wochen-Status eines ganzen Lehrplans zusammen. Das positions-weite Gegenstück zum
/// früheren plan-weiten Fortschritts-Service – Pflicht und Punkte hängen jetzt an der Übung, nicht am Plan.
/// </summary>
public class PositionProgressService(PuglingDbContext db, PositionPlayService play)
{
    /// <summary>Standard-Bestehensgrenze eines Positions-Tests, wenn die Position keine eigene Schwelle setzt.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>Status einer einzelnen Position für einen Tag – genug, damit der Sohn-Client die richtige Aktion rendert.</summary>
    public record PositionStatus(
        int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType, string Renderer,
        int Order, GoalCadence Cadence, ExerciseCheckMode CheckMode, bool UseLeitner, bool Testable,
        bool GoalMet, int DueCount, int PoolSize, int PointsGoalMet);

    /// <summary>Tages-Rollup eines Lehrplans über seine Positionen.</summary>
    public record DayOverview(
        DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded,
        IReadOnlyList<string> Outstanding, IReadOnlyList<PositionStatus> Positions);

    /// <summary>Ein Tag im Verlauf (für die Vater-Auswertung).</summary>
    public record ProgressDay(DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded);

    // ---- Perioden ----

    /// <summary>Montag der Woche, in der <paramref name="day"/> liegt (Woche = Mo–So).</summary>
    private static DateOnly WeekMonday(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>Zeitraum [von, bis] der Periode, in der die Position ihr Ziel erfüllen muss.</summary>
    private static (DateOnly From, DateOnly To) PeriodRange(GoalCadence cadence, DateOnly day) => cadence switch
    {
        GoalCadence.Weekly => (WeekMonday(day), WeekMonday(day).AddDays(6)),
        _ => (day, day),
    };

    /// <summary>Eindeutiger Schlüssel der Periode für die idempotente Belohnung (Tag bzw. Wochen-Montag).</summary>
    private static string PeriodKey(GoalCadence cadence, DateOnly day) =>
        (cadence == GoalCadence.Weekly ? WeekMonday(day) : day).ToString("yyyy-MM-dd");

    // ---- Erledigt-Regel je Prüfmodus ----

    /// <summary>Prüfmodus der Übung dieser Position (Standard <see cref="ExerciseCheckMode.None"/>).</summary>
    private static ExerciseCheckMode CheckModeOf(PlanPosition pos) =>
        pos.Exercise is { } ex ? ExerciseManifests.ByType(ex.Type)?.CheckMode ?? ExerciseCheckMode.None : ExerciseCheckMode.None;

    /// <summary>
    /// Ist das Ziel der Position in ihrer Periode um <paramref name="day"/> erledigt? Reine Inhalts-/
    /// Leseübungen (<see cref="ExerciseCheckMode.None"/>) gelten als erledigt, sobald eine Übungssitzung
    /// mit Aktivität vorliegt; prüfbare Typen (Test/Katalog-Check), sobald ein Test in der Periode
    /// bestanden wurde (bei <see cref="PlanPosition.RequireTypedTest"/> nur ein gewerteter Versuch).
    /// </summary>
    public async Task<bool> IsGoalMetAsync(PlanPosition pos, DateOnly day)
    {
        var (from, to) = PeriodRange(pos.Cadence, day);
        if (CheckModeOf(pos) == ExerciseCheckMode.None)
            // Nur echte Lern-Sitzungen zählen aufs Ziel – Info-Sitzungen (freies Üben ohne Feedback) nicht.
            return await db.PracticeSessions.AnyAsync(s =>
                s.PlanPositionId == pos.Id && s.Day >= from && s.Day <= to && s.Mode == PlayMode.Lern
                && (s.EndedAt != null || s.ActiveSeconds > 0));

        return await db.TestAttempts.AnyAsync(t =>
            t.PlanPositionId == pos.Id && t.Day >= from && t.Day <= to
            && t.CompletedAt != null && t.Passed && (!pos.RequireTypedTest || t.Graded));
    }

    // ---- Rollup + Punkte ----

    private Task<List<PlanPosition>> LoadPositionsAsync(int planId) =>
        db.PlanPositions.Include(p => p.Exercise)
            .Where(p => p.StudyPlanId == planId)
            .OrderBy(p => p.Order).ThenBy(p => p.Id)
            .ToListAsync();

    /// <summary>
    /// Punkte, die dieser Plan an genau diesem Kalendertag aus erreichten Positions-Zielen gebucht hat.
    /// Bewusst über <see cref="PositionGoalReward.Day"/> (der Buchungstag) statt über den <see cref="PositionGoalReward.PeriodKey"/>:
    /// Wochenziele tragen den Wochen-Montag als Perioden-Schlüssel; würde man danach filtern, zählte dieselbe
    /// Wochen-Belohnung an jedem Tag der Woche mit und der aufsummierte Verlauf (Progress) überhöhte die Punkte um bis zu 7×.
    /// </summary>
    private async Task<int> PointsAwardedAsync(int planId, DateOnly day) =>
        await db.PositionGoalRewards
            .Where(r => r.PlanPosition!.StudyPlanId == planId && r.Day == day)
            .SumAsync(r => (int?)r.Points) ?? 0;

    /// <summary>Berechnet den Tages-Status eines Plans über seine Positionen (ohne Punkte zu vergeben).</summary>
    public async Task<DayOverview> ComputeDayAsync(StudyPlan plan, DateOnly day)
    {
        var positions = await LoadPositionsAsync(plan.Id);
        var statuses = new List<PositionStatus>(positions.Count);

        foreach (var pos in positions)
        {
            var manifest = pos.Exercise is { } ex ? ExerciseManifests.ByType(ex.Type) : null;
            var checkMode = CheckModeOf(pos);
            var items = await play.ItemsOfAsync(pos);
            var poolSize = play.PoolSize(pos, items.Count);
            var dueCount = pos.UseLeitner ? (await play.DueItemIndicesAsync(pos, day)).Count : 0;
            var goalMet = pos.Cadence == GoalCadence.None || await IsGoalMetAsync(pos, day);

            statuses.Add(new PositionStatus(
                pos.Id, pos.ExerciseId, pos.Exercise?.Title ?? "", pos.Exercise?.Type.ToString() ?? "",
                manifest?.Renderer ?? "", pos.Order, pos.Cadence, checkMode, pos.UseLeitner,
                checkMode != ExerciseCheckMode.None, goalMet, dueCount, poolSize, pos.PointsGoalMet));
        }

        // Pflicht des Tages = alle Positionen mit Ziel (Tag heute / Woche in dieser Woche) erledigt.
        var obligations = statuses.Where(s => s.Cadence != GoalCadence.None).ToList();
        var met = obligations.Count(s => s.GoalMet);
        var dutyDone = obligations.Count > 0 && met == obligations.Count;
        var outstanding = obligations.Where(s => !s.GoalMet)
            .Select(s => $"{s.ExerciseTitle} ({(s.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")}) offen")
            .ToList();

        return new DayOverview(day, dutyDone, obligations.Count, met,
            await PointsAwardedAsync(plan.Id, day), outstanding, statuses);
    }

    /// <summary>
    /// Wertet den Tag aus und schreibt für jede Position mit erreichtem Ziel die Ziel-Punkte einmalig gut
    /// (idempotent je Periode via <see cref="PositionGoalReward"/>). Gibt den aktuellen Tages-Status zurück.
    /// </summary>
    public async Task<DayOverview> EvaluateAndAwardAsync(StudyPlan plan, DateOnly day)
    {
        var positions = await LoadPositionsAsync(plan.Id);
        foreach (var pos in positions.Where(p => p.Cadence != GoalCadence.None && p.PointsGoalMet > 0))
        {
            if (!await IsGoalMetAsync(pos, day)) continue;
            var periodKey = PeriodKey(pos.Cadence, day);
            if (await db.PositionGoalRewards.AnyAsync(r => r.PlanPositionId == pos.Id && r.PeriodKey == periodKey))
                continue;

            db.PositionGoalRewards.Add(new PositionGoalReward { PlanPositionId = pos.Id, PeriodKey = periodKey, Day = day, Points = pos.PointsGoalMet });
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = PointKind.Goal,
                Amount = pos.PointsGoalMet,
                Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {(pos.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")} erreicht",
            });
        }
        await db.SaveChangesAsync();
        return await ComputeDayAsync(plan, day);
    }

    /// <summary>Tag-für-Tag-Status über die Laufzeit bis heute (für die Vater-Auswertung).</summary>
    public async Task<IReadOnlyList<ProgressDay>> ProgressAsync(StudyPlan plan, DateOnly until)
    {
        var days = new List<ProgressDay>();
        for (var d = plan.StartDate; d <= plan.EndDate && d <= until; d = d.AddDays(1))
        {
            var o = await ComputeDayAsync(plan, d);
            days.Add(new ProgressDay(d, o.DutyDone, o.GoalsTotal, o.GoalsMet, o.PointsAwarded));
        }
        return days;
    }

    /// <summary>Aktuelle Streak: aufeinanderfolgende erledigte Tage bis <paramref name="today"/> (rückwärts).</summary>
    public static int Streak(IEnumerable<ProgressDay> days, DateOnly today)
    {
        var streak = 0;
        foreach (var d in days.Where(x => x.Day <= today).Reverse())
        {
            if (d.DutyDone) streak++; else break;
        }
        return streak;
    }

    /// <summary>
    /// Aufbereiteter Verlauf für die Vater-Auswertung: die Kennzahlen (<see cref="ProgressView.DaysComplete"/>
    /// / <see cref="ProgressView.TotalPoints"/> / <see cref="ProgressView.CurrentStreak"/>) beziehen sich stets
    /// auf die <b>gesamte</b> Laufzeit; Filter (<paramref name="from"/>/<paramref name="to"/>/<paramref name="dutyDone"/>)
    /// und Sortierung (<paramref name="sort"/>: <c>day</c>/<c>-day</c>/<c>points</c>/<c>-points</c>) wirken nur auf
    /// die zurückgegebenen <see cref="ProgressView.Days"/>. Das HTTP-seitige Paging setzt der Controller darauf.
    /// </summary>
    public async Task<ProgressView> ProgressViewAsync(StudyPlan plan, DateOnly today,
        DateOnly? from, DateOnly? to, bool? dutyDone, string? sort)
    {
        var days = await ProgressAsync(plan, plan.EndDate);
        var totalDays = plan.EndDate.DayNumber - plan.StartDate.DayNumber + 1;

        IEnumerable<ProgressDay> filtered = days;
        if (from is not null) filtered = filtered.Where(d => d.Day >= from);
        if (to is not null) filtered = filtered.Where(d => d.Day <= to);
        if (dutyDone is not null) filtered = filtered.Where(d => d.DutyDone == dutyDone);

        filtered = sort switch
        {
            "-day" => filtered.OrderByDescending(d => d.Day),
            "points" => filtered.OrderBy(d => d.PointsAwarded).ThenBy(d => d.Day),
            "-points" => filtered.OrderByDescending(d => d.PointsAwarded).ThenBy(d => d.Day),
            _ => filtered.OrderBy(d => d.Day),
        };

        return new ProgressView(days.Count(d => d.DutyDone), totalDays, days.Sum(d => d.PointsAwarded),
            Streak(days, today), filtered.ToList());
    }

    /// <summary>Aggregierter Verlauf: laufzeitweite Kennzahlen + gefilterte/sortierte Tagesliste.</summary>
    public record ProgressView(int DaysComplete, int TotalDays, int TotalPoints, int CurrentStreak,
        IReadOnlyList<ProgressDay> Days);
}
