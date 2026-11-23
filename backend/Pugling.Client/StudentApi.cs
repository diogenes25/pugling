namespace Pugling.Client;

/// <summary>
/// Typed access to the <b>read views</b> of the Student tier (<c>api/v1/student/…</c>): a child's
/// cross-plan vocabulary learning state, their word rollup, and the catalog-hierarchical
/// drill-down view. These endpoints are deliberately only <c>[Authorize]</c> and separate the roles inline –
/// a <b>Supervisor</b> account may therefore also read its child's state. This is exactly what an agent's
/// weakness analysis relies on; playing itself (practice/test) is out of scope here.
/// </summary>
public sealed class StudentApi(HttpClient http)
{
    private const string Root = "api/v1/student";

    /// <summary>The underlying HttpClient – an escape hatch for endpoints that don't (yet) have a wrapper.</summary>
    public HttpClient Http { get; } = http;

    // ---------------------------------------------------------------- Vocabulary progress (flat)

    /// <summary>
    /// The item learning state of a child across all study plans. <paramref name="onlyWeak"/> returns the
    /// shaky candidates (low box / poor hit rate) – the basis for targeted remedial exercises.
    /// </summary>
    public Task<IReadOnlyList<ItemProgressResponse>> ListVocabularyProgressAsync(int childId,
        int? exerciseId = null, int? maxBox = null, bool onlyWeak = false,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ItemProgressResponse>>($"{Root}/children/{childId}/vocabulary-progress"
            + PuglingHttp.Query(("exerciseId", exerciseId), ("maxBox", maxBox), ("onlyWeak", onlyWeak),
                ("skip", skip), ("take", take)), ct);

    /// <summary>
    /// The learning state per <b>word</b> (aggregated across all exercises using this store word) – the
    /// "poorly learned words" view, because the same word can appear in multiple exercises.
    /// <paramref name="sourceLang"/>/<paramref name="targetLang"/> narrow it to one language pair (omitted = all).
    /// </summary>
    public Task<IReadOnlyList<WordMasteryResponse>> ListWordMasteryAsync(int childId, bool onlyWeak = false,
        int skip = 0, int take = 50, string? sourceLang = null, string? targetLang = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<WordMasteryResponse>>($"{Root}/children/{childId}/vocabulary-progress/by-word"
            + PuglingHttp.Query(("onlyWeak", onlyWeak), ("sourceLang", sourceLang), ("targetLang", targetLang),
                ("skip", skip), ("take", take)), ct);

    /// <summary>The answer history of an item (what was answered when and how).</summary>
    public Task<IReadOnlyList<HistoryResponse>> ListItemHistoryAsync(int childId, int itemId,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<HistoryResponse>>($"{Root}/children/{childId}/vocabulary-progress/{itemId}/history"
            + PuglingHttp.Query(("skip", skip), ("take", take)), ct);

    // ---------------------------------------------------------------- Progress along the catalog

    /// <summary>
    /// The subjects relevant to the child with aggregated progress. <paramref name="active"/> distinguishes
    /// currently assigned content from purely historical content.
    /// </summary>
    public Task<IReadOnlyList<SubjectProgressResponse>> ListSubjectProgressAsync(int childId,
        string? search = null, bool? active = null, string? sort = null, string? dir = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SubjectProgressResponse>>($"{Root}/children/{childId}/learn/subjects"
            + PuglingHttp.Query(("search", search), ("active", active), ("sort", sort), ("dir", dir),
                ("skip", skip), ("take", take)), ct);

    /// <summary>The series units of a subject with aggregated progress.</summary>
    public Task<IReadOnlyList<SeriesUnitProgressResponse>> ListSeriesUnitProgressAsync(int childId, int subjectId,
        string? search = null, bool? active = null, string? sort = null, string? dir = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SeriesUnitProgressResponse>>($"{Root}/children/{childId}/learn/subjects/{subjectId}/series-units"
            + PuglingHttp.Query(("search", search), ("active", active), ("sort", sort), ("dir", dir),
                ("skip", skip), ("take", take)), ct);

    /// <summary>The vocabulary exercises of a series unit with the child's progress per exercise.</summary>
    public Task<IReadOnlyList<ExerciseProgressResponse>> ListExerciseProgressAsync(int childId, int subjectId,
        int seriesUnitId, string? search = null, bool? active = null, string? sort = null, string? dir = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ExerciseProgressResponse>>(
            $"{Root}/children/{childId}/learn/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary"
            + PuglingHttp.Query(("search", search), ("active", active), ("sort", sort), ("dir", dir),
                ("skip", skip), ("take", take)), ct);

    /// <summary>
    /// "Different image": rejects the frozen image choice of a carrier and redraws. Without an alternative
    /// the existing choice remains unchanged (<c>409 media_no_alternative</c>) instead of making the card imageless.
    /// </summary>
    public Task<SelectedMediaResponse> ReshuffleMediaAsync(int childId, ReshuffleMediaDto dto,
        CancellationToken ct = default) =>
        Http.PostAsync<SelectedMediaResponse>($"{Root}/children/{childId}/media-picks/reshuffle", dto, ct);

    // ---------------------------------------------------------------- The child describes itself (B-50)

    /// <summary>
    /// The selectable interest keywords the child may pick from. Unlike the creator's view this list
    /// carries no usage counts - it is a picking list, not a catalog view.
    /// </summary>
    public Task<IReadOnlyList<InterestTagOptionResponse>> ListInterestOptionsAsync(InterestFacet? facet = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<InterestTagOptionResponse>>($"{Root}/interest-tags"
            + PuglingHttp.Query(("facet", facet), ("skip", skip), ("take", take)), ct);

    /// <summary>The logged-in child's own interest profile (weighted keywords plus free-text wishes).</summary>
    public Task<MyInterestProfileResponse> GetMyInterestsAsync(CancellationToken ct = default) =>
        Http.GetAsync<MyInterestProfileResponse>($"{Root}/me/interests", ct);

    /// <summary>The child's earned coins this week against its own previous week up to now (B-253).</summary>
    public Task<WeekCoinsResponse> GetWeekCoinsAsync(CancellationToken ct = default) =>
        Http.GetAsync<WeekCoinsResponse>($"{Root}/me/points/week", ct);

    /// <summary>
    /// Replaces the child's own interests entirely. Resolves against <b>existing</b> tags only - an unknown
    /// id yields <c>invalid_reference</c> rather than quietly creating taxonomy.
    /// </summary>
    public Task<MyInterestProfileResponse> SetMyInterestsAsync(SetMyInterestsDto dto, CancellationToken ct = default) =>
        Http.PutAsync<MyInterestProfileResponse>($"{Root}/me/interests", dto, ct);

    /// <summary>Adds a free-text wish outside the taxonomy; it lands in the supervisor-visible interest list.</summary>
    public Task<MyInterestProfileResponse> AddMyInterestWishAsync(AddInterestWishDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MyInterestProfileResponse>($"{Root}/me/interests/wishes", dto, ct);

    // ---------------------------------------------------------------- Bonus training

    /// <summary>The vocabulary positions the child's tower game can build a run from.</summary>
    public Task<TowerGameOverview> GetTowerGameAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<TowerGameOverview>($"{Root}/children/{childId}/tower-game", ct);

    /// <summary>Starts a tower-game run from one plan position - the child's own seat only.</summary>
    public Task<TowerRunResponse> StartTowerRunAsync(int childId, StartTowerRunDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TowerRunResponse>($"{Root}/children/{childId}/tower-game/runs", dto, ct);

    /// <summary>Assigns a token to a tower and returns the server's grading.</summary>
    public Task<TowerAssignResult> AssignTowerTokenAsync(int childId, AssignTowerTokenDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TowerAssignResult>($"{Root}/children/{childId}/tower-game/assign", dto, ct);

    /// <summary>Counts play time of a running tower-game level; <c>TimeUp</c> tells the app to end it.</summary>
    public Task<TowerHeartbeatResult> TowerHeartbeatAsync(int childId, int runId, CancellationToken ct = default) =>
        Http.PostAsync<TowerHeartbeatResult>($"{Root}/children/{childId}/tower-game/runs/{runId}/heartbeat", null, ct);

    /// <summary>Ends a tower-game level; the server decides whether it is won and pays the coins.</summary>
    public Task<TowerRunEndResult> EndTowerRunAsync(int childId, int runId, CancellationToken ct = default) =>
        Http.PostAsync<TowerRunEndResult>($"{Root}/children/{childId}/tower-game/runs/{runId}/end", null, ct);

    /// <summary>Today's bonus training of the child: open or locked, bonus coins against the cap, the exercise pool.</summary>
    public Task<ExtraTrainingOverview> GetExtraTrainingAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<ExtraTrainingOverview>($"{Root}/children/{childId}/extra-training", ct);

    /// <summary>Starts (or resumes) a bonus run of an exercise - the child's own seat only.</summary>
    public Task<ExtraRunResponse> StartExtraRunAsync(int childId, int exerciseId, CancellationToken ct = default) =>
        Http.PostAsync<ExtraRunResponse>($"{Root}/children/{childId}/extra-training/{exerciseId}/runs", null, ct);

    /// <summary>Submits the answers of a bonus run and returns the graded result with the coins booked.</summary>
    public Task<ExtraRunResult> SubmitExtraRunAsync(int childId, int runId, SubmitExtraRunDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ExtraRunResult>($"{Root}/children/{childId}/extra-training/runs/{runId}/submit", dto, ct);

    // ---------------------------------------------------------------- Mixed review (B-764)

    /// <summary>Today's mixed review of the child: the bonus lock, the cards due for it, an open round, the shared coins.</summary>
    public Task<MixedReviewOverview> GetMixedReviewAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<MixedReviewOverview>($"{Root}/children/{childId}/mixed-review", ct);

    /// <summary>Starts (or resumes) a mixed review round - the child's own seat only.</summary>
    public Task<MixedReviewRunResponse> StartMixedReviewAsync(int childId, CancellationToken ct = default) =>
        Http.PostAsync<MixedReviewRunResponse>($"{Root}/children/{childId}/mixed-review/runs", null, ct);

    // ---------------------------------------------------------------- The child's day

    /// <summary>Today's duties of all active plans of the child, with the streak and the daily chest (B-606).</summary>
    public Task<TodayResponse> GetTodayAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<TodayResponse>($"{Root}/children/{childId}/today", ct);

    // ---------------------------------------------------------------- Daily reward chest

    /// <summary>Opens the oldest waiting reward chest and books its draw - the child's own seat only.</summary>
    public Task<DailyBoxOpened> OpenDailyBoxAsync(int childId, CancellationToken ct = default) =>
        Http.PostAsync<DailyBoxOpened>($"{Root}/children/{childId}/daily-box/open", null, ct);

    /// <summary>The child reports a card (or the whole exercise) of a position with reasons and/or a text (B-628).</summary>
    public Task<FeedbackReceipt> ReportFeedbackAsync(int planId, int positionId, CreateFeedbackDto dto,
        CancellationToken ct = default) =>
        Http.PostAsync<FeedbackReceipt>($"{Root}/study-plans/{planId}/positions/{positionId}/feedback", dto, ct);

    /// <summary>The fixed feedback reasons with their reach (B-628).</summary>
    public Task<IReadOnlyList<FeedbackReasonInfo>> ListFeedbackReasonsAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<FeedbackReasonInfo>>($"{Root}/feedback-reasons", ct);
}
