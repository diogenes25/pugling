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
    public async Task<IEnumerable<SubjectResponse>> List() =>
        await db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Chapters.Count))
            .ToListAsync();

    /// <summary>Ein einzelnes Fach.</summary>
    [HttpGet("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Get(int subjectId)
    {
        var subject = await db.Subjects
            .Where(s => s.Id == subjectId)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.CreatedAt, s.Chapters.Count))
            .FirstOrDefaultAsync();
        return subject is null ? NotFound() : subject;
    }

    /// <summary>Erstellt ein Fach.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectResponse>> Create(CreateSubjectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var subject = new Subject { Name = dto.Name.Trim() };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        var response = new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { subjectId = subject.Id }, response);
    }

    /// <summary>Ändert ein Fach (partiell).</summary>
    [HttpPatch("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(int subjectId, UpdateSubjectDto dto)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId);
        if (subject is null) return NotFound();

        if (dto.Name is not null) subject.Name = dto.Name.Trim();
        await db.SaveChangesAsync();

        return new SubjectResponse(subject.Id, subject.Name, subject.CreatedAt,
            await db.Chapters.CountAsync(c => c.SubjectId == subjectId));
    }

    /// <summary>
    /// Löscht ein Fach samt aller Kapitel und Übungen. Nicht möglich, solange eine Übung darunter
    /// in einem Lehrplan oder einer Klassenarbeit verwendet wird.
    /// </summary>
    [HttpDelete("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId)
    {
        var subject = await db.Subjects.FindAsync(subjectId);
        if (subject is null) return NotFound();
        // Subject→Chapter→Exercise kaskadiert, PlanPosition→Exercise ist Restrict: ohne diese Prüfung
        // stirbt das Löschen als FK-Verletzung in einer nackten 500, statt zu sagen, was im Weg steht.
        if (await db.PlanPositions.AnyAsync(p => p.Exercise!.Chapter!.SubjectId == subjectId)
            || await db.KlassenarbeitExercises.AnyAsync(x => x.Exercise!.Chapter!.SubjectId == subjectId))
            return this.ProblemWithCode(ApiErrors.ExerciseInUse,
                "Exercises in this subject are used in a study plan or a class test; remove them there first.");
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
