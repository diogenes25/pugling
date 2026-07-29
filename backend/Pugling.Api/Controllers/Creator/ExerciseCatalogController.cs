using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Fachübergreifende Übungssuche über strukturierte Metadaten – die Vorfilterung als Grundlage
/// für die (spätere) automatische Lehrplan-Erstellung. Beispiel: Fach Englisch, 9. Klasse,
/// Gymnasium, Art „Grammatik" → passende Übungskandidaten.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises")]
[Tags("Creator – Exercise Catalog")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseCatalogController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// Sucht Übungen über die Metadaten. Alle Parameter sind optional und werden UND-verknüpft.
    /// Nullbare Grenzen/„None"-Schulart bedeuten „passt immer" und werden nicht ausgeschlossen.
    /// </summary>
    /// <param name="subjectId">Fach.</param>
    /// <param name="chapterId">Kapitel (setzt in der Regel ein Fach voraus).</param>
    /// <param name="grade">Klassenstufe des Kindes; passt, wenn sie in [GradeMin, GradeMax] liegt.</param>
    /// <param name="schoolType">Schulart; passt, wenn die Übung sie enthält oder für alle gilt.</param>
    /// <param name="categoryId">Fachabhängige Art.</param>
    /// <param name="type">Übungstyp.</param>
    /// <param name="search">Freitext in Titel oder Beschreibung (Teilstring).</param>
    /// <param name="mineOnly">Nur eigene Übungen des anfragenden Vaters (Verwaltung statt Entdeckung).</param>
    /// <param name="sort">Sortierspalte: <c>title</c>, <c>type</c>, <c>grade</c>, <c>source</c>, <c>created</c>.
    /// Kurzform <c>-title</c> = absteigend. Ohne Angabe: Fach → Kapitel → Reihenfolge.</param>
    /// <param name="dir"><c>asc</c> (Default) oder <c>desc</c>; hat Vorrang vor einem <c>-</c>-Präfix in <paramref name="sort"/>.</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet]
    public async Task<IEnumerable<ExerciseSummary>> Search(
        [FromQuery] int? subjectId, [FromQuery] int? chapterId, [FromQuery] int? grade, [FromQuery] SchoolTypes? schoolType,
        [FromQuery] int? categoryId, [FromQuery] string? type, [FromQuery] string? search,
        [FromQuery] bool? mineOnly, [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var fid = User.AdultId();
        var isAdmin = User.IsAdmin();
        var query = db.Exercises.AsNoTracking().AsQueryable();

        // „Nur meine": Übungen, die der Creator ändern darf (Owner- oder Write-Grant) – Verwaltung statt Entdeckung.
        // Ohne bekannten fid bewusst leere Menge (fail-closed) statt aller autorlosen System-Übungen.
        if (mineOnly == true)
            query = query.Where(e => fid != null && e.Grants.Any(g => g.CreatorId == fid
                && (g.Permission == GrantPermission.Owner || g.Permission == GrantPermission.Write)));

        if (subjectId is int sid)
            query = query.Where(e => e.Chapter!.SubjectId == sid);

        if (chapterId is int chid)
            query = query.Where(e => e.ChapterId == chid);

        if (grade is int g)
            query = query.Where(e => (e.GradeMin == null || e.GradeMin <= g)
                && (e.GradeMax == null || e.GradeMax >= g));

        // Schulart-Filter: Übungen ohne Angabe (None) gelten für alle; sonst muss das Bit gesetzt sein.
        if (schoolType is SchoolTypes st && st != SchoolTypes.None)
            query = query.Where(e => e.SchoolTypes == SchoolTypes.None || (e.SchoolTypes & st) != 0);

        if (categoryId is int cid)
            query = query.Where(e => e.CategoryId == cid);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.Type == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Title.Contains(term)
                || (e.Description != null && e.Description.Contains(term)));
        }

        return await ApplySort(query, SortingExtensions.ParseSort(sort, dir))
            .Select(e => new ExerciseSummary(e.Id, e.ChapterId, e.Chapter!.SubjectId, e.Type, e.Title,
                e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category!.Name,
                e.AuthorAdultId, e.Author!.Name,
                // IsOwn = darf ändern (Owner/Write-Grant); IsOwner = darf verwalten (Owner-Grant). Admin sieht beides als true.
                isAdmin || (fid != null && e.Grants.Any(g => g.CreatorId == fid
                    && (g.Permission == GrantPermission.Owner || g.Permission == GrantPermission.Write))),
                isAdmin || (fid != null && e.Grants.Any(g => g.CreatorId == fid && g.Permission == GrantPermission.Owner)),
                e.ExecutePublic, e.Description,
                e.DefaultUseLeitner, e.DefaultRequireTypedTest))
            .ToPagedListAsync(Response, skip, take);
    }

    /// <summary>
    /// Wendet die per Whitelist erlaubte Sortierung an; jede Variante endet mit <c>Id</c> als Tiebreaker,
    /// damit das Paging-Fenster deterministisch bleibt. Unbekannte/leere Keys → fachlicher Standard
    /// (Fach → Kapitel → Reihenfolge).
    /// </summary>
    private static IOrderedQueryable<Exercise> ApplySort(IQueryable<Exercise> q, (string? Key, bool Desc) sort) =>
        (sort.Key?.ToLowerInvariant(), sort.Desc) switch
        {
            ("title", false) => q.OrderBy(e => e.Title).ThenBy(e => e.Id),
            ("title", true) => q.OrderByDescending(e => e.Title).ThenBy(e => e.Id),
            ("type", false) => q.OrderBy(e => e.Type).ThenBy(e => e.Id),
            ("type", true) => q.OrderByDescending(e => e.Type).ThenBy(e => e.Id),
            ("grade", false) => q.OrderBy(e => e.GradeMin).ThenBy(e => e.Id),
            ("grade", true) => q.OrderByDescending(e => e.GradeMin).ThenBy(e => e.Id),
            ("source", false) => q.OrderBy(e => e.Source).ThenBy(e => e.Id),
            ("source", true) => q.OrderByDescending(e => e.Source).ThenBy(e => e.Id),
            ("created", false) => q.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id),
            ("created", true) => q.OrderByDescending(e => e.CreatedAt).ThenBy(e => e.Id),
            // Fachliche Standardreihenfolge (kein per-Spalte klickbarer Sortier-Key): bewusst immer aufsteigend –
            // eine Richtungsumkehr des Katalog-Baums (Fach → Kapitel → Reihenfolge) wäre nicht sinnvoll.
            _ => q.OrderBy(e => e.Chapter!.SubjectId).ThenBy(e => e.ChapterId).ThenBy(e => e.OrderIndex).ThenBy(e => e.Id),
        };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);


    /// <summary>Eine einzelne Übung typ-übergreifend per Id (mit Config + Metadaten).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDetail>> Get(int id)
    {
        var e = await db.Exercises.AsNoTracking()
            .Include(x => x.Chapter!).ThenInclude(c => c.Subject)
            .Include(x => x.Category)
            .Include(x => x.Author)
            .Include(x => x.Grants)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        var fid = User.AdultId();
        var isAdmin = User.IsAdmin();
        return new ExerciseDetail(e.Id, e.ChapterId, e.Chapter?.Name ?? "", e.Chapter?.SubjectId ?? 0,
            e.Chapter?.Subject?.Name ?? "", e.Type.ToString(), e.Title, e.OrderIndex, e.RewardPoints,
            e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category?.Name,
            e.SuggestedBonus, e.DefaultStage, e.DefaultItemCount,
            e.AuthorAdultId, e.Author?.Name,
            ExercisePermissionService.CanWrite(e.Grants, fid, isAdmin), ExercisePermissionService.CanAdminister(e.Grants, fid, isAdmin),
            e.ExecutePublic, e.Grants.Count,
            JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(e.ConfigJson) ? "{}" : e.ConfigJson, JsonOptions),
            e.Description, e.DefaultUseLeitner, e.DefaultRequireTypedTest);
    }

    /// <summary>
    /// Gibt eine Übung frei oder <b>zieht sie zurück</b> – die Gegenbewegung zum Veröffentlichen, und der
    /// einzige Weg, Material aus dem Verkehr zu nehmen: Löschen verweigert eine benutzte Übung (der FK
    /// <c>PlanPosition→Exercise</c> ist <c>Restrict</c>), und das ist auch richtig – laufende Pflichten
    /// dürfen nicht unter dem Kind wegbrechen.
    /// <para>
    /// Bewusst ein <b>eigener</b> Endpunkt statt des typisierten Voll-<c>PUT</c>: dieses Flag hat nichts mit
    /// dem Übungstyp zu tun, und für einen einzelnen Schalter die ganze Übung samt <c>ConfigJson</c> zu
    /// ersetzen ist der kurze Weg zum stillen Inhaltsverlust.
    /// </para>
    /// <para>
    /// Nur der <b>Owner</b> darf umschalten (wie beim Rechte-Vergeben) – ein Write-Grantee darf Inhalte
    /// pflegen, aber nicht über die Weitergabe entscheiden.
    /// </para>
    /// </summary>
    [HttpPatch("{id:int}/sharing")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseSharingResponse>> SetSharing(int id, SetExerciseSharingDto dto, CancellationToken ct)
    {
        var e = await db.Exercises.Include(x => x.Grants).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        if (!ExercisePermissionService.CanAdminister(e.Grants, User.AdultId(), User.IsAdmin()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only an owner can share or withdraw this exercise.");

        e.ExecutePublic = dto.ExecutePublic;
        await db.SaveChangesAsync(ct);
        return new ExerciseSharingResponse(e.Id, e.ExecutePublic, e.Grants.Count);
    }

    /// <summary>
    /// In welchen Lehrplänen und Klassenarbeiten (welcher eigenen Kinder) eine Übung steckt.
    /// Lehrpläne über das neue Positions-Modell (<see cref="PlanPosition"/>); Klassenarbeiten direkt
    /// zugewiesen ODER über einen gemeinsamen Tag. Hinweis: das alte StudyPlanItem-Modell trägt keine
    /// Übungs-Referenz und wird daher nicht erfasst.
    /// <para>
    /// Dazu <see cref="UsageResponse.OtherLearnersCount"/>: die <b>Zahl der Kinder</b> fremder Betreuer, die
    /// die Übung einsetzen. Ohne sie behauptete diese Antwort „nirgends", während das Löschen mit <c>409</c>
    /// scheiterte – dieselbe Zählung liefert jetzt beide Stellen (Anmerkung 14). Für einen Creator ohne
    /// eigene Kinder (Lehrer, KI-Creator-App) ist sie die <i>einzige</i> aussagekräftige Angabe hier.
    /// </para>
    /// </summary>
    [HttpGet("{id:int}/usage")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsageResponse>> Usage(int id, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == id, ct)) return NotFound();
        var fid = User.AdultId();

        var plans = (await db.PlanPositions.AsNoTracking()
                .Where(p => p.ExerciseId == id && p.StudyPlan!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
                .Select(p => new PlanUsage(p.StudyPlanId, p.StudyPlan!.Title, p.StudyPlan.ChildId, p.StudyPlan.Child!.Name))
                .ToListAsync(ct))
            .DistinctBy(u => u.PlanId).ToList();

        // Klassenarbeit gilt als Nutzer, wenn die Übung direkt zugewiesen ist oder einen ihr zugeordneten Tag trägt.
        var directTestIds = db.KlassenarbeitExercises.Where(x => x.ExerciseId == id).Select(x => x.KlassenarbeitId);
        var tagTestIds = db.KlassenarbeitTags
            .Where(kt => db.ExerciseTags.Any(et => et.ExerciseId == id && et.TagId == kt.TagId))
            .Select(kt => kt.KlassenarbeitId);
        var testIds = directTestIds.Union(tagTestIds);
        var classTests = await db.Klassenarbeiten.AsNoTracking()
            .Where(k => testIds.Contains(k.Id) && k.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
            .Select(k => new ClassTestUsage(k.Id, k.Title, k.ChildId, k.Child!.Name))
            .ToListAsync(ct);

        // Dieselbe Zählung, die auch das Löschen benutzt – eine Quelle, damit die beiden Auskünfte nicht
        // wieder auseinanderlaufen können. Herausgegeben wird die Zahl der **Kinder**, nicht der Stellen:
        // das ist die Antwort auf „wird mein Material benutzt", und Stellen wären für einen Creator ohne
        // eigene Kinder eine Zahl ohne Bedeutung.
        var blocking = await ExerciseUsageQueries.CountBlockingAsync(db, id, fid, ct);
        return new UsageResponse(plans, classTests, blocking.HiddenLearners);
    }
}
