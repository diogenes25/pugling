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
public class StudyPlansController(PuglingDbContext db, AuthAccess access, TimeProvider time) : ControllerBase
{
    /// <summary>In-memory projection of a tracked container, with the unit label and position count read separately.</summary>
    private static PlanResponse Map(StudyPlan p, string? unitLabel, int positionCount, DateOnly today) =>
        new(p.Id, p.ChildId, p.Title, p.SubjectId, p.StartDate, p.EndDate, p.Active, positionCount, p.Description,
            p.SeriesUnitId, unitLabel)
        {
            IsPlayable = p.Active && p.StartDate <= today && p.EndDate >= today,
        };

    /// <summary>DB projection incl. position count: EF translates <c>p.Positions.Count</c> into a COUNT subquery,
    /// without materializing the position rows. <paramref name="today"/> flows in as a parameter for the
    /// playability computation (the same run-time condition as the child's visibility).</summary>
    internal static Expression<Func<StudyPlan, PlanResponse>> ToResponse(DateOnly today) =>
        p => new PlanResponse(p.Id, p.ChildId, p.Title, p.SubjectId, p.StartDate, p.EndDate, p.Active, p.Positions.Count, p.Description,
            p.SeriesUnitId, p.SeriesUnit == null ? null : p.SeriesUnit.Label)
        {
            IsPlayable = p.Active && p.StartDate <= today && p.EndDate >= today,
        };

