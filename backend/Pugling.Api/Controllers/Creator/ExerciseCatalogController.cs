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
/// Cross-subject exercise search over structured metadata – the pre-filtering as a basis
/// for the (future) automatic study plan generation. Example: subject English, 9th grade,
/// upper secondary school (Gymnasium), category "grammar" → matching exercise candidates.
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
    /// Searches exercises over the metadata. All parameters are optional and are AND-combined.
    /// Nullable bounds/school type "None" mean "always matches" and are not excluded.
    /// </summary>
    /// <param name="subjectId">Subject.</param>
    /// <param name="chapterId">Chapter (usually implies a subject).</param>
    /// <param name="grade">Grade level of the child; matches if it lies within [GradeMin, GradeMax].</param>
    /// <param name="schoolType">School type; matches if the exercise includes it or applies to all.</param>
    /// <param name="categoryId">Subject-dependent category.</param>
    /// <param name="type">Exercise type.</param>
    /// <param name="search">Free text in title or description (substring).</param>
    /// <param name="mineOnly">Only own exercises of the requesting adult (management rather than discovery).</param>
    /// <param name="sort">Sort column: <c>title</c>, <c>type</c>, <c>grade</c>, <c>source</c>, <c>created</c>.
    /// Short form <c>-title</c> = descending. Without a value: subject → chapter → order.</param>
    /// <param name="dir"><c>asc</c> (default) or <c>desc</c>; takes precedence over a <c>-</c> prefix in <paramref name="sort"/>.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<ExerciseSummary>> Search(
        [FromQuery] int? subjectId, [FromQuery] int? chapterId, [FromQuery] int? grade, [FromQuery] SchoolTypes? schoolType,
        [FromQuery] int? categoryId, [FromQuery] string? type, [FromQuery] string? search,
        [FromQuery] bool? mineOnly, [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
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
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>
    /// Applies the sorting allowed via whitelist; every variant ends with <c>Id</c> as a tiebreaker,
    /// so the paging window stays deterministic. Unknown/empty keys → business default
    /// (subject → chapter → order).
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


    /// <summary>A single exercise across all types by id (with config + metadata).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDetail>> Get(int id, CancellationToken ct = default)
    {
        var e = await db.Exercises.AsNoTracking()
            .Include(x => x.Chapter!).ThenInclude(c => c.Subject)
            .Include(x => x.Category)
            .Include(x => x.Author)
            .Include(x => x.Grants)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
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
    /// Publishes an exercise or <b>withdraws it</b> – the counter-move to publishing, and the
    /// only way to take material out of circulation: deleting refuses a used exercise (the FK
    /// <c>PlanPosition→Exercise</c> is <c>Restrict</c>), and rightly so – ongoing mandatory goals
    /// must not break out from under the child.
    /// <para>
    /// Deliberately its <b>own</b> endpoint instead of the typed full <c>PUT</c>: this flag has nothing to do
    /// with the exercise type, and replacing the whole exercise including <c>ConfigJson</c> for a single toggle
    /// is the short path to silent content loss.
    /// </para>
    /// <para>
    /// Only the <b>owner</b> may toggle it (as with granting permissions) – a write grantee may
    /// maintain content but not decide on its distribution.
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
    /// Which study plans and class tests (of which own children) an exercise is embedded in.
    /// Study plans via the position model (<see cref="PlanPosition"/>); class tests either directly
    /// assigned OR via a shared tag.
    /// <para>
    /// In addition <see cref="UsageResponse.OtherLearnersCount"/>: the <b>number of children</b> of other
    /// supervisors who use the exercise. Without it, this response claimed "nowhere" while deletion
    /// failed with <c>409</c> – the same count now feeds both places (remark 14). For a creator without
    /// own children (teacher, AI creator app) it is the <i>only</i> meaningful information here.
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
