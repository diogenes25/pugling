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
    TimeProvider time, PositionProgressService progress) : ControllerBase
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

    private PositionResponse Map(PlanPosition p) =>
        new(p.Id, p.StudyPlanId, p.ExerciseId, p.Exercise?.Title ?? "", p.Exercise?.Type.ToString() ?? "",
            p.Order, p.Stage, p.ItemCount, p.Scope, p.Cadence, p.OrderStrategy, p.GoalThreshold, p.RequireTypedTest,
            p.UseLeitner, p.MaxBox, p.BoxIntervalDays, p.StageSchedule, p.PointsGoalMet, p.PenaltyCoins, p.NewContentPoints,
            p.ComboThreshold, p.ComboBonusPoints, p.SpeedThresholdSeconds, p.SpeedBonusPoints, p.TimeSlots,
            p.NewItemsPerDay, PositionPlayService.DailyDose(p, types),
            types.ByKey(p.Exercise?.Type ?? "") is { } type ? PositionPlayService.LearningCheckOf(p, type) : null,
            p.ForgivingDemotion, p.Remediation, p.CompletionBonus, p.CompletedOn);

    /// <summary>All positions of the study plan in their order.</summary>
    [HttpGet]
    public async Task<IEnumerable<PositionResponse>> List(int planId, CancellationToken ct = default)
    {
        var positions = await db.PlanPositions.AsNoTracking().Include(p => p.Exercise)
            .Where(p => p.StudyPlanId == planId)
            .OrderBy(p => p.Order).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return positions.Select(Map);
    }

    // No default for `ct` (as in IsUnfilledAsync): it would make the call site look correct while the
    // client's cancellation fizzles out.
    private Task<PlanPosition?> FindAsync(int planId, int positionId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);

    /*
     * Die Ziel-Schwelle ist ein PROZENTWERT (siehe PlanPosition.GoalThreshold). Ohne diese Prüfung ist eine
     * Verwechslung mit einer Trefferzahl lautlos und wirkt genau falsch: „3" nimmt der Pflicht die Zähne,
     * statt sie zu verschärfen (jeder Versuch über 3 % gilt als bestanden). Genau das stand einmal im Seed.
     * `null` bleibt der Weg, „Standard" zu sagen – 0 wäre eine zweite Schreibweise dafür.
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
    // with an "easier" button that can never work.
    private static string? RemediationProblem(IExerciseType? type) =>
        type is null || !type.SupportsRemediation
            ? "Remediation needs an exercise type that can be made easier step by step (currently: cloze)."
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

        var exercise = await db.Exercises.FirstOrDefaultAsync(e => e.Id == dto.ExerciseId, ct);
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
        if (dto.Remediation == true && RemediationProblem(createType) is { } remediationProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, remediationProblem);
        if (UntilDoneProblem(dto.Cadence ?? GoalCadence.None, dto.Remediation ?? false, dto.CompletionBonus) is { } untilDoneProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, untilDoneProblem);

        var order = dto.Order ?? ((await db.PlanPositions.Where(p => p.StudyPlanId == planId)
            .MaxAsync(p => (int?)p.Order, ct)) ?? -1) + 1;
        var sb = exercise.SuggestedBonus;

        var createdAt = time.GetUtcNow().UtcDateTime;
        var pos = new PlanPosition
        {
            // The same clock as the day logic that reads it (B-476: a word's ladder counts from the day its position
            // joined the plan); the entity's wall-clock default would put the two on different calendars under test.
            CreatedAt = createdAt,
            DutySince = DateOnly.FromDateTime(createdAt),
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
        if (dto.Remediation == true && !pos.Remediation && RemediationProblem(types.ByKey(exercise.Type)) is { } remediationProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, remediationProblem);
        // Judged on the state after the patch: switching remediation off must take "until done" with it.
        if (UntilDoneProblem(dto.Cadence ?? pos.Cadence, dto.Remediation ?? pos.Remediation, dto.CompletionBonus) is { } untilDoneProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, untilDoneProblem);
        if ((DoseNeedsLeitnerProblem(dto.NewItemsPerDay ?? pos.NewItemsPerDay, dto.UseLeitner ?? pos.UseLeitner)
                ?? (dto.NewItemsPerDay is { } dose && dose != pos.NewItemsPerDay ? DoseOnSetProblem(exercise, dose) : null))
            is { } doseProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, doseProblem);

        if (dto.Order is not null) pos.Order = dto.Order.Value;
        if (dto.Stage is not null) pos.Stage = dto.Stage;
        // Value first, switch second (B-16): "back to the default" wins if a form sends both.
        if (dto.ClearStage) pos.Stage = null;
        if (dto.ItemCount is not null) pos.ItemCount = dto.ItemCount;
        if (dto.ClearItemCount) pos.ItemCount = null;
        if (dto.Scope is not null) pos.Scope = dto.Scope.Value;
        // B-613: a free position that becomes a duty counts from today; a mere save or Daily <-> Weekly keeps the date.
        if (dto.Cadence is { } cadence && cadence != GoalCadence.None && pos.Cadence == GoalCadence.None)
            pos.DutySince = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (dto.Cadence is not null) pos.Cadence = dto.Cadence.Value;
        if (dto.OrderStrategy is not null) pos.OrderStrategy = dto.OrderStrategy.Value;
        if (dto.GoalThreshold is not null) pos.GoalThreshold = dto.GoalThreshold;
        if (dto.ClearGoalThreshold) pos.GoalThreshold = null;
        if (dto.RequireTypedTest is not null) pos.RequireTypedTest = dto.RequireTypedTest.Value;
        if (dto.UseLeitner is not null) pos.UseLeitner = dto.UseLeitner.Value;
        if (dto.MaxBox is > 0) pos.MaxBox = dto.MaxBox.Value;
        if (dto.ForgivingDemotion is not null) pos.ForgivingDemotion = dto.ForgivingDemotion.Value;
        if (dto.Remediation is not null) pos.Remediation = dto.Remediation.Value;
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
        // B-636: a remediation already finished when it becomes "until done" ends at once, or it never could.
        await progress.StampFinishedRemediationAsync(pos, ct);

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
