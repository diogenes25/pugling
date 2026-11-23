using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Position-based learning engine (new model): selects the content due today for a
/// <see cref="PlanPosition"/>, schedules its Leitner box progress (<see cref="PositionItemProgress"/>)
/// and resolves the test stage. The content comes from the exercise config via the
/// <see cref="ExerciseContentProvider"/>, grading stays with the <see cref="AnswerGrader"/>.
/// The counterpart to the former plan-wide schedule/progress services, but per exercise
/// instead of per plan – goals and points now hang off the position.
/// </summary>
public class PositionPlayService(PuglingDbContext db, ExerciseContentResolver content, ExerciseTypeRegistry registry)
{
    /// <summary>
    /// The <see cref="IExerciseType"/> behind an exercise (for stage/facet rules), or <c>null</c> if its
    /// type key is unknown. Null-safe like the content resolution (<see cref="ExerciseContentResolver"/>),
    /// so a data integrity error reaches the caller as a clean <see cref="ApiErrors.UnknownExerciseType"/>
    /// instead of an unhandled exception.
    /// </summary>
    public IExerciseType? TypeOf(Exercise exercise) => registry.ByKey(exercise.Type);

    /// <summary>
    /// The build level the position plays on <paramref name="today"/> (B-849): <see cref="PlanPosition.CurrentBuildLevel"/>,
    /// or before <see cref="PlanPosition.BuildLevelEffectiveOn"/> the level below it on the path - a passed test raises the
    /// level at once, the child plays it from the next learning day. <c>null</c> without a build path. Needs what
    /// <see cref="PlayedExercise"/> needs.
    /// </summary>
    public static int? PlayedBuildLevel(PlanPosition pos, DateOnly today)
    {
        if (pos.CurrentBuildLevel is not { } level) return null;
        if (pos.BuildLevelEffectiveOn is not { } from || today >= from) return level;
        return PathLevels(pos).LastOrDefault(l => l < level) is var below and > 0 ? below : level;
    }

    /// <summary>
    /// The levels of the position's build path in order (B-848, decision 5): the target's prerequisite levels plus the
    /// target. Throws like <see cref="PlayedExercise"/> when the prerequisites are not loaded.
    /// </summary>
    public static IReadOnlyList<int> PathLevels(PlanPosition pos)
    {
        var prerequisites = pos.Exercise?.Prerequisites ?? throw new InvalidOperationException(
            $"Position {pos.Id} has a build path; load Exercise.Prerequisites to resolve its levels.");
        return [.. prerequisites.Where(e => e.BuildLevel is < Exercise.BuildLevelTarget).Select(e => e.BuildLevel!.Value)
            .Distinct().Order(), Exercise.BuildLevelTarget];
    }

    /// <summary>
    /// The exercise the position plays on <paramref name="today"/> (B-848, B-849): its own exercise, or on a build path
    /// the prerequisite of its <see cref="PlayedBuildLevel"/>. The one place a play path asks "which exercise" - type,
    /// config, default stage, item count and learning state all follow it. A level whose prerequisite is gone plays the
    /// target. A build path read without <c>Exercise</c> and <c>Exercise.Prerequisites</c> loaded throws: a forgotten
    /// include would otherwise play the target in silence.
    /// </summary>
    public static Exercise? PlayedExercise(PlanPosition pos, DateOnly today)
    {
        if (PlayedBuildLevel(pos, today) is not { } level || level >= Exercise.BuildLevelTarget) return pos.Exercise;
        var prerequisites = pos.Exercise?.Prerequisites ?? throw new InvalidOperationException(
            $"Position {pos.Id} plays build level {level}; load Exercise.Prerequisites to resolve the played exercise.");
        // Lowest id first: nothing forbids two prerequisites on one level, and the choice must not hang on load order.
        return prerequisites.Where(e => e.BuildLevel == level).MinBy(e => e.Id) ?? pos.Exercise;
    }

    /// <summary>
    /// The level a test started on <paramref name="today"/> examines (B-849, decision 3), as stamped on
    /// <see cref="TestAttempt.BuildLevel"/>: the place of the played exercise on the path - the target counts as
    /// <see cref="Exercise.BuildLevelTarget"/>, also where a gone prerequisite made it the one played. <c>null</c> without a
    /// build path.
    /// </summary>
    public static int? ExaminedBuildLevel(PlanPosition pos, DateOnly today) =>
        pos.CurrentBuildLevel is null ? null
        : PlayedExercise(pos, today) is { } played && played.Id != pos.ExerciseId && played.BuildLevel is { } level ? level
        : Exercise.BuildLevelTarget;

    /// <summary>
    /// Has the father's level change overtaken a round or a test (B-880, B-881)? It was stamped with the level it played
    /// (<paramref name="stamped"/>); its frozen order holds item indices of that level's exercise. If its day no longer
    /// resolves to that level, those indices name another exercise's cards and must not be played or graded. The one
    /// answer for rounds and tests alike.
    /// </summary>
    public static bool IsOvertaken(PlanPosition pos, int? stamped, DateOnly day) =>
        stamped != ExaminedBuildLevel(pos, day);

    /// <summary>
    /// The attempts that examined <paramref name="level"/> (B-849): a prerequisite's by its level; the target's on the
    /// target level or without one - an attempt from before the path was set examined the same exercise. For views that
    /// group test results by item index, which names another card on another level.
    /// </summary>
    public static Expression<Func<TestAttempt, bool>> AttemptsOnLevel(int? level) =>
        level is { } l && l < Exercise.BuildLevelTarget
            ? t => t.BuildLevel == l
            : t => t.BuildLevel == null || t.BuildLevel >= Exercise.BuildLevelTarget;

    /// <summary>
    /// The level a passed test of level <paramref name="examined"/> raises the position to (B-849, decisions 1 and 2): the
    /// next level on its path, or <c>null</c> where nothing rises - no build path, the target examined, or the position
    /// already at or above that level. The last case makes the promotion idempotent: a second pass of the same level, or
    /// two completions at once, raise it once.
    /// </summary>
    public static int? PromotedBuildLevel(PlanPosition pos, int? examined)
    {
        if (examined is not { } from || pos.CurrentBuildLevel is not { } current || from >= Exercise.BuildLevelTarget)
            return null;
        var next = PathLevels(pos).First(l => l > from);
        return next > current ? next : null;
    }

    /// <summary>
    /// A completed test scoring below this percentage counts towards a descent (B-702, decision 1): below half is "too
    /// hard", a test missed narrowly (70 % against a bar of 80 %) is not.
    /// </summary>
    public const int DemotionBelowPercent = 50;

    /// <summary>Failed tests in a row (each below <see cref="DemotionBelowPercent"/>) that lower the level by one.</summary>
    public const int DemotionAfterFailures = 2;

