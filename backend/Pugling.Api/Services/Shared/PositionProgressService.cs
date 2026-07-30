using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Ziel-/Punkte-Engine des Positions-Modells (Etappe 4): entscheidet je <see cref="PlanPosition"/>,
/// ob ihr Ziel in der laufenden Periode <em>erledigt</em> ist (Regel nach <see cref="ExerciseCheckMode"/>
/// der referenzierten Übung), schreibt die Ziel-Punkte <b>idempotent</b> gut (<see cref="PositionGoalReward"/>)
/// und rollt den Tages-/Wochen-Status eines ganzen Lehrplans zusammen. Das positions-weite Gegenstück zum
/// früheren plan-weiten Fortschritts-Service – Pflicht und Punkte hängen jetzt an der Übung, nicht am Plan.
/// </summary>
public class PositionProgressService(PuglingDbContext db, PositionPlayService play, ExerciseTypeRegistry registry)
{
    /// <summary>Standard-Bestehensgrenze eines Positions-Tests, wenn die Position keine eigene Schwelle setzt.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>
    /// Wie weit das Lazy Settlement (<see cref="SettleClosedPeriodsAsync"/>) höchstens zurückrechnet. Im
    /// Normalbetrieb ist die einzige offene Periode „gestern"; der Deckel begrenzt nur Nachrechnungen nach
    /// längerer Abwesenheit, ohne je die gesamte Historie zu scannen.
    /// </summary>
    private const int MaxSettleLookbackDays = 14;

    // PositionStatus/DayOverview/ProgressDay/ProgressView leben im Vertrags-Projekt (Pugling.Contracts.Shared).

    // ---- Perioden ----

