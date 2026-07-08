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
[Route(ApiRoutes.V1 + "/learn/subjects")]
[Tags("Learn – Subjects")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class SubjectsController(PuglingDbContext db) : ControllerBase
{
    public record SubjectResponse(int Id, string Name, DateTime CreatedAt, int ChaptersCount);

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

    public record CreateSubjectDto(string Name);

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

    public record UpdateSubjectDto(string? Name);

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

    /// <summary>Löscht ein Fach samt aller Kapitel und Übungen.</summary>
    [HttpDelete("{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId)
    {
        var subject = await db.Subjects.FindAsync(subjectId);
        if (subject is null) return NotFound();
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