    /// <summary>
    /// May this completed attempt move the position along its build path (B-849, B-702)? The same admission as the goal:
    /// the child's own attempt, and not a self-assessed one where the father demands typed tests. A promotion and a
    /// descent read this one predicate, so neither can be reached by a test the goal would not count.
    /// </summary>
    public static bool MovesBuildPath(PlanPosition pos, TestAttempt attempt) =>
        !attempt.BySupervisor && (!pos.RequireTypedTest || attempt.Graded);

    /// <summary>
    /// The level a descent from level <paramref name="examined"/> lowers the position to (B-702, decision 1): the next lower
    /// level on its path, or <c>null</c> where nothing sinks - no build path, or the lowest level reached. Whether the test
    /// examined the level the position plays is <see cref="MoveBuildPath"/>'s question, asked before.
    /// </summary>
    public static int? DemotedBuildLevel(PlanPosition pos, int? examined)
    {
        if (examined is not { } from) return null;
        var below = PathLevels(pos).LastOrDefault(l => l < from);
        return below > 0 ? below : null;
    }

    /// <summary>
    /// The one way a position changes its build level (B-702): the level, the day it is played from, and a fresh count of
    /// failed tests - a descent must be earned on the new level, not carried over from the old one.
    /// <paramref name="effectiveOn"/> <c>null</c> plays the level at once (the father's choice, a descent); a promotion
    /// passes the next learning day.
    /// </summary>
    public static void SetBuildLevel(PlanPosition pos, int? level, DateOnly today, DateOnly? effectiveOn = null)
    {
        pos.CurrentBuildLevel = level;
        pos.BuildLevelEffectiveOn = effectiveOn;
        pos.CurrentBuildLevelSince = level is null ? null : effectiveOn ?? today;
        pos.FailedBuildTestsInRow = level is null ? null : 0;
    }

    /// <summary>
    /// What a completed test does to the position's build path (B-849, B-702), in the completion's own save. Promotion:
    /// a passed test of the level raises it from the next learning day. Descent: the
    /// <see cref="DemotionAfterFailures"/>-th test in a row below <see cref="DemotionBelowPercent"/> lowers it at once
    /// (the user's decision); a test from 50 % on - or any passed one, even below 50 % under a low bar - resets the count.
    /// Only tests of the level the position plays now count: one of the level below on the day of a waiting promotion
    /// examined an older level. Neither direction books points or a penalty.
    /// </summary>
    public static void MoveBuildPath(PlanPosition pos, TestAttempt attempt, DateOnly today)
    {
        if (!MovesBuildPath(pos, attempt)) return;
        if (attempt.Passed && PromotedBuildLevel(pos, attempt.BuildLevel) is { } raised)
        {
            SetBuildLevel(pos, raised, today, effectiveOn: today.AddDays(1));
            return;
        }
        if (attempt.BuildLevel is not { } examined || examined != pos.CurrentBuildLevel) return;
        if (attempt.Passed || attempt.ScorePercent >= DemotionBelowPercent)
        {
            pos.FailedBuildTestsInRow = 0;
            return;
        }
        var failures = (pos.FailedBuildTestsInRow ?? 0) + 1;
        if (failures >= DemotionAfterFailures && DemotedBuildLevel(pos, examined) is { } lowered)
            SetBuildLevel(pos, lowered, today);
        else
            pos.FailedBuildTestsInRow = failures;
    }

    /// <summary>
    /// The position's own stage (B-848): its override, but not while it plays a prerequisite. The father set and the server
    /// checked it against the target's type; a prerequisite may be of another type, where the same number names another
    /// form or none, so it plays its own default stage.
    /// </summary>
    private static int? OwnStage(PlanPosition pos, DateOnly today) => PlaysPrerequisite(pos, today) ? null : pos.Stage;

    /// <summary>The position's stage schedule (B-848), by the same rule as <see cref="OwnStage"/>.</summary>
    private static List<StageStep>? OwnSchedule(PlanPosition pos, DateOnly today) =>
        PlaysPrerequisite(pos, today) ? null : pos.StageSchedule;

    private static bool PlaysPrerequisite(PlanPosition pos, DateOnly today) =>
        pos.CurrentBuildLevel is not null && !ReferenceEquals(PlayedExercise(pos, today), pos.Exercise);

    /// <summary>
    /// The id the position's learning state is kept under (B-848, <see cref="PositionItemProgress.ExerciseId"/>): the
    /// played exercise's. Without a build path it needs nothing loaded.
    /// </summary>
    public static int PlayedExerciseId(PlanPosition pos, DateOnly today) =>
        pos.CurrentBuildLevel is null ? pos.ExerciseId : PlayedExercise(pos, today)?.Id ?? pos.ExerciseId;

    /// <summary>
    /// Level n of m on the position's build path (B-848, decision 5): the target's prerequisite levels plus the target,
    /// in order, and the place of the exercise played on <paramref name="today"/>. <c>null</c> without a build path. Needs
    /// what <see cref="PlayedExercise"/> needs.
    /// </summary>
    public static BuildPath? BuildPathOf(PlanPosition pos, DateOnly today)
    {
        if (pos.CurrentBuildLevel is null || pos.Exercise is not { } target || PlayedExercise(pos, today) is not { } played)
            return null;
        List<int> levels = [.. (target.Prerequisites ?? []).Where(e => e.BuildLevel is not null)
            .Select(e => e.BuildLevel!.Value).Distinct().Order(), Exercise.BuildLevelTarget];
        var current = played.Id == target.Id ? Exercise.BuildLevelTarget : played.BuildLevel ?? Exercise.BuildLevelTarget;
        // The date belongs to the current level; while a promotion waits, the child still plays the one below.
        var since = PlayedBuildLevel(pos, today) == pos.CurrentBuildLevel ? pos.CurrentBuildLevelSince : null;
        return new BuildPath(levels.IndexOf(current) + 1, levels.Count, played.Id, played.Title, target.Id, target.Title,
            since);
    }

    /// <summary>
    /// The daily dose in force: the position's <see cref="PlanPosition.NewItemsPerDay"/>, but 0 while its exercise grades
    /// as a set (an unordered list). Such an exercise accepts any entry not yet named, and a dose would hide most of them -
    /// a right answer outside the day's portion would count as wrong. Assigning rejects the combination; this covers the
    /// list a creator switches to any order after it was assigned (B-478).
    /// </summary>
    public int DailyDose(PlanPosition pos, DateOnly day) => DailyDose(pos, registry, day);

