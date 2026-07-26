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
    /// <summary>
    /// Projiziert samt Reihe und Unit. Die Namen kommen mit, damit ein Aufrufer die Zuordnung anzeigen
    /// kann, ohne den Katalog nachzuladen.
    /// </summary>
    private static IQueryable<TextbookResponse> Project(IQueryable<Textbook> q) =>
        q.Select(t => new TextbookResponse(t.Id, t.Title, t.SubjectName, t.SubjectId, t.Grade, t.Publisher,
            t.Isbn, t.CurrentChapter, t.CreatedAt,
            t.SeriesId, t.Series!.Name, t.CurrentUnitId, t.CurrentUnit!.Label));

    /// <summary>Alle Lehrbücher des Kindes.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TextbookResponse>>> List(int childId, CancellationToken ct) =>
        await Project(db.Textbooks.AsNoTracking().Where(t => t.ChildId == childId)
                .OrderBy(t => t.SubjectName).ThenBy(t => t.Title))
            .ToListAsync(ct);

    /// <summary>Ein einzelnes Lehrbuch des Kindes.</summary>
    [HttpGet("{textbookId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookResponse>> Get(int childId, int textbookId, CancellationToken ct)
    {
        var book = await Project(db.Textbooks.AsNoTracking()
            .Where(t => t.Id == textbookId && t.ChildId == childId)).FirstOrDefaultAsync(ct);
        return book is null ? NotFound() : book;
    }

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
        if (await CatalogProblemAsync(dto.SeriesId, dto.CurrentUnitId, ct) is { } problem) return problem;

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
            SeriesId = dto.SeriesId,
            CurrentUnitId = dto.CurrentUnitId,
        };
        db.Textbooks.Add(book);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { childId, textbookId = book.Id },
            await Project(db.Textbooks.AsNoTracking().Where(t => t.Id == book.Id)).FirstAsync(ct));
    }

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
        /*
         * Gegen den Zielzustand prüfen, nicht gegen den Payload: wer nur die Unit nachträgt, muss die schon
         * gesetzte Reihe nicht mitschicken – die Unit muss aber zu ihr gehören.
         *
         * Beim **Reihenwechsel** ist die gespeicherte Unit hinfällig: sie gehört zur alten Reihe. Sie hier
         * mitzuprüfen hieße, den Wechsel an einer Unit scheitern zu lassen, die der Aufrufer gerade ersetzt –
         * bei einer Reihe ohne Units wäre er nie möglich. Sie fällt darum weg, statt zu blockieren.
         */
        var seriesId = dto.ClearSeries ? null : dto.SeriesId ?? book.SeriesId;
        var seriesChanged = seriesId != book.SeriesId;
        var unitId = dto.ClearSeries || dto.ClearUnit ? null
            : dto.CurrentUnitId ?? (seriesChanged ? null : book.CurrentUnitId);
        if (await CatalogProblemAsync(seriesId, unitId, ct) is { } problem) return problem;

        if (dto.Title is not null) book.Title = dto.Title.Trim();
        if (dto.SubjectName is not null) book.SubjectName = dto.SubjectName.Trim();
        if (dto.SubjectId.HasValue) book.SubjectId = dto.SubjectId;
        if (dto.ClearSubject) { book.SubjectId = null; book.SubjectName = null; }
        if (dto.Grade.HasValue) book.Grade = dto.Grade;
        if (dto.ClearGrade) book.Grade = null;
        if (dto.Publisher is not null) book.Publisher = dto.Publisher.Trim();
        if (dto.Isbn is not null) book.Isbn = dto.Isbn.Trim();
        if (dto.CurrentChapter is not null) book.CurrentChapter = dto.CurrentChapter.Trim();
        // Genau den oben geprüften Zielzustand schreiben – sonst könnten Prüfung und Ergebnis auseinanderlaufen.
        book.SeriesId = seriesId;
        book.CurrentUnitId = unitId;
        await db.SaveChangesAsync(ct);
        return await Project(db.Textbooks.AsNoTracking().Where(t => t.Id == textbookId)).FirstAsync(ct);
    }

    /// <summary>
    /// Prüft die Katalog-Verweise: die Reihe muss existieren, und die Unit muss <b>zu dieser Reihe</b>
    /// gehören. Ohne die zweite Prüfung stünde am Kind eine Unit aus einem fremden Werk – der Creator
    /// bekäme dann den Stoff eines Buchs, das das Kind nicht benutzt.
    /// </summary>
    private async Task<ObjectResult?> CatalogProblemAsync(int? seriesId, int? unitId, CancellationToken ct)
    {
        if (seriesId is int sid && !await db.TextbookSeries.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.ValidationError, "SeriesId does not reference an existing textbook series.");
        if (unitId is not int uid) return null;

        var unit = await db.SeriesUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid, ct);
        if (unit is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "CurrentUnitId does not reference an existing series unit.");
        if (seriesId is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "CurrentUnitId requires SeriesId to be set.");
        if (unit.SeriesId != seriesId)
            return this.ProblemWithCode(ApiErrors.ValidationError, "CurrentUnitId belongs to a different textbook series.");
        return null;
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
