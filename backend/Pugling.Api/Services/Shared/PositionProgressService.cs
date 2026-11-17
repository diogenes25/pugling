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
    TimeProvider time)
{
    /// <summary>Default pass threshold of a plan-position test when the position doesn't set its own.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>
    /// How far back the lazy settlement (<see cref="SettleClosedPeriodsAsync"/>) recalculates at most. In
    /// normal operation the only open period is "yesterday"; the cap only limits catch-up recalculation
    /// after a longer absence, without ever scanning the entire history.
    /// </summary>
    private const int MaxSettleLookbackDays = 14;

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

    // ---- Done rule per check mode ---- ----

    /// <summary>Check mode of this position's exercise (default <see cref="ExerciseCheckMode.None"/>).</summary>
    private ExerciseCheckMode CheckModeOf(PlanPosition pos) =>
        pos.Exercise is { } ex ? registry.ByKey(ex.Type)?.Manifest.CheckMode ?? ExerciseCheckMode.None : ExerciseCheckMode.None;

    /// <summary>
    /// Is the stage that applies on <paramref name="day"/> a free display stage (B-96)? Such a day has no
    /// question to grade, so the exam rejects it (<c>ApiErrors.StageNotTestable</c>) – which is why every rule
    /// that would otherwise demand a test attempt has to know about it.
    /// </summary>
    /// <remarks>Judged at the stage the test plays (B-598): a learn-card day is tested at its target, never shown only.</remarks>
    public bool IsDisplayOnlyDay(PlanPosition pos, StudyPlan plan, DateOnly day) =>
        pos.Exercise is { } ex && registry.ByKey(ex.Type) is { } type
            && type.IsDisplayOnlyStage(PositionPlayService.TestTargetStage(type, pos,
                PositionPlayService.StageForDay(pos, plan, day, type)));

    /// <summary>
    /// Is the position testable on <paramref name="day"/>? False for content types without a check mode
    /// (nothing to grade) and for a free display stage (<see cref="IsDisplayOnlyDay"/>, B-96/B-114) - the
    /// exam rejects that stage with <c>stage_not_testable</c>, so every client affordance that offers a
    /// test/exam action has to gate on this rather than on the exercise type alone.
    /// </summary>
    public bool IsTestable(PlanPosition pos, StudyPlan plan, DateOnly day) =>
        CheckModeOf(pos) != ExerciseCheckMode.None && !IsDisplayOnlyDay(pos, plan, day);

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
        var (from, to) = PeriodRange(pos.Cadence, day);
        if (CheckModeOf(pos) == ExerciseCheckMode.None || IsDisplayOnlyDay(pos, plan, day))
            return await PlayedRoundMeetsGoalAsync(pos, from, to, ct);

        return await db.TestAttempts.AnyAsync(t =>
            t.PlanPositionId == pos.Id && t.Day >= from && t.Day <= to
            && t.CompletedAt != null && t.Passed && (!pos.RequireTypedTest || t.Graded), ct);
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
    private async Task<bool> PlayedRoundMeetsGoalAsync(PlanPosition pos, DateOnly from, DateOnly to,
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
        var items = await play.ItemsOfAsync(pos, ct: ct);
        if (play.PoolSize(pos, items.Count) > 0) return true;
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
        if (CheckModeOf(pos) != ExerciseCheckMode.None && !IsDisplayOnlyDay(pos, plan, day)) return 0;
        var (from, to) = PeriodRange(pos.Cadence, day);
        var rounds = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.PlanPositionId == pos.Id && s.Day >= from && s.Day <= to && s.Mode == PlayMode.Lern)
            .Select(s => new { s.Cursor, s.Order })
            .ToListAsync(ct);
        var percent = Math.Clamp(pos.GoalThreshold ?? DefaultPassPercent, 1, 100);
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
        // One query for the whole plan: this method runs once per day of a history, a lookup per position would
        // multiply with both. The indices come along because the pool is only known per position below - a shrunk
        // pool must not count words beyond it (B-478).
        var introducedByPosition = (await db.PositionItemProgress.AsNoTracking()
                .Where(p => p.PlanPosition!.StudyPlanId == plan.Id && p.IntroducedAt != null)
                .Select(p => new { p.PlanPositionId, p.ItemIndex, Imprinted = p.ImprintedAt != null })
                .ToListAsync(ct))
            .ToLookup(p => p.PlanPositionId, p => (p.ItemIndex, p.Imprinted));

        foreach (var pos in positions)
        {
            var type = pos.Exercise is { } ex ? registry.ByKey(ex.Type) : null;
            var manifest = type?.Manifest;
            var checkMode = CheckModeOf(pos);
            var items = await play.ItemsOfAsync(pos, ct: ct);
            var poolSize = play.PoolSize(pos, items.Count);
            var dueCount = pos.UseLeitner ? (await play.DueItemIndicesAsync(pos, day, ct: ct)).Count : 0;
            var goalMet = pos.Cadence == GoalCadence.None || await IsGoalMetAsync(pos, plan, day, ct);
            var goalProgress = goalMet ? 1 : await OpenGoalProgressAsync(pos, plan, day, ct);
            // Testable is about TODAY, not just about the type: on a free display stage (B-96) the exam
            // answers 400, so a client that offered its test button would send the child into a dead end.
            var testable = IsTestable(pos, plan, day);

            statuses.Add(new PositionStatus(
                pos.Id, pos.ExerciseId, pos.Exercise?.Title ?? "", pos.Exercise?.Type.ToString() ?? "",
                manifest?.Renderer ?? "", pos.Order, pos.Cadence, checkMode, pos.UseLeitner,
                testable, goalMet, goalProgress, dueCount, poolSize, pos.PointsGoalMet,
                introducedByPosition[pos.Id].Count(p => p.ItemIndex < poolSize),
                // A free position is never charged, so it must not advertise a price either.
                pos.Cadence == GoalCadence.None ? 0 : pos.PenaltyCoins,
                pos.Exercise?.SeriesUnitId ?? 0, pos.Exercise?.SeriesUnit?.Label ?? "",
                pos.Exercise?.SeriesUnit?.Series?.Name ?? "", pos.Exercise?.BookPages,
                plan.Id, plan.Title, SubjectNameOf(pos),
                // B-643: the golden ring - imprinted words, or introduced ones where a content needs no learn card.
                introducedByPosition[pos.Id].Count(p => p.ItemIndex < poolSize && (p.Imprinted
                    || type is null || !PositionPlayService.NeedsImprint(type, items[p.ItemIndex])))));
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
    /// The goals the day's rewards (daily box, bonus training) hang on (B-535): the daily goals, or every goal when the
    /// plan has none. Weekly goals keep their own penalty at the end of the week; letting them gate the box would lock
    /// it on every day of a week whose hard topic is still open. Without daily goals the old rule stays, or a plan of
    /// weekly goals would hand out the box for nothing.
    /// </summary>
    public static IReadOnlyList<PositionStatus> RewardObligations(IReadOnlyList<PositionStatus> statuses)
    {
        var obligations = statuses.Where(s => s.Cadence != GoalCadence.None).ToList();
        var daily = obligations.Where(s => s.Cadence == GoalCadence.Daily).ToList();
        return daily.Count > 0 ? daily : obligations;
    }

    /// <summary>The child-facing line for an open goal, shared by the day's list and the bonus lock.</summary>
    public static string OutstandingLabel(PositionStatus s) =>
        $"{s.ExerciseTitle} ({(s.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")}) offen";

    // The subject of the position's content: the catalog subject of its textbook series, else the series' free text.
    private static string SubjectNameOf(PlanPosition pos) =>
        pos.Exercise?.SeriesUnit?.Series is { } series ? series.Subject?.Name ?? series.SubjectName ?? "" : "";

    // ---- The child's day across all plans (B-606) ---- ----

    /// <summary>
    /// The child's day across every plan that is active and running on <paramref name="day"/> - the one source for the
    /// day's duty, the reward lock (daily chest, bonus training) and the child-facing day view (B-606, decision 6).
    /// <para>
    /// Duty: every goal of every such plan. Rewards: each plan contributes its own <see cref="RewardObligations"/>, so a
    /// plan of weekly goals keeps gating while another plan has daily ones - exactly the rule the bonus lock already
    /// applied plan by plan. With a single active plan every figure equals that plan's <see cref="ComputeDayAsync"/>.
    /// </para>
    /// </summary>
    public async Task<ChildDayRollup> ComputeChildDayAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Active && p.StartDate <= day && p.EndDate >= day)
            .OrderBy(p => p.StartDate).ThenBy(p => p.Id)
            .ToListAsync(ct);

        var planDays = new List<ChildPlanDay>(plans.Count);
        foreach (var plan in plans)
            planDays.Add(new ChildPlanDay(plan, await ComputeDayAsync(plan, day, ct)));

        var goalsTotal = planDays.Sum(p => p.Overview.GoalsTotal);
        var goalsMet = planDays.Sum(p => p.Overview.GoalsMet);
        var rewardGoals = planDays.SelectMany(p => RewardObligations(p.Overview.Positions)).ToList();
        var rewardOutstanding = rewardGoals.Where(s => !s.GoalMet).Select(OutstandingLabel).ToList();

        return new ChildDayRollup(day, planDays,
            goalsTotal > 0 && goalsMet == goalsTotal, goalsTotal, goalsMet,
            planDays.SelectMany(p => p.Overview.Outstanding).ToList(),
            rewardOutstanding,
            rewardGoals.Count > 0 && rewardOutstanding.Count == 0,
            planDays.SelectMany(p => p.Overview.Positions).ToList());
    }

    /// <summary>
    /// Is a day complete for the child's streak (B-606, decision 6)? At least one plan with a duty that day met it,
    /// and no plan that is active <b>now</b> left a duty of that day open. The model keeps no activation history, so a
    /// plan switched off since only counts in the child's favour - the same fairness the penalty applies to an inactive
    /// plan (<see cref="PlanDueForPeriod"/>): a chapter change must not break the streak backwards.
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
    /// The duty part of <see cref="ComputeDayAsync"/> alone (<see cref="DayOverview.DutyDone"/> and whether there is a
    /// duty at all), stopping at the first open goal. A streak runs this once per plan and day, so it must not pay for
    /// the pool and due counts the day view shows.
    /// </summary>
    private async Task<PlanDutyState> DutyStateAsync(StudyPlan plan, IReadOnlyList<PlanPosition> positions, DateOnly day,
        CancellationToken ct)
    {
        var obligations = positions.Where(p => p.Cadence != GoalCadence.None).ToList();
        if (obligations.Count == 0) return new PlanDutyState(plan.Active, false, false);
        foreach (var pos in obligations)
            if (!await IsGoalMetAsync(pos, plan, day, ct))
                return new PlanDutyState(plan.Active, true, false);
        return new PlanDutyState(plan.Active, true, true);
    }

    /// <summary>
    /// The child's streak (B-606): consecutive complete days (<see cref="IsCompleteDay"/>) up to and including
    /// <paramref name="today"/>, walked backward over all of the child's plans and stopped at the first incomplete day or
    /// after <paramref name="maxDays"/>. Active plans are judged first, so an open active duty stops the day without
    /// reading the inactive ones.
    /// </summary>
    public async Task<int> ChildStreakAsync(int childId, DateOnly today, int maxDays, CancellationToken ct)
    {
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.StartDate <= today)
            .OrderByDescending(p => p.Active).ThenBy(p => p.Id)
            .ToListAsync(ct);
        if (plans.Count == 0) return 0;

        var positions = new Dictionary<int, List<PlanPosition>>();
        var firstDay = plans.Min(p => p.StartDate);
        var streak = 0;
        for (var day = today; streak < maxDays && day >= firstDay; day = day.AddDays(-1))
        {
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
        var now = time.GetUtcNow().UtcDateTime;
        foreach (var pos in positions.Where(p => p.Cadence != GoalCadence.None && p.PointsGoalMet > 0))
        {
            if (!await IsGoalMetAsync(pos, plan, day, ct)) continue;
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
                AwardedAt = now,
            });
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = PointKind.Goal,
                Amount = pos.PointsGoalMet,
                Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {(pos.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")} erreicht",
                CreatedAt = now,
            });
        }

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
            // not stumble over it again; the day status below is read fresh from the database anyway.
            db.ChangeTracker.Clear();
        }
        return await ComputeDayAsync(plan, day, ct);
    }

    // ---- Penalty for not learning (lazy settlement) ---- ----

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
    /// The day the position becomes due for the first time: <c>max(CreatedAt as UTC date, plan.StartDate)</c> (B-432).
    /// CreatedAt is stored in UTC, so its date is the UTC calendar day like every other day in this service.
    /// </summary>
    private static DateOnly FirstDueDay(PlanPosition pos, StudyPlan plan)
    {
        var created = DateOnly.FromDateTime(pos.CreatedAt);
        return created > plan.StartDate ? created : plan.StartDate;
    }

    /// <summary>
    /// Is the missed <paramref name="day"/> the position's introduction day, and did the child introduce that day's
    /// portion (B-432)? On that day every item is new: the child meets them in round 1 and would have to pass a test about
    /// words seen minutes ago. Doing what the day sensibly asks - introducing - must not cost coins.
    /// <para>
    /// The rule only spares the penalty; it does NOT count as goal met (no reward, no daily box, the overview keeps the
    /// goal open), which is why it lives here and not in <see cref="IsGoalMetAsync"/>: introducing deliberately earns
    /// nothing. The introduction day is the first day the position owes anything: its first due day,
    /// <c>max(CreatedAt as UTC date, plan.StartDate)</c>, or the child's first play day in the plan when that is later
    /// (B-535 - before it nothing is settled, so a fixed earlier day would never be spared). Both are fixed once reached,
    /// so skipping cannot push the exception to tomorrow. Daily cadence only - a weekly goal leaves
    /// days to practise. The portion is the whole pool, or <c>min(dose, pool)</c> under a daily dose (whose positions
    /// introduce every day, hence the fixed day). A position without an introducible pool keeps the old rule. Already
    /// booked penalties stay: this check only runs for periods not yet settled.
    /// </para>
    /// </summary>
    private async Task<bool> IsIntroductionDaySparedAsync(PlanPosition pos, StudyPlan plan, DateOnly firstPlay,
        DateOnly day, CancellationToken ct)
    {
        if (pos.Cadence != GoalCadence.Daily) return false;
        var firstDue = FirstDueDay(pos, plan);
        var introductionDay = firstPlay > firstDue ? firstPlay : firstDue;
        if (day != introductionDay) return false;

        var pool = play.PoolSize(pos, (await play.ItemsOfAsync(pos, ct: ct)).Count);
        if (pool == 0) return false;
        var portion = play.DailyDose(pos) is > 0 and var dose ? Math.Min(dose, pool) : pool;
        var introduced = await db.PositionItemProgress.CountAsync(
            p => p.PlanPositionId == pos.Id && p.IntroducedAt == introductionDay, ct);
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

        // Only punishable positions: a real obligation (daily/weekly) with a penalty set, from the child's plans.
        var positions = await db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan)
            .Where(p => p.StudyPlan!.ChildId == childId && p.Cadence != GoalCadence.None && p.PenaltyCoins > 0)
            .ToListAsync(ct);
        if (positions.Count == 0) return 0;

        var lookbackFloor = today.AddDays(-MaxSettleLookbackDays);
        var now = time.GetUtcNow().UtcDateTime;
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
            foreach (var (from, to) in ClosedPeriods(cadence, windowStart, today))
            {
                if (!PlanDueForPeriod(plan, from, to)) continue;
                // Goal rewarded (reached) in the period, or already punished? → nothing to catch up on.
                if (await db.PositionGoalRewards.AnyAsync(r => r.PlanPositionId == pos.Id
                        && r.Cadence == cadence && r.PeriodStart == from, ct)) continue;
                if (await db.PositionGoalPenalties.AnyAsync(r => r.PlanPositionId == pos.Id
                        && r.Cadence == cadence && r.PeriodStart == from, ct)) continue;
                // Safeguard against a race with the reward path (PointsGoalMet == 0 books no reward, yet the
                // goal can still be met): punish only when the period was actually missed.
                if (await IsGoalMetAsync(pos, plan, to, ct)) continue;
                // Spared, not met: no penalty, but no reward either - the reward path never sees this rule (B-432).
                if (await IsIntroductionDaySparedAsync(pos, plan, firstPlay, from, ct)) continue;

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
                    Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {(pos.Cadence == GoalCadence.Weekly ? "Wochenziel" : "Tagesziel")} gerissen",
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
        for (var d = plan.StartDate; d <= plan.EndDate && d <= until; d = d.AddDays(1))
        {
            var o = await ComputeDayAsync(plan, d, ct);
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
