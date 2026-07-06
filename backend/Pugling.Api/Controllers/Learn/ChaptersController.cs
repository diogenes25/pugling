using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Kapitel innerhalb eines Fachs.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/learn/subjects/{subjectId:int}/chapters")]
[Tags("Learn – Chapters")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ChaptersController(PuglingDbContext db) : ControllerBase
{
    public record ChapterResponse(int Id, int SubjectId, string Name, int OrderIndex, int ExercisesCount);

    Task<bool> SubjectExists(int subjectId) => db.Subjects.AnyAsync(s => s.Id == subjectId);

    Task<ChapterResponse?> ProjectOne(int subjectId, int chapterId) =>
        db.Chapters
            .Where(c => c.Id == chapterId && c.SubjectId == subjectId)
            .Select(c => new ChapterResponse(c.Id, c.SubjectId, c.Name, c.OrderIndex, c.Exercises.Count))
            .FirstOrDefaultAsync();

    /// <summary>Liste der Kapitel eines Fachs.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChapterResponse>>> List(int subjectId)
    {
        if (!await SubjectExists(subjectId)) return NotFound();
        return await db.Chapters
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.OrderIndex).ThenBy(c => c.Id)
            .Select(c => new ChapterResponse(c.Id, c.SubjectId, c.Name, c.OrderIndex, c.Exercises.Count))
            .ToListAsync();
    }

    /// <summary>Ein einzelnes Kapitel.</summary>
    [HttpGet("{chapterId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChapterResponse>> Get(int subjectId, int chapterId)
    {
        var chapter = await ProjectOne(subjectId, chapterId);
        return chapter is null ? NotFound() : chapter;
    }

    public record CreateChapterDto(string Name, int OrderIndex);

    /// <summary>Erstellt ein Kapitel unter einem Fach.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChapterResponse>> Create(int subjectId, CreateChapterDto dto)
    {
        if (!await SubjectExists(subjectId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var chapter = new Chapter { SubjectId = subjectId, Name = dto.Name.Trim(), OrderIndex = dto.OrderIndex };
        db.Chapters.Add(chapter);
        await db.SaveChangesAsync();

        var response = new ChapterResponse(chapter.Id, subjectId, chapter.Name, chapter.OrderIndex, 0);
        return CreatedAtAction(nameof(Get), new { subjectId, chapterId = chapter.Id }, response);
    }

    public record UpdateChapterDto(string? Name, int? OrderIndex);

    /// <summary>Ändert ein Kapitel (partiell).</summary>
    [HttpPatch("{chapterId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChapterResponse>> Update(int subjectId, int chapterId, UpdateChapterDto dto)
    {
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId && c.SubjectId == subjectId);
        if (chapter is null) return NotFound();

        if (dto.Name is not null) chapter.Name = dto.Name.Trim();
        if (dto.OrderIndex.HasValue) chapter.OrderIndex = dto.OrderIndex.Value;
        await db.SaveChangesAsync();

        return (await ProjectOne(subjectId, chapterId))!;
    }

    /// <summary>Löscht ein Kapitel samt aller Übungen.</summary>
    [HttpDelete("{chapterId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId, int chapterId)
    {
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId && c.SubjectId == subjectId);
        if (chapter is null) return NotFound();
        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
