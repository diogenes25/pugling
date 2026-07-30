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
/// Remarks made while testing: questions, observations and findings together with the context in which
/// they came up. Captured in the UI widget, answered by Claude Code – triggered by the human via the
/// id assigned here ("Answer question 123").
/// <para>
/// <b>Deliberately tier-neutral</b> (no <c>creator</c>/<c>supervisor</c>/<c>student</c> prefix, precedent
/// <see cref="AuthController"/>): the resource belongs to none of the three tiers – the same person captures
/// them sometimes from the father web app, sometimes from the child arcade. Gated therefore only with <c>[Authorize]</c>, the
/// roles are separated inline (the pattern of the student endpoints).
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
    /// <summary>Sort key of the list (whitelist – no dynamic property access).</summary>
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
    /// Projection onto the contract. <paramref name="withAnswer"/> hides the answer: it comes from
    /// Claude Code and carries file/line references, i.e. code internals. Same reasoning as why the
    /// export is supervisor-only – without the filter the two would contradict each other, since the widget
    /// of the child arcade shows the answer to its own remark.
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

    /// <summary>Projection of an entry; <paramref name="viewerAccountId"/> determines <c>IsOwn</c> (only own entries are deletable).</summary>
    private static RemarkCommentDto ToDto(RemarkComment c, int? viewerAccountId) => new(
        c.Id, c.RemarkId, c.Body, c.Author, c.AuthorLabel, c.AuthorAccountId,
        c.AuthorAccountId is { } a && a == viewerAccountId, c.CreatedAt);

    /// <summary>
    /// Whether the caller may see answers and history – not a student, see <see cref="ToDto(Remark, int?, bool, int)"/>.
    /// </summary>
    private bool MaySeeAnswers => !User.IsStudent() || User.IsSupervisor();

    /// <summary>
    /// The set of remarks the caller may access – the <b>one</b> place where
    /// visibility is decided.
    /// <para>
    /// <paramref name="allAccounts"/> lifts the restriction. The permission for that is
    /// <see cref="MayReadAllAccounts"/>; on <b>lists</b> the explicit <c>scope=all</c> is required
    /// in addition, so the default stays narrow (otherwise the list in the widget would show entries from
    /// other accounts). When accessing a <b>single id</b>, the permission alone is enough – that's exactly what
    /// the skill needs to answer a remark from any test account.
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
    /// Whether the caller may read across account boundaries.
    /// <para>
    /// This is <b>tied to the <see cref="RemarkOptions.GlobalRead"/> switch, not a role</b>:
    /// testing constantly produces throwaway accounts (a bug often only shows up in a specific
    /// constellation – a fresh father account with no exercises reveals what never shows up with the seeded
    /// dad), and giving each one a flag individually would be administrative work with no payoff. <c>Admin</c>
    /// remains allowed in addition, but doesn't work as the <i>condition</i>: that role also bypasses the RWX
    /// permissions on exercises, and those have nothing to do with remarks.
    /// </para>
    /// <para>
    /// A <b>student</b> is always excluded – even with <c>GlobalRead</c> switched on. Answers and
    /// history carry file and line references; on the day the child sees the widget, it must not be able to
    /// read the adults' testing notes.
    /// </para>
    /// </summary>
    private bool MayReadAllAccounts => !User.IsStudent() && (options.GlobalRead || User.IsAdmin());

    /// <summary>
    /// The accounts whose remarks the caller may see: always its own, and for a supervisor
    /// additionally the accounts of the children it supervises.
    /// <para>
    /// The separation is not a formality: answers carry file and line references, i.e. code internals.
    /// On the day the child sees the widget, it must be able to read neither the father's testing notes nor
    /// their answers – which is why a student sees <b>exclusively</b> its own remarks.
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
    /// Returns the id if the reference exists – otherwise <c>null</c>. Only what is
    /// set is checked; in the normal case (no reference) no query is made.
    /// </summary>
    private static async Task<int?> ExistingAsync(int? id, Func<int, Task<bool>> exists) =>
        id is { } value && await exists(value) ? value : null;

    /// <summary>
    /// Capture a remark. Only the text is mandatory; author and role come from the token, the
    /// context from the widget. The response carries the <b>id</b> – it's the key used to later
    /// redeem the question in Claude Code.
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
        var childId = await ExistingAsync(ctx?.ChildId, id => access.OwnsChildAsync(User, id, ct));
        var planId = await ExistingAsync(ctx?.StudyPlanId, async id =>
            await db.StudyPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) is { } plan
            && await access.OwnsPlanAsync(User, plan, ct));
        var positionId = await ExistingAsync(ctx?.PlanPositionId, async id =>
            await db.PlanPositions.AsNoTracking().Include(p => p.StudyPlan)
                .FirstOrDefaultAsync(p => p.Id == id, ct) is { StudyPlan: { } plan }
            && await access.OwnsPlanAsync(User, plan, ct));
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
    /// List remarks. <c>mine=true</c> restricts to one's own – that's the query behind the
    /// list in the widget; without the filter a supervisor would also see the child's.
    /// <para>
    /// <c>scope=all</c> lifts the account boundary – that's the view of the follow-up skill that
    /// collects remarks from all test accounts. Allowed if <see cref="MayReadAllAccounts"/> holds
    /// (switch <c>Remarks:GlobalRead</c>, on in development), otherwise <c>403</c>. Deliberately an
    /// <b>explicit</b> parameter: the default must stay narrow, otherwise the list in the widget would
    /// suddenly show entries from other accounts. <c>mine=true</c> always wins.
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
    /// A single remark – the entry point of the skill for "Answer question 123".
    /// <para>
    /// An admin accesses this <b>without</b> <c>scope=all</c>: targeting a single id is exactly the
    /// break-glass case, and a parameter you'd always have to send along would just be noise.
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
    /// Change a remark – text/categorization/status and the return channel (<c>answer</c>). PATCH semantics:
    /// <c>null</c> leaves the value as is, clearing happens only via the <c>clear…</c> switches.
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
    /// The history of a remark, <b>oldest first</b> – a case reads chronologically, unlike
    /// the list of remarks (newest first).
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
    /// Add an entry to the history.
    /// <para>
    /// <b>Reopening:</b> an entry by the <b>human</b> on a done or rejected remark
    /// resets it to <see cref="RemarkStatus.Open"/> – that's exactly what turns the history into a
    /// workflow, since the follow-up skill re-presents open remarks on its next run.
    /// An entry from Claude leaves the status untouched: it reports, it doesn't follow up. Otherwise every
    /// implementation note would reopen its own remark.
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
    /// Retract one of your own entries (typo). Entries from others may only be removed by an admin – a
    /// history from which anyone can delete anything is no longer a record.
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
    /// Markdown snapshot of the visible remarks. The skill saves it under <c>docs/anmerkungen/</c>
    /// – that way the state is version-controlled and no server needs to be running during follow-up.
    /// <para>
    /// At the same time the <b>only bridge to the test skills</b>: <c>creator</c>/<c>supervisor</c>/<c>student</c>
    /// run against a throwaway DB and can only reach the real remarks via this file.
    /// </para>
    /// Supervisor only: the answers carry file and line references, i.e. code internals.
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
    /// Delete a remark.
    /// <para>
    /// Deliberately <b>narrow</b>, even with <c>GlobalRead</c> switched on: the switch is called "global
    /// <i>read</i>" and means exactly that. Answering and commenting across account boundaries is the point of
    /// the feature; discarding another account's observation is not. An admin may still do it
    /// (break-glass).
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
