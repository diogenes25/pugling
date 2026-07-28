namespace Pugling.Client;

/// <summary>
/// Typisierter Zugriff auf die Creator-Ebene (<c>api/v1/creator/…</c>): Katalog, Vokabelspeicher,
/// typisierte Übungen samt Items, Vorschau und Rechte. Die Methoden sind bewusst dünn – sie bilden je
/// einen Endpunkt ab und werfen bei Fehlern eine <see cref="PuglingApiException"/> mit dem stabilen
/// <c>code</c>. Das Konto hinter dem Client braucht die <b>Creator</b>-Rolle.
/// </summary>
public sealed class CreatorApi(HttpClient http)
{
    private const string Root = "api/v1/creator";

    /// <summary>Der zugrunde liegende HttpClient – Ausweg für Endpunkte, die (noch) keinen Wrapper haben.</summary>
    public HttpClient Http { get; } = http;

    // ---------------------------------------------------------------- Typ-Manifest

    /// <summary>
    /// Das Übungstyp-Manifest: welche Typen es gibt, unter welchem <c>authoringRoute</c>-Segment sie
    /// angelegt werden und welche Fähigkeiten sie haben. Ein Agent sollte das <b>vor</b> dem Anlegen
    /// einer Übung lesen, statt Segmente zu raten.
    /// </summary>
    public Task<IReadOnlyList<ExerciseTypeManifest>> GetExerciseTypesAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ExerciseTypeManifest>>($"{Root}/exercise-types", ct);

    // ---------------------------------------------------------------- Fächer

    /// <summary>Alle Fächer.</summary>
    public Task<IReadOnlyList<SubjectResponse>> ListSubjectsAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SubjectResponse>>($"{Root}/subjects", ct);

    /// <summary>Ein Fach.</summary>
    public Task<SubjectResponse> GetSubjectAsync(int subjectId, CancellationToken ct = default) =>
        Http.GetAsync<SubjectResponse>($"{Root}/subjects/{subjectId}", ct);

    /// <summary>Legt ein Fach an.</summary>
    public Task<SubjectResponse> CreateSubjectAsync(CreateSubjectDto dto, CancellationToken ct = default) =>
        Http.PostAsync<SubjectResponse>($"{Root}/subjects", dto, ct);

