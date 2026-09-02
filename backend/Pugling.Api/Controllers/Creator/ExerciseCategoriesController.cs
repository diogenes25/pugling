using System.Linq.Expressions;
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
/// <b>Creating stays open to every creator</b> (B-157, decision 2): with the subject's rule being
/// fail-closed, a gated create would have frozen the category axis of all seeded subjects, which are the
/// only ones an ordinary user has.
/// </para>
/// <para>
/// Renaming and deleting need <b>one of two</b> owner rights (B-170): the category's own
/// <c>ExerciseCategory.OwnerAdultId</c> <b>or</b> the subject's <c>Subject.OwnerAdultId</c>. Two bearers,
/// because one alone breaks in each direction: without the category's, whoever adds a category to a seeded
/// subject can never remove it again - a typo stayed in the study-plan pre-filter forever, which is exactly
/// what B-157 caused and B-170 undid. Without the subject's, a stranger could plant a category in my
/// subject that I cannot get rid of. The check is deliberately one condition without a special case for
/// <c>null</c>: "remove what you added" is the symmetric counterpart of a create that is open everywhere.
/// </para>
/// <para>
/// A <b>platform admin</b> passes as well (break-glass, B-178) - for every subject, not only ownerless
/// ones, exactly as at the exercise. It remains the only way to clear a <i>seeded</i> category, which has
/// neither bearer.
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

    /// <summary>
    /// Whether the caller may rename or delete a category: its owner, the subject's owner, or an admin
    /// (B-170). Used by <c>Update</c> and <c>Delete</c>, which hold a loaded entity.
    /// <para>
    /// The read paths cannot call this - a projection has to be translatable to SQL, and a method call over
    /// four arguments is not. They use <see cref="Projection"/> instead, which spells the <b>same</b> rule
    /// as an expression. Two forms of one rule is a drift risk and therefore named rather than hidden: the
    /// write form is pinned by <c>Anleger_DarfSeineArt_ImSeedFach_Loeschen</c> and
    /// <c>Fach_Eigentuemer_DarfEineFremdeArt_InSeinemFach_Loeschen</c> (one case per branch, each measured
    /// to fall when its branch is removed), the read form by
    /// <c>IsOwn_TrenntDieEigeneArt_VonDerGeseedeten_InListeUndEinzelabruf</c> and
    /// <c>Fach_Eigentuemer_DarfEineFremdeArt_InSeinemFach_Loeschen</c> - one case per branch again, over
    /// both read paths and the created row, which shares the projection rather than repeating the rule.
    /// Whoever changes the rule has to touch two places, and each has a case that fails if they touch
    /// only one.
    /// </para>
    /// </summary>
    private static bool MayWrite(int? categoryOwner, int? subjectOwner, int? fid, bool isAdmin) =>
        ClaimsPrincipalExtensions.IsOwnedBy(categoryOwner, fid)
        || ClaimsPrincipalExtensions.IsOwnedBy(subjectOwner, fid)
        || isAdmin;

    /// <summary>
    /// The single projection both read paths use, so <c>isOwn</c> cannot mean one thing in the list and
    /// another in the single fetch. <c>fid</c> and <c>isAdmin</c> come in as captured values because
    /// <c>User</c> itself is not translatable.
    /// </summary>
    private static Expression<Func<ExerciseCategory, CategoryResponse>> Projection(int? fid, bool isAdmin) =>
        c => new CategoryResponse(c.Id, c.SubjectId, c.Name, c.CreatedAt,
            isAdmin || (fid != null && (c.OwnerAdultId == fid || c.Subject!.OwnerAdultId == fid)));

    Task<CategoryResponse?> ProjectOne(int subjectId, int categoryId, CancellationToken ct) =>
        db.ExerciseCategories
            .Where(c => c.Id == categoryId && c.SubjectId == subjectId)
            .Select(Projection(User.CreatorId(), User.IsAdmin()))
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
            .Select(Projection(User.CreatorId(), User.IsAdmin()))
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
        // everyone, because their owner is null and `IsOwnedBy` is fail-closed. Since B-170 the creator is
        // recorded, which is what makes the open create survivable: whoever adds may also remove.
        if (!await SubjectExists(subjectId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var name = dto.Name.Trim();
        if (await db.ExerciseCategories.AnyAsync(c => c.SubjectId == subjectId && c.Name == name, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateCategoryName, "This category already exists in the subject.");

        var category = new ExerciseCategory { SubjectId = subjectId, Name = name, OwnerAdultId = User.CreatorId() };
        db.ExerciseCategories.Add(category);
        await db.SaveChangesAsync(ct);

        // The created row goes out through the SAME projection as both read paths instead of spelling
        // `isOwn` a third time. "The caller has just become the owner" is true today, but it is a copy of
        // the rule that nothing keeps in step: it was the only place `isOwn` was computed without a test,
        // and the checked-in example answer is captured from here. One extra SELECT buys that the created
        // row and the next `Get` cannot disagree. Non-null right after `SaveChangesAsync` - the row exists
        // in this very context.
        var response = (await ProjectOne(subjectId, category.Id, ct))!;
        return CreatedAtAction(nameof(Get), new { subjectId, categoryId = category.Id }, response);
    }

    /// <summary>Changes a category (partial). Only its creator, the owner of its subject, or a platform admin, may do so.</summary>
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
        // Two owner rights, either of which suffices (B-170), plus the admin break-glass (B-178). The
        // admin's REACH is wider than its reason: a platform admin passes here for any subject, including
        // one another creator owns. That is deliberate and matches the exercise
        // (`ExercisePermissionService.CanWrite` takes `IsAdmin` the same way, for the same kind of
        // emergency); narrowing it to `OwnerAdultId is null` would be a second, differently-shaped rule for
        // the same idea. Since B-170 it is no longer the only way out of a household's own typo - it is the
        // only way to touch a SEEDED category, which has neither bearer.
        if (!MayWrite(category.OwnerAdultId, category.Subject?.OwnerAdultId, User.CreatorId(), User.IsAdmin()))
            return this.ProblemWithCode(ApiErrors.NotOwner,
                "Only the creator of this category, or the owner of its subject, may change it.");

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
    /// Deletes a category; assigned exercises remain (FK is set to null). Only its creator, the owner of its
    /// subject, or a platform admin, may do so. There is deliberately no in-use conflict: <c>Exercise.CategoryId</c> is optional, so the
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

        // Same rule as in Update (B-170/B-178) - see the reasoning there.
        if (!MayWrite(category.OwnerAdultId, category.Subject?.OwnerAdultId, User.CreatorId(), User.IsAdmin()))
            return this.ProblemWithCode(ApiErrors.NotOwner,
                "Only the creator of this category, or the owner of its subject, may delete it.");

        db.ExerciseCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
