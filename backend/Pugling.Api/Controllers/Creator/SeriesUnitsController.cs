using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The units of a textbook series. Volume and unit live at <b>one</b> level (<c>grade</c> = volume):
/// "Access 8, Unit 3" is one row. The subject-matter value lives in <c>topics</c>/<c>grammar</c>/
/// <c>vocabularyNotes</c> – that is the material an AI creator must know instead of inventing it.
/// Any creator may read, only the owner of the series may write.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/textbook-series/{seriesId:int}/units")]
[Tags("Creator – Textbook Series")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class SeriesUnitsController(PuglingDbContext db) : ControllerBase
{
    // Expects the topic links loaded: every read path includes them, both write paths hold the tracked entity.
    static SeriesUnitResponse Map(SeriesUnit u) =>
        new(u.Id, u.SeriesId, u.Grade, u.OrderIndex, u.Label, u.BookType, u.Topics, u.Grammar, u.VocabularyNotes, u.CreatedAt,
            [.. u.GrammarTopicLinks.Select(l => l.GrammarTopicId).OrderBy(id => id)]);

    /// <summary>All units of the series, by volume and order.</summary>
    /// <param name="seriesId">The series.</param>
    /// <param name="grade">Only units of this volume.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeriesUnitResponse>>> List(int seriesId,
        [FromQuery] int? grade, CancellationToken ct)
    {
        if (!await db.TextbookSeries.AnyAsync(s => s.Id == seriesId, ct)) return NotFound();

        var query = db.SeriesUnits.AsNoTracking().Include(u => u.GrammarTopicLinks).Where(u => u.SeriesId == seriesId);
        if (grade is int g) query = query.Where(u => u.Grade == g);

        // Materialize first: the projection reads the loaded topic links, which a client-side Select would miss.
        var units = await query.OrderBy(u => u.Grade).ThenBy(u => u.OrderIndex).ThenBy(u => u.Id).ToListAsync(ct);
        return units.Select(Map).ToList();
    }

    /// <summary>A unit of the series.</summary>
    [HttpGet("{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesUnitResponse>> Get(int seriesId, int unitId, CancellationToken ct)
    {
        var unit = await db.SeriesUnits.AsNoTracking().Include(u => u.GrammarTopicLinks)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.SeriesId == seriesId, ct);
        return unit is null ? NotFound() : Map(unit);
    }

    /// <summary>Appends a unit to the series (owner only). Without <c>orderIndex</c> it lands at the end of the volume.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesUnitResponse>> Create(int seriesId, CreateSeriesUnitDto dto, CancellationToken ct)
    {
        var series = await db.TextbookSeries.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the series may add units.");
        if (string.IsNullOrWhiteSpace(dto.Label)) return this.ProblemWithCode(ApiErrors.ValidationError, "Label is required.");

        var topics = CleanTopics(dto.Topics);
        if (ValidateTopics(topics) is { } topicProblem) return topicProblem;
        if (await GrammarTopicsProblemAsync(series.SubjectId, dto.GrammarTopicIds, ct) is { } grammarProblem) return grammarProblem;

        var unit = new SeriesUnit
        {
            SeriesId = seriesId,
            Grade = dto.Grade,
            OrderIndex = dto.OrderIndex ?? await NextOrderIndexAsync(seriesId, dto.Grade, ct),
            Label = dto.Label.Trim(),
            BookType = dto.BookType ?? BookType.Textbook,
            Topics = topics,
            Grammar = Trimmed(dto.Grammar),
            VocabularyNotes = Trimmed(dto.VocabularyNotes),
        };
        SetGrammarLinks(unit, dto.GrammarTopicIds);
        db.SeriesUnits.Add(unit);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { seriesId, unitId = unit.Id }, Map(unit));
    }

    /// <summary>Changes a unit (partial, owner of the series only).</summary>
    [HttpPatch("{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesUnitResponse>> Update(int seriesId, int unitId,
        UpdateSeriesUnitDto dto, CancellationToken ct)
    {
        var unit = await db.SeriesUnits.Include(u => u.Series).Include(u => u.GrammarTopicLinks)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.SeriesId == seriesId, ct);
        if (unit is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(unit.Series?.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the series may change its units.");
        if (await GrammarTopicsProblemAsync(unit.Series?.SubjectId, dto.GrammarTopicIds, ct) is { } grammarProblem) return grammarProblem;

        if (dto.Label is not null)
        {
            var label = dto.Label.Trim();
            if (label.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Label must not be empty.");
            unit.Label = label;
        }
        if (dto.Grade.HasValue) unit.Grade = dto.Grade;
        if (dto.OrderIndex.HasValue) unit.OrderIndex = dto.OrderIndex.Value;
        if (dto.BookType.HasValue) unit.BookType = dto.BookType.Value;
        // Assign a new list (no in-place mutation - the JSON column pitfall, cf. CLAUDE.md).
        if (dto.Topics is not null)
        {
            var topics = CleanTopics(dto.Topics);
            if (ValidateTopics(topics) is { } topicProblem) return topicProblem;
            unit.Topics = topics;
        }
        if (dto.Grammar is not null) unit.Grammar = Trimmed(dto.Grammar);
        if (dto.VocabularyNotes is not null) unit.VocabularyNotes = Trimmed(dto.VocabularyNotes);
        // PATCH semantics: null leaves the topics alone, a list replaces them entirely.
        if (dto.GrammarTopicIds is not null) SetGrammarLinks(unit, dto.GrammarTopicIds);

        await db.SaveChangesAsync(ct);
        return Map(unit);
    }

    /// <summary>
    /// Deletes a unit along with its exercises (owner of the series only). Not possible while an
    /// exercise in it is used in a study plan, a class test or an objective milestone, or while an objective milestone
    /// points at the unit itself (<c>409 unit_content_in_use</c>, B-413). Child textbooks pointing to the unit itself only lose the assignment (SetNull) – the child's learning progress
    /// does not hang off the unit directly.
    /// </summary>
    [HttpDelete("{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int seriesId, int unitId, CancellationToken ct)
    {
        var unit = await db.SeriesUnits.Include(u => u.Series)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.SeriesId == seriesId, ct);
        if (unit is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(unit.Series?.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the series may delete its units.");
        // Unit→exercise cascades, PlanPosition→Exercise is Restrict - cf. ExerciseControllerBase.Delete.
        if (await ExerciseUsageQueries.AnyBlockingAsync(db,
                db.Exercises.Where(x => x.SeriesUnitId == unitId),
                db.SeriesUnits.Where(u => u.Id == unitId), ct))
            return this.ProblemWithCode(ApiErrors.UnitContentInUse,
                "Cannot delete: an exercise of this unit is still used in a study plan, a class test or an objective "
                + "milestone, or an objective milestone points at the unit itself.");

        db.SeriesUnits.Remove(unit);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Next free position in the volume – so the caller does not need to know the order.</summary>
    private async Task<int> NextOrderIndexAsync(int seriesId, int? grade, CancellationToken ct)
    {
        var highest = await db.SeriesUnits
            .Where(u => u.SeriesId == seriesId && u.Grade == grade)
            .Select(u => (int?)u.OrderIndex)
            .MaxAsync(ct);
        return (highest ?? 0) + 1;
    }

    private static string? Trimmed(string? value) => value?.Trim() is { Length: > 0 } v ? v : null;

    /// <summary>
    /// Validates the unit's grammar topics (B-155): every id must exist and belong to the series' subject - the
    /// same two rules the exercise applies, so a unit cannot carry a topic its exercises could not. A series
    /// without a subject can carry no topics at all.
    /// </summary>
    private async Task<ObjectResult?> GrammarTopicsProblemAsync(int? subjectId, List<int>? topicIds, CancellationToken ct)
    {
        if (topicIds is null || topicIds.Count == 0) return null;
        var wanted = topicIds.Distinct().ToList();
        var known = await db.GrammarTopics.Where(t => wanted.Contains(t.Id))
            .Select(t => new { t.Id, t.SubjectId }).ToListAsync(ct);
        var unknown = wanted.Except(known.Select(k => k.Id)).OrderBy(id => id).ToList();
        if (unknown.Count > 0)
            return this.ProblemWithCode(ApiErrors.InvalidReference, $"Unknown grammar topic id(s): {string.Join(", ", unknown)}.");
        // Fail closed, but with the true reason: a series without a subject can carry no topic at all.
        if (subjectId is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "This series has no subject; it cannot carry grammar topics.");
        var foreign = known.Where(k => k.SubjectId != subjectId).Select(k => k.Id).OrderBy(id => id).ToList();
        return foreign.Count == 0
            ? null
            : this.ProblemWithCode(ApiErrors.ValidationError,
                $"Grammar topic(s) {string.Join(", ", foreign)} belong to another subject than this series.");
    }

    /// <summary>Sets the unit's grammar topics as the complete set - replacement, not merge (the exercise pattern).</summary>
    private void SetGrammarLinks(SeriesUnit unit, List<int>? topicIds)
    {
        var wanted = (topicIds ?? []).Distinct().ToHashSet();
        var stale = unit.GrammarTopicLinks.Where(l => !wanted.Contains(l.GrammarTopicId)).ToList();
        db.SeriesUnitGrammarTopics.RemoveRange(stale);
        foreach (var link in stale) unit.GrammarTopicLinks.Remove(link);
        foreach (var topicId in wanted.Where(id => unit.GrammarTopicLinks.All(l => l.GrammarTopicId != id)))
            unit.GrammarTopicLinks.Add(new SeriesUnitGrammarTopic { GrammarTopicId = topicId });
    }

    /// <summary>Trims and discards empty entries – topics are a plain list, not a validated taxonomy.</summary>
    private static List<string> CleanTopics(List<string>? values) =>
        [.. (values ?? []).Select(t => t.Trim()).Where(t => t.Length > 0)];

    /// <summary>Longest single topic - the length this field carried as a column before it became JSON.</summary>
    private const int MaxTopicLength = 200;

    /// <summary>Most topics per unit. Generous on purpose: a real unit lists a handful, not dozens.</summary>
    private const int MaxTopics = 50;

    /// <summary>
    /// Bounds the topic list. <c>SeriesUnit.Topics</c> is an intentionally unlimited JSON column
    /// (<c>PuglingDbContext.UnlimitedByDesign</c>) - but that exception covers the <b>column</b>, not the
    /// entries inside it. While a topic was a 200-character column the database held the line; since the
    /// type changed, nothing did, and an authenticated caller could store arbitrarily much text (B-130).
    ///
    /// Rejects instead of truncating: a silently shortened topic is exactly what the AI creator reads as
    /// the material it may dress up but not replace - a half topic would quietly become a wrong one.
    /// </summary>
    private ObjectResult? ValidateTopics(List<string> topics)
    {
        if (topics.Count > MaxTopics)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"A unit takes at most {MaxTopics} topics; {topics.Count} were sent.");
        if (topics.Find(t => t.Length > MaxTopicLength) is { } tooLong)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"A topic must not exceed {MaxTopicLength} characters; one has {tooLong.Length}.");
        return null;
    }
}
