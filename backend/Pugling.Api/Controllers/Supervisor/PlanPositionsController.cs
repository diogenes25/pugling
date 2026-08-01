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
public class PlanPositionsController(PuglingDbContext db, ExercisePermissionService perms, ExerciseTypeRegistry types) : ControllerBase
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

    private static PositionResponse Map(PlanPosition p) =>
        new(p.Id, p.StudyPlanId, p.ExerciseId, p.Exercise?.Title ?? "", p.Exercise?.Type.ToString() ?? "",
            p.Order, p.Stage, p.ItemCount, p.Scope, p.Cadence, p.OrderStrategy, p.GoalThreshold, p.RequireTypedTest,
            p.UseLeitner, p.MaxBox, p.BoxIntervalDays, p.StageSchedule, p.PointsGoalMet, p.PenaltyCoins, p.NewContentPoints,
            p.ComboThreshold, p.ComboBonusPoints, p.SpeedThresholdSeconds, p.SpeedBonusPoints);

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
        if (ThresholdProblem(dto.GoalThreshold) is { } problem)
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

        var order = dto.Order ?? ((await db.PlanPositions.Where(p => p.StudyPlanId == planId)
            .MaxAsync(p => (int?)p.Order, ct)) ?? -1) + 1;
        var sb = exercise.SuggestedBonus;

        var pos = new PlanPosition
        {
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
            BoxIntervalDays = dto.BoxIntervalDays,
            StageSchedule = dto.StageSchedule,
            // Points/bonus: position override → exercise suggestion → model default.
            PointsGoalMet = dto.PointsGoalMet ?? 20,
            // The penalty is opt-in per position (default 0 = reward only, the previous behavior).
            PenaltyCoins = dto.PenaltyCoins ?? 0,
            NewContentPoints = dto.NewContentPoints ?? sb?.NewContentPoints ?? 10,
            ComboThreshold = dto.ComboThreshold ?? sb?.ComboThreshold ?? 5,
            ComboBonusPoints = dto.ComboBonusPoints ?? sb?.ComboBonusPoints ?? 5,
            SpeedThresholdSeconds = dto.SpeedThresholdSeconds ?? sb?.SpeedThresholdSeconds ?? 0,
            SpeedBonusPoints = dto.SpeedBonusPoints ?? sb?.SpeedBonusPoints ?? 0,
        };
        db.PlanPositions.Add(pos);
        await db.SaveChangesAsync(ct);

        pos.Exercise = exercise;
        return CreatedAtAction(nameof(Get), new { planId, positionId = pos.Id }, Map(pos));
    }

    /// <summary>Changes a position (partial). Only fields that are set are changed.</summary>
    [HttpPatch("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> Update(int planId, int positionId, UpdatePositionDto dto, CancellationToken ct = default)
    {
        if (ThresholdProblem(dto.GoalThreshold) is { } problem)
            return this.ProblemWithCode(ApiErrors.ValidationError, problem);

        var pos = await FindAsync(planId, positionId, ct);
        if (pos is null) return NotFound();

        if (dto.Order is not null) pos.Order = dto.Order.Value;
        if (dto.Stage is not null) pos.Stage = dto.Stage;
        if (dto.ItemCount is not null) pos.ItemCount = dto.ItemCount;
        if (dto.Scope is not null) pos.Scope = dto.Scope.Value;
        if (dto.Cadence is not null) pos.Cadence = dto.Cadence.Value;
        if (dto.OrderStrategy is not null) pos.OrderStrategy = dto.OrderStrategy.Value;
        if (dto.GoalThreshold is not null) pos.GoalThreshold = dto.GoalThreshold;
        if (dto.RequireTypedTest is not null) pos.RequireTypedTest = dto.RequireTypedTest.Value;
        if (dto.UseLeitner is not null) pos.UseLeitner = dto.UseLeitner.Value;
        if (dto.MaxBox is > 0) pos.MaxBox = dto.MaxBox.Value;
        if (dto.BoxIntervalDays is not null) pos.BoxIntervalDays = dto.BoxIntervalDays;
        if (dto.StageSchedule is not null) pos.StageSchedule = dto.StageSchedule;
        if (dto.PointsGoalMet is not null) pos.PointsGoalMet = dto.PointsGoalMet.Value;
        if (dto.PenaltyCoins is not null) pos.PenaltyCoins = dto.PenaltyCoins.Value;
        if (dto.NewContentPoints is not null) pos.NewContentPoints = dto.NewContentPoints.Value;
        if (dto.ComboThreshold is not null) pos.ComboThreshold = dto.ComboThreshold.Value;
        if (dto.ComboBonusPoints is not null) pos.ComboBonusPoints = dto.ComboBonusPoints.Value;
        if (dto.SpeedThresholdSeconds is not null) pos.SpeedThresholdSeconds = dto.SpeedThresholdSeconds.Value;
        if (dto.SpeedBonusPoints is not null) pos.SpeedBonusPoints = dto.SpeedBonusPoints.Value;

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