    /// <summary>
    /// <see cref="DailyDose(PlanPosition, DateOnly)"/> for callers without the service (the position response, B-480) - the one place
    /// the rule lives, so what the father reads and what the play applies cannot drift apart. Needs the exercise loaded.
    /// </summary>
    public static int DailyDose(PlanPosition pos, ExerciseTypeRegistry types, DateOnly today) =>
        pos.NewItemsPerDay > 0 && PlayedExercise(pos, today) is { } exercise && types.ByKey(exercise.Type)?.GradesAsSet(ConfigOf(exercise)) == true
            ? 0
            : pos.NewItemsPerDay;

    /// <summary>Default Leitner intervals in days (index = box; index 0 unused).</summary>
    private static readonly int[] DefaultBoxIntervalDays = [0, 1, 2, 4, 7, 14];

    /// <summary>Leitner intervals of the position (own, otherwise default).</summary>
    public IReadOnlyList<int> BoxIntervals(PlanPosition pos) =>
        pos.BoxIntervalDays is { Count: > 1 } custom ? custom : DefaultBoxIntervalDays;

    /// <summary>
    /// The content items of this position's exercise (store-resolved for referenced vocabulary items).
    /// <paramref name="childId"/> unlocks image selection – only the practice card passes it, because only
    /// it shows an image. Everything else skips the selection, and not merely to save a query: selecting
    /// <b>freezes</b> the child's motif for good (see <c>MediaSelector</c>), so a path that renders no image
    /// would silently decide what the child sees later.
    /// </summary>
    public async Task<IReadOnlyList<ContentItem>> ItemsOfAsync(PlanPosition pos, DateOnly day, int? childId = null,
        CancellationToken ct = default) =>
        PlayedExercise(pos, day) is { } ex ? await content.ItemsOfAsync(ex, childId, ct) : [];

    /// <summary>
    /// May the child play this plan today (practice/test)? Only an active plan within its
    /// runtime is playable – this way the child can't pick an easy or expired plan for convenient
    /// point farming (anti-cheating). The supervisor is exempt from this (preview/backfill).
    /// </summary>
    public static bool PlanPlayableForChild(StudyPlan plan, DateOnly today) =>
        plan.Active && plan.StartDate <= today && today <= plan.EndDate;

    /// <summary>
    /// Test stage of the position applicable for a given day: study plan (if set) → position override →
    /// exercise default → method default. Enforced server-side (not selectable by the client).
    /// </summary>
    public static int StageForDay(PlanPosition pos, StudyPlan plan, DateOnly day, IExerciseType type)
    {
        var dayNumber = day.DayNumber - plan.StartDate.DayNumber + 1;
        var step = OwnSchedule(pos, day)?
            .Where(s => s.DayNumber <= dayNumber)
            .OrderByDescending(s => s.DayNumber)
            .FirstOrDefault();
        return step?.Stage ?? OwnStage(pos, day) ?? PlayedExercise(pos, day)?.DefaultStage ?? type.DefaultStage;
    }

    /// <summary>
    /// What meets the position's goal (B-550), from the same rules the test and <c>IsGoalMetAsync</c> grade by: a type
    /// without a final test counts playthrough; otherwise every stage the position can be on - the base stage
    /// (position → exercise default → type default, as in <see cref="StageForDay"/>) and each step of the schedule - is
    /// a display stage (playthrough), a typed stage (graded) or a self-assessed one. "Typed tests only" makes a
    /// self-assessed day unreachable, since such an attempt is never graded.
    /// </summary>
    public static LearningCheck LearningCheckOf(PlanPosition pos, IExerciseType type, DateOnly today) =>
        LearningCheckOf(type, pos.RequireTypedTest,
            OwnStage(pos, today) ?? PlayedExercise(pos, today)?.DefaultStage ?? type.DefaultStage, OwnSchedule(pos, today));

    /// <summary>
    /// The same rule over loose values (B-552), so a request can be judged before anything is written: validation and
    /// the father's display share one segment computation instead of two copies that could drift apart.
    /// </summary>
    public static LearningCheck LearningCheckOf(IExerciseType type, bool requireTypedTest, int baseStage,
        IReadOnlyList<StageStep>? schedule)
    {
        if (type.Manifest.CheckMode == ExerciseCheckMode.None) return new LearningCheck(LearningCheckKind.NoTest);

        var steps = (schedule ?? []).OrderBy(s => s.DayNumber).ToList();
        // Segments (first day, stage) in plan order; the base stage holds until the first step takes over.
        var segments = new List<(int Day, int Stage)>();
        if (steps.Count == 0 || steps[0].DayNumber > 1) segments.Add((1, baseStage));
        segments.AddRange(steps.Select(s => (Math.Max(1, s.DayNumber), s.Stage)));

        // A learn-card day (B-598) is checked at the station's target, as its test plays it.
        var kinds = segments.Select(s => TestTargetStage(type, baseStage, s.Stage))
            .Select(stage => type.IsDisplayOnlyStage(stage) ? LearningCheckKind.Playthrough
                : type.IsTypedStage(stage) ? LearningCheckKind.Typed : LearningCheckKind.SelfAssessed).ToList();
        if (requireTypedTest && kinds.Contains(LearningCheckKind.SelfAssessed))
            return new LearningCheck(LearningCheckKind.Unreachable);
        if (kinds.Distinct().Count() == 1) return new LearningCheck(kinds[0]);

        // The day from which every later segment is typed - the point the father reads as "from then on it is checked".
        int? typedFrom = null;
        for (var i = segments.Count - 1; i >= 0 && kinds[i] == LearningCheckKind.Typed; i--) typedFrom = segments[i].Day;
        return new LearningCheck(LearningCheckKind.Ladder, typedFrom);
    }

    /// <summary>
    /// The stage a never-seen card is shown at first (B-419): the one the type names (B-598: vocabulary's learn card,
    /// otherwise its free display stage), or <c>null</c> for a type that has none - there the card is played at the
    /// day's stage as before, because there is no way to show it without grading it.
    /// </summary>
    public static int? IntroductionStage(IExerciseType type) => type.IntroductionStage;

    /// <summary>
    /// The stage a test plays for a station whose stage is <paramref name="stage"/> (B-598, decision 9): a learn card
    /// shows the solution and is never tested, so its day is tested at the station's target - the position's own stage,
    /// or, where that is a learn card or a display stage itself, the type's most telling one. Every other stage is tested
    /// as it is. Testability, the father's check and the test start all read this one rule.
    /// </summary>
    public static int TestTargetStage(IExerciseType type, int baseStage, int stage) =>
        !type.IsLearnCardStage(stage) ? stage
        : type.IsLearnCardStage(baseStage) || type.IsDisplayOnlyStage(baseStage) ? type.PreviewStage
        : baseStage;

