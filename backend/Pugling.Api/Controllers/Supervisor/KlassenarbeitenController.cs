using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Klassenarbeiten eines Kindes: der Vater plant sie, weist relevante Übungen zu (direkt oder über
/// Tags) und trägt nach dem Schreiben die Note nach. Sohn und Vater können daraus gezielt für eine
/// anstehende Arbeit üben oder Übungen schlecht benoteter Arbeiten wiederholen.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/class-tests")]
[Tags("Learn – Class Tests")]
[Produces("application/json")]
[Authorize]
public class KlassenarbeitenController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    /// <summary>Grenze, ab der eine Note als „schlecht" gilt (deutsche Skala, höher = schlechter).</summary>
    private const decimal DefaultBadGrade = 4.0m;

    public record TagRef(int Id, string Name, string? Color);

    /// <summary>Klassenarbeit in Listen-/Zusammenfassungssicht.</summary>
    public record KlassenarbeitResponse(int Id, int ChildId, int? SubjectId, string? SubjectName,
        string Title, string? Topic, DateOnly ScheduledDate, KlassenarbeitStatus Status,
        decimal? Grade, string? GradeComment, int DirectExerciseCount, IReadOnlyList<TagRef> Tags, DateTime CreatedAt);

    /// <summary>Klassenarbeit mit den direkt zugewiesenen Übungen.</summary>
    public record KlassenarbeitDetail(KlassenarbeitResponse Klassenarbeit, IReadOnlyList<ExerciseBrief> AssignedExercises);

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

    private async Task<Klassenarbeit?> FindOwnedAsync(int id)
    {
        var k = await WithRelations().FirstOrDefaultAsync(k => k.Id == id);
        if (k is null) return null;
        return await access.OwnsChildAsync(User, k.ChildId) ? k : null;
    }

    private static string? ValidateGrade(decimal? grade) =>
        grade is { } g && (g < 1.0m || g > 6.0m) ? "Grade must be between 1.0 and 6.0." : null;

    // ---- Lesen ----

    /// <summary>Klassenarbeiten eines Kindes, optional nach Status/Fach gefiltert (nur eigene).</summary>
    /// <param name="childId">Kind, dessen Klassenarbeiten gelesen werden.</param>
    /// <param name="status">Optionaler Statusfilter.</param>
    /// <param name="subjectId">Optionaler Fachfilter.</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<KlassenarbeitResponse>>> List(
        [FromQuery] int childId, [FromQuery] KlassenarbeitStatus? status, [FromQuery] int? subjectId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        if (!await access.OwnsChildAsync(User, childId)) return Forbid();

        var query = WithRelations().AsNoTracking().Where(k => k.ChildId == childId);
        if (status is not null) query = query.Where(k => k.Status == status);
        if (subjectId is not null) query = query.Where(k => k.SubjectId == subjectId);

        var list = await query.OrderBy(k => k.ScheduledDate).ThenBy(k => k.Id).ToPagedListAsync(Response, skip, take);
        return list.Select(Map).ToList();
    }

    /// <summary>Eine Klassenarbeit inkl. der direkt zugewiesenen Übungen (nur eigene).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitDetail>> Get(int id)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();

        var exIds = k.Exercises.Select(x => x.ExerciseId).ToList();
        var exercises = await LoadExercisesAsync(e => exIds.Contains(e.Id));
        return new KlassenarbeitDetail(Map(k), exercises);
    }

    // ---- Anlegen / Ändern (nur Vater) ----

    public record CreateDto(int ChildId, string Title, string? Topic, int? SubjectId, DateOnly ScheduledDate,
        KlassenarbeitStatus? Status, decimal? Grade, string? GradeComment, List<int>? ExerciseIds, List<int>? TagIds);

    /// <summary>Plant eine Klassenarbeit (oder trägt eine bereits geschriebene nach). Nur Vater, nur eigene Kinder.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KlassenarbeitDetail>> Create(CreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (!await access.OwnsChildAsync(User, dto.ChildId)) return Forbid();
        if (ValidateGrade(dto.Grade) is { } gradeError) return this.ProblemWithCode(ApiErrors.ValidationError, gradeError);
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid))
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

        if (await BuildExerciseLinksAsync(dto.ChildId, dto.ExerciseIds, k.Exercises) is { } exErr) return this.ProblemWithCode(ApiErrors.InvalidReference, exErr);
        if (await BuildTagLinksAsync(dto.ChildId, dto.TagIds, k.Tags) is { } tagErr) return this.ProblemWithCode(ApiErrors.InvalidReference, tagErr);

        db.Klassenarbeiten.Add(k);
        await db.SaveChangesAsync();

        var created = (await FindOwnedAsync(k.Id))!;
        var exIds = created.Exercises.Select(x => x.ExerciseId).ToList();
        return CreatedAtAction(nameof(Get), new { id = k.Id },
            new KlassenarbeitDetail(Map(created), await LoadExercisesAsync(e => exIds.Contains(e.Id))));
    }

    public record UpdateDto(string? Title, string? Topic, int? SubjectId, DateOnly? ScheduledDate,
        KlassenarbeitStatus? Status, decimal? Grade, bool ClearGrade, string? GradeComment);

    /// <summary>Ändert eine Klassenarbeit partiell – u. a. Note nachtragen und Status setzen. Nur Vater.</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitResponse>> Update(int id, UpdateDto dto)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();

        if (dto.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title must not be empty.");
            k.Title = dto.Title.Trim();
        }
        if (dto.Topic is not null) k.Topic = dto.Topic.Trim() is { Length: > 0 } t ? t : null;
        if (dto.SubjectId is { } sid)
        {
            if (!await db.Subjects.AnyAsync(s => s.Id == sid)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
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

        await db.SaveChangesAsync();
        return Map(k);
    }

    /// <summary>Löscht eine Klassenarbeit (Zuordnungen und Tag-Verknüpfungen verschwinden mit). Nur Vater.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();
        db.Klassenarbeiten.Remove(k);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Übungen zuweisen (nur Vater) ----

    public record AssignExercisesDto(List<int> ExerciseIds);

    /// <summary>Weist der Klassenarbeit Übungen direkt zu (bereits zugewiesene werden übersprungen). Nur Vater.</summary>
    [HttpPost("{id:int}/exercises")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitDetail>> AssignExercises(int id, AssignExercisesDto dto)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();
        if (dto.ExerciseIds is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one exercise is required.");
        if (await BuildExerciseLinksAsync(k.ChildId, dto.ExerciseIds, k.Exercises) is { } error) return this.ProblemWithCode(ApiErrors.InvalidReference, error);

        await db.SaveChangesAsync();
        var exIds = k.Exercises.Select(x => x.ExerciseId).ToList();
        return new KlassenarbeitDetail(Map(k), await LoadExercisesAsync(e => exIds.Contains(e.Id)));
    }

    /// <summary>Entfernt die direkte Zuordnung einer Übung. Nur Vater.</summary>
    [HttpDelete("{id:int}/exercises/{exerciseId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignExercise(int id, int exerciseId)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();
        var link = k.Exercises.FirstOrDefault(x => x.ExerciseId == exerciseId);
        if (link is null) return NotFound();
        db.KlassenarbeitExercises.Remove(link);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Verknüpft einen Tag mit der Klassenarbeit: alle so markierten Übungen gelten als relevant. Nur Vater.</summary>
    [HttpPost("{id:int}/tags/{tagId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KlassenarbeitResponse>> LinkTag(int id, int tagId)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();
        if (!await db.Tags.AnyAsync(t => t.Id == tagId && t.ChildId == k.ChildId))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "The tag does not belong to this child.");
        if (k.Tags.All(t => t.TagId != tagId))
        {
            k.Tags.Add(new KlassenarbeitTag { TagId = tagId });
            await db.SaveChangesAsync();
        }
        return Map((await FindOwnedAsync(id))!);
    }

    /// <summary>Löst die Verknüpfung eines Tags mit der Klassenarbeit. Nur Vater.</summary>
    [HttpDelete("{id:int}/tags/{tagId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkTag(int id, int tagId)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();
        var link = k.Tags.FirstOrDefault(t => t.TagId == tagId);
        if (link is null) return NotFound();
        db.KlassenarbeitTags.Remove(link);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Üben / Wiederholen ----

    /// <summary>Relevante Übungen einer Klassenarbeit zum gezielten Üben (Tage bis zum Termin inklusive).</summary>
    public record PracticeResponse(int KlassenarbeitId, string Title, DateOnly ScheduledDate, int DaysUntil,
        IReadOnlyList<ExerciseBrief> Exercises);

    /// <summary>
    /// Alle für die Klassenarbeit relevanten Übungen: direkt zugewiesene UND über verknüpfte Tags markierte
    /// (ohne Dubletten). Grundlage zum gezielten Üben für eine anstehende Arbeit.
    /// </summary>
    [HttpGet("{id:int}/practice")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PracticeResponse>> Practice(int id)
    {
        var k = await FindOwnedAsync(id);
        if (k is null) return NotFound();

        var exercises = await LoadRelevantExercisesAsync(new[] { id });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new PracticeResponse(k.Id, k.Title, k.ScheduledDate, k.ScheduledDate.DayNumber - today.DayNumber, exercises);
    }

    /// <summary>Übungen, die wegen schlecht benoteter Klassenarbeiten wiederholt werden sollten.</summary>
    public record RepeatResponse(decimal MinBadGrade, IReadOnlyList<KlassenarbeitResponse> Sources,
        IReadOnlyList<ExerciseBrief> Exercises);

    /// <summary>
    /// Sammelt die relevanten Übungen aller geschriebenen Klassenarbeiten eines Kindes, deren Note
    /// mindestens <paramref name="minBadGrade"/> (Standard 4,0) beträgt – zum gezielten Wiederholen.
    /// </summary>
    [HttpGet("repeat")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RepeatResponse>> Repeat([FromQuery] int childId, [FromQuery] decimal? minBadGrade)
    {
        if (!await access.OwnsChildAsync(User, childId)) return Forbid();
        var threshold = minBadGrade ?? DefaultBadGrade;

        var sources = await WithRelations()
            .Where(k => k.ChildId == childId && k.Status == KlassenarbeitStatus.Written
                        && k.Grade != null && k.Grade >= threshold)
            .OrderByDescending(k => k.ScheduledDate)
            .ToListAsync();

        var exercises = sources.Count == 0
            ? new List<ExerciseBrief>()
            : await LoadRelevantExercisesAsync(sources.Select(k => k.Id).ToList());

        return new RepeatResponse(threshold, sources.Select(Map).ToList(), exercises);
    }

    // ---- Helfer ----

    /// <summary>Lädt Übungen nach Prädikat inkl. Kapitel/Fach, sortiert und ohne Tracking.</summary>
    private async Task<List<ExerciseBrief>> LoadExercisesAsync(
        System.Linq.Expressions.Expression<Func<Exercise, bool>> predicate)
    {
        var exercises = await db.Exercises
            .Where(predicate)
            .Include(e => e.Chapter!).ThenInclude(c => c.Subject)
            .OrderBy(e => e.Chapter!.SubjectId).ThenBy(e => e.ChapterId).ThenBy(e => e.OrderIndex)
            .AsNoTracking()
            .ToListAsync();
        return exercises.Select(ExerciseBrief.From).ToList();
    }

    /// <summary>
    /// Vereinigt (dublettenfrei) die direkt zugewiesenen und die über verknüpfte Tags relevanten
    /// Übungen der angegebenen Klassenarbeiten.
    /// </summary>
    private async Task<List<ExerciseBrief>> LoadRelevantExercisesAsync(IReadOnlyCollection<int> klassenarbeitIds)
    {
        var directIds = await db.KlassenarbeitExercises
            .Where(x => klassenarbeitIds.Contains(x.KlassenarbeitId))
            .Select(x => x.ExerciseId).ToListAsync();
        var tagIds = await db.KlassenarbeitTags
            .Where(x => klassenarbeitIds.Contains(x.KlassenarbeitId))
            .Select(x => x.TagId).ToListAsync();

        return await LoadExercisesAsync(e => directIds.Contains(e.Id)
            || db.ExerciseTags.Any(et => et.ExerciseId == e.Id && tagIds.Contains(et.TagId)));
    }

    /// <summary>Prüft die Übungs-Ids und hängt neue Zuordnungen an; gibt eine Fehlermeldung zurück oder null.</summary>
    private async Task<string?> BuildExerciseLinksAsync(int childId, List<int>? exerciseIds, List<KlassenarbeitExercise> target)
    {
        if (exerciseIds is not { Count: > 0 }) return null;
        var ids = exerciseIds.Distinct().ToList();
        var known = await db.Exercises.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync();
        var missing = ids.Except(known).ToList();
        if (missing.Count > 0) return $"Unknown exercise IDs: {string.Join(", ", missing)}";

        var already = target.Select(x => x.ExerciseId).ToHashSet();
        foreach (var exId in ids.Where(exId => already.Add(exId)))
            target.Add(new KlassenarbeitExercise { ExerciseId = exId });
        return null;
    }

    /// <summary>Prüft die Tag-Ids (müssen zum Kind gehören) und hängt neue Verknüpfungen an.</summary>
    private async Task<string?> BuildTagLinksAsync(int childId, List<int>? tagIds, List<KlassenarbeitTag> target)
    {
        if (tagIds is not { Count: > 0 }) return null;
        var ids = tagIds.Distinct().ToList();
        var known = await db.Tags.Where(t => ids.Contains(t.Id) && t.ChildId == childId).Select(t => t.Id).ToListAsync();
        var invalid = ids.Except(known).ToList();
        if (invalid.Count > 0) return $"Tags do not belong to this child or do not exist: {string.Join(", ", invalid)}";

        var already = target.Select(x => x.TagId).ToHashSet();
        foreach (var tagId in ids.Where(tagId => already.Add(tagId)))
            target.Add(new KlassenarbeitTag { TagId = tagId });
        return null;
    }
}
