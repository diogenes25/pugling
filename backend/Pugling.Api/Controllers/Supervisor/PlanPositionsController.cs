using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Positions-CRUD des neuen Lehrplan-Modells: der Vater stellt einen Lehrplan aus <b>globalen</b>
/// Katalog-Übungen zusammen. Jede <see cref="PlanPosition"/> verweist auf eine <see cref="Exercise"/>
/// (der Inhalt bleibt dort – keine Kopie) und trägt ihre eigenen Overrides (Stufe/Menge/Umfang),
/// Ziele (Rhythmus + Schwelle), Punkte und Leitner-Einstellungen. Gespielt werden die Positionen über
/// den <see cref="PositionPracticeController"/> bzw. <see cref="PositionTestsController"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/study-plans/{planId:int}/positions")]
[Tags("Supervisor – Plan Positions")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PlanPositionsController(PuglingDbContext db) : ControllerBase
{
    public record PositionResponse(int Id, int StudyPlanId, int ExerciseId, string ExerciseTitle,
        string ExerciseType, int Order, int? Stage, int? ItemCount, ItemScope Scope, GoalCadence Cadence,
        PracticeOrder OrderStrategy, int? GoalThreshold, bool RequireTypedTest, bool UseLeitner, int MaxBox,
        List<int>? BoxIntervalDays, List<StageStep>? StageSchedule, int PointsGoalMet, int PenaltyCoins,
        int NewContentPoints, int ComboThreshold, int ComboBonusPoints, int SpeedThresholdSeconds, int SpeedBonusPoints);

    private static PositionResponse Map(PlanPosition p) =>
        new(p.Id, p.StudyPlanId, p.ExerciseId, p.Exercise?.Title ?? "", p.Exercise?.Type.ToString() ?? "",
            p.Order, p.Stage, p.ItemCount, p.Scope, p.Cadence, p.OrderStrategy, p.GoalThreshold, p.RequireTypedTest,
            p.UseLeitner, p.MaxBox, p.BoxIntervalDays, p.StageSchedule, p.PointsGoalMet, p.PenaltyCoins, p.NewContentPoints,
            p.ComboThreshold, p.ComboBonusPoints, p.SpeedThresholdSeconds, p.SpeedBonusPoints);

    /// <summary>Alle Positionen des Lehrplans in ihrer Reihenfolge.</summary>
    [HttpGet]
    public async Task<IEnumerable<PositionResponse>> List(int planId)
    {
        var positions = await db.PlanPositions.AsNoTracking().Include(p => p.Exercise)
            .Where(p => p.StudyPlanId == planId)
            .OrderBy(p => p.Order).ThenBy(p => p.Id)
            .ToListAsync();
        return positions.Select(Map);
    }

    private Task<PlanPosition?> FindAsync(int planId, int positionId) =>
        db.PlanPositions.Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId);

    /// <summary>Eine einzelne Position.</summary>
    [HttpGet("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> Get(int planId, int positionId)
    {
        var pos = await FindAsync(planId, positionId);
        return pos is null ? NotFound() : Map(pos);
    }

    /// <summary>
    /// Anlegen einer Position. Leere Override-Felder erben den Vorschlag der Übung (Hybrid-Prinzip):
    /// <see cref="PlanPosition.Stage"/>/<see cref="PlanPosition.ItemCount"/> bleiben dann <c>null</c> und
    /// werden erst beim Spielen aus <see cref="Exercise.DefaultStage"/>/<see cref="Exercise.DefaultItemCount"/>
    /// aufgelöst; die Punkte-/Bonus-Felder werden aus <see cref="Exercise.SuggestedBonus"/> vorbelegt.
    /// </summary>
    public record CreatePositionDto(int ExerciseId, int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
        GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
        bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
        int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
        int? SpeedThresholdSeconds, int? SpeedBonusPoints);

    /// <summary>Fügt dem Lehrplan eine Position auf eine Katalog-Übung hinzu.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PositionResponse>> Create(int planId, CreatePositionDto dto)
    {
        var exercise = await db.Exercises.FirstOrDefaultAsync(e => e.Id == dto.ExerciseId);
        if (exercise is null) return this.ProblemWithCode(ApiErrors.InvalidReference, $"Exercise {dto.ExerciseId} not found.");

        var order = dto.Order ?? ((await db.PlanPositions.Where(p => p.StudyPlanId == planId)
            .MaxAsync(p => (int?)p.Order)) ?? -1) + 1;
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
            // Leitner/getippt erben ihren Standard von der Übung (Hybrid-Prinzip), solange die Position nichts vorgibt.
            RequireTypedTest = dto.RequireTypedTest ?? exercise.DefaultRequireTypedTest,
            UseLeitner = dto.UseLeitner ?? exercise.DefaultUseLeitner,
            MaxBox = dto.MaxBox is > 0 ? dto.MaxBox.Value : 5,
            BoxIntervalDays = dto.BoxIntervalDays,
            StageSchedule = dto.StageSchedule,
            // Punkte/Bonus: Position-Override → Übungs-Vorschlag → Modell-Default.
            PointsGoalMet = dto.PointsGoalMet ?? 20,
            // Malus ist opt-in pro Position (Default 0 = reine Belohnung, bisheriges Verhalten).
            PenaltyCoins = dto.PenaltyCoins ?? 0,
            NewContentPoints = dto.NewContentPoints ?? sb?.NewContentPoints ?? 10,
            ComboThreshold = dto.ComboThreshold ?? sb?.ComboThreshold ?? 5,
            ComboBonusPoints = dto.ComboBonusPoints ?? sb?.ComboBonusPoints ?? 5,
            SpeedThresholdSeconds = dto.SpeedThresholdSeconds ?? sb?.SpeedThresholdSeconds ?? 0,
            SpeedBonusPoints = dto.SpeedBonusPoints ?? sb?.SpeedBonusPoints ?? 0,
        };
        db.PlanPositions.Add(pos);
        await db.SaveChangesAsync();

        pos.Exercise = exercise;
        return CreatedAtAction(nameof(Get), new { planId, positionId = pos.Id }, Map(pos));
    }

    /// <summary>Partielle Änderung der Overrides/Ziele/Punkte. Die referenzierte Übung ist unveränderlich (Fortschritts-Indizes).</summary>
    public record UpdatePositionDto(int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
        GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
        bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
        int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
        int? SpeedThresholdSeconds, int? SpeedBonusPoints);

    /// <summary>Ändert eine Position (partiell). Setzt nur die angegebenen Felder.</summary>
    [HttpPatch("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> Update(int planId, int positionId, UpdatePositionDto dto)
    {
        var pos = await FindAsync(planId, positionId);
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

        await db.SaveChangesAsync();
        return Map(pos);
    }

    /// <summary>
    /// Löscht eine Position (der zugehörige <see cref="PositionItemProgress"/> verschwindet per Cascade mit).
    /// Nicht möglich, solange bereits Testversuche für die Position vorliegen – sonst ginge diese Lernhistorie
    /// verloren (der Fremdschlüssel wäre sonst nur auf <c>null</c> gesetzt).
    /// </summary>
    [HttpDelete("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int planId, int positionId)
    {
        var pos = await FindAsync(planId, positionId);
        if (pos is null) return NotFound();

        if (await db.TestAttempts.AnyAsync(t => t.PlanPositionId == positionId)
            || await db.PracticeSessions.AnyAsync(s => s.PlanPositionId == positionId))
            return this.ProblemWithCode(ApiErrors.PositionHasData, "This position already has practice/test data and cannot be deleted.");

        db.PlanPositions.Remove(pos);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
