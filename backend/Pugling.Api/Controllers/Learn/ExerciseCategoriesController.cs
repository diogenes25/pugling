using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Fachabhängige Übungs-Arten (z. B. Grammatik/Vokabeln bei Englisch). Kontrolliertes
/// Vokabular je Fach als Grundlage für die Vorfilterung bei der Lehrplan-Erstellung.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/learn/subjects/{subjectId:int}/categories")]
[Tags("Learn – Exercise Categories")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ExerciseCategoriesController(PuglingDbContext db) : ControllerBase
{
    public record CategoryResponse(int Id, int SubjectId, string Name, DateTime CreatedAt);

    Task<bool> SubjectExists(int subjectId) => db.Subjects.AnyAsync(s => s.Id == subjectId);

    Task<CategoryResponse?> ProjectOne(int subjectId, int categoryId) =>
        db.ExerciseCategories
            .Where(c => c.Id == categoryId && c.SubjectId == subjectId)
            .Select(c => new CategoryResponse(c.Id, c.SubjectId, c.Name, c.CreatedAt))
            .FirstOrDefaultAsync();

    /// <summary>Liste der Arten eines Fachs.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> List(int subjectId)
    {
        if (!await SubjectExists(subjectId)) return NotFound();
        return await db.ExerciseCategories
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.SubjectId, c.Name, c.CreatedAt))
            .ToListAsync();
    }

    /// <summary>Eine einzelne Art.</summary>
    [HttpGet("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Get(int subjectId, int categoryId)
    {
        var category = await ProjectOne(subjectId, categoryId);
        return category is null ? NotFound() : category;
    }

    public record CreateCategoryDto(string Name);

    /// <summary>Erstellt eine Art unter einem Fach.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(int subjectId, CreateCategoryDto dto)
    {
        if (!await SubjectExists(subjectId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return Problem(statusCode: 400, detail: "Name ist erforderlich.");

        var name = dto.Name.Trim();
        if (await db.ExerciseCategories.AnyAsync(c => c.SubjectId == subjectId && c.Name == name))
            return Problem(statusCode: 409, detail: "Diese Art existiert im Fach bereits.");

        var category = new ExerciseCategory { SubjectId = subjectId, Name = name };
        db.ExerciseCategories.Add(category);
        await db.SaveChangesAsync();

        var response = new CategoryResponse(category.Id, subjectId, category.Name, category.CreatedAt);
        return CreatedAtAction(nameof(Get), new { subjectId, categoryId = category.Id }, response);
    }

    public record UpdateCategoryDto(string? Name);

    /// <summary>Ändert eine Art (partiell).</summary>
    [HttpPatch("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(int subjectId, int categoryId, UpdateCategoryDto dto)
    {
        var category = await db.ExerciseCategories.FirstOrDefaultAsync(c => c.Id == categoryId && c.SubjectId == subjectId);
        if (category is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return Problem(statusCode: 400, detail: "Name darf nicht leer sein.");
            if (name != category.Name &&
                await db.ExerciseCategories.AnyAsync(c => c.SubjectId == subjectId && c.Name == name))
                return Problem(statusCode: 409, detail: "Diese Art existiert im Fach bereits.");
            category.Name = name;
        }
        await db.SaveChangesAsync();

        return (await ProjectOne(subjectId, categoryId))!;
    }

    /// <summary>Löscht eine Art; zugeordnete Übungen bleiben erhalten (FK wird auf null gesetzt).</summary>
    [HttpDelete("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId, int categoryId)
    {
        var category = await db.ExerciseCategories.FirstOrDefaultAsync(c => c.Id == categoryId && c.SubjectId == subjectId);
        if (category is null) return NotFound();
        db.ExerciseCategories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
