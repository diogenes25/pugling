using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The shared grammar-topic vocabulary (B-155): "present perfect", "passé composé" - one entry per subject and
/// phenomenon, carried by exercises and by textbook units, so a supervisor can fill a plan by topic across series
/// and grades. Reading is open to every creator; changing and deleting follow the owner pattern of the subject
/// (<c>SubjectsController</c>): anyone may create, only the creator who made a topic may change or delete it, and a
/// seeded topic (no owner) is changeable by nobody - it carries search semantics in other people's plans.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/grammar-topics")]
[Tags("Creator – Grammar Topics")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class GrammarTopicsController(PuglingDbContext db) : ControllerBase
{
    private IQueryable<GrammarTopicResponse> Project(IQueryable<GrammarTopic> q)
    {
        var fid = User.CreatorId();
        return q.Select(t => new GrammarTopicResponse(t.Id, t.Slug, t.Label, t.Synonyms, t.Color, t.SubjectId,
            t.OwnerAdultId, fid != null && t.OwnerAdultId == fid, t.ExerciseLinks.Count, t.UnitLinks.Count, t.CreatedAt));
    }

    /// <summary>
    /// The topics, by label, optionally narrowed to a subject or a search term. The total count (before paging)
    /// is in the <c>X-Total-Count</c> header.
    /// </summary>
    /// <param name="subjectId">Only topics of this subject - the picker in the exercise and unit forms asks like this.</param>
    /// <param name="search">Substring in slug or label.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<GrammarTopicResponse>> List(
        [FromQuery] int? subjectId = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.GrammarTopics.AsNoTracking().AsQueryable();
        if (subjectId is int sid) query = query.Where(t => t.SubjectId == sid);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE, not Contains: EF maps Contains to SQLite's byte-exact instr() - see SearchPattern (B-135). The
            // synonyms are not searched here: they sit in a JSON column, and a LIKE over the serialized list would
            // be a query on a representation - the picker filters them in memory where it needs to.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(t => EF.Functions.Like(t.Slug, pattern, SearchPattern.Escape)
                                     || EF.Functions.Like(t.Label, pattern, SearchPattern.Escape));
        }
        return await Project(query.OrderBy(t => t.Label).ThenBy(t => t.Id)).ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A topic by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrammarTopicResponse>> Get(int id, CancellationToken ct = default)
    {
        var topic = await Project(db.GrammarTopics.AsNoTracking().Where(t => t.Id == id)).FirstOrDefaultAsync(ct);
        return topic is null ? NotFound() : topic;
    }

    /// <summary>
    /// Creates a topic. The slug is derived from the subject and the label; if it already exists, the existing
    /// entry comes back (idempotent, 200) - so the same label typed twice never becomes two topics.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GrammarTopicResponse>> Create(CreateGrammarTopicDto dto, CancellationToken ct = default)
    {
        var subject = await db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.SubjectId, ct);
        if (subject is null) return this.ProblemWithCode(ApiErrors.ValidationError, "Unknown subject.");
        var (slug, problem) = this.DeriveRequiredSlug(dto.Label, "Label");
        if (problem is not null) return problem;
        // Subject-qualified (decision 4): a phenomenon belongs to a language, "Konjunktiv" in German and in Latin
        // are two entries. The subject name is the qualifier because a subject carries no language code.
        var qualified = $"{InterestSlug.From(subject.Name)}-{slug}";

        var existing = await Project(db.GrammarTopics.AsNoTracking().Where(t => t.Slug == qualified)).FirstOrDefaultAsync(ct);
        if (existing is not null) return Ok(existing);

        var topic = new GrammarTopic
        {
            Slug = qualified,
            Label = dto.Label.Trim(),
            Synonyms = Clean(dto.Synonyms),
            Color = dto.Color?.Trim() is { Length: > 0 } c ? c : null,
            SubjectId = subject.Id,
            OwnerAdultId = User.CreatorId(),
        };
        db.GrammarTopics.Add(topic);
        await db.SaveChangesAsync(ct);

        // Both usage counts are zero by construction: nothing can point at a topic that exists as of this line.
        return CreatedAtAction(nameof(Get), new { id = topic.Id },
            new GrammarTopicResponse(topic.Id, topic.Slug, topic.Label, topic.Synonyms, topic.Color, topic.SubjectId,
                topic.OwnerAdultId, topic.OwnerAdultId != null, 0, 0, topic.CreatedAt));
    }

    /// <summary>
    /// Changes label, synonyms or color (owner only; a seeded topic is changeable by nobody). The slug and the
    /// subject are deliberately <b>immutable</b>: the slug is the stable reference exercises and units hang off,
    /// and a topic that changed its subject would silently leave the exercises it is on.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrammarTopicResponse>> Update(int id, UpdateGrammarTopicDto dto, CancellationToken ct = default)
    {
        var topic = await db.GrammarTopics.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (topic is null) return NotFound();
        if (!OwnedByCaller(topic))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may change this grammar topic; a seeded topic has none.");

        if (dto.Label is not null)
        {
            var label = dto.Label.Trim();
            if (label.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Label must not be empty.");
            topic.Label = label;
        }
        // Assign a new list (no in-place mutation - the JSON column pitfall).
        if (dto.Synonyms is not null) topic.Synonyms = Clean(dto.Synonyms);
        if (dto.Color is not null) topic.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync(ct);
        return await Project(db.GrammarTopics.AsNoTracking().Where(t => t.Id == id)).FirstAsync(ct);
    }

    /// <summary>
    /// Deletes a topic nobody uses (owner only). A topic carried by an exercise or a unit is refused with 409: the
    /// assignment is authored information nothing else records, so a cascade would erase it silently. Ownership is
    /// decided before usage, as with the subject - a stranger must not learn from the 409 what the catalog holds.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var topic = await db.GrammarTopics.FindAsync([id], ct);
        if (topic is null) return NotFound();
        if (!OwnedByCaller(topic))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may delete this grammar topic; a seeded topic has none.");

        if (await db.ExerciseGrammarTopics.AnyAsync(l => l.GrammarTopicId == id, ct)
            || await db.SeriesUnitGrammarTopics.AnyAsync(l => l.GrammarTopicId == id, ct))
            return this.ProblemWithCode(ApiErrors.GrammarTopicInUse,
                "This grammar topic is carried by exercises or units. Remove it from them first.");

        db.GrammarTopics.Remove(topic);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Fail closed: a seeded topic (no owner) belongs to nobody, so nobody - not even an admin - may change it.</summary>
    private bool OwnedByCaller(GrammarTopic topic) =>
        topic.OwnerAdultId is int owner && owner == User.CreatorId();

    /// <summary>Trims, discards empty entries, and deduplicates - synonyms are a pure search aid.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
