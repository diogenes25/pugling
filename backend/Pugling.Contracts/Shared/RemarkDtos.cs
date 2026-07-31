namespace Pugling.Contracts.Shared;

// Ebenen-übergreifender Vertrag der Test-Anmerkungen: Supervisor wie Student erfassen über dieselbe
// Ressource, die Trennung passiert über die Sichtbarkeit im Controller, nicht über zwei Verträge.

/// <summary>
/// The context snapshot at the time of capture – the real value of a remark.
/// The widget fills it in automatically; the human only types the text.
/// </summary>
/// <param name="Route">Path in the SPA, e.g. <c>/vater/kind/3/lernstand</c>.</param>
/// <param name="AppArea">Application area (<c>vater</c>/<c>sohn</c>).</param>
/// <param name="ChildId">Child that was selected at capture time.</param>
/// <param name="ExerciseId">Exercise that was open at capture time.</param>
/// <param name="StudyPlanId">Study plan that was open at capture time.</param>
/// <param name="PlanPositionId">Position that was open at capture time.</param>
/// <param name="ContextJson">State snapshot (filter, selection, open modal) as raw JSON – only IDs and filter values, never loaded entities.</param>
/// <param name="RecentErrorsJson">
/// Ring buffer of the most recent errors as raw JSON. <b>Metadata only</b> (method, path, status, error <c>code</c>,
/// timestamp) – no bodies, headers, or tokens: the login request carries the PIN in the body.
/// </param>
public record RemarkContextDto(
    string? Route,
    string? AppArea,
    int? ChildId,
    int? ExerciseId,
    int? StudyPlanId,
    int? PlanPositionId,
    string? ContextJson,
    string? RecentErrorsJson);

/// <summary>Capture a new remark. Only the text is required – everything else comes from the widget or stays empty.</summary>
/// <param name="Text">The observation/question text.</param>
/// <param name="Category">Optional categorization; defaults to <see cref="RemarkCategory.Unspecified"/> if omitted (the skill fills it in later).</param>
/// <param name="Context">Automatically captured context.</param>
/// <param name="ParentRemarkId">Optional reference to the remark this one arose from (set by the skill, not the widget).</param>
public record CreateRemarkDto(
    string Text,
    RemarkCategory? Category,
    RemarkContextDto? Context,
    int? ParentRemarkId);

/// <summary>
/// Change a remark. PATCH semantics: <c>null</c> means "not specified" (the value stays), <b>not</b>
/// "clear" – that is what the explicit <c>Clear…</c> switches are for.
/// </summary>
/// <param name="Text">New text.</param>
/// <param name="Category">New categorization.</param>
/// <param name="Status">New processing state.</param>
/// <param name="Answer">The answer (written back by the skill); retained even at <see cref="RemarkStatus.Planned"/>.</param>
/// <param name="AnsweredBy">Who answered, e.g. <c>claude-code</c>. Only takes effect together with <paramref name="Answer"/>.</param>
/// <param name="ClearAnswer">Clear the answer together with its timestamp/author.</param>
/// <param name="ClearChild">Clear the child reference.</param>
/// <param name="ClearExercise">Clear the exercise reference.</param>
/// <param name="ClearStudyPlan">Clear the study plan reference.</param>
/// <param name="ClearPlanPosition">Clear the position reference.</param>
/// <param name="ClearParent">Clear the reference to the parent remark.</param>
public record UpdateRemarkDto(
    string? Text,
    RemarkCategory? Category,
    RemarkStatus? Status,
    string? Answer,
    string? AnsweredBy,
    bool ClearAnswer = false,
    bool ClearChild = false,
    bool ClearExercise = false,
    bool ClearStudyPlan = false,
    bool ClearPlanPosition = false,
    bool ClearParent = false);

/// <summary>A remark, as delivered by the API.</summary>
/// <param name="Id">The "log id" shown to the human – the key for "answer question 123".</param>
/// <param name="Text">The observation/question text.</param>
/// <param name="Category">Categorization.</param>
/// <param name="Status">Processing state.</param>
/// <param name="Answer">Answer, if present.</param>
/// <param name="AnsweredAt">Time the answer was given.</param>
/// <param name="AnsweredBy">Author of the answer.</param>
/// <param name="ParentRemarkId">Predecessor remark, if this one arose from an answer.</param>
/// <param name="AccountId">Account of the person who captured it.</param>
/// <param name="AuthorRole">Role at the time of capture.</param>
/// <param name="IsOwn">Whether the remark originates from the requesting account – the widget shows only its own.</param>
/// <param name="Context">The captured context.</param>
/// <param name="UserAgent">Browser identifier.</param>
/// <param name="CreatedAt">Capture time (UTC).</param>
/// <param name="CommentCount">
/// Number of comments in the thread. Kept on the remark so the list and widget can show "3 comments"
/// without reloading the thread for every row.
/// </param>
public record RemarkDto(
    int Id,
    string Text,
    RemarkCategory Category,
    RemarkStatus Status,
    string? Answer,
    DateTime? AnsweredAt,
    string? AnsweredBy,
    int? ParentRemarkId,
    int AccountId,
    ProfileRole AuthorRole,
    bool IsOwn,
    RemarkContextDto Context,
    string? UserAgent,
    DateTime CreatedAt,
    int CommentCount);

/// <summary>
/// A comment in the thread of a remark. Complements <see cref="RemarkDto.Answer"/> (the one authoritative
/// resolution) rather than replacing it: analysis, follow-up question, and implementation note sit
/// side by side instead of overwriting one another.
/// </summary>
/// <param name="Id">Id of the comment.</param>
/// <param name="RemarkId">Remark it belongs to.</param>
/// <param name="Body">The text.</param>
/// <param name="Author">Human or Claude.</param>
/// <param name="AuthorLabel">Display name, e.g. <c>claude-code</c>.</param>
/// <param name="AuthorAccountId">Account of the writer, if known.</param>
/// <param name="IsOwn">Whether the comment originates from the requesting account – only own comments can be deleted.</param>
/// <param name="CreatedAt">Timestamp (UTC).</param>
public record RemarkCommentDto(
    int Id,
    int RemarkId,
    string Body,
    RemarkCommentAuthor Author,
    string? AuthorLabel,
    int? AuthorAccountId,
    bool IsOwn,
    DateTime CreatedAt);

/// <summary>
/// Add a comment to the thread.
/// <para>
/// <b>Deliberate side effect:</b> A <see cref="RemarkCommentAuthor.Human"/> comment on a resolved or
/// dismissed remark pulls it back to <see cref="RemarkStatus.Open"/> – so the follow-up skill surfaces it
/// again on its next run. A <see cref="RemarkCommentAuthor.Assistant"/> comment leaves the state
/// untouched; it reports, it does not ask back.
/// </para>
/// </summary>
/// <param name="Body">The text – required.</param>
/// <param name="Author">Origin; defaults to <see cref="RemarkCommentAuthor.Human"/> if omitted.</param>
/// <param name="AuthorLabel">Display name, e.g. <c>claude-code</c>.</param>
public record CreateRemarkCommentDto(
    string Body,
    RemarkCommentAuthor? Author,
    string? AuthorLabel);
