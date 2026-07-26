using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Die geteilte Interessen-/Stil-Taxonomie – <b>ein</b> kontrolliertes Vokabular für zwei Verbraucher:
/// Bilder tragen die Schlagworte als Eigenschaft (<c>creator/media/{id}/tags</c>), Kinder als gewichtete
/// Vorliebe oder Abneigung (<c>supervisor/children/{id}/interests</c>). Genau diese Doppelnutzung macht
/// die individualisierte Bildauswahl berechenbar; zwei getrennte Vokabulare könnten nur raten.
/// Gepflegt wird sie vom Creator, ist aber – wie der Vokabel-Store – kindneutral und global.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/interest-tags")]
[Tags("Creator – Interest Tags")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class InterestTagsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Projiziert samt Nutzungszahlen beider Seiten – sie zeigen, ob ein Tag „tot" ist.</summary>
    private static IQueryable<InterestTagResponse> Project(IQueryable<InterestTag> q) =>
        q.Select(t => new InterestTagResponse(t.Id, t.Slug, t.Label, t.Facet, t.Synonyms, t.Color,
            t.MediaLinks.Count, t.ChildInterests.Count, t.CreatedAt));

    /// <summary>
    /// Alle Schlagworte (alphabetisch nach Slug), optional gefiltert. Die Gesamtzahl (vor Paging) steht
    /// im Header <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Teilstring in Slug oder Label.</param>
    /// <param name="facet">Nur Schlagworte dieser Facette (z. B. nur Stile).</param>
    /// <param name="unused">true = nur Schlagworte ohne jede Verwendung (Aufräum-Sicht).</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500).</param>
    [HttpGet]
    public async Task<IEnumerable<InterestTagResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] InterestFacet? facet = null,
        [FromQuery] bool? unused = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var query = db.InterestTags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Slug.Contains(search) || t.Label.Contains(search));
        if (facet is not null)
            query = query.Where(t => t.Facet == facet);
        if (unused is true)
            query = query.Where(t => t.MediaLinks.Count == 0 && t.ChildInterests.Count == 0);

        return await Project(query.OrderBy(t => t.Slug).ThenBy(t => t.Id)).ToPagedListAsync(Response, skip, take);
    }

    /// <summary>Ein Schlagwort per Id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterestTagResponse>> Get(int id)
    {
        var tag = await Project(db.InterestTags.AsNoTracking().Where(t => t.Id == id)).FirstOrDefaultAsync();
        return tag is null ? NotFound() : tag;
    }

    /// <summary>
    /// Legt ein Schlagwort an. Fehlt der Slug, wird er aus dem Label abgeleitet. Existiert der Slug
    /// bereits, kommt der bestehende Eintrag zurück (idempotent) – so kann ein Agent denselben
    /// Katalog-Aufbau gefahrlos wiederholen.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InterestTagResponse>> Create(CreateInterestTagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Label)) return this.ProblemWithCode(ApiErrors.ValidationError, "Label is required.");

        var slug = InterestSlug.From(string.IsNullOrWhiteSpace(dto.Slug) ? dto.Label : dto.Slug);
        if (slug.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Label must contain at least one letter or digit.");

        var existing = await Project(db.InterestTags.AsNoTracking().Where(t => t.Slug == slug)).FirstOrDefaultAsync();
        if (existing is not null) return Ok(existing);

        var tag = new InterestTag
        {
            Slug = slug,
            Label = dto.Label.Trim(),
            Facet = dto.Facet,
            Synonyms = Clean(dto.Synonyms),
            Color = dto.Color?.Trim() is { Length: > 0 } c ? c : null,
        };
        db.InterestTags.Add(tag);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = tag.Id },
            new InterestTagResponse(tag.Id, tag.Slug, tag.Label, tag.Facet, tag.Synonyms, tag.Color, 0, 0, tag.CreatedAt));
    }

    /// <summary>
    /// Ändert Label, Facette, Synonyme oder Farbe. Der <c>Slug</c> ist bewusst <b>unveränderlich</b> –
    /// er ist die stabile Referenz, an der Bilder und Kind-Profile hängen.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterestTagResponse>> Update(int id, UpdateInterestTagDto dto)
    {
        var tag = await db.InterestTags.FirstOrDefaultAsync(t => t.Id == id);
        if (tag is null) return NotFound();

        if (dto.Label is not null)
        {
            var label = dto.Label.Trim();
            if (label.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Label must not be empty.");
            tag.Label = label;
        }
        if (dto.Facet.HasValue) tag.Facet = dto.Facet.Value;
        // Neue Liste zuweisen (kein In-Place-Mutieren – JSON-Spalten-Fallstrick).
        if (dto.Synonyms is not null) tag.Synonyms = Clean(dto.Synonyms);
        if (dto.Color is not null) tag.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync();
        return await Project(db.InterestTags.AsNoTracking().Where(t => t.Id == id)).FirstAsync();
    }

    /// <summary>
    /// Löscht ein Schlagwort samt seiner Verknüpfungen zu Bildern und Kindern (Cascade). Bewusst ohne
    /// Verwendungs-Sperre: ein Tag trägt keine Inhalte, sein Verlust kostet nur Auswahl-Qualität.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var tag = await db.InterestTags.FindAsync(id);
        if (tag is null) return NotFound();
        db.InterestTags.Remove(tag);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Trimmt, verwirft Leereinträge und dedupliziert – Synonyme sind reine Suchhilfe.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