    /// <summary><see cref="TestTargetStage(IExerciseType, int, int)"/> for a position: its base stage as in <see cref="StageForDay"/>.</summary>
    public static int TestTargetStage(IExerciseType type, PlanPosition pos, int stage, DateOnly today) =>
        TestTargetStage(type, OwnStage(pos, today) ?? PlayedExercise(pos, today)?.DefaultStage ?? type.DefaultStage, stage);

    // The type's free display stage, if it has one.
    private static int? DisplayStage(IExerciseType type) =>
        type.StageOptions.Where(o => type.IsDisplayOnlyStage(o.Value)).Select(o => (int?)o.Value).FirstOrDefault();

    /// <summary>
    /// Is this card an introduction (B-419)? The child must see an atom once with its solution before it is asked
    /// for it, so first contact is a property of the card, not of the day: an atom without an introduction date is
    /// shown at <see cref="IntroductionStage"/> in a learn round, whatever the schedule says. The info mode is left
    /// out because it records nothing - every card would stay "new" there forever. A day that is already a display
    /// stage needs no second rule.
    /// </summary>
    public static bool IsIntroduction(IExerciseType type, int dayStage, PlayMode mode, bool introduced) =>
        mode != PlayMode.Info && !introduced && !type.IsDisplayOnlyStage(dayStage)
            && IntroductionStage(type) is not null;

