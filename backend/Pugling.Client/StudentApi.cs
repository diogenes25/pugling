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
    /// </summary>
    public Task<IReadOnlyList<WordMasteryResponse>> ListWordMasteryAsync(int childId, bool onlyWeak = false,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<WordMasteryResponse>>($"{Root}/children/{childId}/vocabulary-progress/by-word"
            + PuglingHttp.Query(("onlyWeak", onlyWeak), ("skip", skip), ("take", take)), ct);

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
}
