using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child's read paths to its class tests (B-750/B-751): which ones are coming up, and how sure it already is in
/// their vocabulary. Planning stays with the supervisor (<c>supervisor/class-tests</c>); the child sees no grade here.
/// The supervisor may read along for his child, like the other student views.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/class-tests")]
[Tags("Student – Class Tests")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildClassTestsController(PuglingDbContext db, ExerciseTypeRegistry types, LearningDay learningDay)
    : ControllerBase
{
    /// <summary>
    /// The child's upcoming class tests, earliest first: planned ones dated today or later. Cancelled, written and
    /// overdue tests are not listed - the child should not count down to a test that no longer comes.
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<UpcomingClassTestResponse>>> Upcoming(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var tests = await db.Klassenarbeiten.AsNoTracking()
            .Where(k => k.ChildId == childId && k.Status == KlassenarbeitStatus.Planned && k.ScheduledDate >= today)
            .OrderBy(k => k.ScheduledDate).ThenBy(k => k.Id)
            .Select(k => new { k.Id, k.Title, k.SubjectId, SubjectName = k.Subject != null ? k.Subject.Name : null, k.ScheduledDate })
            .ToListAsync(ct);
        return tests.Select(k => new UpcomingClassTestResponse(k.Id, k.Title, k.SubjectId, k.SubjectName, k.ScheduledDate,
            k.ScheduledDate.DayNumber - today.DayNumber)).ToList();
    }

    /// <summary>
    /// How sure the child is in the vocabulary of the class test's bundle: of all vocabulary items of the bundle
    /// (practised or not), how many are not weak. A bundle without vocabulary has no measure (both counts null).
    /// </summary>
    [HttpGet("{id:int}/readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassTestReadinessResponse>> Readiness(int childId, int id, CancellationToken ct)
    {
        if (!await db.Klassenarbeiten.AnyAsync(k => k.Id == id && k.ChildId == childId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Class test not found.");

        var exerciseIds = await ClassTestBundle.ExerciseIdsAsync(db, [id], ct);
        var exerciseTypes = await db.Exercises.AsNoTracking()
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Type })
            .ToListAsync(ct);
        // Only types whose items live in the item table carry a mastery (ItemProgress); for the others nothing is estimated.
        var vocabIds = exerciseTypes
            .Where(e => types.ByKey(e.Type)?.StoreResolution == StoreResolution.ItemTable)
            .Select(e => e.Id).ToList();

        var itemIds = db.ExerciseItems.Where(i => vocabIds.Contains(i.ExerciseId)).Select(i => i.Id);
        var total = await itemIds.CountAsync(ct);
        // Unpractised items have no progress row and count as not secure: the denominator is every item of the bundle.
        var secure = total == 0 ? 0 : await db.ItemProgress.CountAsync(p => p.ChildId == childId
            && itemIds.Contains(p.ItemId) && p.MasteryPercent >= ItemProgress.WeakBelowPercent, ct);

        return new ClassTestReadinessResponse(id, exerciseTypes.Count, vocabIds.Count,
            total == 0 ? null : total, total == 0 ? null : secure, ItemProgress.WeakBelowPercent);
    }
}
