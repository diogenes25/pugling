using System.Text.Json;
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
    /// <summary>
    /// Schlanke Trefferzeile der Übungssuche (kindneutraler Katalog). <c>AuthorFatherId</c>/<c>AuthorName</c> tragen die
    /// Attribution der geteilten Bibliothek (<c>null</c> = geseedete System-Übung); <c>IsOwn</c> = der anfragende Vater
    /// darf die Übung ändern/löschen.
    /// </summary>
    public record ExerciseSummary(int Id, int ChapterId, int SubjectId, string Type, string Title,
        int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
        int? AuthorFatherId, string? AuthorName, bool IsOwn);

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
    /// <param name="mineOnly">Nur eigene Übungen des anfragenden Vaters (Verwaltung statt Entdeckung).</param>
    [HttpGet]
    public async Task<IEnumerable<ExerciseSummary>> Search(
        [FromQuery] int? subjectId, [FromQuery] int? grade, [FromQuery] SchoolTypes? schoolType,
        [FromQuery] int? categoryId, [FromQuery] ExerciseType? type, [FromQuery] string? search,
        [FromQuery] bool? mineOnly)
    {
        var fid = User.FatherId();
        var query = db.Exercises.AsNoTracking().AsQueryable();

        // „Nur meine": zeigt dem Vater ausschließlich seine eigenen Übungen (Verwaltung statt Entdeckung).
        // Ohne bekannten fid bewusst leere Menge (fail-closed) statt aller autorlosen System-Übungen.
        if (mineOnly == true)
            query = query.Where(e => fid != null && e.AuthorFatherId == fid);

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
                e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category!.Name,
                e.AuthorFatherId, e.Author!.Name, fid != null && e.AuthorFatherId == fid))
            .ToListAsync();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Vollständige, typ-übergreifende Sicht auf eine Übung inklusive roher Config und aller Metadaten –
    /// die Grundlage zum Bearbeiten (Config in den typ-spezifischen Editor laden; gespeichert wird über
    /// den per-Typ-PUT <c>.../chapters/{}/&lt;typ&gt;/{id}</c>).
    /// </summary>
    public record ExerciseDetail(int Id, int ChapterId, string ChapterName, int SubjectId, string SubjectName,
        string Type, string Title, int OrderIndex, int RewardPoints, int? GradeMin, int? GradeMax,
        SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
        SuggestedBonus? SuggestedBonus, int? DefaultStage, int? DefaultItemCount,
        int? AuthorFatherId, string? AuthorName, bool IsOwn, JsonElement Config);

    /// <summary>Eine einzelne Übung typ-übergreifend per Id (mit Config + Metadaten).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDetail>> Get(int id)
    {
        var e = await db.Exercises.AsNoTracking()
            .Include(x => x.Chapter!).ThenInclude(c => c.Subject)
            .Include(x => x.Category)
            .Include(x => x.Author)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        return new ExerciseDetail(e.Id, e.ChapterId, e.Chapter?.Name ?? "", e.Chapter?.SubjectId ?? 0,
            e.Chapter?.Subject?.Name ?? "", e.Type.ToString(), e.Title, e.OrderIndex, e.RewardPoints,
            e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category?.Name,
            e.SuggestedBonus, e.DefaultStage, e.DefaultItemCount,
            e.AuthorFatherId, e.Author?.Name, User.Owns(e),
            JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(e.ConfigJson) ? "{}" : e.ConfigJson, JsonOptions));
    }

    public record PlanUsage(int PlanId, string PlanTitle, int ChildId, string ChildName);
    public record ClassTestUsage(int Id, string Title, int ChildId, string ChildName);
    /// <summary>Wo eine Übung verwendet wird (nur Ressourcen der eigenen Kinder).</summary>
    public record UsageResponse(IReadOnlyList<PlanUsage> Plans, IReadOnlyList<ClassTestUsage> ClassTests);

    /// <summary>
    /// In welchen Lehrplänen und Klassenarbeiten (welcher eigenen Kinder) eine Übung steckt.
    /// Lehrpläne über das neue Positions-Modell (<see cref="PlanPosition"/>); Klassenarbeiten direkt
    /// zugewiesen ODER über einen gemeinsamen Tag. Hinweis: das alte StudyPlanItem-Modell trägt keine
    /// Übungs-Referenz und wird daher nicht erfasst.
    /// </summary>
    [HttpGet("{id:int}/usage")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsageResponse>> Usage(int id)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == id)) return NotFound();
        var fid = User.FatherId();

        var plans = (await db.PlanPositions.AsNoTracking()
                .Where(p => p.ExerciseId == id && p.StudyPlan!.Child!.FatherId == fid)
                .Select(p => new PlanUsage(p.StudyPlanId, p.StudyPlan!.Title, p.StudyPlan.ChildId, p.StudyPlan.Child!.Name))
                .ToListAsync())
            .DistinctBy(u => u.PlanId).ToList();

        // Klassenarbeit gilt als Nutzer, wenn die Übung direkt zugewiesen ist oder einen ihr zugeordneten Tag trägt.
        var directTestIds = db.KlassenarbeitExercises.Where(x => x.ExerciseId == id).Select(x => x.KlassenarbeitId);
        var tagTestIds = db.KlassenarbeitTags
            .Where(kt => db.ExerciseTags.Any(et => et.ExerciseId == id && et.TagId == kt.TagId))
            .Select(kt => kt.KlassenarbeitId);
        var testIds = directTestIds.Union(tagTestIds);
        var classTests = await db.Klassenarbeiten.AsNoTracking()
            .Where(k => testIds.Contains(k.Id) && k.Child!.FatherId == fid)
            .Select(k => new ClassTestUsage(k.Id, k.Title, k.ChildId, k.Child!.Name))
            .ToListAsync();

        return new UsageResponse(plans, classTests);
    }
}
