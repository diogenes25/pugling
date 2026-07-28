using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers;

/// <summary>
/// Anmerkungen beim Testen: Fragen, Beobachtungen und Befunde samt dem Kontext, in dem sie aufgefallen
/// sind. Erfasst wird im UI-Widget, beantwortet wird durch Claude Code – angestoßen vom Menschen über die
/// hier vergebene Id („Beantworte die Frage 123").
/// <para>
/// <b>Bewusst tier-neutral</b> (kein <c>creator</c>/<c>supervisor</c>/<c>student</c>-Präfix, Präzedenz
/// <see cref="AuthController"/>): Die Ressource gehört keiner der drei Ebenen – derselbe Mensch erfasst
/// mal aus dem Vater-Web, mal aus der Sohn-Arcade. Gegated wird darum nur mit <c>[Authorize]</c>, die
/// Rollen werden inline getrennt (Muster der Student-Endpunkte).
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/remarks")]
[Tags("Remarks")]
[Produces("application/json")]
[Authorize]
public class RemarksController(
    PuglingDbContext db, RemarkExportService export, AuthAccess access, RemarkOptions options) : ControllerBase
{
    /// <summary>Sortierschlüssel der Liste (Whitelist – kein dynamischer Property-Zugriff).</summary>
    private static IQueryable<Remark> ApplySort(IQueryable<Remark> q, string? key, bool desc) => key switch
    {
        // Tiebreaker wie in den anderen Zweigen: Das Widget ist ein Schnellerfassungs-Werkzeug, zwei
        // Anmerkungen können denselben Zeitstempel tragen. Ohne ihn wäre das Paging-Fenster nicht
        // deterministisch (eine Zeile erschiene auf zwei Seiten oder auf keiner).
        "createdAt" => desc
            ? q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            : q.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
        "status" => desc ? q.OrderByDescending(r => r.Status).ThenByDescending(r => r.Id) : q.OrderBy(r => r.Status).ThenBy(r => r.Id),
        "category" => desc ? q.OrderByDescending(r => r.Category).ThenByDescending(r => r.Id) : q.OrderBy(r => r.Category).ThenBy(r => r.Id),
        // Vorgabe: neueste zuerst – so liest das Widget die eigene Liste.
        _ => q.OrderByDescending(r => r.Id),
    };

    /// <summary>
    /// Projektion auf den Vertrag. <paramref name="withAnswer"/> blendet die Antwort aus: Sie stammt aus
    /// Claude Code und trägt Datei-/Zeilenverweise, also Code-Interna. Dieselbe Begründung, mit der der
    /// Export Supervisor-only ist – ohne den Filter widerspräche sich beides, denn das Widget der
    /// Sohn-Arcade zeigt die Antwort zur eigenen Anmerkung an.
    /// </summary>
    private static RemarkDto ToDto(Remark r, int? viewerAccountId, bool withAnswer, int commentCount) => new(
        r.Id, r.Text, r.Category, r.Status,
        withAnswer ? r.Answer : null,
        withAnswer ? r.AnsweredAt : null,
        withAnswer ? r.AnsweredBy : null,
        r.ParentRemarkId,
        r.AccountId, r.AuthorRole, r.AccountId == viewerAccountId,
        new RemarkContextDto(r.Route, r.AppArea, r.ChildId, r.ExerciseId, r.StudyPlanId, r.PlanPositionId,
            r.ContextJson, r.RecentErrorsJson),
        r.UserAgent, r.CreatedAt,
        // Für einen Student immer 0: Er darf den Verlauf nicht sehen, und schon die Anzahl wäre eine
        // Auskunft darüber, dass über seine Anmerkung gesprochen wurde.
        withAnswer ? commentCount : 0);

    /// <summary>Projektion eines Beitrags; <paramref name="viewerAccountId"/> entscheidet über <c>IsOwn</c> (nur eigene sind löschbar).</summary>
    private static RemarkCommentDto ToDto(RemarkComment c, int? viewerAccountId) => new(
        c.Id, c.RemarkId, c.Body, c.Author, c.AuthorLabel, c.AuthorAccountId,
        c.AuthorAccountId is { } a && a == viewerAccountId, c.CreatedAt);

    /// <summary>
    /// Ob der Aufrufer Antworten und Verlauf sehen darf – Student nicht, siehe <see cref="ToDto(Remark, int?, bool, int)"/>.
    /// </summary>
    private bool MaySeeAnswers => !User.IsStudent() || User.IsSupervisor();

    /// <summary>
    /// Der Anmerkungs-Bestand, auf den der Aufrufer zugreifen darf – die <b>eine</b> Stelle, an der die
    /// Sichtbarkeit entschieden wird.
    /// <para>
    /// <paramref name="allAccounts"/> hebt die Einschränkung auf. Die Berechtigung dafür ist
    /// <see cref="MayReadAllAccounts"/>; auf den <b>Listen</b> kommt das ausdrückliche <c>scope=all</c>
    /// hinzu, damit die Vorgabe eng bleibt (sonst zeigte die Liste im Widget fremde Einträge). Beim Zugriff
    /// auf eine <b>einzelne Id</b> genügt die Berechtigung – genau das braucht der Skill, um eine Anmerkung
    /// aus einem beliebigen Testkonto zu beantworten.
    /// </para>
    /// </summary>
    private async Task<IQueryable<Remark>> ScopedAsync(bool allAccounts, CancellationToken ct)
    {
        if (allAccounts) return db.Remarks;
        var visible = await VisibleAccountIdsAsync(ct);
        return db.Remarks.Where(r => visible.Contains(r.AccountId));
    }

    private static bool WantsAllAccounts(string? scope) => string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ob der Aufrufer über Konto-Grenzen lesen darf.
    /// <para>
    /// Das ist <b>an den Schalter <see cref="RemarkOptions.GlobalRead"/> gebunden, nicht an eine Rolle</b>:
    /// Beim Testen entstehen ständig Wegwerf-Konten (ein Fehler zeigt sich oft nur in einer bestimmten
    /// Konstellation – ein frischer Vater ohne Übungen deckt auf, was beim geseedeten Papa nie auffällt), und
    /// jedes einzeln mit einem Flag zu versehen wäre Verwaltungsarbeit ohne Gegenwert. <c>Admin</c> bleibt
    /// zusätzlich erlaubt, taugt aber nicht als <i>Bedingung</i>: Die Rolle umgeht auch die RWX-Rechte auf
    /// Übungen, und die haben mit Anmerkungen nichts zu tun.
    /// </para>
    /// <para>
    /// Ein <b>Student</b> ist immer ausgeschlossen – auch mit eingeschaltetem <c>GlobalRead</c>. Antworten und
    /// Verlauf tragen Datei- und Zeilenverweise; an dem Tag, an dem das Kind das Widget sieht, darf es die
    /// Testnotizen der Erwachsenen nicht mitlesen.
    /// </para>
    /// </summary>
    private bool MayReadAllAccounts => !User.IsStudent() && (options.GlobalRead || User.IsAdmin());

    /// <summary>
    /// Die Konten, deren Anmerkungen der Aufrufer sehen darf: immer das eigene, für einen Supervisor
    /// zusätzlich die Konten der von ihm betreuten Kinder.
    /// <para>
    /// Die Trennung ist keine Förmlichkeit: Antworten tragen Datei- und Zeilenverweise, also Code-Interna.
    /// An dem Tag, an dem das Kind das Widget sieht, darf es weder die Testnotizen des Vaters noch deren
    /// Antworten mitlesen – deshalb sieht ein Student <b>ausschließlich</b> eigene Anmerkungen.
    /// </para>
    /// </summary>
    private async Task<List<int>> VisibleAccountIdsAsync(CancellationToken ct)
    {
        var self = User.AccountId();
        var ids = new List<int>();
        if (self is { } s) ids.Add(s);

        var fid = User.SupervisorId();
        if (User.IsSupervisor() && fid is not null)
        {
            var supervised = await db.AccountProfiles
                .AsNoTracking()
                .Where(p => p.Role == ProfileRole.Student && p.ChildId != null
                    && db.SupervisorLinks.Any(l => l.SupervisorId == fid && l.StudentId == p.ChildId))
                .Select(p => p.AccountId)
                .ToListAsync(ct);
            ids.AddRange(supervised);
        }

        return ids.Distinct().ToList();
    }

    /// <summary>
    /// Liefert die Id zurück, wenn der Verweis existiert – sonst <c>null</c>. Geprüft wird nur, was
    /// gesetzt ist; für den Normalfall (kein Bezug) fällt keine Abfrage an.
    /// </summary>
    private static async Task<int?> ExistingAsync(int? id, Func<int, Task<bool>> exists) =>
        id is { } value && await exists(value) ? value : null;

    /// <summary>
    /// Eine Anmerkung erfassen. Pflicht ist allein der Text; Autor und Rolle kommen aus dem Token, der
    /// Kontext aus dem Widget. Die Antwort trägt die <b>Id</b> – sie ist der Schlüssel, mit dem die Frage
    /// später in Claude Code eingelöst wird.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RemarkDto>> Create(CreateRemarkDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Text)) return this.ProblemWithCode(ApiErrors.ValidationError, "Text is required.");

        var accountId = User.AccountId();
        if (accountId is null) return this.ProblemWithCode(ApiErrors.Unauthorized, "Token carries no account.");

        // Die Vorgänger-Anmerkung muss sichtbar sein – sonst ließe sich über den Verweis auf fremde
        // Einträge schließen.
        if (dto.ParentRemarkId is { } parentId)
        {
            var visible = await VisibleAccountIdsAsync(ct);
            var exists = await db.Remarks.AnyAsync(r => r.Id == parentId && visible.Contains(r.AccountId), ct);
            if (!exists) return this.ProblemWithCode(ApiErrors.InvalidReference, "Parent remark not found.");
        }

        var ctx = dto.Context;
        // Kontext-Bezüge, die ins Leere zeigen **oder dem Aufrufer nicht gehören**, werden still
        // verworfen statt den POST scheitern zu lassen. Das Widget schickt sie automatisch mit – auch
        // aus der URL gelesene (`/vater/kind/999`). Ein gelöschtes Kind oder ein Tippfehler in der
        // Adresse dürfte sonst das Erfassen verhindern und den getippten Text vernichten. Dieselbe
        // Haltung wie die `SetNull`-FKs: Der Kontext darf verblassen, er darf nichts blockieren.
        //
        // Die Eigentumsprüfung ist kein Beiwerk: Ohne sie wäre die Antwort ein Auskunftsdienst darüber,
        // welche Ids es gibt – ein Student könnte fremde Kind-/Plan-Ids durchprobieren und am
        // zurückgespiegelten Kontext ablesen, welche existieren.
        //
        // Der `ParentRemarkId` oben wird bewusst anders behandelt (400): Den setzt der Skill ausdrücklich,
        // ein Fehlgriff dort ist ein Fehler und keine verwelkte Automatik.
        var childId = await ExistingAsync(ctx?.ChildId, id => access.OwnsChildAsync(User, id));
        var planId = await ExistingAsync(ctx?.StudyPlanId, async id =>
            await db.StudyPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) is { } plan
            && await access.OwnsPlanAsync(User, plan));
        var positionId = await ExistingAsync(ctx?.PlanPositionId, async id =>
            await db.PlanPositions.AsNoTracking().Include(p => p.StudyPlan)
                .FirstOrDefaultAsync(p => p.Id == id, ct) is { StudyPlan: { } plan }
            && await access.OwnsPlanAsync(User, plan));
        // Übungen sind bewusst global lesbar (der geteilte Katalog) – ihre Existenz ist kein Geheimnis,
        // hier genügt die Existenzprüfung.
        var exerciseId = await ExistingAsync(ctx?.ExerciseId, id => db.Exercises.AnyAsync(x => x.Id == id, ct));

        var remark = new Remark
        {
            Text = dto.Text.Trim(),
            Category = dto.Category ?? RemarkCategory.Unspecified,
            Status = RemarkStatus.Open,
            AccountId = accountId.Value,
            // Die Rolle, in der gerade getestet wurde. Ein Vater-Konto trägt Creator und Supervisor;
            // für die Einordnung zählt die steuernde Ebene.
            AuthorRole = User.IsStudent() ? ProfileRole.Student : ProfileRole.Supervisor,
            ParentRemarkId = dto.ParentRemarkId,
            Route = ctx?.Route?.Trim() ?? "",
            AppArea = ctx?.AppArea?.Trim() ?? "",
            ChildId = childId,
            ExerciseId = exerciseId,
            StudyPlanId = planId,
            PlanPositionId = positionId,
            ContextJson = ctx?.ContextJson,
            RecentErrorsJson = ctx?.RecentErrorsJson,
            UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
        };

        db.Remarks.Add(remark);
        await db.SaveChangesAsync(ct);
        // Frisch erfasst: Der Verlauf ist zwangsläufig leer.
        return CreatedAtAction(nameof(GetOne), new { id = remark.Id, version = "1.0" }, ToDto(remark, accountId, MaySeeAnswers, 0));
    }

    /// <summary>
    /// Anmerkungen auflisten. <c>mine=true</c> beschränkt auf die eigenen – das ist die Abfrage hinter der
    /// Liste im Widget; ohne den Filter sähe ein Supervisor auch die des Kindes.
    /// <para>
    /// <c>scope=all</c> hebt die Konten-Grenze auf – das ist die Sicht des Nachbereitungs-Skills, der
    /// Anmerkungen aus allen Testkonten einsammelt. Erlaubt, wenn <see cref="MayReadAllAccounts"/> gilt
    /// (Schalter <c>Remarks:GlobalRead</c>, in der Entwicklung an), sonst <c>403</c>. Bewusst ein
    /// <b>ausdrücklicher</b> Parameter: Die Vorgabe muss eng bleiben, sonst zeigte die Liste im Widget
    /// plötzlich fremde Einträge. <c>mine=true</c> gewinnt immer.
    /// </para>
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<RemarkDto>>> List(
        [FromQuery] RemarkStatus? status,
        [FromQuery] RemarkCategory? category,
        [FromQuery] int? childId,
        [FromQuery] string? appArea,
        [FromQuery] bool mine = false,
        [FromQuery] string? scope = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var self = User.AccountId();
        var all = WantsAllAccounts(scope) && !mine;
        if (all && !MayReadAllAccounts)
            return this.ProblemWithCode(ApiErrors.RemarkScopeForbidden,
                "Reading across accounts is disabled on this instance.");

        var q = mine
            ? db.Remarks.Where(r => self != null && r.AccountId == self)
            : await ScopedAsync(all, ct);
        q = q.AsNoTracking();

        if (status is { } st) q = q.Where(r => r.Status == st);
        if (category is { } cat) q = q.Where(r => r.Category == cat);
        if (childId is { } cid) q = q.Where(r => r.ChildId == cid);
        if (!string.IsNullOrWhiteSpace(appArea)) q = q.Where(r => r.AppArea == appArea);

        var (key, desc) = SortingExtensions.ParseSort(sort, dir);
        // Die Beitragszahl als **Projektion**, nicht per `Include`: Sonst lüde die Liste die vollständigen
        // Verläufe aller Zeilen mit, nur um sie zu zählen.
        var rows = await ApplySort(q, key, desc)
            .Select(r => new { Remark = r, Comments = r.Comments.Count })
            .ToPagedListAsync(Response, skip, take, ct);
        return rows.Select(x => ToDto(x.Remark, self, MaySeeAnswers, x.Comments)).ToList();
    }

    /// <summary>
    /// Eine einzelne Anmerkung – der Einstieg des Skills für „Beantworte die Frage 123".
    /// <para>
    /// Ein Admin greift hier <b>ohne</b> <c>scope=all</c> zu: Eine Id gezielt aufzurufen ist genau der
    /// Break-Glass-Fall, und ein Parameter, den man immer mitschicken müsste, wäre nur Rauschen.
    /// </para>
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemarkDto>> GetOne(int id, CancellationToken ct)
    {
        var scoped = await ScopedAsync(MayReadAllAccounts, ct);
        var row = await scoped.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { Remark = r, Comments = r.Comments.Count })
            .FirstOrDefaultAsync(ct);
        if (row is null) return this.ProblemWithCode(ApiErrors.RemarkNotFound, "Remark not found.");
        return ToDto(row.Remark, User.AccountId(), MaySeeAnswers, row.Comments);
    }

    /// <summary>
    /// Anmerkung ändern – Text/Einordnung/Stand und der Rückkanal (<c>answer</c>). PATCH-Semantik:
    /// <c>null</c> lässt den Wert stehen, geleert wird nur über die <c>clear…</c>-Schalter.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemarkDto>> Update(int id, UpdateRemarkDto dto, CancellationToken ct)
    {
        var scoped = await ScopedAsync(MayReadAllAccounts, ct);
        var remark = await scoped.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (remark is null) return this.ProblemWithCode(ApiErrors.RemarkNotFound, "Remark not found.");

        if (dto.Text is { } text)
        {
            if (string.IsNullOrWhiteSpace(text)) return this.ProblemWithCode(ApiErrors.ValidationError, "Text must not be empty.");
            remark.Text = text.Trim();
        }
        if (dto.Category is { } cat) remark.Category = cat;
        if (dto.Status is { } st) remark.Status = st;

        if (dto.Answer is { } answer)
        {
            remark.Answer = answer;
            remark.AnsweredAt = DateTime.UtcNow;
            // Auch hier gilt „null heißt nicht angegeben": Korrigiert der Skill nur den Wortlaut der
            // Antwort, darf der Urheber nicht verschwinden – der Export schriebe sonst „(unbekannt)".
            if (dto.AnsweredBy is { } by) remark.AnsweredBy = by;
        }

        // Erst der Wert, dann der Schalter – so gewinnt „leeren", wenn ein Formular beides schickt.
        if (dto.ClearAnswer) { remark.Answer = null; remark.AnsweredAt = null; remark.AnsweredBy = null; }
        if (dto.ClearChild) remark.ChildId = null;
        if (dto.ClearExercise) remark.ExerciseId = null;
        if (dto.ClearStudyPlan) remark.StudyPlanId = null;
        if (dto.ClearPlanPosition) remark.PlanPositionId = null;
        if (dto.ClearParent) remark.ParentRemarkId = null;

        await db.SaveChangesAsync(ct);
        var comments = await db.RemarkComments.CountAsync(c => c.RemarkId == remark.Id, ct);
        return ToDto(remark, User.AccountId(), MaySeeAnswers, comments);
    }

    // ── Verlauf ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Der Verlauf macht aus der Anmerkung einen Vorgang, der einen Arbeitsgang übersteht: Analyse,
    // Rückfrage und Umsetzungsnotiz stehen nebeneinander, statt einander zu überschreiben. Was er
    // ausdrücklich *nicht* ist: ein Chat. Es gibt keine Zustellung und keine Ungelesen-Marker – gelesen
    // wird beim nächsten Testen oder im nächsten Skill-Lauf.

    /// <summary>
    /// Der Verlauf einer Anmerkung, <b>älteste zuerst</b> – ein Vorgang liest sich chronologisch, anders als
    /// die Liste der Anmerkungen (neueste zuerst).
    /// </summary>
    [HttpGet("{id:int}/comments")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RemarkCommentDto>>> Comments(int id, CancellationToken ct)
    {
        if (!MaySeeAnswers) return this.ProblemWithCode(ApiErrors.Forbidden, "Students cannot read the discussion.");

        var scoped = await ScopedAsync(MayReadAllAccounts, ct);
        if (!await scoped.AnyAsync(r => r.Id == id, ct))
            return this.ProblemWithCode(ApiErrors.RemarkNotFound, "Remark not found.");

        var rows = await db.RemarkComments.AsNoTracking()
            .Where(c => c.RemarkId == id)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(ct);
        var self = User.AccountId();
        return rows.Select(c => ToDto(c, self)).ToList();
    }

    /// <summary>
    /// Einen Beitrag zum Verlauf hinzufügen.
    /// <para>
    /// <b>Wiederaufnahme:</b> Ein Beitrag des <b>Menschen</b> zu einer erledigten oder verworfenen Anmerkung
    /// setzt sie zurück auf <see cref="RemarkStatus.Open"/> – genau das macht aus dem Verlauf einen
    /// Arbeitsablauf, denn der Nachbereitungs-Skill legt offene Anmerkungen beim nächsten Lauf wieder vor.
    /// Ein Beitrag von Claude lässt den Stand unberührt: Er berichtet, er hakt nicht nach. Sonst würde jede
    /// Umsetzungsnotiz die eigene Anmerkung wieder aufreißen.
    /// </para>
    /// </summary>
    [HttpPost("{id:int}/comments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemarkCommentDto>> AddComment(int id, CreateRemarkCommentDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Body)) return this.ProblemWithCode(ApiErrors.ValidationError, "Body is required.");
        if (!MaySeeAnswers) return this.ProblemWithCode(ApiErrors.Forbidden, "Students cannot take part in the discussion.");

        var accountId = User.AccountId();
        if (accountId is null) return this.ProblemWithCode(ApiErrors.Unauthorized, "Token carries no account.");

        var scoped = await ScopedAsync(MayReadAllAccounts, ct);
        var remark = await scoped.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (remark is null) return this.ProblemWithCode(ApiErrors.RemarkNotFound, "Remark not found.");

        var author = dto.Author ?? RemarkCommentAuthor.Human;
        // Ohne Angabe den Anzeigenamen des Kontos einsetzen: Im Export steht sonst „Human", und wer den
        // Schnappschuss Wochen später liest, weiß nicht, wer geschrieben hat.
        var label = dto.AuthorLabel?.Trim() is { Length: > 0 } given
            ? given
            : await db.Accounts.AsNoTracking().Where(a => a.Id == accountId).Select(a => a.DisplayName).FirstOrDefaultAsync(ct);

        var comment = new RemarkComment
        {
            RemarkId = id,
            Body = dto.Body.Trim(),
            Author = author,
            AuthorLabel = label,
            AuthorAccountId = accountId,
        };
        db.RemarkComments.Add(comment);

        if (author == RemarkCommentAuthor.Human && remark.Status is RemarkStatus.Done or RemarkStatus.Rejected)
            remark.Status = RemarkStatus.Open;

        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Comments), new { id, version = "1.0" }, ToDto(comment, accountId));
    }

    /// <summary>
    /// Einen eigenen Beitrag zurücknehmen (Tippfehler). Fremde Beiträge darf nur ein Admin entfernen – ein
    /// Verlauf, aus dem jeder alles löschen kann, ist kein Protokoll mehr.
    /// </summary>
    [HttpDelete("{id:int}/comments/{commentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(int id, int commentId, CancellationToken ct)
    {
        var scoped = await ScopedAsync(MayReadAllAccounts, ct);
        if (!await scoped.AnyAsync(r => r.Id == id, ct))
            return this.ProblemWithCode(ApiErrors.RemarkNotFound, "Remark not found.");

        var self = User.AccountId();
        var comment = await db.RemarkComments.FirstOrDefaultAsync(c => c.Id == commentId && c.RemarkId == id, ct);
        // Fremder Beitrag: bewusst 404 statt 403 – ein „das darfst du nicht" wäre die Auskunft, dass es ihn gibt.
        if (comment is null || (comment.AuthorAccountId != self && !User.IsAdmin()))
            return this.ProblemWithCode(ApiErrors.RemarkCommentNotFound, "Remark comment not found.");

        db.RemarkComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Markdown-Schnappschuss der sichtbaren Anmerkungen. Der Skill legt ihn unter <c>docs/anmerkungen/</c>
    /// ab – damit ist der Stand versioniert und beim Nacharbeiten muss kein Server laufen.
    /// <para>
    /// Zugleich die <b>einzige Brücke zu den Test-Skills</b>: <c>creator</c>/<c>supervisor</c>/<c>student</c>
    /// laufen gegen eine Wegwerf-DB und kommen an die echten Anmerkungen nur über diese Datei.
    /// </para>
    /// Nur für Supervisor: Die Antworten tragen Datei- und Zeilenverweise, also Code-Interna.
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = Roles.Supervisor)]
    [Produces("text/markdown")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromQuery] RemarkStatus? status,
        [FromQuery] string? scope = null,
        [FromQuery] int take = 200,
        CancellationToken ct = default)
    {
        var all = WantsAllAccounts(scope);
        if (all && !MayReadAllAccounts)
            return this.ProblemWithCode(ApiErrors.RemarkScopeForbidden,
                "Reading across accounts is disabled on this instance.");

        var scoped = await ScopedAsync(all, ct);
        var q = scoped.AsNoTracking();
        if (status is { } st) q = q.Where(r => r.Status == st);

        // Älteste zuerst: Der Export wird gelesen wie ein Protokoll, und beim Nacharbeiten ist die
        // Reihenfolge des Auffallens die hilfreichere – anders als in der Widget-Liste.
        // Der Verlauf kommt per `Include` mit: Hier wird alles gerendert, anders als in der Liste, wo nur
        // gezählt wird.
        var rows = await q.OrderBy(r => r.Id)
            .Include(r => r.Comments)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync(ct);

        var filterNote = status is { } s ? $"status={s}" : "alle";
        // Bei kontenübergreifendem Export muss das Autor-Konto mit in den Text: Im Repo-Schnappschuss wäre
        // sonst nicht erkennbar, wessen Beobachtung eine Zeile ist.
        var markdown = export.Render(rows, filterNote, DateTime.UtcNow, showAccounts: all);
        return Content(markdown, "text/markdown", Encoding.UTF8);
    }

    /// <summary>
    /// Anmerkung löschen.
    /// <para>
    /// Bewusst <b>eng</b>, auch bei eingeschaltetem <c>GlobalRead</c>: Der Schalter heißt „global
    /// <i>read</i>" und meint genau das. Beantworten und Kommentieren über Kontogrenzen sind der Sinn der
    /// Sache; die Beobachtung eines anderen Kontos wegzuwerfen ist es nicht. Ein Admin darf es weiterhin
    /// (Break-Glass).
    /// </para>
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var scoped = await ScopedAsync(User.IsAdmin(), ct);
        var remark = await scoped.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (remark is null) return this.ProblemWithCode(ApiErrors.RemarkNotFound, "Remark not found.");

        db.Remarks.Remove(remark);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
