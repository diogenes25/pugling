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
    /// Deletes a subject along with its exercise categories. Since B-106 a subject no longer cascades to
    /// any exercise (those hang off a textbook series unit instead) - deleting it only clears the FK on
    /// textbook series that reference it (SetNull) and on exercises pointing at one of its categories.
    /// </summary>
    [HttpDelete("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FindAsync([subjectId], ct);
        if (subject is null) return NotFound();
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