    /// <summary>List study plans. Child sees only its own, father only those of his children.</summary>
    /// <param name="childId">Optional filter on one child (supervisor only).</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Optional page size (0..500). Without it the whole list is returned; the total count is always in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<PlanResponse>> List(
        [FromQuery] int? childId = null, [FromQuery] int skip = 0, [FromQuery] int? take = null,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        IQueryable<StudyPlan> scoped = db.StudyPlans.AsNoTracking();
        if (User.IsStudent())
        {
            // The child only sees its playable plans (active + within their runtime, at most one per subject);
            // inactive/expired ones stay hidden so it cannot pick an easy plan to farm points.
            scoped = scoped.Where(p => p.ChildId == User.ChildId() && p.Active && p.StartDate <= today && p.EndDate >= today);
        }
        else
        {
            var fid = User.AdultId();
            scoped = scoped.Where(p => db.SupervisorLinks.Any(l => l.StudentId == p.ChildId && l.SupervisorId == fid));
            if (childId is not null) scoped = scoped.Where(p => p.ChildId == childId);
        }
        return await scoped.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id).Select(ToResponse(today))
            .ToOptionallyPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A study plan (own only).</summary>
    [HttpGet("{planId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Get(int planId, CancellationToken ct = default)
    {
        var plan = await db.StudyPlans.AsNoTracking().Where(p => p.Id == planId)
            .Select(ToResponse(DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime))).FirstOrDefaultAsync(ct);
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
        // Ownership check first: a uniform 404 for "does not exist" and "not my child".
        if (!await access.SupervisorOwnsChildAsync(User, dto.ChildId, ct)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
        UnitRef? unit = null;
        if (dto.SeriesUnitId is { } unitId)
        {
            unit = await FindUnitAsync(unitId, ct);
            if (unit is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Series unit not found.");
        }
        var (subjectId, subjectError) = ResolveSubject(dto.SubjectId, unit);
        if (subjectError is not null) return this.ProblemWithCode(ApiErrors.ValidationError, subjectError);
        if (subjectId is null) return this.ProblemWithCode(ApiErrors.ValidationError, SubjectRequired);

        var start = dto.StartDate ?? DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var duration = dto.DurationDays > 0 ? dto.DurationDays : 10;
        var plan = new StudyPlan
        {
            ChildId = dto.ChildId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            SubjectId = subjectId,
            SeriesUnitId = unit?.Id,
            StartDate = start,
            EndDate = start.AddDays(duration - 1),
            Active = dto.Active,
            // B-613: a plan counts as switched on since its start - a backdated plan keeps its past rounds (the positions
            // carry their own lower bound), and a plan created inactive gets today once it is switched on.
            ActivatedAt = start,
        };
        db.StudyPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        // Invariant "at most one active plan per subject and child": a new plan (active by default) replaces the
        // active plan of its subject. Creating one inactive is the opt-out for a plan that must be reviewed first,
        // and it deliberately leaves the running plan alone.
        if (plan.Active) await DeactivateSameSubjectPlansAsync(plan, ct);
        return CreatedAtAction(nameof(Get), new { planId = plan.Id },
            Map(plan, unit?.Label, 0, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)));
    }

    private const string SubjectRequired =
        "A subject is required: send subjectId or a seriesUnitId whose series has a subject.";

    /// <summary>A catalog unit as far as the plan needs it: its label and the subject of its series.</summary>
    private sealed record UnitRef(int Id, string Label, int? SubjectId);

    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles out.
    private Task<UnitRef?> FindUnitAsync(int unitId, CancellationToken ct) =>
        db.SeriesUnits.AsNoTracking().Where(u => u.Id == unitId)
            .Select(u => new UnitRef(u.Id, u.Label, u.Series!.SubjectId)).FirstOrDefaultAsync(ct);

    /// <summary>
    /// The plan's subject from the requested one and the unit: the unit's series fixes it, and a differing request is
    /// rejected - no English plan with a French unit. Without a unit subject the requested one stands, possibly
    /// <c>null</c>, which the caller judges.
    /// </summary>
    private static (int? SubjectId, string? Error) ResolveSubject(int? requested, UnitRef? unit)
    {
        if (unit?.SubjectId is not { } unitSubject) return (requested, null);
        if (requested is { } r && r != unitSubject)
            return (null, "subjectId does not match the subject of the unit's series.");
        return (unitSubject, null);
    }

    /// <summary>
    /// Enforces "at most one active plan per subject and child" (B-539): switches off the child's other active plans
    /// of the plan's subject. Plans of other subjects stay on - each subject runs in its own rhythm; within one
    /// subject the child cannot pick the easier of two plans (anti-cheating). Plans without a subject form one group.
    /// </summary>
    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles out.
    private Task DeactivateSameSubjectPlansAsync(StudyPlan plan, CancellationToken ct)
    {
        var (childId, keepPlanId, subjectId) = (plan.ChildId, plan.Id, plan.SubjectId);
        return db.StudyPlans
            .Where(p => p.ChildId == childId && p.Id != keepPlanId && p.Active && p.SubjectId == subjectId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Active, false), ct);
    }

    /// <summary>Changes the study plan container (partial, father/own only). <see cref="UpdatePlanDto.ChildId"/> assigns the plan to another own child.</summary>
    [HttpPatch("{planId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Update(int planId, UpdatePlanDto dto, CancellationToken ct = default)
    {
        // Only scalar fields change - the positions stay untouched and need not be loaded/tracked.
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return NotFound();

        // Reassigning to another child: only to a child of this supervisor (otherwise 404, as on create).
        if (dto.ChildId is { } newChildId && newChildId != plan.ChildId)
        {
            if (!await access.SupervisorOwnsChildAsync(User, newChildId, ct)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
            plan.ChildId = newChildId;
        }
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");

        // The unit after this change: cleared (the switch wins), newly set, or the one the plan already has.
        var newUnitId = dto.ClearSeriesUnit ? null : dto.SeriesUnitId;
        UnitRef? unit = null;
        if (!dto.ClearSeriesUnit && (newUnitId ?? plan.SeriesUnitId) is { } unitId)
        {
            unit = await FindUnitAsync(unitId, ct);
            if (unit is null && newUnitId is not null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Series unit not found.");
        }
        var subjectId = plan.SubjectId;
        // Consistency is judged only when the request touches subject or unit: an unrelated edit must not fail on a
        // catalog that moved under the plan. A newly set unit brings its subject along unless one is sent.
        if (dto.SubjectId is not null || newUnitId is not null)
        {
            var (resolved, subjectError) = ResolveSubject(dto.SubjectId ?? (newUnitId is null ? plan.SubjectId : null), unit);
            if (subjectError is not null) return this.ProblemWithCode(ApiErrors.ValidationError, subjectError);
            subjectId = resolved ?? plan.SubjectId;
        }
        // Switching off must never fail on a mandatory field: a request that only sets active:false is exempt.
        var onlySwitchesOff = dto == new UpdatePlanDto(null, null, null, null, false);
        if (subjectId is null && !onlySwitchesOff) return this.ProblemWithCode(ApiErrors.ValidationError, SubjectRequired);

        var startMoved = dto.StartDate is { } movedTo && movedTo != plan.StartDate;
        var oldStart = plan.StartDate;
        var oldEnd = plan.EndDate;
        if (dto.Title is not null && dto.Title.Trim().Length > 0) plan.Title = dto.Title.Trim();
        if (dto.Description is not null) plan.Description = dto.Description.Trim() is { Length: > 0 } d ? d : null;
        plan.SubjectId = subjectId;
        plan.SeriesUnitId = unit?.Id;
        if (dto.StartDate is not null) plan.StartDate = dto.StartDate.Value;
        if (dto.EndDate is not null) plan.EndDate = dto.EndDate.Value;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        // B-613: only the switch from off to on moves the date; a repeated "active: true" must not shift it.
        if (dto.Active == true && !plan.Active) plan.ActivatedAt = today;
        if (dto.Active is not null) plan.Active = dto.Active.Value;
        // A moved start changes the first day the child can play an active plan: the new start, but never before today
        // (days before it were not playable then, so they stay in the child's favour). Without this, a future plan pulled
        // forward to today would keep its old start as switch-on day and owe nothing until then.
        if (startMoved && plan.Active)
        {
            // B-672: under the old start the plan was playable only from min(old start, today). A start moved back into
            // the past must not turn the days before that into duties, so the switch-on day first rises to it.
            var playableBefore = oldStart < today ? oldStart : today;
            if (playableBefore > plan.ActivatedAt) plan.ActivatedAt = playableBefore;
            var playableFrom = plan.StartDate > today ? plan.StartDate : today;
            if (playableFrom < plan.ActivatedAt) plan.ActivatedAt = playableFrom;
        }
        // B-783: an expired plan extended later was not playable between its old end and today. Like a switch back on,
        // it then counts from today; an end of yesterday leaves no such gap and changes nothing. An end first moved into the
        // past and then extended again frees the days in between as well - only in the child's favour, and only by the father.
        if (plan.Active && plan.EndDate > oldEnd && oldEnd.AddDays(1) < today)
            plan.ActivatedAt = today;
        await db.SaveChangesAsync(ct);
        // After activating, moving to another child or changing the subject, restore "one active plan per subject".
        if (plan.Active) await DeactivateSameSubjectPlansAsync(plan, ct);
        var positionCount = await db.PlanPositions.CountAsync(pp => pp.StudyPlanId == planId, ct);
        return Map(plan, unit?.Label, positionCount, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime));
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
