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
        // Tiebreaker as in the other branches: the widget is a quick-capture tool, two remarks can carry the
        // same timestamp. Without it the paging window would not be deterministic (a row would show up on two
        // pages or on none).
        "createdAt" => desc
            ? q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            : q.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
        "status" => desc ? q.OrderByDescending(r => r.Status).ThenByDescending(r => r.Id) : q.OrderBy(r => r.Status).ThenBy(r => r.Id),
        "category" => desc ? q.OrderByDescending(r => r.Category).ThenByDescending(r => r.Id) : q.OrderBy(r => r.Category).ThenBy(r => r.Id),
        // Default: newest first - that is how the widget reads its own list.
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
        // Always 0 for a student: they must not see the history, and even the count would disclose that their
        // remark has been discussed.
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

        // The parent remark must be visible - otherwise the reference would allow inferring other people's entries.
        if (dto.ParentRemarkId is { } parentId)
        {
            var visible = await VisibleAccountIdsAsync(ct);
            var exists = await db.Remarks.AnyAsync(r => r.Id == parentId && visible.Contains(r.AccountId), ct);
            if (!exists) return this.ProblemWithCode(ApiErrors.InvalidReference, "Parent remark not found.");
        }

        var ctx = dto.Context;
        // Context references that point nowhere **or do not belong to the caller** are dropped silently
        // instead of failing the POST. The widget sends them automatically - including ones read from the URL
        // (`/vater/kind/999`). A deleted child or a typo in the address must not block capturing and destroy
        // the typed text. Same stance as the `SetNull` FKs: the context may fade, it must block nothing.
        //
        // The ownership check is not decoration: without it the response would be an oracle for which ids
        // exist - a student could try other people's child/plan ids and read the answer off the echoed context.
        //
        // `ParentRemarkId` above is handled differently on purpose (400): the skill sets it explicitly, a
        // mistake there is a bug and not a withered piece of automation.
        var childId = await ExistingAsync(ctx?.ChildId, id => access.OwnsChildAsync(User, id, ct));
        var planId = await ExistingAsync(ctx?.StudyPlanId, async id =>
            await db.StudyPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) is { } plan
            && await access.OwnsPlanAsync(User, plan, ct));
        var positionId = await ExistingAsync(ctx?.PlanPositionId, async id =>
            await db.PlanPositions.AsNoTracking().Include(p => p.StudyPlan)
                .FirstOrDefaultAsync(p => p.Id == id, ct) is { StudyPlan: { } plan }
            && await access.OwnsPlanAsync(User, plan, ct));
        // Exercises are globally readable on purpose (the shared catalog) - their existence is no secret, an
        // existence check is enough here.
        var exerciseId = await ExistingAsync(ctx?.ExerciseId, id => db.Exercises.AnyAsync(x => x.Id == id, ct));

        var remark = new Remark
        {
            Text = dto.Text.Trim(),
            Category = dto.Category ?? RemarkCategory.Unspecified,
            Status = RemarkStatus.Open,
            AccountId = accountId.Value,
            // The role that was being tested in. A supervisor account carries creator and supervisor; for the
            // classification the driving tier counts.
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
        // Freshly captured: the history is necessarily empty.
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
        // The comment count as a **projection**, not through `Include`: otherwise the list would load the full
        // histories of all rows just to count them.
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
            // "null means not specified" holds here too: if the skill only corrects the wording of the answer,
            // the author must not disappear - the export would otherwise write "(unknown)".
            if (dto.AnsweredBy is { } by) remark.AnsweredBy = by;
        }

        // Value first, switch second - that way "clear" wins if a form sends both.
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

    // ── History ───────────────────────────────────────────────────────────────────────────────────────
    //
    // The history turns a remark into a case that survives a work step: analysis, follow-up question and
    // implementation note stand side by side instead of overwriting each other. What it explicitly is *not*:
    // a chat. There is no delivery and no unread markers - it is read during the next testing session or on
    // the next skill run.

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
        // With nothing given, use the account's display name: otherwise the export says "Human", and whoever
        // reads the snapshot weeks later does not know who wrote it.
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
        // Someone else's entry: 404 on purpose instead of 403 - a "you may not" would disclose that it exists.
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

        // Oldest first: the export is read like a log, and when following up, the order in which things came
        // up is the more helpful one - unlike in the widget list.
        // The history comes along through `Include`: everything is rendered here, unlike in the list, where it
        // is only counted.
        var rows = await q.OrderBy(r => r.Id)
            .Include(r => r.Comments)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync(ct);

        var filterNote = status is { } s ? $"status={s}" : "alle";
        // For a cross-account export the author account must go into the text: in the repository snapshot it
        // would otherwise be impossible to tell whose observation a line is.
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
