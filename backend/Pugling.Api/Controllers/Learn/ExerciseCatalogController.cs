using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Fachübergreifende Übungssuche über strukturierte Metadaten – die Vorfilterung als Grundlage
/// für die (spätere) automatische Lehrplan-Erstellung. Beispiel: Fach Englisch, 9. Klasse,
/// Gymnasium, Art „Grammatik" → passende Übungskandidaten.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/learn/exercises")]
[Tags("Learn – Exercise Catalog")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ExerciseCatalogController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Schlanke Trefferzeile der Übungssuche (kindneutraler Katalog).</summary>
    public record ExerciseSummary(int Id, int ChapterId, int SubjectId, string Type, string Title,
        int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName);

    /// <summary>
    /// Sucht Übungen über die Metadaten. Alle Parameter sind optional und werden UND-verknüpft.
    /// Nullbare Grenzen/„None"-Schulart bedeuten „passt immer" und werden nicht ausgeschlossen.
    /// </summary>
    /// <param name="subjectId">Fach.</param>
    /// <param name="grade">Klassenstufe des Kindes; passt, wenn sie in [GradeMin, GradeMax] liegt.</param>
    /// <param name="schoolType">Schulart; passt, wenn die Übung sie enthält oder für alle gilt.</param>
    /// <param name="categoryId">Fachabhängige Art.</param>
    /// <param name="type">Übungstyp.</param>
    /// <param name="search">Freitext im Titel (Teilstring).</param>
    [HttpGet]
    public async Task<IEnumerable<ExerciseSummary>> Search(
        [FromQuery] int? subjectId, [FromQuery] int? grade, [FromQuery] SchoolTypes? schoolType,
        [FromQuery] int? categoryId, [FromQuery] ExerciseType? type, [FromQuery] string? search)
    {
        var query = db.Exercises.AsNoTracking().AsQueryable();

        if (subjectId is int sid)
            query = query.Where(e => e.Chapter!.SubjectId == sid);

        if (grade is int g)
            query = query.Where(e => (e.GradeMin == null || e.GradeMin <= g)
                && (e.GradeMax == null || e.GradeMax >= g));

        // Schulart-Filter: Übungen ohne Angabe (None) gelten für alle; sonst muss das Bit gesetzt sein.
        if (schoolType is SchoolTypes st && st != SchoolTypes.None)
            query = query.Where(e => e.SchoolTypes == SchoolTypes.None || (e.SchoolTypes & st) != 0);

        if (categoryId is int cid)
            query = query.Where(e => e.CategoryId == cid);

        if (type is ExerciseType t)
            query = query.Where(e => e.Type == t);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Title.Contains(term));
        }

        return await query
            .OrderBy(e => e.Chapter!.SubjectId).ThenBy(e => e.ChapterId)
            .ThenBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .Select(e => new ExerciseSummary(e.Id, e.ChapterId, e.Chapter!.SubjectId, e.Type.ToString(), e.Title,
                e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category!.Name))
            .ToListAsync();
    }
}
