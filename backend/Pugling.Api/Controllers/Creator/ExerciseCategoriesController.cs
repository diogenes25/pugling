using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Subject-dependent exercise categories (e.g. grammar/vocabulary for English). Controlled
/// vocabulary per subject as the basis for pre-filtering during study plan creation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/subjects/{subjectId:int}/categories")]
[Tags("Creator – Exercise Categories")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseCategoriesController(PuglingDbContext db) : ControllerBase
{
    Task<bool> SubjectExists(int subjectId, CancellationToken ct) => db.Subjects.AnyAsync(s => s.Id == subjectId, ct);

    Task<CategoryResponse?> ProjectOne(int subjectId, int categoryId, CancellationToken ct) =>
        db.ExerciseCategories
            .Where(c => c.Id == categoryId && c.SubjectId == subjectId)
            .Select(c => new CategoryResponse(c.Id, c.SubjectId, c.Name, c.CreatedAt))
            .FirstOrDefaultAsync(ct);

    /// <summary>List of a subject's categories.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> List(int subjectId, CancellationToken ct = default)
    {
        if (!await SubjectExists(subjectId, ct)) return NotFound();
        return await db.ExerciseCategories
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.SubjectId, c.Name, c.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>A single category.</summary>
    [HttpGet("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Get(int subjectId, int categoryId, CancellationToken ct = default)
    {
        var category = await ProjectOne(subjectId, categoryId, ct);
        return category is null ? NotFound() : category;
    }

    /// <summary>Creates a category under a subject.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(int subjectId, CreateCategoryDto dto, CancellationToken ct = default)
    {
        if (!await SubjectExists(subjectId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var name = dto.Name.Trim();
        if (await db.ExerciseCategories.AnyAsync(c => c.SubjectId == subjectId && c.Name == name, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateCategoryName, "This category already exists in the subject.");

        var category = new ExerciseCategory { SubjectId = subjectId, Name = name };
        db.ExerciseCategories.Add(category);
        await db.SaveChangesAsync(ct);

        var response = new CategoryResponse(category.Id, subjectId, category.Name, category.CreatedAt);
        return CreatedAtAction(nameof(Get), new { subjectId, categoryId = category.Id }, response);
    }

    /// <summary>Changes a category (partial).</summary>
    [HttpPatch("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(int subjectId, int categoryId, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        var category = await db.ExerciseCategories.FirstOrDefaultAsync(c => c.Id == categoryId && c.SubjectId == subjectId, ct);
        if (category is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            if (name != category.Name &&
                await db.ExerciseCategories.AnyAsync(c => c.SubjectId == subjectId && c.Name == name, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateCategoryName, "This category already exists in the subject.");
            category.Name = name;
        }
        await db.SaveChangesAsync(ct);

        return (await ProjectOne(subjectId, categoryId, ct))!;
    }

    /// <summary>Deletes a category; assigned exercises remain (FK is set to null).</summary>
    [HttpDelete("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId, int categoryId, CancellationToken ct = default)
    {
        var category = await db.ExerciseCategories.FirstOrDefaultAsync(c => c.Id == categoryId && c.SubjectId == subjectId, ct);
        if (category is null) return NotFound();
        db.ExerciseCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
