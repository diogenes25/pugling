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
public class PositionProgressService(PuglingDbContext db, PositionPlayService play, ExerciseTypeRegistry registry,
    TimeProvider time, LearningDay learningDay, DutyCalendar calendar)
{
    /// <summary>Default pass threshold of a plan-position test when the position doesn't set its own.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>
    /// The score a test of the position must reach to pass: its own threshold, else the default. The one rule for handing
    /// in a test and for correcting it later (B-768).
    /// </summary>
    public static int TestPassPercent(PlanPosition pos) => pos.GoalThreshold is > 0 ? pos.GoalThreshold.Value : DefaultPassPercent;

    /// <summary>
    /// How far back the lazy settlement (<see cref="SettleClosedPeriodsAsync"/>) recalculates at most. In
    /// normal operation the only open period is "yesterday"; the cap only limits catch-up recalculation
    /// after a longer absence, without ever scanning the entire history. Shared with the homework settlement (B-760), so both
    /// sticks reach equally far back.
    /// </summary>
    internal const int MaxSettleLookbackDays = 14;

    // PositionStatus/DayOverview/ProgressDay/ProgressView live in the contract project (Pugling.Contracts.Shared).

    // ---- Periods ---- ----

    /// <summary>The Monday of the week <paramref name="day"/> lies in - the one week start of the app (weekly goals, B-39).</summary>
    public static DateOnly WeekMonday(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>Range [from, to] of the period in which the position must meet its goal.</summary>
    private static (DateOnly From, DateOnly To) PeriodRange(GoalCadence cadence, DateOnly day) => cadence switch
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

    // Fixed list instead of the de-DE culture: culture data differs between ICU, NLS and invariant mode ("Mo." vs. "Mo").
    private static readonly string[] GermanWeekdays = ["So", "Mo", "Di", "Mi", "Do", "Fr", "Sa"];

    /// <summary>
    /// The missed period as the penalty ledger names it (B-951): "Tagesziel vom Mo 23.11." or "Wochenziel der Woche 16.–22.11.".
    /// The penalty is booked lazily at the next login, so the booking date says when the app noticed, not which day the
    /// child missed - without the period a penalty booked on a still open day reads as "today is already lost".
    /// </summary>
    internal static string MissedPeriodLabel(GoalCadence cadence, DateOnly from, DateOnly to)
    {
        if (cadence != GoalCadence.Weekly)
            return $"Tagesziel vom {GermanWeekdays[(int)from.DayOfWeek]} {from.Day}.{from.Month}.";
        var start = from.Month == to.Month ? $"{from.Day}." : $"{from.Day}.{from.Month}.";
        return $"Wochenziel der Woche {start}–{to.Day}.{to.Month}.";
    }

    /// <summary>
    /// Does the position carry a duty on <paramref name="day"/>? The one rule behind every "is this an obligation" branch
    /// (B-636): a free position never does, and a position "daily, until done" stops after the day its last task was done
    /// - no penalty, no reward and no place in the day's mission from then on, also for days settled only later.
    /// </summary>
    public static bool HasDutyOn(PlanPosition pos, DateOnly day) =>
        DutyCadenceOn(pos, day) is var cadence && cadence != GoalCadence.None
        && !(cadence == GoalCadence.UntilDone && pos.CompletedOn is { } end && day > end);

    /// <summary>
    /// The cadence the position carried on <paramref name="day"/> (B-659): up to <see cref="PlanPosition.DutyUntil"/> the
    /// cadence before the father freed it, after that none - the duty has ended, whatever the position carries today (B-248
    /// shares the span for a duty that ends while its cadence stays). Without a span: the current cadence. Every question about a
    /// day reads this instead of <see cref="PlanPosition.Cadence"/>, so a duty freed later keeps the days the child met
    /// it - streak, missions, timeline, daily box and the goal reward alike. The penalty settlement deliberately does not
    /// (decision 5 of B-659): freeing a duty waives its unsettled periods.
    /// </summary>
    public static GoalCadence DutyCadenceOn(PlanPosition pos, DateOnly day) =>
        pos.DutyUntil is not { } until ? pos.Cadence
        : day <= until ? pos.CadenceUntil ?? pos.Cadence
        : GoalCadence.None;

    /// <summary>
    /// The first day the position carries a duty at all (B-613): the later of its creation day (the learning day of <c>CreatedAt</c>, B-730) and
    /// <see cref="PlanPosition.DutySince"/> - a free position switched to a cadence owes nothing for the days it was free.
    /// The plan's start is not part of it; the callers walk the plan's runtime anyway (<see cref="FirstDueDay"/> adds it).
    /// </summary>
    public DateOnly DutyBegins(PlanPosition pos)
    {
        var created = learningDay.DayOf(pos.CreatedAt);
        return pos.DutySince > created ? pos.DutySince : created;
    }

    /// <summary>
    /// Is the position an obligation on <paramref name="day"/> for streak, missions, timeline and history (B-613)?
    /// <see cref="HasDutyOn"/> from <see cref="DutyBegins"/> on: a duty that did not exist yet cannot have been missed,
    /// and it does not count in the child's favour either. Deliberately not part of <see cref="IsGoalMetAsync"/>, which
    /// also feeds rewards - a reward for such a day goes in the child's favour.
    /// </summary>
    public bool IsObligationOn(PlanPosition pos, DateOnly day) => HasDutyOn(pos, day) && day >= DutyBegins(pos);

    /// <summary>
    /// Was the plan switched on on <paramref name="day"/> (B-613)? Active now and the day not before its last switch-on
    /// (<see cref="StudyPlan.ActivatedAt"/>): on an earlier day the child could not play it, so it counts like an
    /// inactive plan - only in the child's favour.
    /// </summary>
    internal static bool ActiveOn(StudyPlan plan, DateOnly day) => plan.Active && day >= plan.ActivatedAt;

    /// <summary>A duty due every day: daily goals and the daily remediation until it is done (B-636).</summary>
    public static bool IsDailyDuty(GoalCadence cadence) => cadence is GoalCadence.Daily or GoalCadence.UntilDone;

    /// <summary>
    /// The cadences whose booked rows close the same period as <paramref name="cadence"/> (B-666): a daily goal and
    /// "daily, until done" both settle one day, so a row booked under one of them closes that day for the other - the
    /// father switching a running remediation must not charge or reward a settled day twice. Rows keep the cadence they
    /// were booked with (a snapshot); only the existence checks read the family, so the unique index guards one cadence
    /// only - a race needs the switch and the booking in the same instant, which is accepted. A day met without a row (a goal
    /// paying no points books none, by design) is still judged by the rule in force at settlement - accepted as well: it
    /// needs a goal without points, a penalty and a switch before the next settlement. Daily and weekly stay apart: their
    /// periods differ.
    /// </summary>
    private static GoalCadence[] SamePeriodCadences(GoalCadence cadence) =>
        IsDailyDuty(cadence) ? [GoalCadence.Daily, GoalCadence.UntilDone] : [cadence];

    // ---- Done rule per check mode ---- ----

    /// <summary>Check mode of the exercise the position plays (B-848; default <see cref="ExerciseCheckMode.None"/>).</summary>
    private ExerciseCheckMode CheckModeOf(PlanPosition pos, DateOnly day) =>
        PositionPlayService.PlayedExercise(pos, day) is { } ex ? registry.ByKey(ex.Type)?.Manifest.CheckMode ?? ExerciseCheckMode.None : ExerciseCheckMode.None;

    /// <summary>
    /// Is the stage that applies on <paramref name="day"/> a free display stage (B-96)? Such a day has no
    /// question to grade, so the exam rejects it (<c>ApiErrors.StageNotTestable</c>) – which is why every rule
    /// that would otherwise demand a test attempt has to know about it.
    /// </summary>
    /// <remarks>Judged at the stage the test plays (B-598): a learn-card day is tested at its target, never shown only.</remarks>
    public bool IsDisplayOnlyDay(PlanPosition pos, StudyPlan plan, DateOnly day) =>
        PositionPlayService.PlayedExercise(pos, day) is { } ex && registry.ByKey(ex.Type) is { } type
            && type.IsDisplayOnlyStage(PositionPlayService.TestTargetStage(type, pos,
                PositionPlayService.StageForDay(pos, plan, day, type), day));

    /// <summary>
    /// Is the position testable on <paramref name="day"/>? False for content types without a check mode
    /// (nothing to grade) and for a free display stage (<see cref="IsDisplayOnlyDay"/>, B-96/B-114) - the
    /// exam rejects that stage with <c>stage_not_testable</c>, so every client affordance that offers a
    /// test/exam action has to gate on this rather than on the exercise type alone.
    /// </summary>
    public bool IsTestable(PlanPosition pos, StudyPlan plan, DateOnly day) =>
        CheckModeOf(pos, day) != ExerciseCheckMode.None && !IsDisplayOnlyDay(pos, plan, day);

    /// <summary>
    /// How many test attempts the child gets per <b>day</b>. A second chance is pedagogically right, the
    /// fifth is grade farming: without a cap the child can restart until the result is good and silently drop
    /// every bad run (an abandoned attempt writes nothing – see <c>PositionTestsController.Answer</c>).
    ///
    /// Per day and deliberately <em>not</em> per goal period: what the cap defends against is the immediate
    /// restart, and a fresh day is fresh learning rather than farming. Tied to the period, a weekly position
    /// would grant two attempts for the entire week – two Monday failures would lock the child out of its own
    /// duty until Sunday and then fine it (<see cref="PlanPosition.PenaltyCoins"/>) for missing it.
    ///
    /// It also holds for positions without a duty (<see cref="GoalCadence.None"/>): a test always writes item
    /// progress and feeds the metric missions, so unlimited restarts are farming there too.
    ///
    /// Deliberately a constant and not a field on the position: a per-position cap would be a schema change.
    /// If the cap ever needs to be configurable, this is the one place to replace.
    /// </summary>
    public const int MaxTestAttemptsPerDay = 2;

    /// <summary>
    /// The exam attempts still startable for the child on <paramref name="day"/> - the single count behind the
    /// start's cap, the submission's <c>AttemptsRemaining</c> and the practice session's <c>TestAttemptsLeft</c>
    /// (B-522), so the offer on screen and the lock on the server cannot drift apart. An open attempt is resumed
    /// rather than restarted, so it keeps at least one. Supervisor attempts belong to a different actor and are
    /// not counted; the supervisor himself is exempt from the cap and gets <see cref="MaxTestAttemptsPerDay"/>.
    /// </summary>
    public async Task<int> TestAttemptsLeftAsync(int positionId, DateOnly day, bool forSupervisor, CancellationToken ct)
    {
        if (forSupervisor) return MaxTestAttemptsPerDay;
        var childAttempts = db.TestAttempts.Where(t => t.PlanPositionId == positionId && t.Day == day && !t.BySupervisor);
        var left = Math.Max(0, MaxTestAttemptsPerDay - await childAttempts.CountAsync(ct));
        return left == 0 && await childAttempts.AnyAsync(t => t.CompletedAt == null, ct) ? 1 : left;
    }

    /// <summary>
    /// Minimum practice seconds an <b>empty</b> round must carry when the exercise has no content atoms at
    /// all. Deliberately weak – seconds are producible by leaving a tab open – but it is the only evidence
    /// that exists there, and it is strictly more than "a POST happened". See <see cref="IsGoalMetAsync"/>.
    /// </summary>
    private const int MinSecondsForContentlessRound = 60;

    /// <summary>
    /// Has enough of a session's frozen order been played to count the round as done? A content exercise has
    /// no gradable answer, so the only honest measure is how far the round was actually played:
    /// mere presence must not fulfil a duty, and active seconds are producible by leaving a tab open.
    /// <para>
    /// An <b>empty</b> order proves nothing either way – its two very different causes are told apart by
    /// <see cref="IsGoalMetAsync"/>, which alone knows whether the position has a pool.
    /// </para>
    /// </summary>
    private static bool PlayedEnough(PlanPosition pos, int cursor, int total)
    {
        if (total == 0) return false;
        var percent = Math.Clamp(pos.GoalThreshold ?? DefaultPassPercent, 1, 100);
        return cursor >= (int)Math.Ceiling(total * percent / 100.0);
    }

    /// <summary>
    /// Is the position's goal done in its period around <paramref name="day"/>? Pure content/reading
    /// exercises (<see cref="ExerciseCheckMode.None"/>) count as done once a learn session has played
    /// <see cref="PlanPosition.GoalThreshold"/> percent of its frozen order (see <see cref="PlayedEnough"/>);
    /// checkable types (test/catalog check) count as done as soon as a test has been
    /// passed within the period (with <see cref="PlanPosition.RequireTypedTest"/> only a graded attempt counts).
    /// <para>
    /// A <b>free display stage</b> (B-96) is measured by the played round as well, even on a checkable type:
    /// the exam refuses that stage, so demanding a test attempt would make the obligation unmeetable and book
    /// a penalty for a duty the product itself blocks. It is a fallback, not a replacement – a period whose
    /// evaluated day carries a real stage keeps requiring the test.
    /// </para>
    /// </summary>
    public async Task<bool> IsGoalMetAsync(PlanPosition pos, StudyPlan plan, DateOnly day, CancellationToken ct = default)
    {
        var cadence = DutyCadenceOn(pos, day);
        if (cadence == GoalCadence.UntilDone) return await RemediationDayMetAsync(pos, day, ct);
        var (from, to) = PeriodRange(cadence, day);
        if (CheckModeOf(pos, day) == ExerciseCheckMode.None || IsDisplayOnlyDay(pos, plan, day))
            return await PlayedRoundMeetsGoalAsync(pos, day, from, to, ct);

        // B-849, decision 4: a passed test of a prerequisite raises the level but meets no goal - only the target does.
        // "Passed" after a confirmed correction (B-768).
        return await db.TestAttempts.Where(TestAttempt.CountsAsPassed).AnyAsync(t =>
            t.PlanPositionId == pos.Id && t.Day >= from && t.Day <= to
            && t.CompletedAt != null && (!pos.RequireTypedTest || t.Graded)
            && (t.BuildLevel == null || t.BuildLevel >= Exercise.BuildLevelTarget), ct);
    }

    /// <summary>
    /// Keeps the duty phase of a position the father frees (B-659, closing rule): called on the switch from Daily, Weekly or
    /// a running remediation "daily, until done" (B-820) to <see cref="GoalCadence.None"/>, BEFORE the cadence is
    /// overwritten, because "met?" is judged by the old cadence. A met running period counts to its end (a weekly goal met
    /// on Wednesday keeps Thursday to Sunday), an open one does not count at all - the duty then ends the day before the
    /// period begins. A remediation's period is one day; its end after the last task stays with <c>CompletedOn</c>, which
    /// <see cref="HasDutyOn"/> reads beside the span. Only one earlier phase is kept; the switch back to a duty clears it
    /// (decision 3).
    /// </summary>
    public async Task CloseDutyAsync(PlanPosition pos, StudyPlan plan, DateOnly today, CancellationToken ct)
    {
        if (pos.Cadence is not (GoalCadence.Daily or GoalCadence.Weekly or GoalCadence.UntilDone)) return;
        // A duty that ended earlier while its cadence stayed (B-752) keeps that end: closing it again would stretch the
        // span up to today and turn the dutiless days in between into missed ones.
        if (pos.DutyUntil is { } ended && ended < today) return;
        var (from, to) = PeriodRange(pos.Cadence, today);
        pos.DutyUntil = await IsGoalMetAsync(pos, plan, today, ct) ? to : from.AddDays(-1);
        // An ended remediation counts as met from its end on; the span stops there, or the day view - which reads the
        // span, not CompletedOn - would bring the ended duty back as a met one and unlock the chest without play.
        if (pos.Cadence == GoalCadence.UntilDone && pos.CompletedOn is { } end && end < pos.DutyUntil)
            pos.DutyUntil = end;
        pos.CadenceUntil = pos.Cadence;
    }

    /// <summary>
    /// The day's duty of a remediation "daily, until done" (B-636, decision 5 of B-632): one round played through all the
    /// tasks it brought meets it, even with tasks still open - the threshold and the test do not apply, the round is the
    /// duty. From the day the last task was done on, the duty is met by definition.
    /// </summary>
    private async Task<bool> RemediationDayMetAsync(PlanPosition pos, DateOnly day, CancellationToken ct)
    {
        if (pos.CompletedOn is { } end && day >= end) return true;
        var rounds = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.PlanPositionId == pos.Id && s.Day == day && s.Mode == PlayMode.Lern)
            .Select(s => new { s.Cursor, s.Order })
            .ToListAsync(ct);
        return rounds.Any(r => r.Order.Count > 0 && r.Cursor >= r.Order.Count);
    }

    /// <summary>
    /// Did a learn round in [<paramref name="from"/>,<paramref name="to"/>] carry the position's goal?
    /// <para>
    /// An <b>empty</b> frozen order has two causes that look identical on the session but mean the opposite:
    /// either the position has a pool and Leitner simply had nothing due (nothing to play → done), or the
    /// exercise carries no content atoms at all (an essay, a text without questions). Only the pool tells
    /// them apart – and without that distinction the second case would let any duty be discharged by opening
    /// and closing a round, which is exactly the presence this rule exists to reject.
    /// </para>
    /// </summary>
    private async Task<bool> PlayedRoundMeetsGoalAsync(PlanPosition pos, DateOnly day, DateOnly from, DateOnly to,
        CancellationToken ct)
    {
        // Only real learning sessions count towards the goal - info sessions (free practice without
        // feedback) do not. The set is filtered in the DB; the comparison cursor↔Order.Count then runs in
        // memory, because `Order` is a JSON column - `s.Order.Count` is not translatable and would silently
        // force client-side evaluation over ALL sessions.
        var rounds = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.PlanPositionId == pos.Id && s.Day >= from && s.Day <= to && s.Mode == PlayMode.Lern)
            .Select(s => new { s.Cursor, s.Order, s.ActiveSeconds, s.EndedAt })
            .ToListAsync(ct);
        if (rounds.Any(r => PlayedEnough(pos, r.Cursor, r.Order.Count))) return true;

        var emptyRounds = rounds.Where(r => r.Order.Count == 0).ToList();
        if (emptyRounds.Count == 0) return false;
        // The pool is the only thing that separates "nothing was due" from "there is nothing at all";
        // it is queried here and not above so that the normal, played round costs no extra round-trip.
        var items = await play.ItemsOfAsync(pos, day, ct: ct);
        if (play.PoolSize(pos, day, items.Count) > 0) return true;
        // Contentless exercise: there is no cursor to measure. What is left as evidence is that the child
        // stayed for a while and closed the round on purpose - see MinSecondsForContentlessRound.
        return emptyRounds.Any(r => r.EndedAt != null && r.ActiveSeconds >= MinSecondsForContentlessRound);
    }

    /// <summary>
    /// How far an <b>open</b> goal has come (0…1, never 1): the best learn round of the period, measured against the
    /// part of its order <see cref="PlayedEnough"/> requires. A goal measured by a passed test has no partial state -
    /// a failed attempt is not part of a pass - so it stays 0. Kept apart from <see cref="IsGoalMetAsync"/> so the
    /// rule that books points and penalties does not depend on a display value.
    /// </summary>
    private async Task<double> OpenGoalProgressAsync(PlanPosition pos, StudyPlan plan, DateOnly day, CancellationToken ct)
    {
        var cadence = DutyCadenceOn(pos, day);
        var untilDone = cadence == GoalCadence.UntilDone;
        if (!untilDone && CheckModeOf(pos, day) != ExerciseCheckMode.None && !IsDisplayOnlyDay(pos, plan, day)) return 0;
        var (from, to) = PeriodRange(cadence, day);
        var rounds = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.PlanPositionId == pos.Id && s.Day >= from && s.Day <= to && s.Mode == PlayMode.Lern)
            .Select(s => new { s.Cursor, s.Order })
            .ToListAsync(ct);
        // A remediation round is measured against its whole order (B-636), like RemediationDayMetAsync.
        var percent = untilDone ? 100 : Math.Clamp(pos.GoalThreshold ?? DefaultPassPercent, 1, 100);
        var best = rounds.Where(r => r.Order.Count > 0)
            .Select(r => r.Cursor / Math.Ceiling(r.Order.Count * percent / 100.0))
            .DefaultIfEmpty(0).Max();
        // Below 1 on purpose: a full bar must mean "done", and only IsGoalMetAsync decides that.
        return Math.Clamp(best, 0, 0.99);
    }

    // ---- Rollup + points ---- ----

    // Unit and series ride along in the same query: the day rollup reads their names and runs once per day of a
    // history, so a lookup per position would multiply with both.
    private Task<List<PlanPosition>> LoadPositionsAsync(int planId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise).ThenInclude(e => e!.SeriesUnit).ThenInclude(u => u!.Series)
            .ThenInclude(s => s!.Subject)
            // B-848: the child sees unit and pages of the prerequisite it plays.
            .Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites!).ThenInclude(e => e.SeriesUnit)
            .ThenInclude(u => u!.Series)
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
    public async Task<DayOverview> ComputeDayAsync(StudyPlan plan, DateOnly day, CancellationToken ct = default) =>
        await ComputeDayAsync(plan, day, await calendar.ForChildAsync(plan.ChildId, ct), ct);

    /// <summary>
    /// Like <see cref="ComputeDayAsync(StudyPlan, DateOnly, CancellationToken)"/>, with the child's duty days loaded once
    /// by a caller that walks many days or plans.
    /// </summary>
    public async Task<DayOverview> ComputeDayAsync(StudyPlan plan, DateOnly day, ChildDutyDays dutyDays, CancellationToken ct)
    {
        var positions = await LoadPositionsAsync(plan.Id, ct);
        var statuses = new List<PositionStatus>(positions.Count);
        // One query for the whole plan: this method runs once per day of a history, a lookup per position would
        // multiply with both. The indices come along because the pool is only known per position below - a shrunk
        // pool must not count words beyond it (B-478).
        var introducedByPosition = (await db.PositionItemProgress.AsNoTracking()
                .Where(p => p.PlanPosition!.StudyPlanId == plan.Id && p.IntroducedAt != null)
                .Select(p => new { p.PlanPositionId, p.ExerciseId, p.ItemIndex, Imprinted = p.ImprintedAt != null })
                .ToListAsync(ct))
            // B-848: counted on the exercise the position plays - a word met on the easier level is new on the target.
            .Where(p => positions.Any(pos => pos.Id == p.PlanPositionId && PositionPlayService.PlayedExerciseId(pos, day) == p.ExerciseId))
            .ToLookup(p => p.PlanPositionId, p => (p.ItemIndex, p.Imprinted));
        var planSubject = await PlanSubjectNameAsync(plan, ct);

        // A remediation that ended before this day has left the mission (B-636); on its last day it still shows as met.
        foreach (var pos in positions.Where(p => p.Cadence != GoalCadence.UntilDone || HasDutyOn(p, day)))
        {
            // B-848: on a build path the child plays, sees and looks up the prerequisite (decision 5).
            var played = PositionPlayService.PlayedExercise(pos, day);
            var type = played is { } ex ? registry.ByKey(ex.Type) : null;
            var manifest = type?.Manifest;
            var checkMode = CheckModeOf(pos, day);
            // B-613: before the position became a duty it is a free position on that day - no open goal, no price.
            var cadence = day >= DutyBegins(pos) ? DutyCadenceOn(pos, day) : GoalCadence.None;
            var items = await play.ItemsOfAsync(pos, day, ct: ct);
            var poolSize = play.PoolSize(pos, day, items.Count);
            var dueCount = pos.UseLeitner ? (await play.DueItemIndicesAsync(pos, day, ct: ct)).Count : 0;
            var goalMet = cadence == GoalCadence.None || await IsGoalMetAsync(pos, plan, day, ct);
            var goalProgress = goalMet ? 1 : await OpenGoalProgressAsync(pos, plan, day, ct);
            // Testable is about TODAY, not just about the type: on a free display stage (B-96) the exam
            // answers 400, so a client that offered its test button would send the child into a dead end.
            var testable = IsTestable(pos, plan, day);

            statuses.Add(new PositionStatus(
                pos.Id, played?.Id ?? pos.ExerciseId, played?.Title ?? "", played?.Type.ToString() ?? "",
                manifest?.Renderer ?? "", pos.Order, cadence, checkMode, pos.UseLeitner,
                testable, goalMet, goalProgress, dueCount, poolSize, pos.PointsGoalMet,
                introducedByPosition[pos.Id].Count(p => p.ItemIndex < poolSize),
                // A free position is never charged, so it must not advertise a price either.
                cadence == GoalCadence.None ? 0 : pos.PenaltyCoins,
                OwesDutyOn(pos, plan, cadence, day, dutyDays),
                played?.SeriesUnitId ?? 0, played?.SeriesUnit?.Label ?? "",
                played?.SeriesUnit?.Series?.Name ?? "", played?.BookPages,
                plan.Id, plan.Title, planSubject ?? SeriesSubjectNameOf(pos),
                // B-643: the golden ring - imprinted words, or introduced ones where a content needs no learn card.
                introducedByPosition[pos.Id].Count(p => p.ItemIndex < poolSize && (p.Imprinted
                    || type is null || !PositionPlayService.NeedsImprint(type, items[p.ItemIndex]))),
                PositionPlayService.BuildPathOf(pos, day)));
        }

        // The day's obligation = every position with a goal (daily today / weekly this week) done.
        var obligations = statuses.Where(s => s.Cadence != GoalCadence.None).ToList();
        var met = obligations.Count(s => s.GoalMet);
        var dutyDone = obligations.Count > 0 && met == obligations.Count;
        var outstanding = obligations.Where(s => !s.GoalMet).Select(OutstandingLabel).ToList();
        var rewardObligations = RewardObligations(statuses);

        return new DayOverview(day, dutyDone, obligations.Count, met,
            await PointsAwardedAsync(plan.Id, day, ct), outstanding, statuses,
            rewardObligations.Count > 0 && rewardObligations.All(s => s.GoalMet));
    }

    /// <summary>
    /// Does the period of <paramref name="cadence"/> around <paramref name="day"/> owe its duty (B-1022)? The same gates
    /// the settlement applies before it books a penalty - plan due, duty days (Sunday, pause, a week touched by a pause),
    /// the duty's end - so the child is never shown a price that is never charged. One gate stays out on purpose: the
    /// settlement starts at the child's first play in the plan (B-535), yet an unopened plan still shows its price - the
    /// threat is what is meant to open it (whether it should is an open question to the supervisor, B-1024).
    /// </summary>
    private static bool OwesDutyOn(PlanPosition pos, StudyPlan plan, GoalCadence cadence, DateOnly day, ChildDutyDays dutyDays)
    {
        if (cadence == GoalCadence.None) return false;
        var (from, to) = PeriodRange(cadence, day);
        return PlanDueForPeriod(plan, from, to) && dutyDays.OwesDuty(from, to)
            // Mirrors the settlement; DutyCadenceOn already ends the duty after DutyUntil, so this only guards a future caller.
            && !(pos.DutyUntil is { } until && from > until);
    }

    /// <summary>
    /// The goals the day's rewards (daily box, bonus training) hang on (B-535): the daily goals, or every goal when the
    /// list has none. Weekly goals keep their own penalty at the end of the week; letting them gate the box would lock
    /// it on every day of a week whose hard topic is still open. Without daily goals the old rule stays, or weekly goals
    /// alone would hand out the box for nothing. One list of positions: a plan's for its own overview, or the whole
    /// child's day across all plans (<see cref="ComputeChildDayAsync"/>, B-611).
    /// </summary>
    public static IReadOnlyList<PositionStatus> RewardObligations(IReadOnlyList<PositionStatus> statuses)
    {
        var obligations = statuses.Where(s => s.Cadence != GoalCadence.None).ToList();
        var daily = obligations.Where(s => IsDailyDuty(s.Cadence)).ToList();
        return daily.Count > 0 ? daily : obligations;
    }

    /// <summary>
    /// The kind of the reward goals (B-612): <see cref="GoalCadence.Daily"/> when they are the daily goals (a remediation
    /// "until done" is one), <see cref="GoalCadence.Weekly"/> when there are only weekly ones, <see cref="GoalCadence.None"/>
    /// without any goal. What the child's screens name as the thing still between it and the chest.
    /// </summary>
    public static GoalCadence RewardCadence(IReadOnlyList<PositionStatus> rewardGoals) =>
        rewardGoals.Any(s => IsDailyDuty(s.Cadence)) ? GoalCadence.Daily
        : rewardGoals.Count > 0 ? GoalCadence.Weekly
        : GoalCadence.None;

    /// <summary>The child-facing line for an open goal, shared by the day's list and the bonus lock.</summary>
    public static string OutstandingLabel(PositionStatus s) =>
        $"{s.ExerciseTitle} ({(s.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")}) offen";

    // The subject a position is filed under is its plan's (B-614): the child's day is grouped the way the father filed
    // the plans, not by how a textbook spells its subject. Callers load the plan differently, so a missing navigation is
    // looked up here - a forgotten Include would otherwise fall back to the series' name without a sound.
    private async Task<string?> PlanSubjectNameAsync(StudyPlan plan, CancellationToken ct) =>
        plan.Subject?.Name
        ?? (plan.SubjectId is { } subjectId
            ? await db.Subjects.AsNoTracking().Where(s => s.Id == subjectId).Select(s => s.Name).FirstOrDefaultAsync(ct)
            : null);

    // Fallback for a plan without a subject (legacy rows, deleted subject): the catalog subject of the exercise's series,
    // else the series' free text.
    private static string SeriesSubjectNameOf(PlanPosition pos) =>
        pos.Exercise?.SeriesUnit?.Series is { } series ? series.Subject?.Name ?? series.SubjectName ?? "" : "";

    // ---- The child's day across all plans (B-606) ---- ----

    /// <summary>
    /// The child's day across every plan that is active and running on <paramref name="day"/> - the one source for the
    /// day's duty, the reward lock (daily chest, bonus training) and the child-facing day view (B-606, decision 6).
    /// <para>
    /// Duty: every goal of every such plan. Rewards: <see cref="RewardObligations"/> over the child's whole day (B-611) -
    /// the daily goals of all plans as soon as any plan has one, else every goal. Plan borders are the father's filing,
    /// not a reward rule: a plan of weekly goals must not lock the chest on a day whose daily goals are all met. The
    /// reward figures (<see cref="ChildDayRollup.RewardGoalsTotal"/> and siblings, B-612) come from the same list, so no
    /// client recounts them. With a single active plan every figure equals that plan's
    /// <see cref="ComputeDayAsync(StudyPlan, DateOnly, CancellationToken)"/>.
    /// </para>
    /// </summary>
    public async Task<ChildDayRollup> ComputeChildDayAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var plans = await db.StudyPlans.AsNoTracking().Include(p => p.Subject)
            .Where(p => p.ChildId == childId && p.Active && p.StartDate <= day && p.EndDate >= day)
            .OrderBy(p => p.StartDate).ThenBy(p => p.Id)
            .ToListAsync(ct);

        var planDays = new List<ChildPlanDay>(plans.Count);
        var dutyDays = await calendar.ForChildAsync(childId, ct);
        foreach (var plan in plans)
            planDays.Add(new ChildPlanDay(plan, await ComputeDayAsync(plan, day, dutyDays, ct)));

        var goalsTotal = planDays.Sum(p => p.Overview.GoalsTotal);
        var goalsMet = planDays.Sum(p => p.Overview.GoalsMet);
        var positions = planDays.SelectMany(p => p.Overview.Positions).ToList();
        var rewardGoals = RewardObligations(positions);
        var rewardOutstanding = rewardGoals.Where(s => !s.GoalMet).Select(OutstandingLabel).ToList();

        return new ChildDayRollup(day, planDays,
            goalsTotal > 0 && goalsMet == goalsTotal, goalsTotal, goalsMet,
            planDays.SelectMany(p => p.Overview.Outstanding).ToList(),
            rewardOutstanding,
            rewardGoals.Count > 0 && rewardOutstanding.Count == 0,
            positions,
            rewardGoals.Count, rewardGoals.Count(s => s.GoalMet), RewardCadence(rewardGoals));
    }

    /// <summary>
    /// Is a day complete for the child's streak (B-606, decision 6)? At least one plan with a duty that day met it,
    /// and no plan that was switched on that day (<see cref="ActiveOn"/>) left a duty of that day open. A plan switched off
    /// since, or switched on only later, counts in the child's favour only - the same fairness the penalty applies
    /// (<see cref="PlanDueForPeriod"/>): a chapter change or a switch back on must not break the streak backwards.
    /// </summary>
    private static bool IsCompleteDay(IEnumerable<PlanDutyState> plansOfDay)
    {
        var withDuty = plansOfDay.Where(p => p.HasDuty).ToList();
        return withDuty.Any(p => p.Done) && !withDuty.Any(p => p.Active && !p.Done);
    }

    /// <summary>
    /// The day's duty for the supervisor's timeline (B-558): <see cref="IsCompleteDay"/>, or <c>null</c> when no plan
    /// carries a duty that may count - an open duty of a plan switched off since is no duty of the day, so a never used
    /// draft neither breaks nor fulfils a period.
    /// </summary>
    internal static bool? TimelineDutyDone(IReadOnlyCollection<PlanDutyState> plansOfDay) =>
        plansOfDay.Any(p => p.HasDuty && (p.Active || p.Done)) ? IsCompleteDay(plansOfDay) : null;

    /// <summary>One plan's duty on one day, as far as the streak and the timeline need it.</summary>
    internal readonly record struct PlanDutyState(bool Active, bool HasDuty, bool Done);

    /// <summary>
    /// The duty part of <see cref="ComputeDayAsync(StudyPlan, DateOnly, CancellationToken)"/> alone
    /// (<see cref="DayOverview.DutyDone"/> and whether there is a duty at all), stopping at the first open goal. A streak runs this once per plan and day, so it must not pay for
    /// the pool and due counts the day view shows.
    /// </summary>
    private async Task<PlanDutyState> DutyStateAsync(StudyPlan plan, IReadOnlyList<PlanPosition> positions, DateOnly day,
        CancellationToken ct)
    {
        var active = ActiveOn(plan, day);
        var obligations = positions.Where(p => IsObligationOn(p, day)).ToList();
        if (obligations.Count == 0) return new PlanDutyState(active, false, false);
        foreach (var pos in obligations)
            if (!await IsGoalMetAsync(pos, plan, day, ct))
                return new PlanDutyState(active, true, false);
        return new PlanDutyState(active, true, true);
    }

    /// <summary>
    /// The child's streak (B-606): consecutive complete days (<see cref="IsCompleteDay"/>) up to and including
    /// <paramref name="today"/>, walked backward over all of the child's plans and stopped at the first incomplete day or
    /// after <paramref name="maxDays"/>. Active plans are judged first, so an open active duty stops the day without
    /// reading the inactive ones. A day without duty (Sunday, a pause - <see cref="DutyCalendar"/>, B-731) is stepped over:
    /// it neither counts nor breaks the streak, also when the child learnt on it.
    /// </summary>
    public async Task<int> ChildStreakAsync(int childId, DateOnly today, int maxDays, CancellationToken ct)
    {
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.StartDate <= today)
            .OrderByDescending(p => p.Active).ThenBy(p => p.Id)
            .ToListAsync(ct);
        if (plans.Count == 0) return 0;

        var positions = new Dictionary<int, List<PlanPosition>>();
        var dutyDays = await calendar.ForChildAsync(childId, ct);
        var firstDay = plans.Min(p => p.StartDate);
        var streak = 0;
        for (var day = today; streak < maxDays && day >= firstDay; day = day.AddDays(-1))
        {
            if (!dutyDays.IsDutyDay(day)) continue;
            var states = new List<PlanDutyState>();
            foreach (var plan in plans.Where(p => p.StartDate <= day && p.EndDate >= day))
            {
                if (!positions.TryGetValue(plan.Id, out var list))
                    positions[plan.Id] = list = await LoadPositionsAsync(plan.Id, ct);
                var state = await DutyStateAsync(plan, list, day, ct);
                states.Add(state);
                // An open duty of an active plan already decides the day; an inactive one can only add to it.
                if (state is { Active: true, HasDuty: true, Done: false }) break;
            }
            if (!IsCompleteDay(states)) break;
            streak++;
        }
        return streak;
    }

    /// <summary>
    /// Every complete day (<see cref="IsCompleteDay"/>) of the child up to and including <paramref name="until"/>, over
    /// the whole runtime of all its plans - the day count and streak behind missions and achievements.
    /// </summary>
    public async Task<IReadOnlySet<DateOnly>> CompleteChildDaysAsync(int childId, DateOnly until, CancellationToken ct)
    {
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.StartDate <= until)
            .ToListAsync(ct);

        var byDay = new Dictionary<DateOnly, List<PlanDutyState>>();
        foreach (var plan in plans)
        {
            var positions = await LoadPositionsAsync(plan.Id, ct);
            for (var d = plan.StartDate; d <= plan.EndDate && d <= until; d = d.AddDays(1))
            {
                if (!byDay.TryGetValue(d, out var states)) byDay[d] = states = [];
                states.Add(await DutyStateAsync(plan, positions, d, ct));
            }
        }
        return byDay.Where(kv => IsCompleteDay(kv.Value)).Select(kv => kv.Key).ToHashSet();
    }

    /// <summary>
    /// Evaluates the day and credits the goal points once for every position with a reached goal
    /// (idempotent per period via <see cref="PositionGoalReward"/>). Returns nothing: the hand-in path never reads a day
    /// view here, and whoever needs one afterwards calls <see cref="ComputeDayAsync(StudyPlan, DateOnly, CancellationToken)"/> (B-609).
    /// <para>
    /// The existence check below is only the fast pre-check; the <b>guarantee</b> lives in the unique index on
    /// <c>(PlanPositionId, Cadence, PeriodStart)</c> - per cadence only, see <see cref="SamePeriodCadences"/>. Exactly as with the penalty
    /// (<see cref="SettleClosedPeriodsAsync"/>), the concurrent loser must not break on this.
    /// </para>
    /// </summary>
    public async Task EvaluateAndAwardAsync(StudyPlan plan, DateOnly day, CancellationToken ct = default)
    {
        var positions = await LoadPositionsAsync(plan.Id, ct);
        var now = time.GetUtcNow().UtcDateTime;
        foreach (var pos in positions) await AddGoalRewardIfDueAsync(pos, plan, day, now, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two simultaneous goal completions of the same period (a double tap on "submit", two open tabs, a
            // React StrictMode double call) both pass the existence check and the loser runs into the unique
            // index. Nothing is open in domain terms: the reward is there, it is once per period, and the amount
            // is the same - the conflict here always means "already booked". Rethrowing would only turn a
            // successful completion into a 500. Detach it so that a later SaveChanges of the same request does
            // not stumble over it again; whatever reads the day afterwards reads it fresh from the database.
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Adds the goal reward of the position's period around <paramref name="day"/> when the goal is met there and the
    /// period is not paid yet - unsaved; the caller saves and handles the unique index (see
    /// <see cref="EvaluateAndAwardAsync"/>). Also the path a confirmed correction books a missing reward through (B-768).
    /// </summary>
    /// <returns>Whether a reward was added.</returns>
    public async Task<bool> AddGoalRewardIfDueAsync(PlanPosition pos, StudyPlan plan, DateOnly day, DateTime now,
        CancellationToken ct)
    {
        if (!HasDutyOn(pos, day) || pos.PointsGoalMet <= 0 || !await IsGoalMetAsync(pos, plan, day, ct)) return false;
        // The cadence of that day, not today's (B-659): a weekly goal freed after it paid must find its own week's row.
        var cadence = DutyCadenceOn(pos, day);
        var periodStart = PeriodStart(cadence, day);
        var samePeriod = SamePeriodCadences(cadence);
        if (await db.PositionGoalRewards.AnyAsync(r => r.PlanPositionId == pos.Id
                && samePeriod.Contains(r.Cadence) && r.PeriodStart == periodStart, ct))
            return false;

        db.PositionGoalRewards.Add(new PositionGoalReward
        {
            PlanPositionId = pos.Id,
            Cadence = cadence,
            PeriodStart = periodStart,
            Day = day,
            Points = pos.PointsGoalMet,
            AwardedAt = now,
        });
        db.ChildPointsEntries.Add(new ChildPointsEntry
        {
            ChildId = plan.ChildId,
            Kind = PointKind.Goal,
            Amount = pos.PointsGoalMet,
            Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {(cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")} erreicht",
            CreatedAt = now,
        });
        return true;
    }

    /// <summary>
    /// The penalty booked for the position's period around <paramref name="day"/> and not taken back yet, whatever the
    /// cadence was when it was booked (a daily row for the day, a weekly row for its week), or <c>null</c> (B-768).
    /// </summary>
    public Task<PositionGoalPenalty?> OpenPenaltyAroundAsync(int positionId, DateOnly day, CancellationToken ct)
    {
        var monday = WeekMonday(day);
        return db.PositionGoalPenalties.FirstOrDefaultAsync(p => p.PlanPositionId == positionId && p.RefundedAt == null
            && (p.Cadence == GoalCadence.Weekly ? p.PeriodStart == monday : p.PeriodStart == day), ct);
    }

    /// <summary>
    /// Finishes a remediation once its last task is done (B-636): stamps <see cref="PlanPosition.CompletedOn"/> and, on a
    /// position "daily, until done", books the <see cref="PlanPosition.CompletionBonus"/> exactly once. Called after the
    /// answer that marked a task done was saved, so the count reads the task just finished. The unique index on
    /// <see cref="PositionCompletionBonus"/> is the guarantee for the bonus; the null check on <c>CompletedOn</c> is only the
    /// fast path. Without a bonus two racing answers both stamp the same day, which is harmless. The end is final: tasks
    /// added to the pool later do not reopen the duty (the father assigns a new position for them).
    /// </summary>
    /// <returns>The coins booked now (0 = not finished yet, already finished, or no bonus).</returns>
    public async Task<int> CompleteRemediationIfDoneAsync(PlanPosition pos, StudyPlan plan, DateOnly day,
        CancellationToken ct)
    {
        if (!pos.Remediation || pos.CompletedOn is not null || await LastTaskDoneOnAsync(pos, day, ct) is null) return 0;

        pos.CompletedOn = day;
        var bonus = pos.Cadence == GoalCadence.UntilDone ? pos.CompletionBonus : 0;
        if (bonus > 0)
        {
            var now = time.GetUtcNow().UtcDateTime;
            db.PositionCompletionBonuses.Add(new PositionCompletionBonus
            {
                PlanPositionId = pos.Id,
                Day = day,
                Points = bonus,
                AwardedAt = now,
            });
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = PointKind.RemediationBonus,
                Amount = bonus,
                Reason = $"[{plan.Title} · {pos.Exercise?.Title}] Nacharbeit geschafft",
                CreatedAt = now,
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two final answers raced and the loser hit the unique index: the bonus is booked once already.
            db.ChangeTracker.Clear();
            return 0;
        }
        return bonus;
    }

    /// <summary>
    /// The day the last remediation task was done, or <c>null</c> while one is open (or the pool is empty) - the one
    /// count behind the finish of an answer and the stamp of a switch.
    /// </summary>
    private async Task<DateOnly?> LastTaskDoneOnAsync(PlanPosition pos, DateOnly day, CancellationToken ct)
    {
        var total = play.PoolSize(pos, day, (await play.ItemsOfAsync(pos, day, ct: ct)).Count);
        if (total == 0) return null;
        var exerciseId = PositionPlayService.PlayedExerciseId(pos, day);
        var doneDays = await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == pos.Id && p.ExerciseId == exerciseId && p.RemediationDoneAt != null
                && p.ItemIndex < total)
            .Select(p => p.RemediationDoneAt!.Value)
            .ToListAsync(ct);
        return doneDays.Count < total ? null : doneDays.Max();
    }

    /// <summary>
    /// Stamps the end of a remediation "daily, until done" whose tasks are all done without an answer having finished it
    /// (B-636): done before the switch to "until done", or the pool shrank past the last open task. No answer is left to
    /// finish it, so without the stamp the duty would charge its penalty forever. No bonus - neither the father's switch nor
    /// a shrunk pool is the child's achievement. Does not save; the caller's save carries it.
    /// </summary>
    public async Task StampFinishedRemediationAsync(PlanPosition pos, DateOnly day, CancellationToken ct)
    {
        if (pos is { Remediation: true, Cadence: GoalCadence.UntilDone, CompletedOn: null })
            pos.CompletedOn = await LastTaskDoneOnAsync(pos, day, ct);
    }

    // ---- Penalty for not learning (lazy settlement) ---- ----

    /// <summary>Was the plan due in the period [<paramref name="from"/>,<paramref name="to"/>]? (fairness).</summary>
    /// <remarks>
    /// Overlap variant of the anti-cheating rule <c>PlanPlayableForChild</c>: no penalty if the plan is
    /// inactive or the period doesn't fall within its runtime at all. This avoids punishing days on which
    /// the supervisor had the plan turned off, or which fell outside the date window where learning wasn't allowed at all.
    /// A plan switched back on (B-613) owes only periods that lie wholly after the switch (<see cref="StudyPlan.ActivatedAt"/>):
    /// the child could not play it before. A plan on since its start keeps the overlap rule, so its first partial week
    /// stays due as before.
    /// </remarks>
    private static bool PlanDueForPeriod(StudyPlan plan, DateOnly from, DateOnly to) =>
        plan.Active && (plan.ActivatedAt <= plan.StartDate || from >= plan.ActivatedAt)
        && from <= plan.EndDate && to >= plan.StartDate;

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
            // Only fully closed weeks (Sunday < today); the start is the week's Monday as with the reward.
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
    /// The day the position becomes due for the first time: <c>max(</c><see cref="DutyBegins"/><c>, plan.StartDate)</c>
    /// (B-432, B-613). CreatedAt is stored in UTC; its day is the learning day it falls on (B-730).
    /// </summary>
    private DateOnly FirstDueDay(PlanPosition pos, StudyPlan plan)
    {
        var begins = DutyBegins(pos);
        return begins > plan.StartDate ? begins : plan.StartDate;
    }

    /// <summary>
    /// Is the missed <paramref name="day"/> the position's introduction day, and did the child introduce that day's
    /// portion (B-432)? On that day every item is new: the child meets them in round 1 and would have to pass a test about
    /// words seen minutes ago. Doing what the day sensibly asks - introducing - must not cost coins.
    /// <para>
    /// The rule only spares the penalty; it does NOT count as goal met (no reward, no daily box, the overview keeps the
    /// goal open), which is why it lives here and not in <see cref="IsGoalMetAsync"/>: introducing deliberately earns
    /// nothing. The introduction day is the first day the position owes anything: its first due day,
    /// <c>max(learning day of CreatedAt, plan.StartDate)</c>, or the child's first play day in the plan when that is later
    /// (B-535 - before it nothing is settled, so a fixed earlier day would never be spared) - moved on to the first duty
    /// day from there (B-1021): a Sunday or a paused day is never settled, so an exception resting on it would be lost, and
    /// the portion counts what was introduced from the first due day up to the introduction day, so words met on the free
    /// Sunday still count. Both are fixed once reached, so skipping cannot push the exception to tomorrow. Daily cadence
    /// only - a weekly goal leaves days to practise. The portion is the whole pool, or <c>min(dose, pool)</c> under a daily dose (whose positions
    /// introduce every day, hence the fixed day). A position without an introducible pool keeps the old rule. Already
    /// booked penalties stay: this check only runs for periods not yet settled.
    /// </para>
    /// </summary>
    private async Task<bool> IsIntroductionDaySparedAsync(PlanPosition pos, StudyPlan plan, DateOnly firstPlay,
        DateOnly day, ChildDutyDays dutyDays, CancellationToken ct)
    {
        if (pos.Cadence != GoalCadence.Daily) return false;
        var firstDue = FirstDueDay(pos, plan);
        var introductionDay = firstPlay > firstDue ? firstPlay : firstDue;
        // Bounded by the settled day: past it the answer is "not this day" anyway, however long a pause runs.
        while (introductionDay < day && !dutyDays.IsDutyDay(introductionDay)) introductionDay = introductionDay.AddDays(1);
        if (day != introductionDay) return false;

        var pool = play.PoolSize(pos, day, (await play.ItemsOfAsync(pos, day, ct: ct)).Count);
        if (pool == 0) return false;
        var portion = play.DailyDose(pos, day) is > 0 and var dose ? Math.Min(dose, pool) : pool;
        var exerciseId = PositionPlayService.PlayedExerciseId(pos, day);
        var introduced = await db.PositionItemProgress.CountAsync(
            p => p.PlanPositionId == pos.Id && p.ExerciseId == exerciseId
                && p.IntroducedAt >= firstDue && p.IntroducedAt <= introductionDay, ct);
        return introduced >= portion;
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

        // Only punishable positions: a real obligation (daily/weekly) with a penalty set, from the child's plans. The raw
        // cadence on purpose, not DutyCadenceOn (decision 5 of B-659): freeing a duty waives its unsettled periods.
        var positions = await db.PlanPositions.Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites)
            .Include(p => p.StudyPlan)
            .Where(p => p.StudyPlan!.ChildId == childId && p.Cadence != GoalCadence.None && p.PenaltyCoins > 0)
            .ToListAsync(ct);
        if (positions.Count == 0) return 0;

        var lookbackFloor = today.AddDays(-MaxSettleLookbackDays);
        var now = time.GetUtcNow().UtcDateTime;
        var dutyDays = await calendar.ForChildAsync(childId, ct);
        var appliedCoins = 0;
        var changed = false;

        var firstPlayDays = await FirstPlayDaysAsync(positions.Select(p => p.StudyPlanId).Distinct().ToList(), ct);

        foreach (var pos in positions)
        {
            var plan = pos.StudyPlan!;
            // B-535: the penalty starts with the child's first play in this plan - a plan set up days before the child
            // ever opens it must not greet the child with a batch of deductions. From then on every position counts,
            // also one never opened, or skipping a position would be the way out of its duty.
            if (!firstPlayDays.TryGetValue(plan.Id, out var firstPlay)) continue;
            // B-432: a position owes nothing for days before it existed - settlement starts at its first due day.
            // Penalties already booked for such days stay (no retroactive rewrite of the ledger).
            var firstDue = FirstDueDay(pos, plan);
            var windowStart = new[] { firstDue, firstPlay, lookbackFloor }.Max();

            var cadence = pos.Cadence;
            var samePeriod = SamePeriodCadences(cadence);
            foreach (var (from, to) in ClosedPeriods(cadence, windowStart, today))
            {
                if (!PlanDueForPeriod(plan, from, to)) continue;
                // B-731: Sunday and a pause owe nothing; a week owes its goal unless a pause touches it.
                if (!dutyDays.OwesDuty(from, to)) continue;
                // A duty with an end while its cadence stays (B-752: the preparation before a class test ends the day before
                // the test) owes nothing after that end - also for days settled only later. A freed duty never gets here.
                if (pos.DutyUntil is { } until && from > until) continue;
                // Goal rewarded (reached) in the period, or already punished - also under the other daily cadence
                // (B-666)? → nothing to catch up on.
                if (await db.PositionGoalRewards.AnyAsync(r => r.PlanPositionId == pos.Id
                        && samePeriod.Contains(r.Cadence) && r.PeriodStart == from, ct)) continue;
                if (await db.PositionGoalPenalties.AnyAsync(r => r.PlanPositionId == pos.Id
                        && samePeriod.Contains(r.Cadence) && r.PeriodStart == from, ct)) continue;
                // Safeguard against a race with the reward path (PointsGoalMet == 0 books no reward, yet the
                // goal can still be met): punish only when the period was actually missed.
                if (await IsGoalMetAsync(pos, plan, to, ct)) continue;
                // Spared, not met: no penalty, but no reward either - the reward path never sees this rule (B-432).
                if (await IsIntroductionDaySparedAsync(pos, plan, firstPlay, from, dutyDays, ct)) continue;

                db.PositionGoalPenalties.Add(new PositionGoalPenalty
                {
                    PlanPositionId = pos.Id,
                    Cadence = cadence,
                    PeriodStart = from,
                    Day = to,
                    Points = pos.PenaltyCoins,
                    AppliedAt = now,
                });
                db.ChildPointsEntries.Add(new ChildPointsEntry
                {
                    ChildId = childId,
                    Kind = PointKind.GoalPenalty,
                    Amount = -pos.PenaltyCoins,
                    Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {MissedPeriodLabel(cadence, from, to)} gerissen",
                    CreatedAt = now,
                });
                appliedCoins += pos.PenaltyCoins;
                changed = true;
            }
        }

        if (!changed) return 0;

        // Wallet invariant: every debiting path bumps the serialization point of the shared balance, so that a
        // purchase/penalty running in parallel cannot bypass the funds check twice.
        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A parallel settlement hit the unique index or the child concurrency token - benign: the penalty is
            // already there (or is picked up idempotently on the next run). Do not rethrow as an error.
            db.ChangeTracker.Clear();
            return 0;
        }
        return appliedCoins;
    }

    /// <summary>
    /// The first day the child itself played each plan (B-535): its first own answer the server attributes to an atom
    /// (<see cref="PositionItemProgress.IntroducedAt"/>, stamped only for the child, B-433) or its first own test. Opening
    /// a round without answering, or only missing in a set-graded list (a miss credits no atom), does not start it.
    /// Supervisor previews and catch-up rounds do not count - otherwise the father's check would start the child's
    /// penalty clock. Plans the child never played are absent.
    /// </summary>
    private async Task<Dictionary<int, DateOnly>> FirstPlayDaysAsync(IReadOnlyCollection<int> planIds, CancellationToken ct)
    {
        var introduced = await db.PositionItemProgress.AsNoTracking()
            .Where(p => planIds.Contains(p.PlanPosition!.StudyPlanId) && p.IntroducedAt != null)
            .GroupBy(p => p.PlanPosition!.StudyPlanId)
            .Select(g => new { PlanId = g.Key, Day = g.Min(p => p.IntroducedAt!.Value) })
            .ToListAsync(ct);
        var tested = await db.TestAttempts.AsNoTracking()
            .Where(t => planIds.Contains(t.StudyPlanId) && !t.BySupervisor)
            .GroupBy(t => t.StudyPlanId)
            .Select(g => new { PlanId = g.Key, Day = g.Min(t => t.Day) })
            .ToListAsync(ct);

        var first = new Dictionary<int, DateOnly>();
        foreach (var (planId, day) in introduced.Select(x => (x.PlanId, x.Day)).Concat(tested.Select(x => (x.PlanId, x.Day))))
            if (!first.TryGetValue(planId, out var known) || day < known) first[planId] = day;
        return first;
    }

    /// <summary>Day-by-day status across the runtime up to today (for the supervisor evaluation view).</summary>
    public async Task<IReadOnlyList<ProgressDay>> ProgressAsync(StudyPlan plan, DateOnly until,
        CancellationToken ct = default)
    {
        var days = new List<ProgressDay>();
        var dutyDays = await calendar.ForChildAsync(plan.ChildId, ct);
        for (var d = plan.StartDate; d <= plan.EndDate && d <= until; d = d.AddDays(1))
        {
            var o = await ComputeDayAsync(plan, d, dutyDays, ct);
            days.Add(new ProgressDay(d, o.DutyDone, o.GoalsTotal, o.GoalsMet, o.PointsAwarded));
        }
        return days;
    }

    /// <summary>
    /// How far back <see cref="StreakBoundedAsync(int, DateOnly, CancellationToken)"/> counts at most. The daily reward box's escalation
    /// tiers (B-105) cap at 30 days, so a streak at or beyond this bound already sits in the highest tier -
    /// counting further would cost EF round-trips without ever changing the reward. Same reasoning as
    /// <see cref="MaxSettleLookbackDays"/> for the equally full-runtime-shaped penalty settlement.
    /// </summary>
    private const int MaxStreakLookbackDays = 45;

    /// <summary>
    /// The child's streak (<see cref="ChildStreakAsync"/>) for the write path (the daily box), counted back at most
    /// <see cref="MaxStreakLookbackDays"/> days - beyond that bound the exact value doesn't change the reward.
    /// </summary>
    public Task<int> StreakBoundedAsync(int childId, DateOnly today, CancellationToken ct) =>
        ChildStreakAsync(childId, today, MaxStreakLookbackDays, ct);

    /// <summary>
    /// Processed history for the supervisor evaluation view: the key figures (<see cref="ProgressView.DaysComplete"/>
    /// / <see cref="ProgressView.TotalPoints"/>) always relate to the plan's <b>entire</b> runtime, while
    /// <see cref="ProgressView.CurrentStreak"/> is the child's streak over all plans (B-606); the filter (<paramref name="from"/>/<paramref name="to"/>/<paramref name="dutyDone"/>)
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

        // The streak belongs to the child, not to this plan (B-606, decision 7) - the same value as overview and today.
        var streak = await ChildStreakAsync(plan.ChildId, today, int.MaxValue, ct);
        return new ProgressView(days.Count(d => d.DutyDone), totalDays, days.Sum(d => d.PointsAwarded),
            streak, filtered.ToList());
    }

}
