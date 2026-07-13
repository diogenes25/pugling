using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Verwaltung der vom Kind verwendeten Lehrbücher (nur Vater, nur eigene Kinder). Übungsunabhängiges Profil:
/// hält fest, aus welchem Werk und welchem aktuellen Kapitel der Lernstoff kommt – die Grundlage, aus der ein
/// späterer Lehrplan-Generator „was ist gerade dran" ableitet (siehe wiki/09-llm-kochbuch.md). Eigentum
/// sichert der <see cref="ChildOwnershipFilter"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/textbooks")]
[Tags("Supervisor – Textbooks")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class TextbooksController(PuglingDbContext db) : ControllerBase
{
    public record TextbookResponse(int Id, string Title, string? SubjectName, int? SubjectId, int? Grade,
        string? Publisher, string? Isbn, string? CurrentChapter, DateTime CreatedAt);

    static TextbookResponse Map(Textbook t) =>
        new(t.Id, t.Title, t.SubjectName, t.SubjectId, t.Grade, t.Publisher, t.Isbn, t.CurrentChapter, t.CreatedAt);

    /// <summary>Alle Lehrbücher des Kindes.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TextbookResponse>>> List(int childId, CancellationToken ct) =>
        await db.Textbooks.AsNoTracking().Where(t => t.ChildId == childId)
            .OrderBy(t => t.SubjectName).ThenBy(t => t.Title)
            .Select(t => Map(t)).ToListAsync(ct);

    /// <summary>Ein einzelnes Lehrbuch des Kindes.</summary>
    [HttpGet("{textbookId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookResponse>> Get(int childId, int textbookId, CancellationToken ct)
    {
        var book = await db.Textbooks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == textbookId && t.ChildId == childId, ct);
        return book is null ? NotFound() : Map(book);
    }

    public record CreateTextbookDto(string Title, string? SubjectName, int? SubjectId, int? Grade,
        string? Publisher, string? Isbn, string? CurrentChapter);

    /// <summary>Legt ein Lehrbuch für das Kind an.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookResponse>> Create(int childId, CreateTextbookDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (dto.SubjectId is int sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.ValidationError, "SubjectId does not reference an existing subject.");

        var book = new Textbook
        {
            ChildId = childId,
            Title = dto.Title.Trim(),
            SubjectName = dto.SubjectName?.Trim(),
            SubjectId = dto.SubjectId,
            Grade = dto.Grade,
            Publisher = dto.Publisher?.Trim(),
            Isbn = dto.Isbn?.Trim(),
            CurrentChapter = dto.CurrentChapter?.Trim(),
        };
        db.Textbooks.Add(book);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { childId, textbookId = book.Id }, Map(book));
    }

    public record UpdateTextbookDto(string? Title, string? SubjectName, int? SubjectId, int? Grade,
        string? Publisher, string? Isbn, string? CurrentChapter);

    /// <summary>Ändert ein Lehrbuch (partiell). Setzt Felder nur, wenn sie im Payload enthalten sind.</summary>
    [HttpPatch("{textbookId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookResponse>> Update(int childId, int textbookId, UpdateTextbookDto dto, CancellationToken ct)
    {
        var book = await db.Textbooks.FirstOrDefaultAsync(t => t.Id == textbookId && t.ChildId == childId, ct);
        if (book is null) return NotFound();
        if (dto.SubjectId is int sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.ValidationError, "SubjectId does not reference an existing subject.");

        if (dto.Title is not null) book.Title = dto.Title.Trim();
        if (dto.SubjectName is not null) book.SubjectName = dto.SubjectName.Trim();
        if (dto.SubjectId.HasValue) book.SubjectId = dto.SubjectId;
        if (dto.Grade.HasValue) book.Grade = dto.Grade;
        if (dto.Publisher is not null) book.Publisher = dto.Publisher.Trim();
        if (dto.Isbn is not null) book.Isbn = dto.Isbn.Trim();
        if (dto.CurrentChapter is not null) book.CurrentChapter = dto.CurrentChapter.Trim();
        await db.SaveChangesAsync(ct);
        return Map(book);
    }

    /// <summary>Löscht ein Lehrbuch des Kindes.</summary>
    [HttpDelete("{textbookId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int textbookId, CancellationToken ct)
    {
        var book = await db.Textbooks.FirstOrDefaultAsync(t => t.Id == textbookId && t.ChildId == childId, ct);
        if (book is null) return NotFound();
        db.Textbooks.Remove(book);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
