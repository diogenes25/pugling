using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Class tests of a child: the father plans them, assigns relevant exercises (directly or via
/// tags) and records the grade after the test is written. Child and father can use this to practice
/// specifically for an upcoming test or repeat exercises from poorly graded tests.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/class-tests")]
[Tags("Supervisor – Class Tests")]
[Produces("application/json")]
[Authorize]
public class KlassenarbeitenController(PuglingDbContext db, AuthAccess access, ExercisePermissionService perms) : ControllerBase
{
    /// <summary>Threshold from which a grade counts as "bad" (German scale, higher = worse).</summary>
    private const decimal DefaultBadGrade = 4.0m;

    private static KlassenarbeitResponse Map(Klassenarbeit k) => new(
        k.Id, k.ChildId, k.SubjectId, k.Subject?.Name, k.Title, k.Topic, k.ScheduledDate, k.Status,
        k.Grade, k.GradeComment, k.Exercises.Count,
        k.Tags.Where(t => t.Tag is not null)
            .Select(t => new TagRef(t.Tag!.Id, t.Tag!.Name, t.Tag!.Color))
            .OrderBy(t => t.Name).ToList(),
        k.CreatedAt);

    private IQueryable<Klassenarbeit> WithRelations() => db.Klassenarbeiten
        .Include(k => k.Subject)
        .Include(k => k.Exercises)
        .Include(k => k.Tags).ThenInclude(t => t.Tag);

    private async Task<Klassenarbeit?> FindOwnedAsync(int id, CancellationToken ct)
    {
        var k = await WithRelations().FirstOrDefaultAsync(k => k.Id == id, ct);
        if (k is null) return null;
        return await access.OwnsChildAsync(User, k.ChildId, ct) ? k : null;
    }

    private static string? ValidateGrade(decimal? grade) =>
        grade is { } g && (g < 1.0m || g > 6.0m) ? "Grade must be between 1.0 and 6.0." : null;

    // ---- Lesen ----

    /// <summary>Class tests of a child, optionally filtered by status/subject (own only).</summary>
    /// <param name="childId">Child whose class tests are being read.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="subjectId">Optional subject filter.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<KlassenarbeitResponse>>> List(
        [FromQuery] int childId, [FromQuery] KlassenarbeitStatus? status, [FromQuery] int? subjectId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        if (!await access.OwnsChildAsync(User, childId, ct)) return Forbid();

        var query = WithRelations().AsNoTracking().Where(k => k.ChildId == childId);
        if (status is not null) query = query.Where(k => k.Status == status);
        if (subjectId is not null) query = query.Where(k => k.SubjectId == subjectId);

        var list = await query.OrderBy(k => k.ScheduledDate).ThenBy(k => k.Id).ToPagedListAsync(Response, skip, take, ct);
        return list.Select(Map).ToList();
    }

    /// <summary>A class test incl. the directly assigned exercises (own only).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitDetail>> Get(int id, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();

        var exIds = k.Exercises.Select(x => x.ExerciseId).ToList();
        var exercises = await LoadExercisesAsync(e => exIds.Contains(e.Id), ct);
        return new KlassenarbeitDetail(Map(k), exercises);
    }

    // ---- Anlegen / Ändern (nur Vater) ----

    /// <summary>Plans a class test (or records one already written). Father only, own children only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KlassenarbeitDetail>> Create(CreateClassTestDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (!await access.OwnsChildAsync(User, dto.ChildId, ct)) return Forbid();
        if (ValidateGrade(dto.Grade) is { } gradeError) return this.ProblemWithCode(ApiErrors.ValidationError, gradeError);
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");

        var k = new Klassenarbeit
        {
            ChildId = dto.ChildId,
            Title = dto.Title.Trim(),
            Topic = dto.Topic?.Trim(),
            SubjectId = dto.SubjectId,
            ScheduledDate = dto.ScheduledDate,
            Status = dto.Status ?? (dto.Grade is not null ? KlassenarbeitStatus.Written : KlassenarbeitStatus.Planned),
            Grade = dto.Grade,
            GradeComment = dto.GradeComment?.Trim(),
        };

        if (await BuildExerciseLinksAsync(dto.ChildId, dto.ExerciseIds, k.Exercises, ct) is { } exErr) return exErr;
        if (await BuildTagLinksAsync(dto.ChildId, dto.TagIds, k.Tags, ct) is { } tagErr) return this.ProblemWithCode(ApiErrors.InvalidReference, tagErr);

        db.Klassenarbeiten.Add(k);
        await db.SaveChangesAsync(ct);

        var created = (await FindOwnedAsync(k.Id, ct))!;
        var exIds = created.Exercises.Select(x => x.ExerciseId).ToList();
        return CreatedAtAction(nameof(Get), new { id = k.Id },
            new KlassenarbeitDetail(Map(created), await LoadExercisesAsync(e => exIds.Contains(e.Id), ct)));
    }

    /// <summary>Partially changes a class test – among other things, records the grade and sets the status. Father only.</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitResponse>> Update(int id, UpdateClassTestDto dto, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();

        if (dto.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title must not be empty.");
            k.Title = dto.Title.Trim();
        }
        if (dto.Topic is not null) k.Topic = dto.Topic.Trim() is { Length: > 0 } t ? t : null;
        if (dto.SubjectId is { } sid)
        {
            if (!await db.Subjects.AnyAsync(s => s.Id == sid, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
            k.SubjectId = sid;
        }
        if (dto.ScheduledDate is not null) k.ScheduledDate = dto.ScheduledDate.Value;
        if (dto.GradeComment is not null) k.GradeComment = dto.GradeComment.Trim() is { Length: > 0 } c ? c : null;

        if (dto.ClearGrade)
        {
            k.Grade = null;
        }
        else if (dto.Grade is not null)
        {
            if (ValidateGrade(dto.Grade) is { } gradeError) return this.ProblemWithCode(ApiErrors.ValidationError, gradeError);
            k.Grade = dto.Grade;
        }

        // Eine nachgetragene Note bedeutet: geschrieben. Explizit gesetzter Status hat Vorrang.
        if (dto.Status is not null) k.Status = dto.Status.Value;
        else if (k.Grade is not null && k.Status == KlassenarbeitStatus.Planned) k.Status = KlassenarbeitStatus.Written;

        await db.SaveChangesAsync(ct);
        return Map(k);
    }

    /// <summary>Deletes a class test (assignments and tag links disappear with it). Father only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();
        db.Klassenarbeiten.Remove(k);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Übungen zuweisen (nur Vater) ----

    /// <summary>Assigns exercises directly to the class test (already assigned ones are skipped). Father only.</summary>
    [HttpPost("{id:int}/exercises")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitDetail>> AssignExercises(int id, AssignExercisesDto dto, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();
        if (dto.ExerciseIds is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one exercise is required.");
        if (await BuildExerciseLinksAsync(k.ChildId, dto.ExerciseIds, k.Exercises, ct) is { } error) return error;

        await db.SaveChangesAsync(ct);
        var exIds = k.Exercises.Select(x => x.ExerciseId).ToList();
        return new KlassenarbeitDetail(Map(k), await LoadExercisesAsync(e => exIds.Contains(e.Id), ct));
    }

    /// <summary>Removes the direct assignment of an exercise. Father only.</summary>
    [HttpDelete("{id:int}/exercises/{exerciseId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignExercise(int id, int exerciseId, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();
        var link = k.Exercises.FirstOrDefault(x => x.ExerciseId == exerciseId);
        if (link is null) return NotFound();
        db.KlassenarbeitExercises.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Links a tag to the class test: all exercises marked this way count as relevant. Father only.</summary>
    [HttpPost("{id:int}/tags/{tagId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitResponse>> LinkTag(int id, int tagId, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();
        if (!await db.Tags.AnyAsync(t => t.Id == tagId && t.ChildId == k.ChildId, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "The tag does not belong to this child.");
        if (k.Tags.All(t => t.TagId != tagId))
        {
            k.Tags.Add(new KlassenarbeitTag { TagId = tagId });
            await db.SaveChangesAsync(ct);
        }
        return Map((await FindOwnedAsync(id, ct))!);
    }

    /// <summary>Removes the link of a tag with the class test. Father only.</summary>
    [HttpDelete("{id:int}/tags/{tagId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkTag(int id, int tagId, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();
        var link = k.Tags.FirstOrDefault(t => t.TagId == tagId);
        if (link is null) return NotFound();
        db.KlassenarbeitTags.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Üben / Wiederholen ----

    /// <summary>
    /// All exercises relevant to the class test: directly assigned AND marked via linked tags
    /// (without duplicates). Basis for targeted practice for an upcoming test.
    /// </summary>
    [HttpGet("{id:int}/practice")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PracticeResponse>> Practice(int id, CancellationToken ct = default)
    {
        var k = await FindOwnedAsync(id, ct);
        if (k is null) return NotFound();

        var exercises = await LoadRelevantExercisesAsync(new[] { id }, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new PracticeResponse(k.Id, k.Title, k.ScheduledDate, k.ScheduledDate.DayNumber - today.DayNumber, exercises);
    }

    /// <summary>
    /// Collects the relevant exercises of all written class tests of a child whose grade
    /// is at least <paramref name="minBadGrade"/> (default 4.0) – for targeted review.
    /// </summary>
    [HttpGet("repeat")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RepeatResponse>> Repeat(
        [FromQuery] int childId, [FromQuery] decimal? minBadGrade, CancellationToken ct = default)
    {
        if (!await access.OwnsChildAsync(User, childId, ct)) return Forbid();
        var threshold = minBadGrade ?? DefaultBadGrade;

        var sources = await WithRelations()
            .Where(k => k.ChildId == childId && k.Status == KlassenarbeitStatus.Written
                        && k.Grade != null && k.Grade >= threshold)
            .OrderByDescending(k => k.ScheduledDate)
            .ToListAsync(ct);

        var exercises = sources.Count == 0
            ? new List<ExerciseBrief>()
            : await LoadRelevantExercisesAsync(sources.Select(k => k.Id).ToList(), ct);

        return new RepeatResponse(threshold, sources.Select(Map).ToList(), exercises);
    }

    // ---- Helfer ----

    /// <summary>Loads exercises by predicate incl. chapter/subject, sorted and without tracking.</summary>
    private async Task<List<ExerciseBrief>> LoadExercisesAsync(
        System.Linq.Expressions.Expression<Func<Exercise, bool>> predicate, CancellationToken ct)
    {
        var exercises = await db.Exercises
            .Where(predicate)
            .Include(e => e.Chapter!).ThenInclude(c => c.Subject)
            .OrderBy(e => e.Chapter!.SubjectId).ThenBy(e => e.ChapterId).ThenBy(e => e.OrderIndex)
            .AsNoTracking()
            .ToListAsync(ct);
        return exercises.Select(ExerciseBriefMapping.From).ToList();
    }

    /// <summary>
    /// Merges (without duplicates) the directly assigned exercises and those relevant via linked
    /// tags of the given class tests.
    /// </summary>
    private async Task<List<ExerciseBrief>> LoadRelevantExercisesAsync(IReadOnlyCollection<int> klassenarbeitIds, CancellationToken ct)
    {
        var directIds = await db.KlassenarbeitExercises
            .Where(x => klassenarbeitIds.Contains(x.KlassenarbeitId))
            .Select(x => x.ExerciseId).ToListAsync(ct);
        var tagIds = await db.KlassenarbeitTags
            .Where(x => klassenarbeitIds.Contains(x.KlassenarbeitId))
            .Select(x => x.TagId).ToListAsync(ct);

        return await LoadExercisesAsync(e => directIds.Contains(e.Id)
            || db.ExerciseTags.Any(et => et.ExerciseId == e.Id && tagIds.Contains(et.TagId)), ct);
    }

    /// <summary>Checks the exercise ids (existence + execute permission) and attaches new assignments; returns an error result or null.</summary>
    private async Task<ObjectResult?> BuildExerciseLinksAsync(
        int childId, List<int>? exerciseIds, List<KlassenarbeitExercise> target, CancellationToken ct)
    {
        if (exerciseIds is not { Count: > 0 }) return null;
        var ids = exerciseIds.Distinct().ToList();
        var known = await db.Exercises.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        var missing = ids.Except(known.Select(e => e.Id)).ToList();
        if (missing.Count > 0) return this.ProblemWithCode(ApiErrors.InvalidReference, $"Unknown exercise IDs: {string.Join(", ", missing)}");

        // Execute-Gate: nur öffentlich ausführbare oder eigens freigegebene Übungen darf der Vater in die Arbeit aufnehmen.
        foreach (var exercise in known)
            if (!await perms.CanExecuteAsync(User, exercise, ct))
                return this.ProblemWithCode(ApiErrors.ExerciseNotExecutable,
                    $"Exercise {exercise.Id} is not publicly assignable; you need execute permission from its owner.");

        var already = target.Select(x => x.ExerciseId).ToHashSet();
        foreach (var exId in ids.Where(exId => already.Add(exId)))
            target.Add(new KlassenarbeitExercise { ExerciseId = exId });
        return null;
    }

    /// <summary>Checks the tag ids (must belong to the child) and attaches new links.</summary>
    private async Task<string?> BuildTagLinksAsync(
        int childId, List<int>? tagIds, List<KlassenarbeitTag> target, CancellationToken ct)
    {
        if (tagIds is not { Count: > 0 }) return null;
        var ids = tagIds.Distinct().ToList();
        var known = await db.Tags.Where(t => ids.Contains(t.Id) && t.ChildId == childId).Select(t => t.Id).ToListAsync(ct);
        var invalid = ids.Except(known).ToList();
        if (invalid.Count > 0) return $"Tags do not belong to this child or do not exist: {string.Join(", ", invalid)}";

        var already = target.Select(x => x.TagId).ToHashSet();
        foreach (var tagId in ids.Where(tagId => already.Add(tagId)))
            target.Add(new KlassenarbeitTag { TagId = tagId });
        return null;
    }
}
