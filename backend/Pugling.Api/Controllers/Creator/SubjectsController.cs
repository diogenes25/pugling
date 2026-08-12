using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// School subjects in the shared study plan catalog. Ownership as with the textbook series: <b>any creator
/// may read and use</b> every subject, only the owner may rename or delete it (B-13). Creating stays open to
/// everyone – a creator must be able to open a subject without waiting for a clearance.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/subjects")]
[Tags("Creator – Subjects")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class SubjectsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// Projection with the ownership flag. Written out <b>inline</b> instead of calling
    /// <see cref="ClaimsPrincipalExtensions.IsOwnedBy"/>: EF would have to translate the method call and
    /// would break at runtime. Missing <c>fid</c> ⇒ <c>false</c> (fail-closed, same rule as there) – and so
    /// is a subject without an owner, which is why the null check sits on the caller's id, not on the row's.
    /// </summary>
    private static IQueryable<SubjectResponse> Project(IQueryable<Subject> q, int? fid) =>
        q.Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Categories.Count,
            s.OwnerAdultId, fid != null && s.OwnerAdultId == fid));

    /// <summary>List of all subjects.</summary>
    [HttpGet]
    public async Task<IEnumerable<SubjectResponse>> List(CancellationToken ct = default) =>
        await Project(db.Subjects.AsNoTracking().OrderBy(s => s.Name), User.CreatorId()).ToListAsync(ct);

    /// <summary>A single subject.</summary>
    [HttpGet("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Get(int subjectId, CancellationToken ct = default)
    {
        var subject = await Project(db.Subjects.AsNoTracking().Where(s => s.Id == subjectId), User.CreatorId())
            .FirstOrDefaultAsync(ct);
        return subject is null ? NotFound() : subject;
    }

    /// <summary>Creates a subject; the calling creator becomes its owner.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectResponse>> Create(CreateSubjectDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var fid = User.CreatorId();
        // The owner comes from the token, never from the payload - otherwise a creator could hand a subject
        // to somebody else (or to nobody, which would make it permanently uneditable).
        var subject = new Subject { Name = dto.Name.Trim(), OwnerAdultId = fid };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);

        var response = new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt, 0,
            subject.OwnerAdultId, ClaimsPrincipalExtensions.IsOwnedBy(subject.OwnerAdultId, fid));
        return CreatedAtAction(nameof(Get), new { subjectId = subject.Id }, response);
    }

    /// <summary>Changes a subject (partial, owner only).</summary>
    [HttpPatch("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(int subjectId, UpdateSubjectDto dto, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId, ct);
        if (subject is null) return NotFound();
        var fid = User.CreatorId();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(subject.OwnerAdultId, fid))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may change this subject.");

        if (dto.Name is not null) subject.Name = dto.Name.Trim();
        await db.SaveChangesAsync(ct);

        return new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt,
            await db.ExerciseCategories.CountAsync(c => c.SubjectId == subjectId, ct),
            subject.OwnerAdultId, ClaimsPrincipalExtensions.IsOwnedBy(subject.OwnerAdultId, fid));
    }

    /// <summary>
    /// Deletes a subject along with its exercise categories (owner only), unless a row that cannot live
    /// without it points at it.
    /// <para>
    /// The line runs along whether the reference is REQUIRED, not along who owns the row (B-144). Every
    /// optional <c>SubjectId</c> only loses its assignment - textbook series, textbooks, creator profiles,
    /// study plans and class tests are <c>SetNull</c>, and the last three belong to a child just as much
    /// as the two below do. Of the three mandatory ones, the cheap one cascades: an exercise category is
    /// catalog-internal and meaningless without its subject (its exercises survive - <c>CategoryId</c> is
    /// <c>SetNull</c> in turn). The other two are <c>Restrict</c> and block the delete with 409
    /// <c>subject_in_use</c>, because the cascade would take a child's work with it: a key result's
    /// milestone together with the payout it earned, and a timetable entry that was typed by hand.
    /// </para>
    /// <para>
    /// Stating the rule as "child data blocks" would be the trap it was written to close: it reads as a
    /// promise for the next child-owned entity, and one added with a <c>SetNull</c> subject would quietly
    /// fall on the other side of the line.
    /// </para>
    /// <para>
    /// The pre-check is not redundant next to <c>DeleteBehavior.Restrict</c>, and vice versa. Without the
    /// check the database would raise the conflict as a bare 500 with a half-saved state instead of a
    /// readable 409. Without <c>Restrict</c> this check would be the only thing between a child's
    /// milestones and their deletion - and it only guards the one path that runs through here.
    /// </para>
    /// </summary>
    [HttpDelete("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FindAsync([subjectId], ct);
        if (subject is null) return NotFound();
        // Before the usage check, as with the textbook series: who may act is decided ahead of whether the
        // action is possible - otherwise a stranger learns from the 409 what a child's plans contain.
        // Held by FachEigentumTests.FremderCreator_BekommtNotOwner_AuchWennDasFachBenutztIst - swapping
        // these two blocks kept every other test green, which is why that case exists.
        if (!ClaimsPrincipalExtensions.IsOwnedBy(subject.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may delete this subject.");

        // `AnyAsync` rather than a count on purpose: the message names the kind of use without a number,
        // because knowing there are three of them does not make the subject deletable.
        if (await db.KeyResults.AnyAsync(k => k.SubjectId == subjectId, ct)
            || await db.TimetableEntries.AnyAsync(t => t.SubjectId == subjectId, ct))
            return this.ProblemWithCode(ApiErrors.SubjectInUse,
                "This subject is used in a child's objectives or timetable. Remove those entries first.");

        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
