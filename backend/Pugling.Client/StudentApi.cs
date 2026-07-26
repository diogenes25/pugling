namespace Pugling.Client;

/// <summary>
/// Typisierter Zugriff auf die <b>Lesesichten</b> der Student-Ebene (<c>api/v1/student/…</c>): der
/// plan-übergreifende Vokabel-Lernstand eines Kindes, sein Wort-Rollup und die katalog-hierarchische
/// Drill-down-Sicht. Diese Endpunkte sind bewusst nur <c>[Authorize]</c> und trennen die Rollen inline –
/// ein <b>Supervisor</b>-Konto darf den Stand seines Kindes also mitlesen. Genau darauf beruht die
/// Schwächen-Analyse eines Agenten; das Spielen selbst (Üben/Test) bleibt außen vor.
/// </summary>
public sealed class StudentApi(HttpClient http)
{
    private const string Root = "api/v1/student";

    /// <summary>Der zugrunde liegende HttpClient – Ausweg für Endpunkte, die (noch) keinen Wrapper haben.</summary>
    public HttpClient Http { get; } = http;

    // ---------------------------------------------------------------- Vokabel-Lernstand (flach)

    /// <summary>
    /// Der Item-Lernstand eines Kindes über alle Lehrpläne hinweg. <paramref name="onlyWeak"/> liefert die
    /// Wackelkandidaten (niedrige Box / schlechte Trefferquote) – die Grundlage für gezielte Förderübungen.
    /// </summary>
    public Task<IReadOnlyList<ItemProgressResponse>> ListVocabularyProgressAsync(int childId,
        int? exerciseId = null, int? maxBox = null, bool onlyWeak = false,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ItemProgressResponse>>($"{Root}/children/{childId}/vocabulary-progress"
            + PuglingHttp.Query(("exerciseId", exerciseId), ("maxBox", maxBox), ("onlyWeak", onlyWeak),
                ("skip", skip), ("take", take)), ct);

    /// <summary>
    /// Der Lernstand je <b>Wort</b> (über alle Übungen aggregiert, die dieses Store-Wort nutzen) – die Sicht
    /// „schlecht gelernte Wörter", weil dasselbe Wort in mehreren Übungen stecken kann.
    /// </summary>
    public Task<IReadOnlyList<WordMasteryResponse>> ListWordMasteryAsync(int childId, bool onlyWeak = false,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<WordMasteryResponse>>($"{Root}/children/{childId}/vocabulary-progress/by-word"
            + PuglingHttp.Query(("onlyWeak", onlyWeak), ("skip", skip), ("take", take)), ct);

    /// <summary>Die Antwort-Historie eines Items (was wurde wann wie beantwortet).</summary>
    public Task<IReadOnlyList<HistoryResponse>> ListItemHistoryAsync(int childId, int itemId,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<HistoryResponse>>($"{Root}/children/{childId}/vocabulary-progress/{itemId}/history"
            + PuglingHttp.Query(("skip", skip), ("take", take)), ct);

    // ---------------------------------------------------------------- Lernstand entlang des Katalogs

    /// <summary>
    /// Die für das Kind relevanten Fächer mit aggregiertem Fortschritt. <paramref name="active"/> unterscheidet
    /// aktuell zugewiesene von nur noch historischen Inhalten.
    /// </summary>
    public Task<IReadOnlyList<SubjectProgressResponse>> ListSubjectProgressAsync(int childId,
        string? search = null, bool? active = null, string? sort = null, string? dir = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SubjectProgressResponse>>($"{Root}/children/{childId}/learn/subjects"
            + PuglingHttp.Query(("search", search), ("active", active), ("sort", sort), ("dir", dir),
                ("skip", skip), ("take", take)), ct);

    /// <summary>Die Kapitel eines Fachs mit aggregiertem Fortschritt.</summary>
    public Task<IReadOnlyList<ChapterProgressResponse>> ListChapterProgressAsync(int childId, int subjectId,
        string? search = null, bool? active = null, string? sort = null, string? dir = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChapterProgressResponse>>($"{Root}/children/{childId}/learn/subjects/{subjectId}/chapters"
            + PuglingHttp.Query(("search", search), ("active", active), ("sort", sort), ("dir", dir),
                ("skip", skip), ("take", take)), ct);

    /// <summary>Die Vokabelübungen eines Kapitels mit dem Fortschritt des Kindes je Übung.</summary>
    public Task<IReadOnlyList<ExerciseProgressResponse>> ListExerciseProgressAsync(int childId, int subjectId,
        int chapterId, string? search = null, bool? active = null, string? sort = null, string? dir = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ExerciseProgressResponse>>(
            $"{Root}/children/{childId}/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary"
            + PuglingHttp.Query(("search", search), ("active", active), ("sort", sort), ("dir", dir),
                ("skip", skip), ("take", take)), ct);

    /// <summary>
    /// „Anderes Bild": lehnt die eingefrorene Bildwahl eines Trägers ab und zieht neu. Ohne Alternative
    /// bleibt der Bestand unverändert (<c>409 media_no_alternative</c>) statt die Karte bildlos zu machen.
    /// </summary>
    public Task<SelectedMediaResponse> ReshuffleMediaAsync(int childId, ReshuffleMediaDto dto,
        CancellationToken ct = default) =>
        Http.PostAsync<SelectedMediaResponse>($"{Root}/children/{childId}/media-picks/reshuffle", dto, ct);
}
