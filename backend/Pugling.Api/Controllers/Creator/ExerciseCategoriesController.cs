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
/// <para>
/// A category has no owner of its own: it belongs to whoever owns its <b>subject</b> (B-157). Renaming and
/// deleting therefore follow <c>Subject.OwnerAdultId</c>, while <b>creating stays open to every creator</b> -
/// with the subject's rule being fail-closed, a gated create would have frozen the category axis of all
/// seeded subjects, which are the only ones an ordinary user has.
/// </para>
/// <para>
/// A <b>platform admin</b> passes the owner check as well (break-glass, B-178) - for every subject, not only
/// ownerless ones, exactly as at the exercise. Without it a category created under a seeded subject would be
/// editable by nobody at all.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/subjects/{subjectId:int}/categories")]
[Tags("Creator – Exercise Categories")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseCategoriesController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// Whether the subject exists at all - needed by <c>List</c> and <c>Create</c>, which have no category
    /// to look up. <c>Update</c> and <c>Delete</c> deliberately do <b>not</b> use it: their category lookup
    /// absorbs the case, because <c>SubjectId</c> is mandatory and a category without an existing subject
    /// cannot exist. Asking for the <i>owner</i> here would be the trap: a bare <c>int?</c> gives the same
    /// answer for "no such subject" and "nobody owns it", and a write against a missing subject would then
    /// end as 403 rather than 404.
    /// </summary>
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
        // No owner check on purpose (B-157, decision 2): creating stays open to every creator, exactly as
        // for the subject itself. Gating it would make the seeded subjects' category axis unextendable for
        // everyone, because their owner is null and `IsOwnedBy` is fail-closed.
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

    /// <summary>Changes a category (partial). Only the owner of the subject, or a platform admin, may do so.</summary>
    [HttpPatch("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(int subjectId, int categoryId, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        // One query, pattern of the sister controller one level over (`SeriesUnitsController.Update`): the
        // category carries its subject, and "no such subject" is absorbed by "no such category" - the
        // mandatory `SubjectId` means a category without an existing subject cannot exist.
        var category = await db.ExerciseCategories.Include(c => c.Subject)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.SubjectId == subjectId, ct);
        if (category is null) return NotFound();

        // 404 before 403, and that order is right here - unlike the subject's delete (B-13), where the 409
        // would have leaked a child's plans. A category's existence is public by design: `List` and `Get`
        // hand every category to every creator, so there is nothing to hide behind the 404.
        // The admin break-glass (B-178). Its REASON is the ownerless subject: a category created there would
        // otherwise be editable by NOBODY - creating is open (B-157 decision 2), changing follows the subject,
        // and a seeded subject has no owner. Its REACH is wider: a platform admin passes here for any
        // subject, including one another creator owns. That is deliberate and matches the exercise
        // (`ExercisePermissionService.CanWrite` takes `IsAdmin` the same way, for the same kind of emergency);
        // narrowing it to `OwnerAdultId is null` would be a second, differently-shaped rule for the same idea.
        // It does not free a household - no seeded adult carries the flag (checked), so the father still
        // cannot clean up his own typo until B-170 decides the schema.
        if (!ClaimsPrincipalExtensions.IsOwnedBy(category.Subject?.OwnerAdultId, User.CreatorId()) && !User.IsAdmin())
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the subject may change its categories.");

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

    /// <summary>
    /// Deletes a category; assigned exercises remain (FK is set to null). Only the owner of the subject, or a
    /// platform admin, may do so. There is deliberately no in-use conflict: <c>Exercise.CategoryId</c> is optional, so the
    /// exercises only lose their assignment - the behaviour is unchanged by B-157.
    /// </summary>
    [HttpDelete("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId, int categoryId, CancellationToken ct = default)
    {
        var category = await db.ExerciseCategories.Include(c => c.Subject)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.SubjectId == subjectId, ct);
        if (category is null) return NotFound();

        // Same break-glass as in Update (B-178) - see the reasoning there.
        if (!ClaimsPrincipalExtensions.IsOwnedBy(category.Subject?.OwnerAdultId, User.CreatorId()) && !User.IsAdmin())
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the subject may delete its categories.");

        db.ExerciseCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
