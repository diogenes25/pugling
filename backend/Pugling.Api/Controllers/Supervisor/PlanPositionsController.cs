using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Position CRUD of the new study plan model: the father assembles a study plan from <b>global</b>
/// catalog exercises. Each <see cref="PlanPosition"/> refers to an <see cref="Exercise"/>
/// (the content stays there – no copy) and carries its own overrides (stage/count/scope),
/// goals (cadence + threshold), points and Leitner settings. The positions are played via
/// the <see cref="PositionPracticeController"/> or <see cref="PositionTestsController"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/study-plans/{planId:int}/positions")]
[Tags("Supervisor – Plan Positions")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PlanPositionsController(PuglingDbContext db, ExercisePermissionService perms, ExerciseTypeRegistry types,
    TimeProvider time, LearningDay learningDay, PositionProgressService progress) : ControllerBase
{
    /// <summary>
    /// Is the exercise <b>not yet filled</b>? Applies only to types that carry their content as an item table
    /// (<see cref="StoreResolution.ItemTable"/>, currently vocabulary): there, "no item" is an unfinished
    /// data state. For all other types the question would be misframed – an essay *never* has items,
    /// a math drill generates its tasks from rules. That's why this doesn't check for "checkable content"
    /// (that may legitimately be 0), but for the one form of emptiness that nobody intended.
    /// </summary>
    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles out.
    private async Task<bool> IsUnfilledAsync(Exercise exercise, CancellationToken ct) =>
        types.ByKey(exercise.Type)?.StoreResolution == StoreResolution.ItemTable
        && !await db.ExerciseItems.AnyAsync(i => i.ExerciseId == exercise.Id, ct);

    // The supervisor reads the position as it plays today (B-849): a level raised today shows from tomorrow.
    private PositionResponse Map(PlanPosition p) => Map(p, learningDay.Today);

    private PositionResponse Map(PlanPosition p, DateOnly today) =>
        new(p.Id, p.StudyPlanId, p.ExerciseId, p.Exercise?.Title ?? "", p.Exercise?.Type.ToString() ?? "",
            p.Order, p.Stage, p.ItemCount, p.Scope, p.Cadence, p.OrderStrategy, p.GoalThreshold, p.RequireTypedTest,
            p.UseLeitner, p.MaxBox, p.BoxIntervalDays, p.StageSchedule, p.PointsGoalMet, p.PenaltyCoins, p.NewContentPoints,
            p.ComboThreshold, p.ComboBonusPoints, p.SpeedThresholdSeconds, p.SpeedBonusPoints, p.TimeSlots,
            p.NewItemsPerDay, PositionPlayService.DailyDose(p, types, today),
            // B-848: judged on the exercise the position plays, whose type a prerequisite may not share.
            types.ByKey(PositionPlayService.PlayedExercise(p, today)?.Type ?? "") is { } type
                ? PositionPlayService.LearningCheckOf(p, type, today) : null,
            p.ForgivingDemotion, p.Remediation, p.CompletionBonus, p.CompletedOn, p.CurrentBuildLevel,
            PositionPlayService.BuildPathOf(p, today), p.AccentLenient);

    /// <summary>All positions of the study plan in their order.</summary>
    [HttpGet]
    public async Task<IEnumerable<PositionResponse>> List(int planId, CancellationToken ct = default)
    {
        var positions = await db.PlanPositions.AsNoTracking()
            .Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites)
            .Where(p => p.StudyPlanId == planId)
            .OrderBy(p => p.Order).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return positions.Select(Map);
    }

    // No default for `ct` (as in IsUnfilledAsync): it would make the call site look correct while the
    // client's cancellation fizzles out.
    private Task<PlanPosition?> FindAsync(int planId, int positionId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);

    /*
     * The goal threshold is a PERCENTAGE (see PlanPosition.GoalThreshold). Without this check, mistaking it for a
     * hit count is silent and works exactly backwards: "3" takes the teeth out of the duty instead of sharpening it
     * (every attempt above 3 % passes). `null` stays the way to say "default" - 0 would be a second spelling of it.
     */
    private static string? ThresholdProblem(int? goalThreshold) =>
        goalThreshold is null or (>= 1 and <= 100)
            ? null
            : "goalThreshold is a pass percentage and must be between 1 and 100 (omit it for the default of 80).";

    /// <summary>Upper bound of a time-slot factor - beyond this it is a typo, not a setting.</summary>
    private const double MaxMultiplier = 10.0;

    // The rule moved to GoalPointsPolicy (B-192): the catalog search needs the same figure to pre-fill
    // the form with what will actually apply, and a rule that two callers derive separately is the kind
    // of duplicate this repo has paid for. Bound, fallback and their reasoning live there now.

    // The daily dose (B-04) is a count of contents; the upper bound only keeps a typo from reading as "no cap".
    private static string? DoseProblem(int? newItemsPerDay) =>
        newItemsPerDay is < 0 or > 200 ? "newItemsPerDay must be between 0 and 200 (0 = no cap)." : null;

    // Without the Leitner box every known content is due every day, so a dose would only delay the day the round
    // carries the whole pool - the two settings only work together.
    private static string? DoseNeedsLeitnerProblem(int newItemsPerDay, bool useLeitner) =>
        newItemsPerDay > 0 && !useLeitner ? "A daily dose (newItemsPerDay) needs the Leitner box (useLeitner: true); without the box, send newItemsPerDay: 0 in the same request." : null;

    // A set-graded exercise (an unordered list) accepts any entry not yet named - but only among the cards of the
    // round. A dose would freeze a few of them, and a right answer outside that hidden choice would count as wrong.
    private string? DoseOnSetProblem(Exercise exercise, int newItemsPerDay) =>
        newItemsPerDay > 0 && types.ByKey(exercise.Type)?.GradesAsSet(PositionPlayService.ConfigOf(exercise)) == true
            ? "A daily dose (newItemsPerDay) does not fit an exercise whose entries may be named in any order."
            : null;

    /// <summary>
    /// Upper bound on the number of windows per position: a day has 24 hours, and the column is deliberately
    /// unbounded (JSON) - without a limit a client could store an arbitrarily long list there.
    /// </summary>
    private const int MaxTimeSlots = 24;

    /*
     * Both checks catch the same failure mode as the percentage threshold above - the setting that looks valid
     * and does NOTHING: a slot with start >= end never applies, and a factor of 0 takes the points away from
     * every correct answer inside it.
     *
     * OVERLAP is deliberately not checked: it is explicitly allowed and the choice stays deterministic anyway
     * (the narrowest slot wins). Forbidding it would add a validation case to a place that has none on purpose.
     */
    private static string? TimeSlotProblem(List<ScoringTimeSlot>? slots)
    {
        if (slots is null) return null;
        if (slots.Count > MaxTimeSlots)
            return $"A position takes at most {MaxTimeSlots} time slots (got {slots.Count}).";
        foreach (var slot in slots)
        {
            if (slot.Start >= slot.End)
                return $"A time slot needs start < end (got {slot.Start:HH\\:mm}–{slot.End:HH\\:mm}); a slot ending before it starts never applies.";
            if (slot.Multiplier <= 0 || slot.Multiplier > MaxMultiplier)
                return $"A time slot multiplier must be above 0 and at most {MaxMultiplier:0.#} (got {slot.Multiplier:0.##}).";
        }
        return null;
    }

    // Remediation (B-635) plays a ladder of easier forms; a type without one would start as free text and stay there,
    // with an "easier" button that can never work. An exercise without tasks (a cloze without gaps) has an empty pool: no
    // answer can ever finish the remediation, and "until done" would charge its penalty forever (B-668). The pool cannot
    // shrink to empty later - a played exercise's list may not get shorter (B-328) - so this is the one entry.
    private static string? RemediationProblem(IExerciseType? type, Exercise exercise) =>
        type is null || !type.SupportsRemediation
            ? "Remediation needs an exercise type that can be made easier step by step (currently: cloze)."
            : type.ItemsOf(exercise.ConfigJson).Count == 0
                ? "Remediation needs an exercise with at least one task; this one has none."
                : null;

    // B-636: "daily, until done" ends with the last remediation task - on any other position nothing would ever end it, and
    // the duty would cost its penalty forever. The bonus range keeps a typo from paying a fortune.
    private const int MaxCompletionBonus = 1000;

    private static string? UntilDoneProblem(GoalCadence cadence, bool remediation, int? bonus) =>
        cadence == GoalCadence.UntilDone && !remediation
            ? "The cadence \"daily, until done\" needs remediation switched on; nothing else can ever finish it."
            : bonus is < 0 or > MaxCompletionBonus
                ? $"The completion bonus must be between 0 and {MaxCompletionBonus} (got {bonus})."
                : null;

    // B-848: a build level needs a prerequisite on that level (level 3, the target, needs any), and the three are all.
    // Judged only when a level is set now: a prerequisite the creator moves later must not lock the father out of the rest.
    private static string? BuildLevelProblem(Exercise exercise, int? level)
    {
        if (level is null) return null;
        if (level is < 1 or > Exercise.BuildLevelTarget)
            return $"currentBuildLevel must be between 1 and {Exercise.BuildLevelTarget} ({Exercise.BuildLevelTarget} = the exercise itself).";
        var prerequisites = exercise.Prerequisites ?? [];
        if (prerequisites.Count == 0)
            return "This exercise has no prerequisites, so it has no build path; omit currentBuildLevel.";
        return level < Exercise.BuildLevelTarget && !prerequisites.Any(e => e.BuildLevel == level)
            ? $"This exercise has no prerequisite on build level {level}."
            : null;
    }

    // B-848: the exercise a level plays - the target's prerequisite on it (lowest id, as PositionPlayService picks), or the
    // target itself. The checks that judge what the child plays (execute right, remediation) run on this one.
    private static Exercise PlayedAt(Exercise target, int? level) =>
        level is { } l && l < Exercise.BuildLevelTarget
            ? (target.Prerequisites ?? []).Where(e => e.BuildLevel == l).MinBy(e => e.Id) ?? target
            : target;

    private const string PrerequisiteNotExecutable =
        "The exercise on this build level is not publicly assignable; you need execute permission from its owner.";

    // B-848: the round frozen at its start and the open test hold item indices of the old level's exercise - switching
    // under them would grade the child's answers against another exercise. Today's only: an abandoned round of an earlier
    // day plays nothing any more.
    private async Task<bool> InPlayTodayAsync(int positionId, CancellationToken ct)
    {
        var today = learningDay.Today;
        if (await db.TestAttempts.AnyAsync(t => t.PlanPositionId == positionId && t.Day == today && t.CompletedAt == null
                && !t.BySupervisor, ct))
            return true;
        // Order is a JSON column; the cursor comparison runs in memory over today's few rounds.
        return (await db.PracticeSessions.AsNoTracking()
                .Where(s => s.PlanPositionId == positionId && s.Day == today && s.Mode == PlayMode.Lern && s.EndedAt == null)
                .Select(s => new { s.Cursor, s.Order })
                .ToListAsync(ct))
            .Any(s => s.Cursor < s.Order.Count);
    }

    // B-848, decision 3 (B-701 E6): until a duty can follow the level (B-702), a build path takes no daily or weekly duty -
    // the goal would be judged on whatever level the child happens to play.
    private static string? BuildLevelCadenceProblem(int? level, GoalCadence cadence) =>
        level is not null && cadence is GoalCadence.Daily or GoalCadence.Weekly
            ? "A position on a build path takes no daily or weekly duty yet; choose no duty or \"daily, until done\"."
            : null;

    // Why an invalid stage matters (it reveals the answer instead of breaking) and why the check is shared with
    // the creator's `DefaultStage`: see StageValidation. The schedule is checked step by step - one bad day would
    // be enough to hand out the solutions on that day.
    private string? StageProblem(Exercise exercise, int? stage, List<StageStep>? schedule) =>
        StageValidation.ProblemText(types.ByKey(exercise.Type),
            [stage, .. schedule?.Select(s => (int?)s.Stage) ?? []]);

    /// <summary>A single position.</summary>
    [HttpGet("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> Get(int planId, int positionId, CancellationToken ct = default)
    {
        var pos = await FindAsync(planId, positionId, ct);
        return pos is null ? NotFound() : Map(pos);
    }

    /// <summary>Adds a position to the study plan for a catalog exercise.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PositionResponse>> Create(int planId, CreatePositionDto dto, CancellationToken ct)
    {
        if ((ThresholdProblem(dto.GoalThreshold) ?? TimeSlotProblem(dto.TimeSlots) ?? DoseProblem(dto.NewItemsPerDay)) is { } problem)
            return this.ProblemWithCode(ApiErrors.ValidationError, problem);

        var exercise = await db.Exercises.Include(e => e.Prerequisites).FirstOrDefaultAsync(e => e.Id == dto.ExerciseId, ct);
        if (exercise is null) return this.ProblemWithCode(ApiErrors.InvalidReference, $"Exercise {dto.ExerciseId} not found.");
        // Execute gate: an exercise that is not publicly executable may only be assigned by someone holding an owner/write/execute right.
        if (!await perms.CanExecuteAsync(User, exercise, ct))
            return this.ProblemWithCode(ApiErrors.ExerciseNotExecutable, "This exercise is not publicly assignable; you need execute permission from its owner.");
        // An unfilled exercise has to be stopped here, not at creation: "create first, fill later" is a wanted
        // path (POST with empty refs, then /items or /refs-from-tags). Only assigning turns the gap into a
        // problem - the child would get an obligation it cannot play, and used to learn about it only in the
        // test as "no_checkable_content".
        if (await IsUnfilledAsync(exercise, ct))
            return this.ProblemWithCode(ApiErrors.ExerciseEmpty,
                "This exercise has no items yet. Add its content before assigning it to a study plan.");
        // Only checkable once the exercise (and thus its type) is known - unlike the threshold above.
        if (StageProblem(exercise, dto.Stage, dto.StageSchedule) is { } stageProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, stageProblem);
        // The EFFECTIVE value (position override, otherwise the exercise's own default) - an unfulfillable
        // setting must not slip through just because the position itself left it unset (B-93).
        var effectiveRequireTypedTest = dto.RequireTypedTest ?? exercise.DefaultRequireTypedTest;
        var createType = types.ByKey(exercise.Type);
        if (RequireTypedTestValidation.ProblemText(createType, effectiveRequireTypedTest) is { } typedProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, typedProblem);
        if (createType is not null && RequireTypedTestValidation.UnreachableProblemText(createType, effectiveRequireTypedTest,
                dto.Stage ?? exercise.DefaultStage ?? createType.DefaultStage, dto.StageSchedule) is { } unreachableOnCreate)
            return this.ProblemWithCode(ApiErrors.TypedTestUnreachable, unreachableOnCreate);

        if ((DoseNeedsLeitnerProblem(dto.NewItemsPerDay ?? 0, dto.UseLeitner ?? exercise.DefaultUseLeitner)
                ?? DoseOnSetProblem(exercise, dto.NewItemsPerDay ?? 0)) is { } doseProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, doseProblem);
        if (dto.Remediation == true && RemediationProblem(createType, exercise) is { } remediationProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, remediationProblem);
        if (UntilDoneProblem(dto.Cadence ?? GoalCadence.None, dto.Remediation ?? false, dto.CompletionBonus) is { } untilDoneProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, untilDoneProblem);
        if ((BuildLevelProblem(exercise, dto.CurrentBuildLevel)
                ?? BuildLevelCadenceProblem(dto.CurrentBuildLevel, dto.Cadence ?? GoalCadence.None)) is { } buildProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, buildProblem);
        // The child plays the prerequisite: its execute gate and its fitness for remediation count, not only the target's.
        if (PlayedAt(exercise, dto.CurrentBuildLevel) is var createPlayed && createPlayed.Id != exercise.Id)
        {
            if (!await perms.CanExecuteAsync(User, createPlayed, ct))
                return this.ProblemWithCode(ApiErrors.ExerciseNotExecutable, PrerequisiteNotExecutable);
            if (dto.Remediation == true && RemediationProblem(types.ByKey(createPlayed.Type), createPlayed) is { } playedRemediation)
                return this.ProblemWithCode(ApiErrors.ValidationError, playedRemediation);
        }

        var order = dto.Order ?? ((await db.PlanPositions.Where(p => p.StudyPlanId == planId)
            .MaxAsync(p => (int?)p.Order, ct)) ?? -1) + 1;
        var sb = exercise.SuggestedBonus;

        var createdAt = time.GetUtcNow().UtcDateTime;
        var pos = new PlanPosition
        {
            // The same clock as the day logic that reads it (B-476: a word's ladder counts from the day its position
            // joined the plan); the entity's wall-clock default would put the two on different calendars under test.
            CreatedAt = createdAt,
            DutySince = learningDay.DayOf(createdAt),
            StudyPlanId = planId,
            ExerciseId = dto.ExerciseId,
            Order = order,
            Stage = dto.Stage,
            ItemCount = dto.ItemCount,
            Scope = dto.Scope ?? ItemScope.All,
            Cadence = dto.Cadence ?? GoalCadence.None,
            OrderStrategy = dto.OrderStrategy ?? PracticeOrder.WeakestFirst,
            GoalThreshold = dto.GoalThreshold,
            // Leitner/typed inherit their default from the exercise (hybrid principle) as long as the position says nothing.
            RequireTypedTest = dto.RequireTypedTest ?? exercise.DefaultRequireTypedTest,
            UseLeitner = dto.UseLeitner ?? exercise.DefaultUseLeitner,
            MaxBox = dto.MaxBox is > 0 ? dto.MaxBox.Value : 5,
            // How hard a miss resets is a statement about this obligation, not the material: no exercise default.
            ForgivingDemotion = dto.ForgivingDemotion ?? false,
            Remediation = dto.Remediation ?? false,
            // B-624 decision 4: lenient unless the father asks for strict accents.
            AccentLenient = dto.AccentLenient ?? true,
            CompletionBonus = dto.CompletionBonus ?? 0,
            // Empty means "method default" here as in the PATCH (B-547) - no third state next to "default" and "own".
            BoxIntervalDays = dto.BoxIntervalDays is { Count: > 0 } intervals ? intervals : null,
            StageSchedule = dto.StageSchedule is { Count: > 0 } schedule ? schedule : null,
            // Points/bonus: position override → exercise suggestion → model default.
            //
            // The target reward takes the creator's recommendation (B-45), but only within bounds on BOTH
            // sides. A 0 must not win: an old exercise left at 0 would drag every newly created position
            // down to zero target points - a silent regression against the previous hard 20. And a figure
            // above `GoalPointsPolicy.MaxInherited` is a typo or an abuse, not a setting (see there). Like
            // `SuggestedBonus` below this is copied ONCE; a creator raising the recommendation later does
            // not move study plans that are already running.
            PointsGoalMet = dto.PointsGoalMet ?? GoalPointsPolicy.Effective(exercise.RewardPoints),
            // The penalty is opt-in per position (default 0 = reward only, the previous behavior).
            PenaltyCoins = dto.PenaltyCoins ?? 0,
            NewContentPoints = dto.NewContentPoints ?? sb?.NewContentPoints ?? 10,
            ComboThreshold = dto.ComboThreshold ?? sb?.ComboThreshold ?? 5,
            ComboBonusPoints = dto.ComboBonusPoints ?? sb?.ComboBonusPoints ?? 5,
            SpeedThresholdSeconds = dto.SpeedThresholdSeconds ?? sb?.SpeedThresholdSeconds ?? 0,
            SpeedBonusPoints = dto.SpeedBonusPoints ?? sb?.SpeedBonusPoints ?? 0,
            // No exercise suggestion behind it: a time of day is a statement about the family's day, not about
            // the teaching material. An empty list is stored as null, so "no window" has one spelling only.
            TimeSlots = dto.TimeSlots is { Count: > 0 } ? dto.TimeSlots : null,
            NewItemsPerDay = dto.NewItemsPerDay ?? 0,
        };
        PositionPlayService.SetBuildLevel(pos, dto.CurrentBuildLevel, learningDay.Today);
        db.PlanPositions.Add(pos);
        await db.SaveChangesAsync(ct);

        pos.Exercise = exercise;
        return CreatedAtAction(nameof(Get), new { planId, positionId = pos.Id }, Map(pos));
    }

    /// <summary>
    /// Turns the child's weak words into a daily review (B-584): builds a vocabulary exercise from exactly these store
    /// entries (author = caller, not executable for others) and adds it as a daily position. Only words the plan's child
    /// has learning state for (<c>400 invalid_reference</c>), 1 to 25 of one language pair (<c>400 validation_error</c>)
    /// and, if the plan has a subject, of that subject (<c>400 review_subject_mismatch</c>); <c>409
    /// review_already_in_plan</c> when a position of the plan already practises exactly these words.
    /// </summary>
    [HttpPost("from-words")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PositionResponse>> CreateFromWords(int planId, CreateReviewFromWordsDto dto,
        [FromServices] WeakWordReviewService reviews, CancellationToken ct = default)
    {
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return NotFound();
        var outcome = await reviews.CreateAsync(plan, dto.VocabularyIds, User.AdultId(), ct);
        if (outcome.Error is { } error) return this.ProblemWithCode(error, outcome.Detail!);
        var pos = outcome.Position!;
        return CreatedAtAction(nameof(Get), new { planId, positionId = pos.Id }, Map(pos));
    }

    /// <summary>Changes a position (partial). Only fields that are set are changed.</summary>
    [HttpPatch("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> Update(int planId, int positionId, UpdatePositionDto dto, CancellationToken ct = default)
    {
        if ((ThresholdProblem(dto.GoalThreshold) ?? TimeSlotProblem(dto.TimeSlots) ?? DoseProblem(dto.NewItemsPerDay)) is { } problem)
            return this.ProblemWithCode(ApiErrors.ValidationError, problem);

        var pos = await FindAsync(planId, positionId, ct);
        if (pos is null) return NotFound();
        // FindAsync always Include()s Exercise; a position without it is a data state the schema forbids.
        // Fail loud (404, already declared on this action) instead of letting a dropped Include silently
        // skip the checks below (B-95) - `pos.Exercise is { }` used to make that skip look intentional.
        if (pos.Exercise is not { } exercise) return NotFound();
        // Same checks as in Create, and before the first assignment: a rejected PATCH must leave the position
        // untouched, not half-written. The exercise (and its type) never changes on a PATCH, so the type check
        // (ProblemText) only needs an explicit new `true`; whether the stages can still be graded is the next block's
        // question.
        if (StageProblem(exercise, dto.Stage, dto.StageSchedule) is { } stageProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, stageProblem);
        if (RequireTypedTestValidation.ProblemText(types.ByKey(exercise.Type), dto.RequireTypedTest) is { } typedProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, typedProblem);
        // Judged on the combination after the PATCH, but only when this PATCH changes one of its halves (B-552): an older
        // position holding an unreachable combination must stay editable in everything else (points, penalty ...). A
        // CHANGE, not a sent field: the father's form sends requireTypedTest and the stage with every save, so a check on
        // presence would lock him out of exactly those other settings.
        var touchesCheck = (dto.RequireTypedTest is { } typedSent && typedSent != pos.RequireTypedTest)
            || (dto.Stage is { } stageSent && stageSent != pos.Stage)
            || (dto.ClearStage && pos.Stage is not null)
            || dto.StageSchedule is not null;
        if (touchesCheck && types.ByKey(exercise.Type) is { } patchType)
        {
            var stageAfter = dto.ClearStage ? null : dto.Stage ?? pos.Stage;
            // An empty schedule puts it back to the default (B-547), so it counts as none here as well.
            var scheduleAfter = dto.StageSchedule is { } sent ? (sent.Count > 0 ? sent : null) : pos.StageSchedule;
            if (RequireTypedTestValidation.UnreachableProblemText(patchType, dto.RequireTypedTest ?? pos.RequireTypedTest,
                    stageAfter ?? exercise.DefaultStage ?? patchType.DefaultStage, scheduleAfter) is { } unreachableProblem)
                return this.ProblemWithCode(ApiErrors.TypedTestUnreachable, unreachableProblem);
        }

        // The set check only judges a dose being set now: a list the creator switched to any order after it was assigned
        // keeps its stored dose, which play ignores (PositionPlayService.DailyDose) - blocking every other edit of the
        // position over it would lock the father out of a setting he did not touch (B-478).
        // Judged only when switched on now: a position keeps its setting through every other edit.
        if (dto.Remediation == true && !pos.Remediation && RemediationProblem(types.ByKey(exercise.Type), exercise) is { } remediationProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, remediationProblem);
        // Judged on the state after the patch: switching remediation off must take "until done" with it.
        if (UntilDoneProblem(dto.Cadence ?? pos.Cadence, dto.Remediation ?? pos.Remediation, dto.CompletionBonus) is { } untilDoneProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, untilDoneProblem);
        // Judged on the state after the patch: a level changed now (a CHANGE, not a sent field, as for B-552 above - the
        // creator may move a prerequisite later), and the cadence against the level the position keeps.
        var levelChanges = dto.CurrentBuildLevel is { } levelSent && levelSent != pos.CurrentBuildLevel;
        if (((levelChanges ? BuildLevelProblem(exercise, dto.CurrentBuildLevel) : null)
                ?? BuildLevelCadenceProblem(dto.CurrentBuildLevel ?? pos.CurrentBuildLevel, dto.Cadence ?? pos.Cadence))
            is { } buildProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, buildProblem);
        var playedAfter = PlayedAt(exercise, dto.CurrentBuildLevel ?? pos.CurrentBuildLevel);
        if (levelChanges && playedAfter.Id != exercise.Id && !await perms.CanExecuteAsync(User, playedAfter, ct))
            return this.ProblemWithCode(ApiErrors.ExerciseNotExecutable, PrerequisiteNotExecutable);
        // Remediation on the exercise played after the patch, when either of the two changes now.
        if ((dto.Remediation ?? pos.Remediation) && (levelChanges || (dto.Remediation == true && !pos.Remediation))
            && playedAfter.Id != exercise.Id
            && RemediationProblem(types.ByKey(playedAfter.Type), playedAfter) is { } playedRemediation)
            return this.ProblemWithCode(ApiErrors.ValidationError, playedRemediation);
        if (levelChanges && await InPlayTodayAsync(pos.Id, ct))
            return this.ProblemWithCode(ApiErrors.BuildLevelInPlay,
                "The child has a round or a test on this position open today; the level can change once it is finished.");
        if ((DoseNeedsLeitnerProblem(dto.NewItemsPerDay ?? pos.NewItemsPerDay, dto.UseLeitner ?? pos.UseLeitner)
                ?? (dto.NewItemsPerDay is { } dose && dose != pos.NewItemsPerDay ? DoseOnSetProblem(exercise, dose) : null))
            is { } doseProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, doseProblem);

        // B-659: freeing a duty keeps the days the child met it (the old cadence up to DutyUntil); a duty again clears that
        // span, or "after the span, no duty" would deny the new one. Before any setting is written: "met?" is judged by the
        // settings the period ran under. CloseDutyAsync acts only on the switch from a duty (Daily, Weekly, UntilDone): the
        // form sends cadence with every save, and saving a free position again must not move the span.
        if (dto.Cadence == GoalCadence.None)
        {
            var plan = await db.StudyPlans.AsNoTracking().FirstAsync(p => p.Id == planId, ct);
            await progress.CloseDutyAsync(pos, plan, learningDay.Today, ct);
        }
        // Only a real change of cadence clears the span (B-752): a preparation duty carries an end while it stays daily, and the
        // form sends the cadence with every save - a mere save must not turn it into a daily duty without end.
        else if (dto.Cadence is { } dutyAgain && dutyAgain != GoalCadence.None && dutyAgain != pos.Cadence)
        {
            pos.DutyUntil = null;
            pos.CadenceUntil = null;
        }

        if (dto.Order is not null) pos.Order = dto.Order.Value;
        if (dto.Stage is not null) pos.Stage = dto.Stage;
        // Value first, switch second (B-16): "back to the default" wins if a form sends both.
        if (dto.ClearStage) pos.Stage = null;
        if (dto.ItemCount is not null) pos.ItemCount = dto.ItemCount;
        if (dto.ClearItemCount) pos.ItemCount = null;
        if (dto.Scope is not null) pos.Scope = dto.Scope.Value;
        // B-613: a free position that becomes a duty counts from today; a mere save or Daily <-> Weekly keeps the date.
        // B-669: a finished "until done" remediation has no duty after its end either, so a switch to another cadence is a
        // new duty from today - otherwise the settlement would judge the dutiless days since the end by the new rule. The
        // days before the end leave the history with it (no penalty for unsettled ones), which only favours the child.
        if (dto.Cadence is { } cadence && cadence != GoalCadence.None
            && (pos.Cadence == GoalCadence.None
                || (pos is { Cadence: GoalCadence.UntilDone, CompletedOn: not null } && cadence != GoalCadence.UntilDone)))
            pos.DutySince = learningDay.Today;
        if (dto.Cadence is not null) pos.Cadence = dto.Cadence.Value;
        if (dto.OrderStrategy is not null) pos.OrderStrategy = dto.OrderStrategy.Value;
        if (dto.GoalThreshold is not null) pos.GoalThreshold = dto.GoalThreshold;
        if (dto.ClearGoalThreshold) pos.GoalThreshold = null;
        if (dto.RequireTypedTest is not null) pos.RequireTypedTest = dto.RequireTypedTest.Value;
        if (dto.UseLeitner is not null) pos.UseLeitner = dto.UseLeitner.Value;
        if (dto.MaxBox is > 0) pos.MaxBox = dto.MaxBox.Value;
        if (dto.ForgivingDemotion is not null) pos.ForgivingDemotion = dto.ForgivingDemotion.Value;
        if (dto.Remediation is not null) pos.Remediation = dto.Remediation.Value;
        if (dto.AccentLenient is not null) pos.AccentLenient = dto.AccentLenient.Value;
        if (dto.CompletionBonus is not null) pos.CompletionBonus = dto.CompletionBonus.Value;
        // An empty list means "back to the method default" (B-547): no form sends these two, and storing an empty
        // schedule would be a third state next to "default" and "own" that play treats like the default anyway.
        if (dto.BoxIntervalDays is not null) pos.BoxIntervalDays = dto.BoxIntervalDays.Count > 0 ? dto.BoxIntervalDays : null;
        if (dto.StageSchedule is not null) pos.StageSchedule = dto.StageSchedule.Count > 0 ? dto.StageSchedule : null;
        if (dto.PointsGoalMet is not null) pos.PointsGoalMet = dto.PointsGoalMet.Value;
        if (dto.PenaltyCoins is not null) pos.PenaltyCoins = dto.PenaltyCoins.Value;
        if (dto.NewContentPoints is not null) pos.NewContentPoints = dto.NewContentPoints.Value;
        if (dto.ComboThreshold is not null) pos.ComboThreshold = dto.ComboThreshold.Value;
        if (dto.ComboBonusPoints is not null) pos.ComboBonusPoints = dto.ComboBonusPoints.Value;
        if (dto.SpeedThresholdSeconds is not null) pos.SpeedThresholdSeconds = dto.SpeedThresholdSeconds.Value;
        if (dto.SpeedBonusPoints is not null) pos.SpeedBonusPoints = dto.SpeedBonusPoints.Value;
        // Value first, switch second: if a form sends both, "clear" has to win (see UpdatePositionDto). An
        // explicitly sent empty list means "no windows" - reading it as "not specified" would make it a silent
        // no-op, which is the very failure the switch exists against.
        if (dto.TimeSlots is not null) pos.TimeSlots = dto.TimeSlots.Count > 0 ? dto.TimeSlots : null;
        if (dto.ClearTimeSlots) pos.TimeSlots = null;
        if (dto.NewItemsPerDay is not null) pos.NewItemsPerDay = dto.NewItemsPerDay.Value;
        if (levelChanges)
        {
            // The father's level counts at once (B-849): it replaces a raise still waiting for the next learning day, and
            // the count towards a descent starts over (B-702).
            PositionPlayService.SetBuildLevel(pos, dto.CurrentBuildLevel, learningDay.Today);
        }
        // B-636: a remediation already finished when it becomes "until done" ends at once, or it never could.
        await progress.StampFinishedRemediationAsync(pos, learningDay.Today, ct);

        await db.SaveChangesAsync(ct);
        return Map(pos);
    }

    /// <summary>
    /// Deletes a position (the associated <see cref="PositionItemProgress"/> disappears via cascade too).
    /// Not possible while test attempts already exist for the position – otherwise this learning history would be
    /// lost (the foreign key would otherwise only be set to <c>null</c>).
    /// </summary>
    [HttpDelete("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int planId, int positionId, CancellationToken ct = default)
    {
        var pos = await FindAsync(planId, positionId, ct);
        if (pos is null) return NotFound();

        if (await db.TestAttempts.AnyAsync(t => t.PlanPositionId == positionId, ct)
            || await db.PracticeSessions.AnyAsync(s => s.PlanPositionId == positionId, ct))
            return this.ProblemWithCode(ApiErrors.PositionHasData, "This position already has practice/test data and cannot be deleted.");

        db.PlanPositions.Remove(pos);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