    /// <summary>Ändert ein Fach.</summary>
    public Task<SubjectResponse> UpdateSubjectAsync(int subjectId, UpdateSubjectDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<SubjectResponse>($"{Root}/subjects/{subjectId}", dto, ct);

    /// <summary>Löscht ein Fach (kaskadiert Kapitel und Übungen).</summary>
    public Task DeleteSubjectAsync(int subjectId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/subjects/{subjectId}", null, ct);

    // ---------------------------------------------------------------- Kapitel & Arten

    /// <summary>Kapitel eines Fachs.</summary>
    public Task<IReadOnlyList<ChapterResponse>> ListChaptersAsync(int subjectId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChapterResponse>>($"{Root}/subjects/{subjectId}/chapters", ct);

    /// <summary>Legt ein Kapitel an.</summary>
    public Task<ChapterResponse> CreateChapterAsync(int subjectId, CreateChapterDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ChapterResponse>($"{Root}/subjects/{subjectId}/chapters", dto, ct);

    /// <summary>Ändert ein Kapitel.</summary>
    public Task<ChapterResponse> UpdateChapterAsync(int subjectId, int chapterId, UpdateChapterDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<ChapterResponse>($"{Root}/subjects/{subjectId}/chapters/{chapterId}", dto, ct);

    /// <summary>Löscht ein Kapitel (kaskadiert Übungen).</summary>
    public Task DeleteChapterAsync(int subjectId, int chapterId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/subjects/{subjectId}/chapters/{chapterId}", null, ct);

    /// <summary>Fachabhängige Arten (kontrolliertes Vokabular für die Katalog-Vorfilterung).</summary>
    public Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(int subjectId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<CategoryResponse>>($"{Root}/subjects/{subjectId}/categories", ct);

    /// <summary>Legt eine Art an.</summary>
    public Task<CategoryResponse> CreateCategoryAsync(int subjectId, CreateCategoryDto dto, CancellationToken ct = default) =>
        Http.PostAsync<CategoryResponse>($"{Root}/subjects/{subjectId}/categories", dto, ct);

    // ---------------------------------------------------------------- Lehrwerk-Reihen & Units

    /// <summary>Die Lehrwerk-Reihen des geteilten Katalogs (alle Filter optional).</summary>
    public Task<IReadOnlyList<TextbookSeriesResponse>> ListSeriesAsync(string? search = null, int? subjectId = null,
        bool? mineOnly = null, int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<TextbookSeriesResponse>>($"{Root}/textbook-series" + PuglingHttp.Query(
            ("search", search), ("subjectId", subjectId), ("mineOnly", mineOnly), ("skip", skip), ("take", take)), ct);

    /// <summary>Eine Reihe per Id.</summary>
    public Task<TextbookSeriesResponse> GetSeriesAsync(int seriesId, CancellationToken ct = default) =>
        Http.GetAsync<TextbookSeriesResponse>($"{Root}/textbook-series/{seriesId}", ct);

    /// <summary>Legt eine Reihe an; ein bereits vergebener Name liefert die bestehende Reihe (idempotent).</summary>
    public Task<TextbookSeriesResponse> CreateSeriesAsync(CreateTextbookSeriesDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TextbookSeriesResponse>($"{Root}/textbook-series", dto, ct);

    /// <summary>Ändert eine Reihe (nur Owner).</summary>
    public Task<TextbookSeriesResponse> UpdateSeriesAsync(int seriesId, UpdateTextbookSeriesDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<TextbookSeriesResponse>($"{Root}/textbook-series/{seriesId}", dto, ct);

    /// <summary>Löscht eine Reihe samt Units (nur Owner).</summary>
    public Task DeleteSeriesAsync(int seriesId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/textbook-series/{seriesId}", null, ct);

    /// <summary>
    /// Die Units einer Reihe (nach Band und Reihenfolge). Ein Agent sollte sie lesen, <b>bevor</b> er
    /// Stoff erfindet: <c>Topics</c>/<c>Grammar</c>/<c>VocabularyNotes</c> sind der Inhalt der Unit.
    /// </summary>
    public Task<IReadOnlyList<SeriesUnitResponse>> ListUnitsAsync(int seriesId, int? grade = null,
        CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SeriesUnitResponse>>(
            $"{Root}/textbook-series/{seriesId}/units" + PuglingHttp.Query(("grade", grade)), ct);

    /// <summary>Eine Unit per Id.</summary>
    public Task<SeriesUnitResponse> GetUnitAsync(int seriesId, int unitId, CancellationToken ct = default) =>
        Http.GetAsync<SeriesUnitResponse>($"{Root}/textbook-series/{seriesId}/units/{unitId}", ct);

    /// <summary>Hängt eine Unit an die Reihe (nur Owner).</summary>
    public Task<SeriesUnitResponse> CreateUnitAsync(int seriesId, CreateSeriesUnitDto dto, CancellationToken ct = default) =>
        Http.PostAsync<SeriesUnitResponse>($"{Root}/textbook-series/{seriesId}/units", dto, ct);

    /// <summary>Ändert eine Unit (nur Owner der Reihe).</summary>
    public Task<SeriesUnitResponse> UpdateUnitAsync(int seriesId, int unitId, UpdateSeriesUnitDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<SeriesUnitResponse>($"{Root}/textbook-series/{seriesId}/units/{unitId}", dto, ct);

    /// <summary>Löscht eine Unit (nur Owner der Reihe).</summary>
    public Task DeleteUnitAsync(int seriesId, int unitId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/textbook-series/{seriesId}/units/{unitId}", null, ct);

    // ---------------------------------------------------------------- Creator-Profile

    /// <summary>Die Creator-Profile („Fachlehrer"), optional gefiltert.</summary>
    public Task<IReadOnlyList<CreatorProfileResponse>> ListProfilesAsync(int? subjectId = null, int? seriesId = null,
        bool? mineOnly = null, bool? includeInactive = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<CreatorProfileResponse>>($"{Root}/profiles" + PuglingHttp.Query(
            ("subjectId", subjectId), ("seriesId", seriesId), ("mineOnly", mineOnly),
            ("includeInactive", includeInactive)), ct);

    /// <summary>Ein Profil per Id.</summary>
    public Task<CreatorProfileResponse> GetProfileAsync(int profileId, CancellationToken ct = default) =>
        Http.GetAsync<CreatorProfileResponse>($"{Root}/profiles/{profileId}", ct);

    /// <summary>
    /// Die zu einem Kind passenden Profile, bestes zuerst. Braucht ein Konto, das das Kind <b>betreut</b> –
    /// sonst <c>403 forbidden</c>.
    /// </summary>
    public Task<IReadOnlyList<CreatorProfileMatch>> MatchProfilesAsync(int childId, int? subjectId = null,
        CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<CreatorProfileMatch>>($"{Root}/profiles/match" + PuglingHttp.Query(
            ("childId", childId), ("subjectId", subjectId)), ct);

    /// <summary>Legt ein Profil an (Owner = das angemeldete Konto).</summary>
    public Task<CreatorProfileResponse> CreateProfileAsync(CreateCreatorProfileDto dto, CancellationToken ct = default) =>
        Http.PostAsync<CreatorProfileResponse>($"{Root}/profiles", dto, ct);

    /// <summary>Ändert ein Profil (nur Owner).</summary>
    public Task<CreatorProfileResponse> UpdateProfileAsync(int profileId, UpdateCreatorProfileDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<CreatorProfileResponse>($"{Root}/profiles/{profileId}", dto, ct);

    /// <summary>Löscht ein Profil (nur Owner); erzeugte Übungen bleiben unberührt.</summary>
    public Task DeleteProfileAsync(int profileId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/profiles/{profileId}", null, ct);

    // ---------------------------------------------------------------- Kind-skopierte Tags

    /// <summary>Die Tags eines Kindes (kind-skopiert – anders als die kindneutrale Interessen-Taxonomie).</summary>
    public Task<IReadOnlyList<TagResponse>> ListTagsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<TagResponse>>($"{Root}/tags" + PuglingHttp.Query(("childId", childId)), ct);

    /// <summary>Legt einen kind-skopierten Tag an.</summary>
    public Task<TagResponse> CreateTagAsync(CreateTagDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TagResponse>($"{Root}/tags", dto, ct);

    /// <summary>Markiert Übungen mit einem Tag – so hält ein Klausur-Bündel zusammen.</summary>
    public Task<TagResponse> TagExercisesAsync(int tagId, TagExercisesDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TagResponse>($"{Root}/tags/{tagId}/exercises", dto, ct);

    // ---------------------------------------------------------------- Vokabelspeicher

    /// <summary>Durchsucht den Vokabelspeicher (alle Filter optional, UND-verknüpft).</summary>
    public Task<IReadOnlyList<VocabularyResponse>> SearchVocabularyAsync(string? search = null, string? word = null,
        string? translation = null, PartOfSpeech? partOfSpeech = null, string? sourceLanguage = null,
        string? targetLanguage = null, IEnumerable<string>? tags = null, bool? baseFormsOnly = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<VocabularyResponse>>($"{Root}/vocabulary" + PuglingHttp.Query(
            ("search", search), ("word", word), ("translation", translation), ("partOfSpeech", partOfSpeech),
            ("sourceLanguage", sourceLanguage), ("targetLanguage", targetLanguage), ("tag", tags),
            ("baseFormsOnly", baseFormsOnly), ("skip", skip), ("take", take)), ct);

    /// <summary>Eine Store-Vokabel.</summary>
    public Task<VocabularyResponse> GetVocabularyAsync(int vocabularyId, CancellationToken ct = default) =>
        Http.GetAsync<VocabularyResponse>($"{Root}/vocabulary/{vocabularyId}", ct);

    /// <summary>
    /// Prüft in einem Aufruf, welche Wörter schon im Speicher liegen. Vor dem Anlegen aufrufen –
    /// so entstehen keine Dubletten.
    /// </summary>
    public Task<LookupResponse> LookupVocabularyAsync(LookupRequest request, CancellationToken ct = default) =>
        Http.PostAsync<LookupResponse>($"{Root}/vocabulary/lookup", request, ct);

    /// <summary>Legt eine Vokabel an.</summary>
    public Task<VocabularyResponse> CreateVocabularyAsync(CreateVocabularyDto dto, CancellationToken ct = default) =>
        Http.PostAsync<VocabularyResponse>($"{Root}/vocabulary", dto, ct);

    /// <summary>Legt mehrere Vokabeln in einem Aufruf an; das Ergebnis meldet je Zeile Erfolg oder Fehler.</summary>
    public Task<IReadOnlyList<BatchItemResult>> CreateVocabularyBatchAsync(IEnumerable<CreateVocabularyDto> items,
        CancellationToken ct = default) =>
        Http.PostAsync<IReadOnlyList<BatchItemResult>>($"{Root}/vocabulary/batch", items.ToList(), ct);

    /// <summary>Ändert eine Vokabel.</summary>
    public Task<VocabularyResponse> UpdateVocabularyAsync(int vocabularyId, UpdateVocabularyDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<VocabularyResponse>($"{Root}/vocabulary/{vocabularyId}", dto, ct);

    // ---------------------------------------------------------------- Übungen anlegen (typisiert)

    /// <summary>
    /// Legt eine typisierte Übung an. <paramref name="authoringRoute"/> ist das Segment aus dem
    /// Manifest (z. B. <c>vocabulary</c>, <c>cloze</c>, <c>essays</c>), <typeparamref name="TConfig"/>
    /// die dazugehörige Config-Klasse aus dem Vertrag.
    /// </summary>
    public Task<ExerciseResponse<TConfig>> CreateExerciseAsync<TConfig>(int subjectId, int chapterId,
        string authoringRoute, ExercisePayload<TConfig> payload, CancellationToken ct = default) =>
        Http.PostAsync<ExerciseResponse<TConfig>>(ExercisePath(subjectId, chapterId, authoringRoute), payload, ct);

    /// <summary>Liest eine typisierte Übung samt Config.</summary>
    public Task<ExerciseResponse<TConfig>> GetExerciseAsync<TConfig>(int subjectId, int chapterId,
        string authoringRoute, int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<ExerciseResponse<TConfig>>($"{ExercisePath(subjectId, chapterId, authoringRoute)}/{exerciseId}", ct);

    /// <summary>Ersetzt eine typisierte Übung (PUT – die Config wird vollständig überschrieben).</summary>
    public Task<ExerciseResponse<TConfig>> UpdateExerciseAsync<TConfig>(int subjectId, int chapterId,
        string authoringRoute, int exerciseId, ExercisePayload<TConfig> payload, CancellationToken ct = default) =>
        Http.PutAsync<ExerciseResponse<TConfig>>($"{ExercisePath(subjectId, chapterId, authoringRoute)}/{exerciseId}", payload, ct);

    /// <summary>Löscht eine Übung (nur Owner).</summary>
    public Task DeleteExerciseAsync(int subjectId, int chapterId, string authoringRoute, int exerciseId,
        CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{ExercisePath(subjectId, chapterId, authoringRoute)}/{exerciseId}", null, ct);

    // ---------------------------------------------------------------- Vokabel-Items

    /// <summary>Die materialisierten Vokabelpaare einer Vokabelübung.</summary>
    public Task<IReadOnlyList<VocabItemResponse>> ListItemsAsync(int subjectId, int chapterId, int exerciseId,
        CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<VocabItemResponse>>($"{ItemsPath(subjectId, chapterId, exerciseId)}", ct);

    /// <summary>
    /// Hängt ein Item an. Entweder per <c>VocabularyId</c> (bestehende Store-Vokabel) oder inline per
    /// Front/Back – letzteres setzt voraus, dass die Übungs-Config <c>sourceLang</c>/<c>targetLang</c> trägt.
    /// </summary>
    public Task<VocabItemResponse> AddItemAsync(int subjectId, int chapterId, int exerciseId, VocabItemInput input,
        CancellationToken ct = default) =>
        Http.PostAsync<VocabItemResponse>($"{ItemsPath(subjectId, chapterId, exerciseId)}", input, ct);

    /// <summary>Ändert ein Item (weggelassene Felder bleiben unverändert).</summary>
    public Task<VocabItemResponse> UpdateItemAsync(int subjectId, int chapterId, int exerciseId, int itemId,
        VocabItemInput input, CancellationToken ct = default) =>
        Http.PatchAsync<VocabItemResponse>($"{ItemsPath(subjectId, chapterId, exerciseId)}/{itemId}", input, ct);

    /// <summary>Entfernt ein Item. In bereits zugewiesenen Übungen vermeiden – das verschiebt Lernfortschritt.</summary>
    public Task DeleteItemAsync(int subjectId, int chapterId, int exerciseId, int itemId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{ItemsPath(subjectId, chapterId, exerciseId)}/{itemId}", null, ct);

    // ---------------------------------------------------------------- Birkenbihl

    /// <summary>
    /// Hängt einen Satz an eine Birkenbihl-Übung und lässt ihn serverseitig gegen den Vokabelspeicher
    /// Wort für Wort dekodieren. Sätze lassen sich <b>nicht</b> beim Anlegen inline mitgeben – die Übung
    /// wird leer erzeugt und dann über diesen Endpunkt gefüllt.
    /// </summary>
    public Task<DecodedSentence> AddBirkenbihlSentenceAsync(int subjectId, int chapterId, int exerciseId,
        BirkenbihlSentenceInput input, CancellationToken ct = default) =>
        Http.PostAsync<DecodedSentence>(
            $"{ExercisePath(subjectId, chapterId, "birkenbihl")}/{exerciseId}/sentences", input, ct);

    /// <summary>Tauscht die Bedeutung eines einzelnen dekodierten Worts (Homonym-Korrektur).</summary>
    public Task<DecodedWord> OverrideBirkenbihlWordAsync(int subjectId, int chapterId, int exerciseId, int wordId,
        WordOverride input, CancellationToken ct = default) =>
        Http.PutAsync<DecodedWord>(
            $"{ExercisePath(subjectId, chapterId, "birkenbihl")}/{exerciseId}/words/{wordId}", input, ct);

    // ---------------------------------------------------------------- Katalog, Vorschau, Rechte

    /// <summary>Kindneutrale Katalogsuche über die Metadaten (alle Filter optional, UND-verknüpft).</summary>
    public Task<IReadOnlyList<ExerciseSummary>> SearchExercisesAsync(int? subjectId = null, int? chapterId = null,
        int? grade = null, SchoolTypes? schoolType = null, int? categoryId = null, string? type = null,
        string? search = null, bool? mineOnly = null, int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ExerciseSummary>>($"{Root}/exercises" + PuglingHttp.Query(
            ("subjectId", subjectId), ("chapterId", chapterId), ("grade", grade), ("schoolType", schoolType),
            ("categoryId", categoryId), ("type", type), ("search", search), ("mineOnly", mineOnly),
            ("skip", skip), ("take", take)), ct);

    /// <summary>Übungsdetail inklusive roher Config und eigener Rechte (<c>isOwn</c>/<c>isOwner</c>).</summary>
    public Task<ExerciseDetail> GetExerciseDetailAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<ExerciseDetail>($"{Root}/exercises/{exerciseId}", ct);

    /// <summary>Wo eine Übung verwendet wird (Lehrpläne, Klassenarbeiten) – vor dem Ändern/Löschen prüfen.</summary>
    public Task<UsageResponse> GetExerciseUsageAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<UsageResponse>($"{Root}/exercises/{exerciseId}/usage", ct);

    /// <summary>
    /// Gibt eine Übung frei oder zieht sie zurück (nur Owner). <c>false</c> stoppt <b>neue</b> Zuweisungen
    /// durch Fremde; laufende Lehrpläne bleiben unberührt. Für einen Creator-Agenten der Weg, eigenes
    /// Material aus dem Verkehr zu nehmen – Löschen verweigert eine benutzte Übung.
    /// </summary>
    public Task<ExerciseSharingResponse> SetExerciseSharingAsync(int exerciseId, bool executePublic, CancellationToken ct = default) =>
        Http.PatchAsync<ExerciseSharingResponse>($"{Root}/exercises/{exerciseId}/sharing",
            new SetExerciseSharingDto(executePublic), ct);

    /// <summary>
    /// Testmodus: die Aufgaben ohne Lösungen, nebenwirkungsfrei. Typen ohne prüfbare Einzelaufgaben
    /// (z. B. <c>Essay</c>) antworten mit <c>400 no_checkable_content</c>.
    /// </summary>
    public Task<PreviewData> PreviewExerciseAsync(int exerciseId, int? stage = null, CancellationToken ct = default) =>
        Http.GetAsync<PreviewData>($"{Root}/exercises/{exerciseId}/preview" + PuglingHttp.Query(("stage", stage)), ct);

    /// <summary>Prüft Probeantworten gegen die Vorschau – ebenfalls ohne Fortschritt oder Punkte.</summary>
    public Task<PreviewResult> CheckPreviewAsync(int exerciseId, PreviewCheckDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PreviewResult>($"{Root}/exercises/{exerciseId}/preview/check", dto, ct);

    /// <summary>Die vergebenen Rechte einer Übung (nur Owner).</summary>
    public Task<IReadOnlyList<GrantResponse>> ListGrantsAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<GrantResponse>>($"{Root}/exercises/{exerciseId}/grants", ct);

    /// <summary>Erteilt einem anderen Creator ein Recht (Owner ⊃ Write ⊃ Execute).</summary>
    public Task<GrantResponse> AddGrantAsync(int exerciseId, AddGrantDto dto, CancellationToken ct = default) =>
        Http.PostAsync<GrantResponse>($"{Root}/exercises/{exerciseId}/grants", dto, ct);

    /// <summary>Entzieht ein Recht. Der letzte Owner lässt sich nicht entfernen (<c>409 last_owner</c>).</summary>
    public Task RemoveGrantAsync(int exerciseId, int creatorId, GrantPermission permission, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/exercises/{exerciseId}/grants/{creatorId}/{permission}", null, ct);

    // ---------------------------------------------------------------- Interessen-Taxonomie

    /// <summary>
    /// Die geteilte Interessen-/Stil-Taxonomie. Ein Agent sollte sie <b>lesen, bevor er taggt</b>: sie ist
    /// dasselbe Vokabular, aus dem die Kind-Profile schöpfen – nur Treffer darin machen ein Bild auffindbar.
    /// </summary>
    public Task<IReadOnlyList<InterestTagResponse>> ListInterestTagsAsync(string? search = null,
        InterestFacet? facet = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<InterestTagResponse>>(
            $"{Root}/interest-tags" + PuglingHttp.Query(("search", search), ("facet", facet)), ct);

    /// <summary>Legt ein Schlagwort an; ein bereits vergebener Slug liefert den bestehenden Eintrag (idempotent).</summary>
    public Task<InterestTagResponse> CreateInterestTagAsync(CreateInterestTagDto dto, CancellationToken ct = default) =>
        Http.PostAsync<InterestTagResponse>($"{Root}/interest-tags", dto, ct);

    /// <summary>Ändert Label, Facette, Synonyme oder Farbe. Der Slug bleibt unveränderlich.</summary>
    public Task<InterestTagResponse> UpdateInterestTagAsync(int tagId, UpdateInterestTagDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<InterestTagResponse>($"{Root}/interest-tags/{tagId}", dto, ct);

    /// <summary>Löscht ein Schlagwort samt seiner Verknüpfungen zu Bildern und Kindern.</summary>
    public Task DeleteInterestTagAsync(int tagId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/interest-tags/{tagId}", null, ct);

    // ---------------------------------------------------------------- Medien-Store

    /// <summary>
    /// Assets des Medien-Stores. <paramref name="maxRating"/> zieht denselben Eignungs-Schnitt, den die
    /// spätere automatische Auswahl je Kind hart anwendet – damit sieht ein Agent, was ein Kind bekäme.
    /// </summary>
    public Task<IReadOnlyList<MediaAssetResponse>> ListMediaAsync(string? search = null, string[]? tags = null,
        ContentRating? maxRating = null, MediaKind? kind = null, CancellationToken ct = default) =>
        // Query() vervielfältigt Listen-Werte zu wiederholten Parametern (?tag=a&tag=b).
        Http.GetAsync<IReadOnlyList<MediaAssetResponse>>($"{Root}/media" + PuglingHttp.Query(
            ("search", search), ("tag", tags), ("maxRating", maxRating), ("kind", kind)), ct);

    /// <summary>Ein Asset per Id.</summary>
    public Task<MediaAssetResponse> GetMediaAsync(int assetId, CancellationToken ct = default) =>
        Http.GetAsync<MediaAssetResponse>($"{Root}/media/{assetId}", ct);

    /// <summary>Ein Asset per stabilem Key.</summary>
    public Task<MediaAssetResponse> GetMediaByKeyAsync(string key, CancellationToken ct = default) =>
        Http.GetAsync<MediaAssetResponse>($"{Root}/media/by-key/{key}", ct);

    /// <summary>Legt eine Darstellung an – Tags und Auflösungen dürfen gleich mitkommen.</summary>
    public Task<MediaAssetResponse> CreateMediaAsync(CreateMediaAssetDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaAssetResponse>($"{Root}/media", dto, ct);

    /// <summary>
    /// Lädt eine Bilddatei hoch; der Server erzeugt die Auflösungen (Thumb/Card/Full) und die
    /// Platzhalterfarbe selbst. Für einen KI-Agenten der bequeme Weg: er liefert die generierten Bytes und
    /// muss sich weder um Skalierung noch um eine erreichbare URL kümmern.
    /// </summary>
    /// <param name="content">Die Bilddatei (PNG/JPEG/WebP/…).</param>
    /// <param name="fileName">Dateiname – nur fürs Logging/Debugging relevant.</param>
    /// <param name="description">Was zu sehen ist; zugleich Alt-Text. Pflicht.</param>
    /// <param name="tags">Schlagworte der geteilten Taxonomie.</param>
    /// <param name="rating">Eignung; ohne Angabe die strengste Stufe.</param>
    /// <param name="origin">Herkunft; für generierte Bilder <see cref="MediaOrigin.Generated"/>.</param>
    /// <param name="ct">Abbruch-Token.</param>
    public async Task<MediaAssetResponse> UploadMediaAsync(ReadOnlyMemory<byte> content, string fileName,
        string description, IEnumerable<string>? tags = null, ContentRating? rating = null,
        MediaOrigin? origin = null, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var file = new ReadOnlyMemoryContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(description), "description");
        // Multipart kennt keine Listen – der Server erwartet die Schlagworte als kommagetrennte Zeile.
        if (tags is not null) form.Add(new StringContent(string.Join(",", tags)), "tags");
        if (rating is { } r) form.Add(new StringContent(r.ToString()), "rating");
        if (origin is { } o) form.Add(new StringContent(o.ToString()), "origin");

        return await Http.PostContentAsync<MediaAssetResponse>($"{Root}/media/upload", form, ct);
    }

    /// <summary>Ändert ein Asset (partiell); Tags werden ergänzt, nicht ersetzt.</summary>
    public Task<MediaAssetResponse> UpdateMediaAsync(int assetId, UpdateMediaAssetDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<MediaAssetResponse>($"{Root}/media/{assetId}", dto, ct);

    /// <summary>Löscht ein Asset samt Auflösungen und Tag-Verknüpfungen.</summary>
    public Task DeleteMediaAsync(int assetId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/media/{assetId}", null, ct);

    /// <summary>Verknüpft ein Asset mit Schlagworten (create-if-missing).</summary>
    public Task<MediaAssetResponse> TagMediaAsync(int assetId, TagMediaDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaAssetResponse>($"{Root}/media/{assetId}/tags", dto, ct);

    /// <summary>Löst ein Schlagwort vom Asset (der Tag selbst bleibt im Katalog).</summary>
    public Task UntagMediaAsync(int assetId, int tagId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/media/{assetId}/tags/{tagId}", null, ct);

    /// <summary>Die Auflösungen eines Assets.</summary>
    public Task<IReadOnlyList<MediaVariantResponse>> ListMediaVariantsAsync(int assetId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaVariantResponse>>($"{Root}/media/{assetId}/variants", ct);

    /// <summary>Reicht eine Auflösung nach; (Zweck, Format) muss am Asset frei sein.</summary>
    public Task<MediaVariantResponse> AddMediaVariantAsync(int assetId, CreateMediaVariantDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaVariantResponse>($"{Root}/media/{assetId}/variants", dto, ct);

    /// <summary>Ändert eine Auflösung (partiell).</summary>
    public Task<MediaVariantResponse> UpdateMediaVariantAsync(int assetId, int variantId,
        UpdateMediaVariantDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<MediaVariantResponse>($"{Root}/media/{assetId}/variants/{variantId}", dto, ct);

    /// <summary>Löscht eine Auflösung. Das Asset bleibt bestehen.</summary>
    public Task DeleteMediaVariantAsync(int assetId, int variantId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/media/{assetId}/variants/{variantId}", null, ct);

    /// <summary>Wo ein Asset zugeordnet ist – lesen, bevor man es löscht (Löschen ist nicht gesperrt).</summary>
    public Task<IReadOnlyList<MediaUsage>> GetMediaUsageAsync(int assetId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaUsage>>($"{Root}/media/{assetId}/usage", ct);

    // ---------------------------------------------------------------- Zuordnung Bild ⇢ Träger

    /// <summary>
    /// Die Bilder einer Store-Vokabel – die Regelzuordnung, sie wirkt in jeder Übung mit diesem Wort.
    /// <b>Mehrere sind der Normalfall</b>: erst die Auswahl macht die Individualisierung je Kind möglich.
    /// </summary>
    public Task<IReadOnlyList<MediaLinkResponse>> ListVocabularyMediaAsync(int vocabularyId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaLinkResponse>>($"{Root}/vocabulary/{vocabularyId}/media", ct);

    /// <summary>Ordnet der Vokabel ein Bild zu (per Id oder Key).</summary>
    public Task<MediaLinkResponse> LinkVocabularyMediaAsync(int vocabularyId, AddMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaLinkResponse>($"{Root}/vocabulary/{vocabularyId}/media", dto, ct);

    /// <summary>Ändert den redaktionellen Rang einer Vokabel-Zuordnung.</summary>
    public Task<MediaLinkResponse> UpdateVocabularyMediaAsync(int vocabularyId, int linkId,
        UpdateMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<MediaLinkResponse>($"{Root}/vocabulary/{vocabularyId}/media/{linkId}", dto, ct);

    /// <summary>Löst eine Vokabel-Zuordnung; das Bild bleibt im Store.</summary>
    public Task UnlinkVocabularyMediaAsync(int vocabularyId, int linkId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/vocabulary/{vocabularyId}/media/{linkId}", null, ct);

    /// <summary>Die Titelbilder einer Übung (Text-/Lese-/Satzübung ohne Wortbezug).</summary>
    public Task<IReadOnlyList<MediaLinkResponse>> ListExerciseMediaAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaLinkResponse>>($"{Root}/exercises/{exerciseId}/media", ct);

    /// <summary>Ordnet der Übung ein Titelbild zu (Schreibrecht nötig).</summary>
    public Task<MediaLinkResponse> LinkExerciseMediaAsync(int exerciseId, AddMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaLinkResponse>($"{Root}/exercises/{exerciseId}/media", dto, ct);

    /// <summary>Löst ein Titelbild von der Übung (Schreibrecht nötig).</summary>
    public Task UnlinkExerciseMediaAsync(int exerciseId, int linkId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/exercises/{exerciseId}/media/{linkId}", null, ct);

    /// <summary>Die übungslokale Übersteuerung eines Items (schlägt später die Store-Zuordnung).</summary>
    public Task<IReadOnlyList<MediaLinkResponse>> ListItemMediaAsync(int exerciseId, int itemId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaLinkResponse>>($"{Root}/exercises/{exerciseId}/items/{itemId}/media", ct);

    /// <summary>Setzt für dieses Item ein abweichendes Bild, ohne den Store zu verbiegen (Schreibrecht nötig).</summary>
    public Task<MediaLinkResponse> LinkItemMediaAsync(int exerciseId, int itemId, AddMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaLinkResponse>($"{Root}/exercises/{exerciseId}/items/{itemId}/media", dto, ct);

    /// <summary>Löst die Übersteuerung – danach greift wieder das Bild aus dem Store (Schreibrecht nötig).</summary>
    public Task UnlinkItemMediaAsync(int exerciseId, int itemId, int linkId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/exercises/{exerciseId}/items/{itemId}/media/{linkId}", null, ct);

    private static string ExercisePath(int subjectId, int chapterId, string authoringRoute) =>
        $"{Root}/subjects/{subjectId}/chapters/{chapterId}/{authoringRoute}";

    private static string ItemsPath(int subjectId, int chapterId, int exerciseId) =>
        $"{ExercisePath(subjectId, chapterId, "vocabulary")}/{exerciseId}/items";
}
