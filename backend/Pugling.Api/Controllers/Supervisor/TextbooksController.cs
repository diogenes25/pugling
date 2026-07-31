using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Management of the textbooks used by the child (father only, own children only). Exercise-independent profile:
/// records which series and which current chapter the learning material comes from – the foundation from which a
/// later study plan generator derives "what's currently due" (see wiki/09-llm-kochbuch.md). Ownership
/// is secured by the <see cref="ChildOwnershipFilter"/>.
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
    /// Projects together with series and unit. The names are included so a caller can display the
    /// assignment without reloading the catalog.
    /// </summary>
    private static IQueryable<TextbookResponse> Project(IQueryable<Textbook> q) =>
        q.Select(t => new TextbookResponse(t.Id, t.Title, t.SubjectName, t.SubjectId, t.Grade, t.Publisher,
            t.Isbn, t.CurrentChapter, t.CreatedAt,
            t.SeriesId, t.Series!.Name, t.CurrentUnitId, t.CurrentUnit!.Label));

    /// <summary>All textbooks of the child.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TextbookResponse>>> List(int childId, CancellationToken ct) =>
        await Project(db.Textbooks.AsNoTracking().Where(t => t.ChildId == childId)
                .OrderBy(t => t.SubjectName).ThenBy(t => t.Title))
            .ToListAsync(ct);

    /// <summary>A single textbook of the child.</summary>
    [HttpGet("{textbookId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookResponse>> Get(int childId, int textbookId, CancellationToken ct)
    {
        var book = await Project(db.Textbooks.AsNoTracking()
            .Where(t => t.Id == textbookId && t.ChildId == childId)).FirstOrDefaultAsync(ct);
        return book is null ? NotFound() : book;
    }

    /// <summary>Creates a textbook for the child.</summary>
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

    /// <summary>Changes a textbook (partial). Only fields that are set are changed.</summary>
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
    /// Checks the catalog references: the series must exist, and the unit must belong <b>to this series</b>.
    /// Without the second check, the child would end up with a unit from an unrelated series – the creator
    /// would then get the material of a book the child does not use.
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

    /// <summary>Deletes a textbook of the child.</summary>
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
