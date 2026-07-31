using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Goal/points engine of the plan-position model (stage 4): decides per <see cref="PlanPosition"/>
/// whether its goal is <em>done</em> in the current period (rule based on the <see cref="ExerciseCheckMode"/>
/// of the referenced exercise), credits the goal points <b>idempotently</b> (<see cref="PositionGoalReward"/>),
/// and rolls up the daily/weekly status of an entire study plan. The position-wide counterpart to the
/// former plan-wide progress service – mandatory goal and points now hang off the exercise, not the plan.
/// </summary>
public class PositionProgressService(PuglingDbContext db, PositionPlayService play, ExerciseTypeRegistry registry)
{
    /// <summary>Default pass threshold of a plan-position test when the position doesn't set its own.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>
    /// How far back the lazy settlement (<see cref="SettleClosedPeriodsAsync"/>) recalculates at most. In
    /// normal operation the only open period is "yesterday"; the cap only limits catch-up recalculation
    /// after a longer absence, without ever scanning the entire history.
    /// </summary>
    private const int MaxSettleLookbackDays = 14;

    // PositionStatus/DayOverview/ProgressDay/ProgressView leben im Vertrags-Projekt (Pugling.Contracts.Shared).

    // ---- Perioden ----

    /// <summary>Monday of the week that <paramref name="day"/> falls in (week = Mon–Sun).</summary>
    private static DateOnly WeekMonday(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>
    /// Range [from, to] of the period in which the position must meet its goal. Public because the period is
    /// not owned by this service alone: the test attempt cap (<c>PositionTestsController</c>) counts per
    /// period and must use the very same boundaries – a second implementation would drift.
    /// </summary>
    public static (DateOnly From, DateOnly To) PeriodRange(GoalCadence cadence, DateOnly day) => cadence switch
    {
        GoalCadence.Weekly => (WeekMonday(day), WeekMonday(day).AddDays(6)),
        _ => (day, day),
    };

    /// <summary>
    /// First day of the period – together with the cadence it is the identity of the period for the
    /// idempotent booking. Same value as <c>PeriodRange(...).From</c>; the separate method exists so that the
    /// booking path does not have to unpack the unused second part of the range.
    /// </summary>
    private static DateOnly PeriodStart(GoalCadence cadence, DateOnly day) =>
        cadence == GoalCadence.Weekly ? WeekMonday(day) : day;

    // ---- Erledigt-Regel je Prüfmodus ----

    /// <summary>Check mode of this position's exercise (default <see cref="ExerciseCheckMode.None"/>).</summary>
    private ExerciseCheckMode CheckModeOf(PlanPosition pos) =>
        pos.Exercise is { } ex ? registry.ByKey(ex.Type)?.Manifest.CheckMode ?? ExerciseCheckMode.None : ExerciseCheckMode.None;

    /// <summary>
    /// Has enough of a session's frozen order been played to count the round as done? A content exercise has
    /// no gradable answer, so the only honest measure is how far the round was actually played:
    /// mere presence must not fulfil a duty, and active seconds are producible by leaving a tab open.
    /// <para>
    /// An <b>empty</b> order means nothing was due – then the goal counts as met, because there was nothing
    /// to play. Same behaviour as before this rule existed.
    /// </para>
    /// </summary>
    private static bool PlayedEnough(PlanPosition pos, int cursor, int total)
    {
        if (total == 0) return true;
        var percent = Math.Clamp(pos.GoalThreshold ?? DefaultPassPercent, 1, 100);
        return cursor >= (int)Math.Ceiling(total * percent / 100.0);
    }

    /// <summary>
    /// Is the position's goal done in its period around <paramref name="day"/>? Pure content/reading
    /// exercises (<see cref="ExerciseCheckMode.None"/>) count as done once a learn session has played
    /// <see cref="PlanPosition.GoalThreshold"/> percent of its frozen order (see <see cref="PlayedEnough"/>);
    /// checkable types (test/catalog check) count as done as soon as a test has been
    /// passed within the period (with <see cref="PlanPosition.RequireTypedTest"/> only a graded attempt counts).
    /// </summary>
    public async Task<bool> IsGoalMetAsync(PlanPosition pos, DateOnly day, CancellationToken ct = default)
    {
        var (from, to) = PeriodRange(pos.Cadence, day);
        if (CheckModeOf(pos) == ExerciseCheckMode.None)
        {
            // Nur echte Lern-Sitzungen zählen aufs Ziel – Info-Sitzungen (freies Üben ohne Feedback) nicht.
            // Die Menge wird in der DB gefiltert; der Vergleich Cursor↔Order.Count läuft danach im Speicher,
            // weil `Order` eine JSON-Spalte ist – `s.Order.Count` ist nicht übersetzbar und würde die Query
            // still zur Client-Auswertung über ALLE Sitzungen zwingen.
            var rounds = await db.PracticeSessions.AsNoTracking()
                .Where(s => s.PlanPositionId == pos.Id && s.Day >= from && s.Day <= to && s.Mode == PlayMode.Lern)
                .Select(s => new { s.Cursor, s.Order })
                .ToListAsync(ct);
            return rounds.Any(r => PlayedEnough(pos, r.Cursor, r.Order.Count));
        }

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
    /// Points this plan has booked from reached plan-position goals on exactly this calendar day.
    /// Deliberately via <see cref="PositionGoalReward.Day"/> (the booking day) rather than
    /// <see cref="PositionGoalReward.PeriodStart"/>: weekly goals carry the week's Monday there; filtering on
    /// that would count the same weekly reward on every day of the week, and the summed-up progress history
    /// would overstate the points by up to 7×. <b>Both fields are therefore needed</b> – the day for the
    /// metrics, the period for idempotency.
    /// </summary>
    private async Task<int> PointsAwardedAsync(int planId, DateOnly day, CancellationToken ct) =>
        await db.PositionGoalRewards
            .Where(r => r.PlanPosition!.StudyPlanId == planId && r.Day == day)
            .SumAsync(r => (int?)r.Points, ct) ?? 0;

    /// <summary>Computes a plan's daily status across its positions (without awarding points).</summary>
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
    /// Evaluates the day and credits the goal points once for every position with a reached goal
    /// (idempotent per period via <see cref="PositionGoalReward"/>). Returns the current daily status.
    /// <para>
    /// The existence check below is only the fast pre-check; the <b>guarantee</b> lives in the filtered
    /// unique index on <c>(PlanPositionId, PeriodKey)</c>. Exactly as with the penalty
    /// (<see cref="SettleClosedPeriodsAsync"/>), the concurrent loser must not break on this.
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

    /// <summary>Was the plan due in the period [<paramref name="from"/>,<paramref name="to"/>]? (fairness).</summary>
    /// <remarks>
    /// Overlap variant of the anti-cheating rule <c>PlanPlayableForChild</c>: no penalty if the plan is
    /// inactive or the period doesn't fall within its runtime at all. This avoids punishing days on which
    /// the supervisor had the plan turned off, or which fell outside the date window where learning wasn't allowed at all.
    /// </remarks>
    private static bool PlanDueForPeriod(StudyPlan plan, DateOnly from, DateOnly to) =>
        plan.Active && from <= plan.EndDate && to >= plan.StartDate;

    /// <summary>
    /// All already <b>closed</b> periods of a cadence within the window [<paramref name="windowStart"/>, today).
    /// <c>From</c> is at the same time the period start of the booking – the very value that
    /// <see cref="PeriodStart"/> returns for a day <i>inside</i> the period.
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
    /// Recalculates all <b>closed</b> mandatory periods for a child and books the coin penalty
    /// (<see cref="PlanPosition.PenaltyCoins"/>) once for every period <b>missed</b> (goal not reached),
    /// as a negative <see cref="PointKind.GoalPenalty"/> ledger entry. The "penalty" against not learning.
    /// There is no scheduler; this method is called at POST seams (login, shop purchase) and is
    /// <b>idempotent</b> via the unique index (<see cref="PositionGoalPenalty"/>) and the existence
    /// checks – triggering it multiple times doesn't double it. Debt is allowed: the coin balance may go
    /// negative (no clamp).
    /// </summary>
    /// <returns>Sum of the coins deducted in this run (0 = nothing due).</returns>
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

    /// <summary>Day-by-day status across the runtime up to today (for the supervisor evaluation view).</summary>
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

    /// <summary>Current streak: consecutive completed days up to <paramref name="today"/> (counting backward).</summary>
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
    /// Processed history for the supervisor evaluation view: the key figures (<see cref="ProgressView.DaysComplete"/>
    /// / <see cref="ProgressView.TotalPoints"/> / <see cref="ProgressView.CurrentStreak"/>) always relate to
    /// the <b>entire</b> runtime; the filter (<paramref name="from"/>/<paramref name="to"/>/<paramref name="dutyDone"/>)
    /// and sort (<paramref name="sort"/>: <c>day</c>/<c>-day</c>/<c>points</c>/<c>-points</c>) affect only
    /// the returned <see cref="ProgressView.Days"/>. The controller layers its HTTP-side paging on top of this.
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
