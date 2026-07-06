using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Lückentext-Store: Lerngrundlagen für das Lückentext-Verfahren (vom Vater gepflegt).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/learn/cloze-texts")]
[Tags("Learn – Cloze Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ClozeTextsController(PuglingDbContext db) : ControllerBase
{
    public record ClozeResponse(int Id, string Key, string Title, string SourceLanguage, string TargetLanguage,
        string Text, string? Translation, IReadOnlyList<Gap> Gaps, IReadOnlyList<string>? WordBank, DateTime CreatedAt);

    static ClozeResponse Map(ClozeText c) =>
        new(c.Id, c.Key, c.Title, c.SourceLanguage, c.TargetLanguage, c.Text, c.Translation, c.Gaps, c.WordBank, c.CreatedAt);

    /// <summary>Liste der Lückentexte, optional per Volltext gefiltert.</summary>
    /// <param name="search">Freitext in Titel, Text oder Key (Teilstring).</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet]
    public async Task<IEnumerable<ClozeResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var query = db.ClozeTexts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) || c.Text.Contains(search) || c.Key.Contains(search));
        var items = await query.OrderBy(c => c.Key).ToPagedListAsync(Response, skip, take);
        return items.Select(Map);
    }

    /// <summary>Ein Lückentext per Id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClozeResponse>> Get(int id) =>
        await db.ClozeTexts.FindAsync(id) is { } c ? Map(c) : NotFound();

    /// <summary>Ein Lückentext per Key.</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClozeResponse>> GetByKey(string key) =>
        await db.ClozeTexts.FirstOrDefaultAsync(c => c.Key == key) is { } c ? Map(c) : NotFound();

    public record CreateClozeDto(string Key, string Title, string SourceLanguage, string TargetLanguage,
        string Text, List<Gap> Gaps, string? Translation = null, List<string>? WordBank = null);

    /// <summary>Erstellt einen Lückentext. Key muss eindeutig sein; mind. eine Lücke.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClozeResponse>> Create(CreateClozeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key)) return this.ProblemWithCode(ApiErrors.ValidationError, "Key is required.");
        if (string.IsNullOrWhiteSpace(dto.Text)) return this.ProblemWithCode(ApiErrors.ValidationError, "Text is required.");
        if (dto.Gaps is null or { Count: 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one gap is required.");
        if (await db.ClozeTexts.AnyAsync(c => c.Key == dto.Key)) return this.ProblemWithCode(ApiErrors.DuplicateKey, $"Key '{dto.Key}' already exists.");

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
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = cloze.Id }, Map(cloze));
    }

    public record UpdateClozeDto(string? Title, string? Text, string? Translation, List<Gap>? Gaps, List<string>? WordBank);

    /// <summary>Ändert einen Lückentext (partiell).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClozeResponse>> Update(int id, UpdateClozeDto dto)
    {
        var cloze = await db.ClozeTexts.FindAsync(id);
        if (cloze is null) return NotFound();

        if (dto.Gaps is { Count: 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one gap is required.");
        if (dto.Title is not null) cloze.Title = dto.Title;
        if (dto.Text is not null) cloze.Text = dto.Text;
        if (dto.Translation is not null) cloze.Translation = dto.Translation;
        if (dto.Gaps is not null) cloze.Gaps = dto.Gaps;
        if (dto.WordBank is not null) cloze.WordBank = dto.WordBank;
        await db.SaveChangesAsync();
        return Map(cloze);
    }

    /// <summary>Löscht einen Lückentext. Nicht möglich, solange er in einem Lehrplan verwendet wird.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var cloze = await db.ClozeTexts.FindAsync(id);
        if (cloze is null) return NotFound();
        db.ClozeTexts.Remove(cloze);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