    /// <summary>Montag der Woche, in der <paramref name="day"/> liegt (Woche = Mo–So).</summary>
    private static DateOnly WeekMonday(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>Zeitraum [von, bis] der Periode, in der die Position ihr Ziel erfüllen muss.</summary>
    private static (DateOnly From, DateOnly To) PeriodRange(GoalCadence cadence, DateOnly day) => cadence switch
    {
        GoalCadence.Weekly => (WeekMonday(day), WeekMonday(day).AddDays(6)),
        _ => (day, day),
    };

    /// <summary>
    /// Erster Tag der Periode – zusammen mit der Taktung die Identität der Periode für die idempotente
    /// Buchung. Ist derselbe Wert wie <c>PeriodRange(...).From</c>; die eigene Methode existiert, damit der
    /// Buchungspfad nicht den ungenutzten zweiten Teil des Bereichs auspacken muss.
    /// </summary>
    private static DateOnly PeriodStart(GoalCadence cadence, DateOnly day) =>
        cadence == GoalCadence.Weekly ? WeekMonday(day) : day;

    // ---- Erledigt-Regel je Prüfmodus ----

    /// <summary>Prüfmodus der Übung dieser Position (Standard <see cref="ExerciseCheckMode.None"/>).</summary>
    private ExerciseCheckMode CheckModeOf(PlanPosition pos) =>
        pos.Exercise is { } ex ? registry.ByKey(ex.Type)?.Manifest.CheckMode ?? ExerciseCheckMode.None : ExerciseCheckMode.None;

    /// <summary>
    /// Ist das Ziel der Position in ihrer Periode um <paramref name="day"/> erledigt? Reine Inhalts-/
    /// Leseübungen (<see cref="ExerciseCheckMode.None"/>) gelten als erledigt, sobald eine Übungssitzung
    /// mit Aktivität vorliegt; prüfbare Typen (Test/Katalog-Check), sobald ein Test in der Periode
    /// bestanden wurde (bei <see cref="PlanPosition.RequireTypedTest"/> nur ein gewerteter Versuch).
    /// </summary>
    public async Task<bool> IsGoalMetAsync(PlanPosition pos, DateOnly day, CancellationToken ct = default)
    {
        var (from, to) = PeriodRange(pos.Cadence, day);
        if (CheckModeOf(pos) == ExerciseCheckMode.None)
            // Nur echte Lern-Sitzungen zählen aufs Ziel – Info-Sitzungen (freies Üben ohne Feedback) nicht.
            return await db.PracticeSessions.AnyAsync(s =>
                s.PlanPositionId == pos.Id && s.Day >= from && s.Day <= to && s.Mode == PlayMode.Lern
                && (s.EndedAt != null || s.ActiveSeconds > 0), ct);

        return await db.TestAttempts.AnyAsync(t =>
            t.PlanPositionId == pos.Id && t.Day >= from && t.Day <= to
            && t.CompletedAt != null && t.Passed && (!pos.RequireTypedTest || t.Graded), ct);
    }

    // ---- Rollup + Punkte ----

    private Task<List<PlanPosition>> LoadPositionsAsync(int planId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise)
            .Where(p => p.StudyPlanId == planId)
            .OrderBy(p => p.Order).ThenBy(p => p.Id)
            .ToListAsync(ct);

    /// <summary>
    /// Punkte, die dieser Plan an genau diesem Kalendertag aus erreichten Positions-Zielen gebucht hat.
    /// Bewusst über <see cref="PositionGoalReward.Day"/> (der Buchungstag) statt über
    /// <see cref="PositionGoalReward.PeriodStart"/>: Wochenziele tragen dort den Wochen-Montag; würde man danach
    /// filtern, zählte dieselbe Wochen-Belohnung an jedem Tag der Woche mit und der aufsummierte Verlauf
    /// (Progress) überhöhte die Punkte um bis zu 7×. <b>Beide Felder sind darum nötig</b> – der Tag für die
    /// Metriken, die Periode für die Idempotenz.
    /// </summary>
    private async Task<int> PointsAwardedAsync(int planId, DateOnly day, CancellationToken ct) =>
        await db.PositionGoalRewards
            .Where(r => r.PlanPosition!.StudyPlanId == planId && r.Day == day)
            .SumAsync(r => (int?)r.Points, ct) ?? 0;

    /// <summary>Berechnet den Tages-Status eines Plans über seine Positionen (ohne Punkte zu vergeben).</summary>
    public async Task<DayOverview> ComputeDayAsync(StudyPlan plan, DateOnly day, CancellationToken ct = default)
    {
        var positions = await LoadPositionsAsync(plan.Id, ct);
        var statuses = new List<PositionStatus>(positions.Count);

        foreach (var pos in positions)
        {
            var manifest = pos.Exercise is { } ex ? registry.ByKey(ex.Type)?.Manifest : null;
            var checkMode = CheckModeOf(pos);
            var items = await play.ItemsOfAsync(pos, ct: ct);
            var poolSize = play.PoolSize(pos, items.Count);
            var dueCount = pos.UseLeitner ? (await play.DueItemIndicesAsync(pos, day, ct: ct)).Count : 0;
            var goalMet = pos.Cadence == GoalCadence.None || await IsGoalMetAsync(pos, day, ct);

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
            await PointsAwardedAsync(plan.Id, day, ct), outstanding, statuses);
    }

    /// <summary>
    /// Wertet den Tag aus und schreibt für jede Position mit erreichtem Ziel die Ziel-Punkte einmalig gut
    /// (idempotent je Periode via <see cref="PositionGoalReward"/>). Gibt den aktuellen Tages-Status zurück.
    /// <para>
    /// Der Existenz-Check unten ist nur die schnelle Vorprüfung; die <b>Garantie</b> steht im gefilterten
    /// Unique-Index auf <c>(PlanPositionId, PeriodKey)</c>. Genau wie beim Malus
    /// (<see cref="SettleClosedPeriodsAsync"/>) darf der nebenläufige Verlierer daran nicht zerbrechen.
    /// </para>
    /// </summary>
    public async Task<DayOverview> EvaluateAndAwardAsync(StudyPlan plan, DateOnly day, CancellationToken ct = default)
    {
        var positions = await LoadPositionsAsync(plan.Id, ct);
        foreach (var pos in positions.Where(p => p.Cadence != GoalCadence.None && p.PointsGoalMet > 0))
        {
            if (!await IsGoalMetAsync(pos, day, ct)) continue;
            var periodStart = PeriodStart(pos.Cadence, day);
            var cadence = pos.Cadence;
            if (await db.PositionGoalRewards.AnyAsync(r => r.PlanPositionId == pos.Id
                    && r.Cadence == cadence && r.PeriodStart == periodStart, ct))
                continue;

            db.PositionGoalRewards.Add(new PositionGoalReward
            {
                PlanPositionId = pos.Id,
                Cadence = cadence,
                PeriodStart = periodStart,
                Day = day,
                Points = pos.PointsGoalMet,
            });
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = PointKind.Goal,
                Amount = pos.PointsGoalMet,
                Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {(pos.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")} erreicht",
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Zwei gleichzeitige Zielabschlüsse derselben Periode (Doppeltipp auf „Abgeben", zwei offene
            // Tabs, React-StrictMode-Doppelaufruf) laufen beide durch den Existenz-Check und der Verlierer
            // in den Unique-Index. Fachlich ist nichts offen: die Belohnung liegt, sie ist je Periode
            // einmalig, und der Betrag ist derselbe – der Konflikt heißt hier immer „schon gebucht".
            // Ein durchgereichter Fehler hätte als einzige Wirkung einen 500 auf einen gelungenen Abschluss.
            // Abhängen, damit ein späteres SaveChanges desselben Requests nicht erneut darüber stolpert;
            // der Tages-Status unten wird ohnehin frisch aus der Datenbank gelesen.
            db.ChangeTracker.Clear();
        }
        return await ComputeDayAsync(plan, day, ct);
    }

    // ---- Malus fürs Nicht-Lernen (Lazy Settlement) ----

    /// <summary>Ist der Plan in der Periode [<paramref name="from"/>,<paramref name="to"/>] fällig gewesen? (Fairness).</summary>
    /// <remarks>
    /// Überlappungs-Variante der Anti-Schummel-Regel <c>PlanPlayableForChild</c>: kein Malus, wenn der Plan
    /// inaktiv ist oder die Periode gar nicht in seine Laufzeit fällt. So wird nicht für Tage bestraft, an
    /// denen der Vater den Plan aus hatte oder außerhalb des Datumsfensters gar nicht gelernt werden durfte.
    /// </remarks>
    private static bool PlanDueForPeriod(StudyPlan plan, DateOnly from, DateOnly to) =>
        plan.Active && from <= plan.EndDate && to >= plan.StartDate;

    /// <summary>
    /// Alle bereits <b>abgeschlossenen</b> Perioden eines Rhythmus im Fenster [<paramref name="windowStart"/>, heute).
    /// <c>From</c> ist zugleich der Perioden-Anfang der Buchung – derselbe Wert, den <see cref="PeriodStart"/>
    /// für einen Tag <i>in</i> der Periode liefert.
    /// </summary>
    private static IEnumerable<(DateOnly From, DateOnly To)> ClosedPeriods(
        GoalCadence cadence, DateOnly windowStart, DateOnly today)
    {
        if (cadence == GoalCadence.Weekly)
        {
            // Nur voll abgeschlossene Wochen (Sonntag < heute); Anfang = Wochen-Montag wie beim Reward.
            for (var monday = WeekMonday(windowStart); monday.AddDays(6) < today; monday = monday.AddDays(7))
                yield return (monday, monday.AddDays(6));
        }
        else
        {
            for (var d = windowStart; d < today; d = d.AddDays(1))
                yield return (d, d);
        }
    }

    /// <summary>
    /// Rechnet für ein Kind alle <b>abgeschlossenen</b> Pflicht-Perioden nach und bucht für jede
    /// <b>gerissene</b> (Ziel nicht erreicht) einmalig den Münz-Malus (<see cref="PlanPosition.PenaltyCoins"/>)
    /// als negative <see cref="PointKind.GoalPenalty"/>-Buchung. Der „Stick" gegen Nicht-Lernen. Es gibt keinen
    /// Scheduler; diese Methode wird an POST-Nahtstellen (Login, Shop-Kauf) aufgerufen und ist über den
    /// Unique-Index (<see cref="PositionGoalPenalty"/>) sowie die Existenz-Checks <b>idempotent</b> – mehrfaches
    /// Auslösen doppelt nicht. Schulden sind erlaubt: der Münz-Saldo darf negativ werden (kein Clamp).
    /// </summary>
    /// <returns>Summe der in diesem Lauf abgezogenen Münzen (0 = nichts fällig).</returns>
    public async Task<int> SettleClosedPeriodsAsync(int childId, DateOnly today, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return 0;

        // Nur bestrafbare Positionen: echte Pflicht (Tag/Woche) mit gesetztem Malus, aus den Plänen des Kindes.
        var positions = await db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan)
            .Where(p => p.StudyPlan!.ChildId == childId && p.Cadence != GoalCadence.None && p.PenaltyCoins > 0)
            .ToListAsync(ct);
        if (positions.Count == 0) return 0;

        var lookbackFloor = today.AddDays(-MaxSettleLookbackDays);
        var appliedCoins = 0;
        var changed = false;

        foreach (var pos in positions)
        {
            var plan = pos.StudyPlan!;
            var windowStart = plan.StartDate > lookbackFloor ? plan.StartDate : lookbackFloor;

            var cadence = pos.Cadence;
            foreach (var (from, to) in ClosedPeriods(cadence, windowStart, today))
            {
                if (!PlanDueForPeriod(plan, from, to)) continue;
                // Ziel in der Periode belohnt (erreicht) oder bereits bestraft? → nichts nachzuholen.
                if (await db.PositionGoalRewards.AnyAsync(r => r.PlanPositionId == pos.Id
                        && r.Cadence == cadence && r.PeriodStart == from, ct)) continue;
                if (await db.PositionGoalPenalties.AnyAsync(r => r.PlanPositionId == pos.Id
                        && r.Cadence == cadence && r.PeriodStart == from, ct)) continue;
                // Absicherung gegen ein Rennen mit dem Belohnungspfad (PointsGoalMet == 0 bucht keinen Reward,
                // das Ziel kann trotzdem erfüllt sein): nur bei tatsächlich gerissener Periode bestrafen.
                if (await IsGoalMetAsync(pos, to, ct)) continue;

                db.PositionGoalPenalties.Add(new PositionGoalPenalty
                {
                    PlanPositionId = pos.Id,
                    Cadence = cadence,
                    PeriodStart = from,
                    Day = to,
                    Points = pos.PenaltyCoins,
                });
                db.ChildPointsEntries.Add(new ChildPointsEntry
                {
                    ChildId = childId,
                    Kind = PointKind.GoalPenalty,
                    Amount = -pos.PenaltyCoins,
                    Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {(pos.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")} gerissen",
                });
                appliedCoins += pos.PenaltyCoins;
                changed = true;
            }
        }

        if (!changed) return 0;

        // Wallet-Invariante: jeder abbuchende Pfad bumpt den Serialisierungspunkt des geteilten Saldos,
        // damit ein parallel laufender Kauf/Malus den Deckungs-Check nicht doppelt umgeht.
        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Paralleles Settlement traf den Unique-Index bzw. den Kind-Concurrency-Token – gutartig: der Malus
            // liegt bereits (bzw. wird beim nächsten Lauf idempotent nachgeholt). Nicht als Fehler durchreichen.
            db.ChangeTracker.Clear();
            return 0;
        }
        return appliedCoins;
    }

    /// <summary>Tag-für-Tag-Status über die Laufzeit bis heute (für die Vater-Auswertung).</summary>
    public async Task<IReadOnlyList<ProgressDay>> ProgressAsync(StudyPlan plan, DateOnly until,
        CancellationToken ct = default)
    {
        var days = new List<ProgressDay>();
        for (var d = plan.StartDate; d <= plan.EndDate && d <= until; d = d.AddDays(1))
        {
            var o = await ComputeDayAsync(plan, d, ct);
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
        DateOnly? from, DateOnly? to, bool? dutyDone, string? sort, CancellationToken ct = default)
    {
        var days = await ProgressAsync(plan, plan.EndDate, ct);
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

}