    /// <summary>
    /// Box and introduction date of every introduced item of the position, by item index - the one lookup the card
    /// projection needs: presence tells an introduction from a regular card (<see cref="IsIntroduction"/>), box and date
    /// pick the card's rung (<see cref="CardStage"/>).
    /// </summary>
    public async Task<Dictionary<int, CardProgress>> IntroducedCardsAsync(PlanPosition pos, DateOnly day, CancellationToken ct)
    {
        var exerciseId = PlayedExerciseId(pos, day);
        return await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == pos.Id && p.ExerciseId == exerciseId && p.IntroducedAt != null)
            .ToDictionaryAsync(p => p.ItemIndex, p => new CardProgress(p.Box, p.IntroducedAt!.Value, p.ImprintedAt,
                p.RemediationDoneAt != null), ct);
    }

    /// <summary>
    /// B-643: does this content have to be written from memory once before it counts as known? Exactly when its
    /// introduction is a learn card - a recognition-only card (shown on both sides, B-597) and every type without a learn
    /// card count as known once introduced, as before.
    /// </summary>
    public static bool NeedsImprint(IExerciseType type, ContentItem item) =>
        type.IntroductionStage is { } intro && type.IsLearnCardStage(type.CardCeiling(item, intro));

    /// <summary>
    /// B-643: is the card known for the card selection - introduced and, where it needs one, imprinted? An introduced but
    /// not imprinted word is met on the learn card again (decision 2). The one rule every caller of
    /// <see cref="IsIntroduction"/> passes on; due dates and points keep reading the introduction.
    /// </summary>
    public static bool Known(IExerciseType type, CardProgress? card, ContentItem item) =>
        card is { } c && (c.ImprintedAt is not null || !NeedsImprint(type, item));

    /// <summary>
    /// The stage one card is played at. A never-seen card is shown (B-419); an introduced one climbs the type's ladder
    /// with its box up to the day's stage (B-476). The ladder needs a box that moves, so it holds only in a learn round
    /// on a Leitner position - without Leitner the card would stay on the lowest rung for good, and the info mode
    /// records nothing. A card in the position's top box is asked at the target whatever the ladder says: with a
    /// <see cref="PlanPosition.MaxBox"/> below the ladder's top rung it could never climb further, and would be
    /// stuck on a rung below the target for good. The ladder also ends by the calendar (<see cref="LadderDays"/>), in
    /// practice as in the test: a word the test asks in writing must have been asked in writing here as well.
    /// </summary>
    /// <remarks>The card's own ceiling (B-597, <see cref="IExerciseType.CardCeiling"/>) caps whatever the rules above chose.</remarks>
    public static int CardStage(IExerciseType type, PlanPosition pos, int dayStage, PlayMode mode, CardProgress? card,
        DateOnly day, ContentItem item) =>
        type.CardCeiling(item, UncappedCardStage(type, pos, dayStage, mode, card, day, Known(type, card, item)));

    private static int UncappedCardStage(IExerciseType type, PlanPosition pos, int dayStage, PlayMode mode,
        CardProgress? card, DateOnly day, bool known)
    {
        if (IsIntroduction(type, dayStage, mode, known)) return IntroductionStage(type)!.Value;
        // B-598, decision 9: free practice records nothing, so a learn-card day shows the word on its display stage there.
        if (mode == PlayMode.Info && type.IsLearnCardStage(dayStage)) return DisplayStage(type) ?? dayStage;
        if (mode != PlayMode.Lern || !pos.UseLeitner || card is not { } c || c.Box >= pos.MaxBox) return dayStage;
        if (c.IntroducedAt.AddDays(LadderDays) <= day) return dayStage;
        return type.CardStage(dayStage, c.Box);
    }

    /// <summary>
    /// The atoms as they are asked at <paramref name="stage"/> (B-597): every card through
    /// <see cref="IExerciseType.CardForStage"/>, the same index at the same place. The one list a card's display and its
    /// grading read - the asked card and its siblings alike, so a neighbour's word is compared in the same language.
    /// </summary>
    public static IReadOnlyList<ContentItem> AtStage(IExerciseType type, IReadOnlyList<ContentItem> items, int stage) =>
        [.. items.Select(i => type.CardForStage(i, stage))];

    /// <summary>
    /// Days after its introduction from which a word is asked at the target in practice and test alike, whatever its
    /// box (B-476, decision 7). The father's choice, and the honest path's length: with the default intervals a word
    /// practised every day is first asked in writing on day 7 (shown and turned over on day 1, chosen on day 3, typed
    /// on day 7), so the deadline only meets a word the child keeps low - by not practising or by failing on purpose.
    /// It counts from the actual introduction, not from a day the word was due: the daily dose never catches up a day
    /// not played, so a due date would let a week of missed days end the ladder of every later word before the child
    /// ever met it. Each word so gets at most one week of ladder; missed days are the malus' to punish.
    /// </summary>
    public const int LadderDays = 7;

    /// <summary>
    /// The stage one item is examined at in the test (B-476): the ladder of practice, but never below a checked rung -
    /// a self-assessment is a claim, and the duty is "passed under check". A rung the child would only judge itself is
    /// raised box by box to the first checked one (for vocabulary: the choice). An item not introduced yet counts as
    /// box 1 introduced today - the child's test introduces it (<see cref="IntroduceAsync"/>). When the target itself is
    /// unchecked there is nothing to raise to, and without Leitner no box moves - both keep the target for every item.
    /// <para>
    /// The box is the child's to move - not practising keeps a word in box 1, and so does answering wrong on purpose.
    /// So the ladder ends by the calendar too: <see cref="LadderDays"/> days after <paramref name="introducedOn"/> a
    /// word is examined at the target whatever its box, and no behaviour of the child stretches the easier form
    /// beyond that.
    /// </para>
    /// </summary>
    /// <remarks>The card's own ceiling (B-597) caps the result, like in practice.</remarks>
    public static int TestItemStage(IExerciseType type, PlanPosition pos, int target, int? box, DateOnly introducedOn,
        DateOnly day, ContentItem item) =>
        type.CardCeiling(item, UncappedTestItemStage(type, pos, target, box, introducedOn, day));

    private static int UncappedTestItemStage(IExerciseType type, PlanPosition pos, int target, int? box,
        DateOnly introducedOn, DateOnly day)
    {
        if (!pos.UseLeitner || !type.IsTypedStage(target)) return target;
        if (introducedOn.AddDays(LadderDays) <= day) return target;
        for (var b = Math.Max(1, box ?? 1); b < pos.MaxBox; b++)
        {
            var stage = type.CardStage(target, b);
            if (type.IsTypedStage(stage)) return stage;
        }
        return target;
    }

    /// <summary>Number of content items used by the position (override, exercise default, otherwise all available).</summary>
    public int PoolSize(PlanPosition pos, DateOnly day, int available) =>
        (pos.ItemCount ?? PlayedExercise(pos, day)?.DefaultItemCount) is > 0 and var count ? Math.Min(count, available) : available;

    /// <summary>
    /// Selects the item indices due today: limited to the pool (<see cref="PlanPosition.ItemCount"/>),
    /// filtered by <see cref="ItemScope"/> (new/old/all) and – for Leitner – only the due ones
    /// (never seen counts as due). <paramref name="strategy"/> determines the order (default =
    /// weakest first = previous behavior). The progress is loaded for this, but NOT newly created
    /// (that only happens when grading in <see cref="ApplyReview"/>).
    /// </summary>
    public async Task<IReadOnlyList<int>> DueItemIndicesAsync(PlanPosition pos, DateOnly day,
        PracticeOrder strategy = PracticeOrder.WeakestFirst, bool dueOnly = true, CancellationToken ct = default)
    {
        var items = await ItemsOfAsync(pos, day, ct: ct);
        var poolSize = PoolSize(pos, day, items.Count);
        if (poolSize == 0) return [];

        var exerciseId = PlayedExerciseId(pos, day);
        var progress = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == pos.Id && p.ExerciseId == exerciseId && p.ItemIndex < poolSize)
            .ToDictionaryAsync(p => p.ItemIndex, ct);

        var type = PlayedExercise(pos, day) is { } ex ? TypeOf(ex) : null;
        IEnumerable<(int Index, PositionItemProgress? Prog)> due = Enumerable.Range(0, poolSize)
            .Select(i => (Index: i, Prog: progress.GetValueOrDefault(i)))
            .Where(x => ScopeMatch(pos.Scope, x.Prog, day, type, items) && (!dueOnly || !pos.UseLeitner || IsDue(x.Prog, day)))
            .ToList();

        // The dose only shapes a round that is played for the record; free practice (info) introduces nothing.
        // B-643 (decision 2): a word met on an earlier day but not imprinted comes back as a learn card and takes a place of
        // the day's new words - 6 a day with 2 open from yesterday are 2 open and 4 new. Counted from this round's own
        // contents (a word the scope or its due date keeps out takes no place), and for the whole day: imprinted in the
        // first round, it still took its place, or a second round would bring new words beyond the dose.
        if (dueOnly && DailyDose(pos, day) is > 0 and var dose)
            due = WithinDailyDose(due, dose, progress.Values.Count(p => p.IntroducedAt == day)
                + OpenLearnCards(pos, items, due.Select(x => x.Prog), day));

        return OrderIndices(due, strategy);
    }

    /// <summary>
    /// The round of a remediation position (B-635): every task of the pool not yet done, in the order of the exercise -
    /// "der Reihe nach". No Leitner due date and no dose: a remediation round is the list of what is still open, and the
    /// next round is the same rule read again, so a task made easier comes back there and not in this round.
    /// </summary>
    public async Task<IReadOnlyList<int>> RemediationRoundAsync(PlanPosition pos, DateOnly day, CancellationToken ct)
    {
        var poolSize = PoolSize(pos, day, (await ItemsOfAsync(pos, day, ct: ct)).Count);
        var exerciseId = PlayedExerciseId(pos, day);
        var done = await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == pos.Id && p.ExerciseId == exerciseId && p.RemediationDoneAt != null)
            .Select(p => p.ItemIndex).ToListAsync(ct);
        return [.. Enumerable.Range(0, poolSize).Where(i => !done.Contains(i))];
    }

    /// <summary>
    /// The next easier stage of a card (B-635): the next rung of the type's <see cref="IExerciseType.EaseLadder"/> whose
    /// form the card has not shown yet on this ladder. A rung the card cannot take falls back to a form it already showed -
    /// a scramble of an answer that cannot be scrambled is letter boxes again, a choice without distractors too - so it is
    /// skipped: a press must always change the card, and never back to a harder form (decision 3). <c>null</c> = the card
    /// is at its easiest form already.
    /// </summary>
    public static int? EasierStage(IExerciseType type, string configJson, IReadOnlyList<ContentItem> items,
        ContentItem item, int current)
    {
        var ladder = type.EaseLadder;
        var at = -1;
        for (var i = 0; i < ladder.Count; i++) if (ladder[i] == current) at = i;
        if (at < 0) return null;
        // Every form up to the current rung counts as shown: a rung the card skipped had one of these forms anyway.
        var shown = new HashSet<string>();
        for (var i = 0; i <= at; i++) shown.Add(FormOf(type, configJson, items, item, ladder[i]));
        for (var i = at + 1; i < ladder.Count; i++)
            if (!shown.Contains(FormOf(type, configJson, items, item, ladder[i]))) return ladder[i];
        return null;
    }

    // What the child sees as the answer area at a stage - the one thing a press on "easier" has to change.
    private static string FormOf(IExerciseType type, string configJson, IReadOnlyList<ContentItem> items, ContentItem item,
        int stage)
    {
        var f = CardFacets(configJson, items, item, type, stage, type.IsTypedStage(stage), seed: 0);
        return f.Choices is not null ? "choice" : f.LetterTiles is not null ? "tiles" : f.AnswerLength is not null ? "boxes" : "free";
    }

    /// <summary>
    /// B-643: words introduced before <paramref name="day"/> that come back as learn cards that day - not imprinted yet, or
    /// imprinted only today. They take places of the day's new words in the learn round; the test portion reads no scope
    /// and no dose of open cards (B-04).
    /// </summary>
    public int OpenLearnCards(PlanPosition pos, IReadOnlyList<ContentItem> items, IEnumerable<PositionItemProgress?> progress,
        DateOnly day) =>
        PlayedExercise(pos, day) is { } ex && TypeOf(ex) is { } type
            ? progress.Count(p => IsOpenLearnCard(type, items, p, day))
            : 0;

    // B-643/B-653: introduced before the day, not imprinted or imprinted only that day, and the content needs a learn card.
    // One predicate for the dose count and the "new" scope, so a word the dose counts is always in the round it counts in.
    private static bool IsOpenLearnCard(IExerciseType type, IReadOnlyList<ContentItem> items, PositionItemProgress? p,
        DateOnly day) =>
        p is { IntroducedAt: { } at } && at < day && (p.ImprintedAt is null || p.ImprintedAt == day)
            && p.ItemIndex < items.Count && NeedsImprint(type, items[p.ItemIndex]);

    /// <summary>
    /// Applies the daily dose (B-04): of the never-shown contents only as many pass as the day still has room for,
    /// lowest index first - the order of the book. What was introduced earlier today counts against the dose, so a
    /// second round on the same day brings no further new ones. Known contents pass untouched.
    /// </summary>
    private static List<(int Index, PositionItemProgress? Prog)> WithinDailyDose(
        IEnumerable<(int Index, PositionItemProgress? Prog)> due, int newItemsPerDay, int introducedToday)
    {
        var room = Math.Max(0, newItemsPerDay - introducedToday);
        var result = new List<(int Index, PositionItemProgress? Prog)>();
        foreach (var x in due.OrderBy(x => x.Index))
        {
            if (x.Prog?.IntroducedAt is not null) result.Add(x);
            else if (room-- > 0) result.Add(x);
        }
        return result;
    }

    /// <summary>
    /// The contents a test checks on <paramref name="day"/> when the position carries a daily dose (B-04): what the
    /// child met today - introduced, reviewed or due - plus the new contents the day's dose still has room for. A
    /// test over everything introduced would grow with every day of the plan until the duty cannot be met in one
    /// sitting.
    /// <para>
    /// The open rest of the dose is the anti-cheat half: a child that skips practice is asked the day's new contents
    /// cold, and because the caller introduces them (<see cref="IntroduceAsync"/>), they stay due until they are
    /// practised. Skipping practice makes the test longer every day, never shorter.
    /// </para>
    /// <para>
    /// On a day with nothing met and nothing new left, the weakest known contents are checked, twice the dose at
    /// most - the duty stays meetable after the pool is through.
    /// </para>
    /// </summary>
    public static IReadOnlyList<int> DailyTestPortion(int newItemsPerDay, int poolSize,
        IEnumerable<PositionItemProgress> progress, DateOnly day, LearningDay learningDay)
    {
        var introduced = progress.Where(p => p.IntroducedAt is not null && p.ItemIndex < poolSize).ToList();
        var known = introduced.Select(p => p.ItemIndex).ToHashSet();
        // Not reduced by open learn cards (B-643): a child that skips practice must meet more words every day (B-04), and
        // words met cold here come back as learn cards the next day, where they take their places of the dose.
        var room = Math.Max(0, newItemsPerDay - introduced.Count(p => p.IntroducedAt == day));

        var portion = introduced
            .Where(p => p.IntroducedAt == day || IsDue(p, day)
                || (p.LastReviewedAt is { } at && learningDay.DayOf(at) == day))
            .Select(p => p.ItemIndex)
            .Concat(Enumerable.Range(0, poolSize).Where(i => !known.Contains(i)).Take(room))
            .ToList();
        if (portion.Count > 0) return portion;

        return [.. introduced.OrderBy(p => p.Box).ThenBy(p => p.LastReviewedAt).ThenBy(p => p.ItemIndex)
            .Take(newItemsPerDay * 2).Select(p => p.ItemIndex)];
    }

    /// <summary>
    /// Marks the given contents as introduced on <paramref name="day"/> and due at once - for a child's test that asks
    /// never-shown contents (under a dose the portion at the start, <see cref="DailyTestPortion"/>; otherwise each word
    /// as the child answers it). The caller saves.
    /// </summary>
    public async Task IntroduceAsync(PlanPosition pos, IEnumerable<int> itemIndices, DateOnly day, CancellationToken ct)
    {
        foreach (var index in itemIndices)
        {
            var prog = await ProgressForAsync(pos, day, index, ct);
            if (prog.IntroducedAt is not null) continue;
            prog.IntroducedAt = day;
            prog.DueOn ??= day;
        }
    }

    /// <summary>
    /// Advances a cursor across the frozen order (<paramref name="order"/>) past item indices
    /// that have been removed since the start (out-of-range relative to <paramref name="itemCount"/>). Shared by
    /// the practice and the test cursor, so the skip rule lives in exactly one place.
    /// </summary>
    public static int SkipRemoved(IReadOnlyList<int> order, int cursor, int itemCount)
    {
        while (cursor < order.Count && order[cursor] >= itemCount) cursor++;
        return cursor;
    }

    /// <summary>
    /// The raw exercise config – what a type needs for facets it cannot derive from the atoms (the cloze
    /// word bank). Takes the <see cref="Exercise"/> itself, not the position: a <c>pos.Exercise?.…</c> would
    /// swallow a forgotten <c>Include</c> and cost the word bank in silence, the same failure mode the
    /// imagery rules forbid for <c>childId</c>. This way the compiler asks the question.
    /// </summary>
    public static string ConfigOf(Exercise exercise) => exercise.ConfigJson;

    /// <summary>
    /// The language typed on <paramref name="exercise"/> (B-661): the learnt language of its textbook series. The caller
    /// must load <c>SeriesUnit.Series</c> - a missing include would silently drop the special-character bar.
    /// </summary>
    public static string? KeyboardLangOf(Exercise exercise) => exercise.SeriesUnit?.Series?.SourceLanguage;

    /// <summary>
    /// The entry a given answer credits in a <b>set-graded</b> exercise: the first candidate whose accepted
    /// answers match, or <c>null</c> if none does (a wrong answer, or one already credited – the caller keeps
    /// those out of <paramref name="candidates"/>). Candidates are walked in ascending index order, so two
    /// entries accepting the same text always credit the same one; a set must still grade deterministically.
    /// <para>
    /// Lives here rather than in a controller because both play paths need it, and the plain per-index
    /// comparison it replaces already stands written out four times.
    /// </para>
    /// </summary>
    public static int? MatchOpenEntry(IReadOnlyList<ContentItem> items, IEnumerable<int> candidates,
        string? given, AnswerGrader grader)
    {
        foreach (var index in candidates.Where(i => i >= 0 && i < items.Count).Order())
        {
            if (grader.Matches(given, items[index])) return index;
        }
        return null;
    }

    /// <summary>
    /// The representation of a content atom permitted per stage as a card/test item (anti-cheat in one place):
    /// typed stages withhold the solution (<c>Reveal</c>), display/self-assessment reveals it - together with
    /// every equally valid alternative (<c>RevealAlternatives</c>), so the child does not mark itself wrong;
    /// letter boxes give the length, the listening stage the audio source, multiple choice the options.
    /// Shared by practice card (<c>PracticeCard</c>) and test item (<c>TestItem</c>) – the latter drops the
    /// image facets, it renders no image.
    /// </summary>
    public static (string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices,
        string? AudioUrl, string? ImageUrl, string? ImageAlt, int? GapIndex, string? Prompt, string? Passage,
        bool AnyOrder, IReadOnlyList<string>? RevealAlternatives, IReadOnlyList<WordPair>? Decoding,
        string? AnswerPattern, IReadOnlyList<string>? LetterTiles, IReadOnlyList<string>? CueBox,
        string? DictationSpokenAnswer, string? DictationLang)
        CardFacets(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, IExerciseType type,
            int stage, bool typed, int seed)
    {
        // The scramble stage (B-05): the type names the word, the shuffle needs the session's or attempt's id as
        // seed - frozen per session, new per session - and the item index, so two cards of one round differ.
        var tileSource = typed ? type.LetterTileSource(item, stage) : null;
        // Options only where the solution is withheld (see below). A stage that falls back when it has none (B-599)
        // takes its fallback's facets, so a choice without distractors becomes letter boxes instead of a blank field.
        var choices = typed ? ShuffledChoices(type, configJson, items, item, stage, seed) : null;
        var facetStage = choices is null && type.ChoiceFallbackStage(stage) is { } fallback ? fallback : stage;
        var (letterBoxLength, audioUrl, imageUrl, answerPattern) = type.StageFacets(item, facetStage);
        // The cue box replaces the per-atom hint (B-524): next to the box it would name the meaning to find.
        var cueBox = typed ? type.CueBox(configJson, items, stage) : null;
        // B-597, decision 9: without a recording the device reads the dictation aloud, and for that it needs the word -
        // which is the answer. A deliberate exception to "no solution before the answer", accepted by the father for a
        // household app: the child does not see it, only the network tab would. A recording replaces it.
        var dictation = type.AudioReplacesPrompt(stage) && string.IsNullOrWhiteSpace(audioUrl)
            && !string.IsNullOrWhiteSpace(item.DictationLang) && !string.IsNullOrWhiteSpace(item.Answer);
        return (
            // A hint the card already shows as its material (an author's gap prompt, B-633) is no hint: behind the tip
            // button it would only repeat what stands above the text.
            typed && cueBox is null && item.Hint != item.Passage ? item.Hint : null,
            letterBoxLength,
            typed ? null : item.Answer,
            // Options only where the solution is withheld. Today every type that offers a pool does so on a
            // typed stage anyway, so this changes nothing - but it makes the rule structural instead of a
            // coincidence of three unrelated types. Without it, one added display stage would put the answer
            // next to the options and no test would notice.
            choices,
            audioUrl,
            imageUrl,
            // The alt text follows the image: no image, no alt text - otherwise the description ("a unicorn is
            // running") would leak on a typed stage exactly what the image would have given away.
            imageUrl is null ? null : item.ImageAlt,
            // Which placeholder of a shared text is being asked. Unlike the facets above this is no anti-cheat
            // decision - it is the address of the atom, and withholding it makes the card unanswerable.
            item.GapIndex,
            // The prompt is withheld exactly where the recording replaces it (the type decides, see
            // IExerciseType.AudioReplacesPrompt) - and only when a recording actually arrived, so a missing
            // audio source never leaves the card blank on both counts. Whitespace counts as missing: the
            // audio URL is a free-text field, and "" would satisfy a null check while playing nothing.
            type.AudioReplacesPrompt(stage) && !string.IsNullOrWhiteSpace(audioUrl) ? null : item.Prompt,
            // What the question is about, unabridged: reading text, grammar instruction. No anti-cheat rule
            // applies - it is the material, not the solution. One exception: a gap's own prompt (B-633) is also its
            // hint, and the cue box withholds the asked gap's meaning (B-524) - shown above the text it would name it.
            cueBox is not null && item.Passage == item.Hint ? null : item.Passage,
            // Whether any not-yet-named answer counts. Like the gap index this is an address, not a secret:
            // the rule lives in the config the child never sees, so withholding it makes the card a guess.
            // The `typed` proviso is the one both graders apply - it must not promise a rule on a stage that
            // is self-assessed and therefore still graded card by card.
            typed && type.GradesAsSet(configJson),
            // The equally valid answers next to the revealed one - same anti-cheat boundary as `Reveal`, because
            // an alternative is no bigger secret than the primary answer on a stage that shows it anyway.
            // Without them the child grades ITSELF as wrong for a correct answer ("sehr groß" against a revealed
            // "riesig"), which is the very damage B-65 fixed for the typed stages.
            // Index 0 is the primary answer by contract of the `Accepted(...)` helper that builds the list.
            typed || item.AcceptedAnswers.Count <= 1 ? null : item.AcceptedAnswers.Skip(1).ToList(),
            // The word-for-word decoding, unconditional like the passage: it is the method the exercise is named
            // after, not the solution to withhold. Without it a Birkenbihl card is an ordinary translation card.
            item.Decoding,
            // The letter-box mask (B-66) - null outside that stage, same condition as `letterBoxLength`. On the scramble
            // stage it carries the word boundaries of an answer with several words (B-634); one word stays without.
            answerPattern ?? (tileSource is null ? null : StageMechanics.ScrambleWordPattern(tileSource)),
            tileSource is null ? null : StageMechanics.ScrambleLetters(tileSource, unchecked((seed * 397) ^ item.Index)),
            cueBox,
            // The prompt beside it stays: it is the German word, shown only where the device has no voice for the
            // language and the dictation falls back to free text.
            dictation ? item.Answer : null,
            dictation ? item.DictationLang : null);
    }

    // The options in an order of their own per card and session: an author's list (reading questions) usually starts with
    // the solution, and a fixed order teaches the button's position instead of the value. Seeded, so a reload within the
    // session shows the same buttons; mixed with the item index, so two questions of the same length differ.
    private static IReadOnlyList<string>? ShuffledChoices(IExerciseType type, string configJson,
        IReadOnlyList<ContentItem> items, ContentItem item, int stage, int seed)
    {
        var choices = type.Choices(configJson, items, item, stage);
        return choices is null || type.ChoicesShareOnePool(stage)
            ? choices
            : StageMechanics.ShuffleOptions(choices, unchecked((seed * 7919) + item.Index));
    }

    /// <summary>
    /// Orders a set (index, progress) according to the chosen strategy and returns the indices.
    /// Used when freezing the session/exam order; the randomness (Random/NewestWeighted)
    /// therefore falls only <b>once</b> at the start, not on every call.
    /// </summary>
    public static IReadOnlyList<int> OrderIndices(
        IEnumerable<(int Index, PositionItemProgress? Prog)> items, PracticeOrder strategy)
    {
        var list = items.ToList();
        return strategy switch
        {
            PracticeOrder.Serial => list.OrderBy(x => x.Index).Select(x => x.Index).ToList(),
            PracticeOrder.Random => list.OrderBy(_ => Random.Shared.Next()).Select(x => x.Index).ToList(),
            PracticeOrder.NewestWeighted => WeightedNewest(list),
            _ => list.OrderBy(x => x.Prog?.Box ?? 1).ThenBy(x => x.Index).Select(x => x.Index).ToList(),
        };
    }

    /// <summary>
    /// Weighted draw without replacement: most recently introduced (or never introduced) content items receive
    /// significantly higher weight (rank weight 1, 1/2, 1/3 …), so they are placed near the front with high
    /// probability – the "newest first, but not rigid" rule.
    /// </summary>
    private static List<int> WeightedNewest(List<(int Index, PositionItemProgress? Prog)> items)
    {
        // Rank by introduction date descending (null = brand new = highest rank), then the index as a tiebreaker.
        var ranked = items
            .OrderByDescending(x => x.Prog?.IntroducedAt ?? DateOnly.MaxValue)
            .ThenBy(x => x.Index)
            .ToList();
        var pool = ranked.Select((x, rank) => (x.Index, Weight: 1.0 / (rank + 1))).ToList();

        var result = new List<int>(pool.Count);
        while (pool.Count > 0)
        {
            var total = pool.Sum(p => p.Weight);
            var roll = Random.Shared.NextDouble() * total;
            var i = 0;
            for (; i < pool.Count - 1; i++)
            {
                roll -= pool[i].Weight;
                if (roll <= 0) break;
            }
            result.Add(pool[i].Index);
            pool.RemoveAt(i);
        }
        return result;
    }

    // "New" includes the introduction day (B-435): since the first contact is an ungraded introduction card (B-419),
    // a scope of "never introduced" would only ever show a word and never ask it. The word is asked in the follow-up
    // round of its introduction day and becomes review material from the next day on - with or without Leitner,
    // which is why the day decides and not the review count (without Leitner no review is ever counted).
    // "Old" stays "introduced at all", so on the introduction day a word lies in both scopes.
    // B-653: a word met on its learn card but not imprinted stays "new" until it is - a half-learnt word must not drop
    // out of the plan. A content without a learn card, or a position whose type is unknown, keeps the day rule.
    private static bool ScopeMatch(ItemScope scope, PositionItemProgress? prog, DateOnly day, IExerciseType? type,
        IReadOnlyList<ContentItem> items) => scope switch
        {
            ItemScope.New => prog?.IntroducedAt is null || prog.IntroducedAt == day
                || (type is not null && IsOpenLearnCard(type, items, prog, day)),
            ItemScope.Old => prog?.IntroducedAt is not null,
            _ => true,
        };

    // Due when never seen (no progress) or the due date has been reached.
    private static bool IsDue(PositionItemProgress? prog, DateOnly day) =>
        prog is null || prog.DueOn is null || prog.DueOn <= day;

    /// <summary>
    /// Retrieves the progress record of a position's content atom on the exercise it plays (B-848), or creates it
    /// (tracked) if missing.
    /// </summary>
    public async Task<PositionItemProgress> ProgressForAsync(PlanPosition pos, DateOnly day, int itemIndex, CancellationToken ct)
    {
        var positionId = pos.Id;
        var exerciseId = PlayedExerciseId(pos, day);
        // The tracker first: a row added earlier in the same request (introduce, then imprint - B-658) is not in the
        // database yet, and a second Add would violate the unique (position, exercise, item) index on save.
        var prog = db.PositionItemProgress.Local
                .FirstOrDefault(p => p.PlanPositionId == positionId && p.ExerciseId == exerciseId && p.ItemIndex == itemIndex)
            ?? await db.PositionItemProgress
                .FirstOrDefaultAsync(p => p.PlanPositionId == positionId && p.ExerciseId == exerciseId
                    && p.ItemIndex == itemIndex, ct);
        if (prog is null)
        {
            prog = new PositionItemProgress { PlanPositionId = positionId, ExerciseId = exerciseId, ItemIndex = itemIndex };
            db.PositionItemProgress.Add(prog);
        }
        return prog;
    }

    /// <summary>
    /// Records a Leitner review on the progress: correct → one box higher (longer
    /// interval), incorrect → back to box 1 and due again immediately. With <see cref="PlanPosition.ForgivingDemotion"/>
    /// the first miss in a row only drops one box (B-219). Only a <c>verified</c> answer - one the server checked on a
    /// typed stage rather than taking the child's word for it - moves the miss marker
    /// <see cref="PositionItemProgress.LastMissed"/> (B-219 decision 11, B-591). The caller saves.
    /// </summary>
    public void ApplyReview(PlanPosition pos, PositionItemProgress prog, bool correct, bool verified, DateOnly today,
        DateTime nowUtc)
    {
        var intervals = BoxIntervals(pos);
        prog.ReviewCount++;
        prog.LastReviewedAt = nowUtc;

        if (correct)
        {
            prog.Box = Math.Min(pos.MaxBox, Math.Max(1, prog.Box) + 1);
            prog.DueOn = today.AddDays(intervals[Math.Min(prog.Box, intervals.Count - 1)]);
            // An unchecked "known" must not erase a checked miss, or it would pardon the next one, too.
            if (verified) prog.LastMissed = false;
        }
        else
        {
            // Forgiving demotion pardons only the first miss in a row; never below box 1.
            prog.Box = pos.ForgivingDemotion && !prog.LastMissed ? Math.Max(1, prog.Box - 1) : 1;
            prog.DueOn = today; // same day: practice again right away
            if (verified) prog.LastMissed = true;
        }
    }
}

/// <summary>
/// The learning state one introduced card's stage depends on (B-476): its Leitner box and when it was introduced.
/// <paramref name="RemediationDone"/> marks a remediation task already solved - a cloze card shows it filled in.
/// </summary>
public readonly record struct CardProgress(int Box, DateOnly IntroducedAt, DateOnly? ImprintedAt = null,
    bool RemediationDone = false);
