using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>Cloze store: learning material for the cloze method (maintained by the adult).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/cloze-texts")]
[Tags("Creator – Cloze Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ClozeTextsController(PuglingDbContext db) : ControllerBase
{
    static ClozeResponse Map(ClozeText c) =>
        new(c.Id, c.Key, c.Title, c.SourceLanguage, c.TargetLanguage, c.Text, c.Translation, c.Gaps, c.WordBank, c.CreatedAt);

    /// <summary>List of cloze texts, optionally filtered by full text.</summary>
    /// <param name="search">Free text in title, text, or key (substring).</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<ClozeResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.ClozeTexts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) || c.Text.Contains(search) || c.Key.Contains(search));
        var items = await query.OrderBy(c => c.Key).ToPagedListAsync(Response, skip, take, ct);
        return items.Select(Map);
    }

    /// <summary>A cloze text by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClozeResponse>> Get(int id, CancellationToken ct = default) =>
        await db.ClozeTexts.FindAsync([id], ct) is { } c ? Map(c) : NotFound();

    /// <summary>A cloze text by key.</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClozeResponse>> GetByKey(string key, CancellationToken ct = default) =>
        await db.ClozeTexts.AsNoTracking().FirstOrDefaultAsync(c => c.Key == key, ct) is { } c ? Map(c) : NotFound();

    /// <summary>Creates a cloze text. Key must be unique; at least one gap.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClozeResponse>> Create(CreateClozeDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Key)) return this.ProblemWithCode(ApiErrors.ValidationError, "Key is required.");
        if (string.IsNullOrWhiteSpace(dto.Text)) return this.ProblemWithCode(ApiErrors.ValidationError, "Text is required.");
        if (dto.Gaps is null or { Count: 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one gap is required.");
        if (await db.ClozeTexts.AnyAsync(c => c.Key == dto.Key, ct)) return this.ProblemWithCode(ApiErrors.DuplicateKey, $"Key '{dto.Key}' already exists.");

        var cloze = new ClozeText
        {
            Key = dto.Key.Trim(),
            Title = dto.Title,
            SourceLanguage = dto.SourceLanguage,
            TargetLanguage = dto.TargetLanguage,
            Text = dto.Text,
            Translation = dto.Translation,
            Gaps = dto.Gaps,
            WordBank = dto.WordBank,
        };
        db.ClozeTexts.Add(cloze);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = cloze.Id }, Map(cloze));
    }

    /// <summary>Changes a cloze text (partial).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClozeResponse>> Update(int id, UpdateClozeDto dto, CancellationToken ct = default)
    {
        var cloze = await db.ClozeTexts.FindAsync([id], ct);
        if (cloze is null) return NotFound();

        if (dto.Gaps is { Count: 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one gap is required.");
        if (dto.Title is not null) cloze.Title = dto.Title;
        if (dto.Text is not null) cloze.Text = dto.Text;
        if (dto.Gaps is not null) cloze.Gaps = dto.Gaps;
        // Erst der Wert, dann der Lösch-Schalter: schickt ein Formular beides, gewinnt „leeren".
        if (dto.Translation is not null) cloze.Translation = dto.Translation;
        if (dto.ClearTranslation) cloze.Translation = null;
        if (dto.WordBank is not null) cloze.WordBank = dto.WordBank;
        if (dto.ClearWordBank) cloze.WordBank = null;
        await db.SaveChangesAsync(ct);
        return Map(cloze);
    }

    /// <summary>Deletes a cloze text. Not possible while it is used in a study plan.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var cloze = await db.ClozeTexts.FindAsync([id], ct);
        if (cloze is null) return NotFound();
        db.ClozeTexts.Remove(cloze);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
