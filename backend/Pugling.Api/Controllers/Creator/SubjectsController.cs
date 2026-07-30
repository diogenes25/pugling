using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>Schulfächer im gemeinsamen Lehrplan-Katalog.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/subjects")]
[Tags("Creator – Subjects")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class SubjectsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Liste aller Fächer.</summary>
    [HttpGet]
    public async Task<IEnumerable<SubjectResponse>> List(CancellationToken ct = default) =>
        await db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Chapters.Count))
            .ToListAsync(ct);

    /// <summary>Ein einzelnes Fach.</summary>
    [HttpGet("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Get(int subjectId, CancellationToken ct = default)
    {
        var subject = await db.Subjects
            .Where(s => s.Id == subjectId)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Chapters.Count))
            .FirstOrDefaultAsync(ct);
        return subject is null ? NotFound() : subject;
    }

    /// <summary>Erstellt ein Fach.</summary>
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

    /// <summary>Ändert ein Fach (partiell).</summary>
    [HttpPatch("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(int subjectId, UpdateSubjectDto dto, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId, ct);
        if (subject is null) return NotFound();

        if (dto.Name is not null) subject.Name = dto.Name.Trim();
        await db.SaveChangesAsync(ct);

        return new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt,
            await db.Chapters.CountAsync(c => c.SubjectId == subjectId, ct));
    }

    /// <summary>
    /// Löscht ein Fach samt aller Kapitel und Übungen. Nicht möglich, solange eine Übung darunter
    /// in einem Lehrplan oder einer Klassenarbeit verwendet wird.
    /// </summary>
    [HttpDelete("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId, CancellationToken ct = default)
    {
        var subject = await db.Subjects.FindAsync([subjectId], ct);
        if (subject is null) return NotFound();
        // Subject→Chapter→Exercise kaskadiert, PlanPosition→Exercise ist Restrict. Welche Tabellen das
        // Löschen blockieren, weiß ExerciseUsageQueries – hier steht nur der Scope und die Meldung.
        if (await ExerciseUsageQueries.AnyBlockingAsync(db,
                db.Exercises.Where(x => x.Chapter!.SubjectId == subjectId),
                db.Chapters.Where(c => c.SubjectId == subjectId), ct))
            return this.ProblemWithCode(ApiErrors.ExerciseInUse,
                "Content in this subject is still used in a study plan, a class test or an objective "
                + "milestone; remove it there first.");
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
