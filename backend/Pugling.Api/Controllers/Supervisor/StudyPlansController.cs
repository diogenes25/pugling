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
/// Study plans = containers of catalog exercises (<see cref="PlanPosition"/>). This controller only manages
/// the container (title, child, run time, active). Exercises/goals/points run through the
/// <see cref="PlanPositionsController"/>, daily mission/history through the <see cref="PlanOverviewController"/>.
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
    /// <summary>In-memory projection for freshly created containers (positions still empty).</summary>
    private static PlanResponse Map(StudyPlan p, DateOnly today) =>
        new(p.Id, p.ChildId, p.Title, p.SubjectId, p.StartDate, p.EndDate, p.Active, p.Positions.Count, p.Description)
        {
            IsPlayable = p.Active && p.StartDate <= today && p.EndDate >= today,
        };

    /// <summary>DB projection incl. position count: EF translates <c>p.Positions.Count</c> into a COUNT subquery,
    /// without materializing the position rows. <paramref name="today"/> flows in as a parameter for the
    /// playability computation (the same run-time condition as the child's visibility).</summary>
    internal static Expression<Func<StudyPlan, PlanResponse>> ToResponse(DateOnly today) =>
        p => new PlanResponse(p.Id, p.ChildId, p.Title, p.SubjectId, p.StartDate, p.EndDate, p.Active, p.Positions.Count, p.Description)
        {
            IsPlayable = p.Active && p.StartDate <= today && p.EndDate >= today,
        };

    /// <summary>List study plans. Child sees only its own, father only those of his children.</summary>
    [HttpGet]
    public async Task<IEnumerable<PlanResponse>> List([FromQuery] int? childId = null, CancellationToken ct = default)
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
            var fid = User.AdultId();
            scoped = scoped.Where(p => db.SupervisorLinks.Any(l => l.StudentId == p.ChildId && l.SupervisorId == fid));
            if (childId is not null) scoped = scoped.Where(p => p.ChildId == childId);
        }
        return await scoped.OrderByDescending(p => p.CreatedAt).Select(ToResponse(today)).ToListAsync(ct);
    }

    /// <summary>A study plan (own only).</summary>
    [HttpGet("{planId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Get(int planId, CancellationToken ct = default)
    {
        var plan = await db.StudyPlans.AsNoTracking().Where(p => p.Id == planId)
            .Select(ToResponse(DateOnly.FromDateTime(DateTime.UtcNow))).FirstOrDefaultAsync(ct);
        return plan is null ? NotFound() : plan;
    }

    /// <summary>Creates an empty study plan container (father only, own children only).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Create(CreatePlanDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        // Eigentums-Prüfung zuerst: einheitlich 404 für "existiert nicht" und "nicht mein Kind".
        if (!await access.SupervisorOwnsChildAsync(User, dto.ChildId, ct)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");

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
        await db.SaveChangesAsync(ct);
        // Invariante „höchstens ein aktiver Plan je Kind": ein neuer (per Default aktiver) Plan wird zum
        // einzig spielbaren – die bisherigen des Kindes werden stillgelegt.
        if (plan.Active) await DeactivateSiblingPlansAsync(plan.ChildId, plan.Id, ct);
        return CreatedAtAction(nameof(Get), new { planId = plan.Id }, Map(plan, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    /// <summary>
    /// Enforces "at most one active plan per child": deactivates all other plans of the child.
    /// This way the child cannot choose the easiest among several active plans (anti-cheating).
    /// </summary>
    // Kein Vorgabewert für `ct`: er ließe die Aufrufstelle korrekt aussehen, während der Abbruch des
    // Clients verpufft.
    private Task DeactivateSiblingPlansAsync(int childId, int keepPlanId, CancellationToken ct) =>
        db.StudyPlans.Where(p => p.ChildId == childId && p.Id != keepPlanId && p.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Active, false), ct);

    /// <summary>Changes the study plan container (partial, father/own only). <see cref="UpdatePlanDto.ChildId"/> assigns the plan to another own child.</summary>
    [HttpPatch("{planId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Update(int planId, UpdatePlanDto dto, CancellationToken ct = default)
    {
        // Nur Skalarfelder werden geändert – die Positionen bleiben unangetastet und müssen nicht geladen/getrackt werden.
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return NotFound();

        // Umzuweisung an ein anderes Kind: nur an ein eigenes Kind des Vaters (sonst 404, wie beim Anlegen).
        if (dto.ChildId is { } newChildId && newChildId != plan.ChildId)
        {
            if (!await access.SupervisorOwnsChildAsync(User, newChildId, ct)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
            plan.ChildId = newChildId;
        }
        if (dto.Title is not null && dto.Title.Trim().Length > 0) plan.Title = dto.Title.Trim();
        if (dto.Description is not null) plan.Description = dto.Description.Trim() is { Length: > 0 } d ? d : null;
        if (dto.SubjectId is { } sid)
        {
            if (!await db.Subjects.AnyAsync(s => s.Id == sid, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
            plan.SubjectId = sid;
        }
        if (dto.StartDate is not null) plan.StartDate = dto.StartDate.Value;
        if (dto.EndDate is not null) plan.EndDate = dto.EndDate.Value;
        if (dto.Active is not null) plan.Active = dto.Active.Value;
        await db.SaveChangesAsync(ct);
        // Nach Aktivierung oder Umzug die Invariante „ein aktiver Plan je Kind" wiederherstellen.
        if (plan.Active) await DeactivateSiblingPlansAsync(plan.ChildId, plan.Id, ct);
        var positionCount = await db.PlanPositions.CountAsync(pp => pp.StudyPlanId == planId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new PlanResponse(plan.Id, plan.ChildId, plan.Title, plan.SubjectId,
            plan.StartDate, plan.EndDate, plan.Active, positionCount, plan.Description)
        {
            IsPlayable = plan.Active && plan.StartDate <= today && plan.EndDate >= today,
        };
    }

    /// <summary>
    /// Deletes an entire study plan (father/own only). Removes its positions via cascade incl.
    /// progress, practice sessions, test attempts and goal rewards. The referenced catalog exercises
    /// remain untouched (they belong to the child-neutral catalog, not the plan).
    /// </summary>
    [HttpDelete("{planId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int planId, CancellationToken ct = default)
    {
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return NotFound();
        db.StudyPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
