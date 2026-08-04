using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>Chapters within a subject.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/subjects/{subjectId:int}/chapters")]
[Tags("Creator – Chapters")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ChaptersController(PuglingDbContext db) : ControllerBase
{
    Task<bool> SubjectExists(int subjectId, CancellationToken ct) => db.Subjects.AnyAsync(s => s.Id == subjectId, ct);

    Task<ChapterResponse?> ProjectOne(int subjectId, int chapterId, CancellationToken ct) =>
        db.Chapters
            .Where(c => c.Id == chapterId && c.SubjectId == subjectId)
            .Select(c => new ChapterResponse(c.Id, c.SubjectId, c.Name, c.OrderIndex, c.Exercises.Count))
            .FirstOrDefaultAsync(ct);

    /// <summary>List of a subject's chapters.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChapterResponse>>> List(int subjectId, CancellationToken ct = default)
    {
        if (!await SubjectExists(subjectId, ct)) return NotFound();
        return await db.Chapters
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.OrderIndex).ThenBy(c => c.Id)
            .Select(c => new ChapterResponse(c.Id, c.SubjectId, c.Name, c.OrderIndex, c.Exercises.Count))
            .ToListAsync(ct);
    }

    /// <summary>A single chapter.</summary>
    [HttpGet("{chapterId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChapterResponse>> Get(int subjectId, int chapterId, CancellationToken ct = default)
    {
        var chapter = await ProjectOne(subjectId, chapterId, ct);
        return chapter is null ? NotFound() : chapter;
    }

    /// <summary>Creates a chapter under a subject.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChapterResponse>> Create(int subjectId, CreateChapterDto dto, CancellationToken ct = default)
    {
        if (!await SubjectExists(subjectId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var name = dto.Name.Trim();
        if (await db.Chapters.AnyAsync(c => c.SubjectId == subjectId && c.Name == name, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateChapterName, $"Chapter '{name}' already exists in this subject.");

        var chapter = new Chapter { SubjectId = subjectId, Name = name, OrderIndex = dto.OrderIndex };
        db.Chapters.Add(chapter);
        await db.SaveChangesAsync(ct);

        var response = new ChapterResponse(chapter.Id, subjectId, chapter.Name, chapter.OrderIndex, 0);
        return CreatedAtAction(nameof(Get), new { subjectId, chapterId = chapter.Id }, response);
    }

    /// <summary>Changes a chapter (partial).</summary>
    [HttpPatch("{chapterId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChapterResponse>> Update(int subjectId, int chapterId, UpdateChapterDto dto, CancellationToken ct = default)
    {
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId && c.SubjectId == subjectId, ct);
        if (chapter is null) return NotFound();

        // Both checks BEFORE the first assignment, in the shape the sibling controller uses
        // (ExerciseCategoriesController.Update): a rejected PATCH must leave the chapter untouched.
        // Emptiness first - a whitespace name would otherwise slip past the duplicate check and be written as
        // "", which Create forbids and the unique index turns into a 500 on the second attempt.
        // The duplicate check excludes the row itself by ID, not by name: renaming a chapter to its own name
        // must stay legal, and an ID comparison survives a collation change on the column.
        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            if (await db.Chapters.AnyAsync(c => c.SubjectId == subjectId && c.Id != chapterId && c.Name == name, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateChapterName, $"Chapter '{name}' already exists in this subject.");
            chapter.Name = name;
        }
        if (dto.OrderIndex.HasValue) chapter.OrderIndex = dto.OrderIndex.Value;
        await db.SaveChangesAsync(ct);

        return (await ProjectOne(subjectId, chapterId, ct))!;
    }

    /// <summary>
    /// Deletes a chapter along with all its exercises. Not possible while an exercise in it is used in
    /// a study plan or a class test.
    /// </summary>
    [HttpDelete("{chapterId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId, int chapterId, CancellationToken ct = default)
    {
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId && c.SubjectId == subjectId, ct);
        if (chapter is null) return NotFound();
        // Chapter→Exercise cascades, PlanPosition→Exercise is Restrict - cf. ExerciseControllerBase.Delete.
        if (await ExerciseUsageQueries.AnyBlockingAsync(db,
                db.Exercises.Where(x => x.ChapterId == chapterId),
                db.Chapters.Where(c => c.Id == chapterId), ct))
            return this.ProblemWithCode(ApiErrors.ExerciseInUse,
                "Content in this chapter is still used in a study plan, a class test or an objective "
                + "milestone; remove it there first.");
        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
