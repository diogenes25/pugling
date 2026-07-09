using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Lehrpläne = Container aus Katalog-Übungen (<see cref="PlanPosition"/>). Dieser Controller verwaltet nur
/// den Container (Titel, Kind, Laufzeit, aktiv). Übungen/Ziele/Punkte laufen über den
/// <see cref="PlanPositionsController"/>, Tagesmission/Verlauf über den <see cref="PlanOverviewController"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/study-plans")]
[Tags("Supervisor – Plans")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class StudyPlansController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    public record PlanResponse(int Id, int ChildId, string Title, int? SubjectId,
        DateOnly StartDate, DateOnly EndDate, bool Active, int PositionCount, string? Description)
    {
        /// <summary>
        /// Server-autoritative Affordance: Ob dies der eine, aktuell spielbare Plan des Kindes ist
        /// (aktiv <b>und</b> heute in Laufzeit). Für den Sohn ist stets nur dieser sichtbar; dem Vater
        /// zeigt es unter mehreren Plänen den, den der Sohn gerade spielen kann – ohne die Regel im Client nachzubilden.
        /// </summary>
        public bool IsPlayable { get; init; }
    }

    /// <summary>In-Memory-Projektion für frisch erstellte Container (Positionen noch leer).</summary>
    private static PlanResponse Map(StudyPlan p, DateOnly today) =>
        new(p.Id, p.ChildId, p.Title, p.SubjectId, p.StartDate, p.EndDate, p.Active, p.Positions.Count, p.Description)
        {
            IsPlayable = p.Active && p.StartDate <= today && p.EndDate >= today,
        };

    /// <summary>DB-Projektion inkl. Positions-Anzahl: EF übersetzt <c>p.Positions.Count</c> in eine COUNT-Subquery,
    /// ohne die Positions-Zeilen zu materialisieren. <paramref name="today"/> fließt als Parameter in die
    /// Spielbarkeits-Berechnung ein (dieselbe Laufzeit-Bedingung wie die Sohn-Sichtbarkeit).</summary>
    private static Expression<Func<StudyPlan, PlanResponse>> ToResponse(DateOnly today) =>
        p => new PlanResponse(p.Id, p.ChildId, p.Title, p.SubjectId, p.StartDate, p.EndDate, p.Active, p.Positions.Count, p.Description)
        {
            IsPlayable = p.Active && p.StartDate <= today && p.EndDate >= today,
        };

    /// <summary>Lehrpläne auflisten. Sohn sieht nur eigene, Vater nur die seiner Kinder.</summary>
    [HttpGet]
    public async Task<IEnumerable<PlanResponse>> List([FromQuery] int? childId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        IQueryable<StudyPlan> scoped = db.StudyPlans.AsNoTracking();
        if (User.IsStudent())
        {
            // Der Sohn sieht nur seinen einen spielbaren Plan (aktiv + in Laufzeit); inaktive/abgelaufene
            // bleiben verborgen, damit er sich keinen leichten Plan zum Punktesammeln aussuchen kann.
            scoped = scoped.Where(p => p.ChildId == User.ChildId() && p.Active && p.StartDate <= today && p.EndDate >= today);
        }
        else
        {
            var fid = User.FatherId();
            scoped = scoped.Where(p => db.SupervisorLinks.Any(l => l.StudentId == p.ChildId && l.SupervisorId == fid));
            if (childId is not null) scoped = scoped.Where(p => p.ChildId == childId);
        }
        return await scoped.OrderByDescending(p => p.CreatedAt).Select(ToResponse(today)).ToListAsync();
    }

    /// <summary>Ein Lehrplan (nur eigener).</summary>
    [HttpGet("{planId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Get(int planId)
    {
        var plan = await db.StudyPlans.AsNoTracking().Where(p => p.Id == planId)
            .Select(ToResponse(DateOnly.FromDateTime(DateTime.UtcNow))).FirstOrDefaultAsync();
        return plan is null ? NotFound() : plan;
    }

    public record CreatePlanDto(int ChildId, string Title, int? SubjectId, DateOnly? StartDate, int DurationDays,
        string? Description = null);

    /// <summary>Erstellt einen leeren Lehrplan-Container (nur Vater, nur für eigene Kinder).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Create(CreatePlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        // Eigentums-Prüfung zuerst: einheitlich 404 für "existiert nicht" und "nicht mein Kind".
        if (!await access.FatherOwnsChildAsync(User, dto.ChildId)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");

        var start = dto.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var duration = dto.DurationDays > 0 ? dto.DurationDays : 10;
        var plan = new StudyPlan
        {
            ChildId = dto.ChildId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            SubjectId = dto.SubjectId,
            StartDate = start,
            EndDate = start.AddDays(duration - 1),
        };
        db.StudyPlans.Add(plan);
        await db.SaveChangesAsync();
        // Invariante „höchstens ein aktiver Plan je Kind": ein neuer (per Default aktiver) Plan wird zum
        // einzig spielbaren – die bisherigen des Kindes werden stillgelegt.
        if (plan.Active) await DeactivateSiblingPlansAsync(plan.ChildId, plan.Id);
        return CreatedAtAction(nameof(Get), new { planId = plan.Id }, Map(plan, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    /// <summary>
    /// Erzwingt „höchstens ein aktiver Plan je Kind": deaktiviert alle anderen Pläne des Kindes.
    /// So kann der Sohn nicht zwischen mehreren aktiven Plänen den leichtesten wählen (Anti-Schummel).
    /// </summary>
    private Task DeactivateSiblingPlansAsync(int childId, int keepPlanId) =>
        db.StudyPlans.Where(p => p.ChildId == childId && p.Id != keepPlanId && p.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Active, false));

    public record UpdatePlanDto(string? Title, int? SubjectId, DateOnly? StartDate, DateOnly? EndDate, bool? Active,
        string? Description = null, int? ChildId = null);

    /// <summary>Ändert den Lehrplan-Container (partiell, nur Vater/eigener). <see cref="UpdatePlanDto.ChildId"/> weist den Plan einem anderen eigenen Kind zu.</summary>
    [HttpPatch("{planId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Update(int planId, UpdatePlanDto dto)
    {
        // Nur Skalarfelder werden geändert – die Positionen bleiben unangetastet und müssen nicht geladen/getrackt werden.
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();

        // Umzuweisung an ein anderes Kind: nur an ein eigenes Kind des Vaters (sonst 404, wie beim Anlegen).
        if (dto.ChildId is { } newChildId && newChildId != plan.ChildId)
        {
            if (!await access.FatherOwnsChildAsync(User, newChildId)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
            plan.ChildId = newChildId;
        }
        if (dto.Title is not null && dto.Title.Trim().Length > 0) plan.Title = dto.Title.Trim();
        if (dto.Description is not null) plan.Description = dto.Description.Trim() is { Length: > 0 } d ? d : null;
        if (dto.SubjectId is { } sid)
        {
            if (!await db.Subjects.AnyAsync(s => s.Id == sid)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
            plan.SubjectId = sid;
        }
        if (dto.StartDate is not null) plan.StartDate = dto.StartDate.Value;
        if (dto.EndDate is not null) plan.EndDate = dto.EndDate.Value;
        if (dto.Active is not null) plan.Active = dto.Active.Value;
        await db.SaveChangesAsync();
        // Nach Aktivierung oder Umzug die Invariante „ein aktiver Plan je Kind" wiederherstellen.
        if (plan.Active) await DeactivateSiblingPlansAsync(plan.ChildId, plan.Id);
        var positionCount = await db.PlanPositions.CountAsync(pp => pp.StudyPlanId == planId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new PlanResponse(plan.Id, plan.ChildId, plan.Title, plan.SubjectId,
            plan.StartDate, plan.EndDate, plan.Active, positionCount, plan.Description)
        {
            IsPlayable = plan.Active && plan.StartDate <= today && plan.EndDate >= today,
        };
    }

    /// <summary>
    /// Löscht einen ganzen Lehrplan (nur Vater/eigener). Entfernt per Kaskade seine Positionen inkl.
    /// Fortschritt, Übungssitzungen, Testversuche und Ziel-Belohnungen. Die referenzierten Katalog-Übungen
    /// bleiben unberührt (sie gehören dem kindneutralen Katalog, nicht dem Plan).
    /// </summary>
    [HttpDelete("{planId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int planId)
    {
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        db.StudyPlans.Remove(plan);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
