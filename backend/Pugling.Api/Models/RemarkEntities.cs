namespace Pugling.Api.Models;

// RemarkCategory/RemarkStatus live in the contract project (Pugling.Contracts).

/// <summary>
/// A <b>remark</b> captured while testing: a question, an observation or a finding, recorded right where it
/// showed up. Its domain value does not sit in the text – a text document could hold that too – but in the
/// <b>captured context</b> (route, role, child/exercise, recent errors): exactly what a human does not write
/// down while testing, and exactly what costs the time later when reproducing it.
/// <para>
/// Remarks are only ever created through the UI widget. The test skills (<c>creator</c>/<c>supervisor</c>/
/// <c>student</c>, <c>/smoke-test</c>) <b>read</b> them through the markdown export but create none – they run
/// against a throwaway DB, and an entry created there would be deleted with it.
/// </para>
/// </summary>
public class Remark
{
    /// <summary>
    /// The domain-visible "log id": the widget shows it after saving so the human can take it along and
    /// redeem it in Claude Code ("answer question 123").
    /// </summary>
    public int Id { get; set; }

    /// <summary>The actual text – the only mandatory input field.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Classification. Deliberately stays <see cref="RemarkCategory.Unspecified"/> quite often: categorizing
    /// while capturing costs more time than it yields – the skill derives it from the text afterwards.
    /// </summary>
    public RemarkCategory Category { get; set; } = RemarkCategory.Unspecified;

    /// <summary>Processing state. Without it the follow-up skill presents the same remarks again on every run.</summary>
    public RemarkStatus Status { get; set; } = RemarkStatus.Open;

    // --- Answer (the back channel from Claude Code) ---

    /// <summary>
    /// The answer to a question. It is <b>kept even at <see cref="RemarkStatus.Planned"/></b>: a deferred case
    /// is then no longer an open note but an already analyzed backlog entry – the groundwork for the later
    /// implementation is done.
    /// </summary>
    public string? Answer { get; set; }

    /// <summary>Instant the answer was given (UTC).</summary>
    public DateTime? AnsweredAt { get; set; }

    /// <summary>
    /// Who answered, e.g. <c>claude-code</c>. Deliberately a protocol <c>string</c> and not an enum:
    /// a human should be able to stand here later without the schema moving.
    /// </summary>
    public string? AnsweredBy { get; set; }

    /// <summary>
    /// Optional reference to the remark this one grew out of – the trail from the question to the task that
    /// came from it. It is set by the skill, not by the widget.
    /// <para>
    /// Not to be confused with <see cref="Comments"/>: the reference leads <b>between</b> cases
    /// (the question became a task), the history lies <b>within</b> one case.
    /// </para>
    /// </summary>
    public int? ParentRemarkId { get; set; }
    public Remark? ParentRemark { get; set; }

    /// <summary>
    /// The history: analysis addenda, follow-up questions from the human, implementation notes. It complements
    /// <see cref="Answer"/>, it does not replace it – the answer stays the *one* substantiated resolution, the
    /// history carries everything that comes afterwards.
    /// <para>
    /// The history is the reason a remark survives a work step: before it, an implementation note overwrote the
    /// preceding analysis and the groundwork was lost.
    /// </para>
    /// </summary>
    public ICollection<RemarkComment> Comments { get; set; } = [];

    // --- Author ---

    /// <summary>Account of the person capturing it (claim <c>aid</c>).</summary>
    public int AccountId { get; set; }
    public Account? Account { get; set; }

    /// <summary>
    /// Role <b>at the time of capturing</b> (a snapshot like <c>SupervisorId</c> on
    /// <see cref="ShopPurchase"/>). An account can carry several roles; for the classification the one that
    /// was being tested in counts.
    /// </summary>
    public ProfileRole AuthorRole { get; set; }

    // --- Context snapshot (the heart of it) ---

    /// <summary>Path within the SPA, e.g. <c>/vater/kind/3/lernstand</c>.</summary>
    public string Route { get; set; } = "";

    /// <summary>Application area (<c>vater</c>/<c>sohn</c>) – explicit instead of guessed from the route.</summary>
    public string AppArea { get; set; } = "";

    /// <summary>Child that was selected while capturing. FK <c>SetNull</c>: the context may fade, it must block nothing.</summary>
    public int? ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Exercise that was open while capturing.</summary>
    public int? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Study plan that was open while capturing.</summary>
    public int? StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }

    /// <summary>Position that was open while capturing.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }

    /// <summary>
    /// State snapshot (filters, open modal, selection) as raw JSON. Deliberately a <c>string</c> instead of a
    /// typed column: the backend never reads it in a domain sense – only the follow-up skill does. That also
    /// makes the <c>ValueComparer</c> unnecessary that a mapped JSON column would need.
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// Ring buffer of the most recent failed requests and JS errors, as raw JSON (same rationale as
    /// <see cref="ContextJson"/>).
    /// <para>
    /// <b>Security rule:</b> metadata only – method, path, status, error <c>code</c>, timestamp. <b>No</b>
    /// request/response bodies, <b>no</b> headers, <b>no</b> tokens: the login request carries the PIN in its
    /// body, and a raw capture would put it into the DB in clear text and carry it into the repository through
    /// the export.
    /// </para>
    /// </summary>
    public string? RecentErrorsJson { get; set; }

    /// <summary>Browser identification – separates phone observations from desktop observations.</summary>
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One entry in the history of a <see cref="Remark"/>: an analysis addendum, a follow-up question from the
/// human, an implementation note.
/// <para>
/// <b>Why it exists:</b> with only one <see cref="Remark.Answer"/> field, every second work step overwrote the
/// first – the substantiated analysis vanished behind the "built: …". The history records the order that
/// matches how the work actually happens: analyze, defer, implement later.
/// </para>
/// <para>
/// <b>And what it is not:</b> a chat. There is no delivery, no unread markers and no expectation that somebody
/// is waiting – it is read during the next testing session or on the next skill run.
/// </para>
/// </summary>
public class RemarkComment
{
    public int Id { get; set; }

    /// <summary>The remark the entry belongs to. FK <b>cascade</b>: a history without its case is pointless.</summary>
    public int RemarkId { get; set; }
    public Remark? Remark { get; set; }

    /// <summary>The text – the only mandatory field.</summary>
    public string Body { get; set; } = "";

    /// <summary>
    /// Human or Claude. It drives the reopening: a <see cref="RemarkCommentAuthor.Human"/> entry pulls a
    /// finished remark back to <see cref="RemarkStatus.Open"/>.
    /// </summary>
    public RemarkCommentAuthor Author { get; set; } = RemarkCommentAuthor.Human;

    /// <summary>
    /// Display name of the author, e.g. <c>claude-code</c>. Deliberately a protocol <c>string</c> like
    /// <see cref="Remark.AnsweredBy"/> – that way another participant can join later without a schema rebuild.
    /// </summary>
    public string? AuthorLabel { get; set; }

    /// <summary>
    /// Account of the writer. FK <c>SetNull</c>, because a deleted account must not take the history with it –
    /// the entry's domain statement still holds.
    /// </summary>
    public int? AuthorAccountId { get; set; }
    public Account? AuthorAccount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
