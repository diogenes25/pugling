using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>School subjects in the shared study plan catalog.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/subjects")]
[Tags("Creator – Subjects")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class SubjectsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>List of all subjects.</summary>
    [HttpGet]
    public async Task<IEnumerable<SubjectResponse>> List(CancellationToken ct = default) =>
        await db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Categories.Count))
            .ToListAsync(ct);

    /// <summary>A single subject.</summary>
    [HttpGet("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Get(int subjectId, CancellationToken ct = default)
    {
        var subject = await db.Subjects
            .Where(s => s.Id == subjectId)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Categories.Count))
            .FirstOrDefaultAsync(ct);
        return subject is null ? NotFound() : subject;
    }

    /// <summary>Creates a subject.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectResponse>> Create(CreateSubjectDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var subject = new Subject { Name = dto.Name.Trim() };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);

        var response = new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { subjectId = subject.Id }, response);
    }

    /// <summary>Changes a subject (partial).</summary>
    [HttpPatch("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(int subjectId, UpdateSubjectDto dto, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId, ct);
        if (subject is null) return NotFound();

        if (dto.Name is not null) subject.Name = dto.Name.Trim();
        await db.SaveChangesAsync(ct);

        return new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt,
            await db.ExerciseCategories.CountAsync(c => c.SubjectId == subjectId, ct));
    }

    /// <summary>
    /// Deletes a subject along with its exercise categories, unless a child's data points at it.
    /// <para>
    /// Two groups, split by what the delete would cost (B-144). Catalog-internal references only lose
    /// their assignment: textbook series, textbooks, creator profiles, study plans and class tests are
    /// <c>SetNull</c>, exercise categories cascade (but their exercises survive - <c>CategoryId</c> is
    /// <c>SetNull</c> in turn). Rows that belong to a CHILD block the delete with 409
    /// <c>subject_in_use</c>: a key result's subject scope is mandatory, so a cascade would delete the
    /// milestone together with the payout it earned, and a timetable entry was typed by hand.
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FindAsync([subjectId], ct);
        if (subject is null) return NotFound();

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
