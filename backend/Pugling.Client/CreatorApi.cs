namespace Pugling.Client;

/// <summary>
/// Typed access to the Creator tier (<c>api/v1/creator/…</c>): catalog, vocabulary store,
/// typed exercises with items, preview and permissions. The methods are deliberately thin – each maps
/// one endpoint and throws a <see cref="PuglingApiException"/> with the stable
/// <c>code</c> on failure. The account behind the client needs the <b>Creator</b> role.
/// </summary>
public sealed class CreatorApi(HttpClient http)
{
    private const string Root = "api/v1/creator";

    /// <summary>The underlying HttpClient – an escape hatch for endpoints that don't (yet) have a wrapper.</summary>
    public HttpClient Http { get; } = http;

    // ---------------------------------------------------------------- Type manifest

    /// <summary>
    /// The exercise-type manifest: which types exist, under which <c>authoringRoute</c> segment they
    /// are created, and what capabilities they have. An agent should read this <b>before</b> creating
    /// an exercise instead of guessing segments.
    /// </summary>
    public Task<IReadOnlyList<ExerciseTypeManifest>> GetExerciseTypesAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ExerciseTypeManifest>>($"{Root}/exercise-types", ct);

    // ---------------------------------------------------------------- Subjects

    /// <summary>All subjects.</summary>
    public Task<IReadOnlyList<SubjectResponse>> ListSubjectsAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SubjectResponse>>($"{Root}/subjects", ct);

    /// <summary>A subject.</summary>
    public Task<SubjectResponse> GetSubjectAsync(int subjectId, CancellationToken ct = default) =>
        Http.GetAsync<SubjectResponse>($"{Root}/subjects/{subjectId}", ct);

    /// <summary>Creates a subject.</summary>
    public Task<SubjectResponse> CreateSubjectAsync(CreateSubjectDto dto, CancellationToken ct = default) =>
        Http.PostAsync<SubjectResponse>($"{Root}/subjects", dto, ct);

    /// <summary>Updates a subject.</summary>
    public Task<SubjectResponse> UpdateSubjectAsync(int subjectId, UpdateSubjectDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<SubjectResponse>($"{Root}/subjects/{subjectId}", dto, ct);

    /// <summary>Deletes a subject (cascades chapters and exercises).</summary>
    public Task DeleteSubjectAsync(int subjectId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/subjects/{subjectId}", null, ct);

    // ---------------------------------------------------------------- Chapters & categories

    /// <summary>Chapters of a subject.</summary>
    public Task<IReadOnlyList<ChapterResponse>> ListChaptersAsync(int subjectId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChapterResponse>>($"{Root}/subjects/{subjectId}/chapters", ct);

    /// <summary>Creates a chapter.</summary>
    public Task<ChapterResponse> CreateChapterAsync(int subjectId, CreateChapterDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ChapterResponse>($"{Root}/subjects/{subjectId}/chapters", dto, ct);

    /// <summary>Updates a chapter.</summary>
    public Task<ChapterResponse> UpdateChapterAsync(int subjectId, int chapterId, UpdateChapterDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<ChapterResponse>($"{Root}/subjects/{subjectId}/chapters/{chapterId}", dto, ct);

    /// <summary>Deletes a chapter (cascades exercises).</summary>
    public Task DeleteChapterAsync(int subjectId, int chapterId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/subjects/{subjectId}/chapters/{chapterId}", null, ct);

    /// <summary>Subject-dependent categories (controlled vocabulary for catalog pre-filtering).</summary>
    public Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(int subjectId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<CategoryResponse>>($"{Root}/subjects/{subjectId}/categories", ct);

    /// <summary>Creates a category.</summary>
    public Task<CategoryResponse> CreateCategoryAsync(int subjectId, CreateCategoryDto dto, CancellationToken ct = default) =>
        Http.PostAsync<CategoryResponse>($"{Root}/subjects/{subjectId}/categories", dto, ct);

    // ---------------------------------------------------------------- Textbook series & units

    /// <summary>The textbook series of the shared catalog (all filters optional).</summary>
    public Task<IReadOnlyList<TextbookSeriesResponse>> ListSeriesAsync(string? search = null, int? subjectId = null,
        bool? mineOnly = null, int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<TextbookSeriesResponse>>($"{Root}/textbook-series" + PuglingHttp.Query(
            ("search", search), ("subjectId", subjectId), ("mineOnly", mineOnly), ("skip", skip), ("take", take)), ct);

    /// <summary>A series by id.</summary>
    public Task<TextbookSeriesResponse> GetSeriesAsync(int seriesId, CancellationToken ct = default) =>
        Http.GetAsync<TextbookSeriesResponse>($"{Root}/textbook-series/{seriesId}", ct);

    /// <summary>Creates a series; a name already taken returns the existing series (idempotent).</summary>
    public Task<TextbookSeriesResponse> CreateSeriesAsync(CreateTextbookSeriesDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TextbookSeriesResponse>($"{Root}/textbook-series", dto, ct);

    /// <summary>Updates a series (owner only).</summary>
    public Task<TextbookSeriesResponse> UpdateSeriesAsync(int seriesId, UpdateTextbookSeriesDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<TextbookSeriesResponse>($"{Root}/textbook-series/{seriesId}", dto, ct);

    /// <summary>Deletes a series including its units (owner only).</summary>
    public Task DeleteSeriesAsync(int seriesId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/textbook-series/{seriesId}", null, ct);

    /// <summary>
    /// The units of a series (by volume and order). An agent should read them <b>before</b> inventing
    /// material: <c>Topics</c>/<c>Grammar</c>/<c>VocabularyNotes</c> are the content of the unit.
    /// </summary>
    public Task<IReadOnlyList<SeriesUnitResponse>> ListUnitsAsync(int seriesId, int? grade = null,
        CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SeriesUnitResponse>>(
            $"{Root}/textbook-series/{seriesId}/units" + PuglingHttp.Query(("grade", grade)), ct);

    /// <summary>A unit by id.</summary>
    public Task<SeriesUnitResponse> GetUnitAsync(int seriesId, int unitId, CancellationToken ct = default) =>
        Http.GetAsync<SeriesUnitResponse>($"{Root}/textbook-series/{seriesId}/units/{unitId}", ct);

    /// <summary>Appends a unit to the series (owner only).</summary>
    public Task<SeriesUnitResponse> CreateUnitAsync(int seriesId, CreateSeriesUnitDto dto, CancellationToken ct = default) =>
        Http.PostAsync<SeriesUnitResponse>($"{Root}/textbook-series/{seriesId}/units", dto, ct);

    /// <summary>Updates a unit (series owner only).</summary>
    public Task<SeriesUnitResponse> UpdateUnitAsync(int seriesId, int unitId, UpdateSeriesUnitDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<SeriesUnitResponse>($"{Root}/textbook-series/{seriesId}/units/{unitId}", dto, ct);

    /// <summary>Deletes a unit (series owner only).</summary>
    public Task DeleteUnitAsync(int seriesId, int unitId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/textbook-series/{seriesId}/units/{unitId}", null, ct);

    // ---------------------------------------------------------------- Creator profiles

    /// <summary>The creator profiles ("subject teacher"), optionally filtered.</summary>
    public Task<IReadOnlyList<CreatorProfileResponse>> ListProfilesAsync(int? subjectId = null, int? seriesId = null,
        bool? mineOnly = null, bool? includeInactive = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<CreatorProfileResponse>>($"{Root}/profiles" + PuglingHttp.Query(
            ("subjectId", subjectId), ("seriesId", seriesId), ("mineOnly", mineOnly),
            ("includeInactive", includeInactive)), ct);

    /// <summary>A profile by id.</summary>
    public Task<CreatorProfileResponse> GetProfileAsync(int profileId, CancellationToken ct = default) =>
        Http.GetAsync<CreatorProfileResponse>($"{Root}/profiles/{profileId}", ct);

    /// <summary>
    /// The profiles matching a child, best first. Requires an account that <b>supervises</b> the
    /// child – otherwise <c>403 forbidden</c>.
    /// </summary>
    public Task<IReadOnlyList<CreatorProfileMatch>> MatchProfilesAsync(int childId, int? subjectId = null,
        CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<CreatorProfileMatch>>($"{Root}/profiles/match" + PuglingHttp.Query(
            ("childId", childId), ("subjectId", subjectId)), ct);

    /// <summary>Creates a profile (owner = the logged-in account).</summary>
    public Task<CreatorProfileResponse> CreateProfileAsync(CreateCreatorProfileDto dto, CancellationToken ct = default) =>
        Http.PostAsync<CreatorProfileResponse>($"{Root}/profiles", dto, ct);

    /// <summary>Updates a profile (owner only).</summary>
    public Task<CreatorProfileResponse> UpdateProfileAsync(int profileId, UpdateCreatorProfileDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<CreatorProfileResponse>($"{Root}/profiles/{profileId}", dto, ct);

    /// <summary>Deletes a profile (owner only); exercises it generated remain untouched.</summary>
    public Task DeleteProfileAsync(int profileId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/profiles/{profileId}", null, ct);

    // ---------------------------------------------------------------- Child-scoped tags

    /// <summary>The tags of a child (child-scoped – unlike the child-neutral interest taxonomy).</summary>
    public Task<IReadOnlyList<TagResponse>> ListTagsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<TagResponse>>($"{Root}/tags" + PuglingHttp.Query(("childId", childId)), ct);

    /// <summary>Creates a child-scoped tag.</summary>
    public Task<TagResponse> CreateTagAsync(CreateTagDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TagResponse>($"{Root}/tags", dto, ct);

    /// <summary>Tags exercises with a tag – this is how a class-test bundle is held together.</summary>
    public Task<TagResponse> TagExercisesAsync(int tagId, TagExercisesDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TagResponse>($"{Root}/tags/{tagId}/exercises", dto, ct);

    // ---------------------------------------------------------------- Vocabulary store

    /// <summary>Searches the vocabulary store (all filters optional, AND-combined).</summary>
    public Task<IReadOnlyList<VocabularyResponse>> SearchVocabularyAsync(string? search = null, string? word = null,
        string? translation = null, PartOfSpeech? partOfSpeech = null, string? sourceLanguage = null,
        string? targetLanguage = null, IEnumerable<string>? tags = null, bool? baseFormsOnly = null,
        int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<VocabularyResponse>>($"{Root}/vocabulary" + PuglingHttp.Query(
            ("search", search), ("word", word), ("translation", translation), ("partOfSpeech", partOfSpeech),
            ("sourceLanguage", sourceLanguage), ("targetLanguage", targetLanguage), ("tag", tags),
            ("baseFormsOnly", baseFormsOnly), ("skip", skip), ("take", take)), ct);

    /// <summary>A vocabulary-store entry.</summary>
    public Task<VocabularyResponse> GetVocabularyAsync(int vocabularyId, CancellationToken ct = default) =>
        Http.GetAsync<VocabularyResponse>($"{Root}/vocabulary/{vocabularyId}", ct);

    /// <summary>
    /// Checks in a single call which words already exist in the store. Call before creating –
    /// this avoids duplicates.
    /// </summary>
    public Task<LookupResponse> LookupVocabularyAsync(LookupRequest request, CancellationToken ct = default) =>
        Http.PostAsync<LookupResponse>($"{Root}/vocabulary/lookup", request, ct);

    /// <summary>Creates a vocabulary entry.</summary>
    public Task<VocabularyResponse> CreateVocabularyAsync(CreateVocabularyDto dto, CancellationToken ct = default) =>
        Http.PostAsync<VocabularyResponse>($"{Root}/vocabulary", dto, ct);

    /// <summary>Creates multiple vocabulary entries in a single call; the result reports success or failure per row.</summary>
    public Task<IReadOnlyList<BatchItemResult>> CreateVocabularyBatchAsync(IEnumerable<CreateVocabularyDto> items,
        CancellationToken ct = default) =>
        Http.PostAsync<IReadOnlyList<BatchItemResult>>($"{Root}/vocabulary/batch", items.ToList(), ct);

    /// <summary>Updates a vocabulary entry.</summary>
    public Task<VocabularyResponse> UpdateVocabularyAsync(int vocabularyId, UpdateVocabularyDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<VocabularyResponse>($"{Root}/vocabulary/{vocabularyId}", dto, ct);

    // ---------------------------------------------------------------- Create exercises (typed)

    /// <summary>
    /// Creates a typed exercise. <paramref name="authoringRoute"/> is the segment from the
    /// manifest (e.g. <c>vocabulary</c>, <c>cloze</c>, <c>essays</c>), <typeparamref name="TConfig"/>
    /// the corresponding config class from the contract.
    /// </summary>
    public Task<ExerciseResponse<TConfig>> CreateExerciseAsync<TConfig>(int subjectId, int chapterId,
        string authoringRoute, ExercisePayload<TConfig> payload, CancellationToken ct = default) =>
        Http.PostAsync<ExerciseResponse<TConfig>>(ExercisePath(subjectId, chapterId, authoringRoute), payload, ct);

    /// <summary>Reads a typed exercise including its config.</summary>
    public Task<ExerciseResponse<TConfig>> GetExerciseAsync<TConfig>(int subjectId, int chapterId,
        string authoringRoute, int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<ExerciseResponse<TConfig>>($"{ExercisePath(subjectId, chapterId, authoringRoute)}/{exerciseId}", ct);

    /// <summary>Replaces a typed exercise (PUT – the config is fully overwritten).</summary>
    public Task<ExerciseResponse<TConfig>> UpdateExerciseAsync<TConfig>(int subjectId, int chapterId,
        string authoringRoute, int exerciseId, ExercisePayload<TConfig> payload, CancellationToken ct = default) =>
        Http.PutAsync<ExerciseResponse<TConfig>>($"{ExercisePath(subjectId, chapterId, authoringRoute)}/{exerciseId}", payload, ct);

    /// <summary>Deletes an exercise (owner only).</summary>
    public Task DeleteExerciseAsync(int subjectId, int chapterId, string authoringRoute, int exerciseId,
        CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{ExercisePath(subjectId, chapterId, authoringRoute)}/{exerciseId}", null, ct);

    // ---------------------------------------------------------------- Vocabulary items

    /// <summary>The materialized vocabulary pairs of a vocabulary exercise.</summary>
    public Task<IReadOnlyList<VocabItemResponse>> ListItemsAsync(int subjectId, int chapterId, int exerciseId,
        CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<VocabItemResponse>>($"{ItemsPath(subjectId, chapterId, exerciseId)}", ct);

    /// <summary>
    /// Appends an item. Either via <c>VocabularyId</c> (existing store vocabulary entry) or inline via
    /// Front/Back – the latter requires the exercise config to carry <c>sourceLang</c>/<c>targetLang</c>.
    /// </summary>
    public Task<VocabItemResponse> AddItemAsync(int subjectId, int chapterId, int exerciseId, VocabItemInput input,
        CancellationToken ct = default) =>
        Http.PostAsync<VocabItemResponse>($"{ItemsPath(subjectId, chapterId, exerciseId)}", input, ct);

    /// <summary>Updates an item (omitted fields remain unchanged).</summary>
    public Task<VocabItemResponse> UpdateItemAsync(int subjectId, int chapterId, int exerciseId, int itemId,
        VocabItemInput input, CancellationToken ct = default) =>
        Http.PatchAsync<VocabItemResponse>($"{ItemsPath(subjectId, chapterId, exerciseId)}/{itemId}", input, ct);

    /// <summary>Removes an item. Avoid in already-assigned exercises – this shifts learning progress.</summary>
    public Task DeleteItemAsync(int subjectId, int chapterId, int exerciseId, int itemId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{ItemsPath(subjectId, chapterId, exerciseId)}/{itemId}", null, ct);

    // ---------------------------------------------------------------- Birkenbihl

    /// <summary>
    /// Appends a sentence to a Birkenbihl exercise and has it decoded word for word server-side
    /// against the vocabulary store. Sentences <b>cannot</b> be supplied inline when creating – the exercise
    /// is created empty and then filled via this endpoint.
    /// </summary>
    public Task<DecodedSentence> AddBirkenbihlSentenceAsync(int subjectId, int chapterId, int exerciseId,
        BirkenbihlSentenceInput input, CancellationToken ct = default) =>
        Http.PostAsync<DecodedSentence>(
            $"{ExercisePath(subjectId, chapterId, "birkenbihl")}/{exerciseId}/sentences", input, ct);

    /// <summary>Swaps the meaning of a single decoded word (homonym correction).</summary>
    public Task<DecodedWord> OverrideBirkenbihlWordAsync(int subjectId, int chapterId, int exerciseId, int wordId,
        WordOverride input, CancellationToken ct = default) =>
        Http.PutAsync<DecodedWord>(
            $"{ExercisePath(subjectId, chapterId, "birkenbihl")}/{exerciseId}/words/{wordId}", input, ct);

    // ---------------------------------------------------------------- Catalog, preview, rights

    /// <summary>Child-neutral catalog search over the metadata (all filters optional, AND-combined).</summary>
    public Task<IReadOnlyList<ExerciseSummary>> SearchExercisesAsync(int? subjectId = null, int? chapterId = null,
        int? grade = null, SchoolTypes? schoolType = null, int? categoryId = null, string? type = null,
        string? search = null, bool? mineOnly = null, int skip = 0, int take = 50, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ExerciseSummary>>($"{Root}/exercises" + PuglingHttp.Query(
            ("subjectId", subjectId), ("chapterId", chapterId), ("grade", grade), ("schoolType", schoolType),
            ("categoryId", categoryId), ("type", type), ("search", search), ("mineOnly", mineOnly),
            ("skip", skip), ("take", take)), ct);

    /// <summary>Exercise detail including raw config and own permissions (<c>isOwn</c>/<c>isOwner</c>).</summary>
    public Task<ExerciseDetail> GetExerciseDetailAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<ExerciseDetail>($"{Root}/exercises/{exerciseId}", ct);

    /// <summary>Where an exercise is used (study plans, class tests) – check before changing/deleting.</summary>
    public Task<UsageResponse> GetExerciseUsageAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<UsageResponse>($"{Root}/exercises/{exerciseId}/usage", ct);

    /// <summary>
    /// Publishes or withdraws an exercise (owner only). <c>false</c> stops <b>new</b> assignments
    /// by others; running study plans remain unaffected. For a creator agent, this is the way to take
    /// own material out of circulation – deleting refuses an exercise that is in use.
    /// </summary>
    public Task<ExerciseSharingResponse> SetExerciseSharingAsync(int exerciseId, bool executePublic, CancellationToken ct = default) =>
        Http.PatchAsync<ExerciseSharingResponse>($"{Root}/exercises/{exerciseId}/sharing",
            new SetExerciseSharingDto(executePublic), ct);

    /// <summary>
    /// Test mode: the tasks without solutions, side-effect free. Types without checkable individual
    /// tasks (e.g. <c>Essay</c>) respond with <c>400 no_checkable_content</c>.
    /// </summary>
    public Task<PreviewData> PreviewExerciseAsync(int exerciseId, int? stage = null, CancellationToken ct = default) =>
        Http.GetAsync<PreviewData>($"{Root}/exercises/{exerciseId}/preview" + PuglingHttp.Query(("stage", stage)), ct);

    /// <summary>Checks trial answers against the preview – likewise without progress or points.</summary>
    public Task<PreviewResult> CheckPreviewAsync(int exerciseId, PreviewCheckDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PreviewResult>($"{Root}/exercises/{exerciseId}/preview/check", dto, ct);

    /// <summary>The permissions granted for an exercise (owner only).</summary>
    public Task<IReadOnlyList<GrantResponse>> ListGrantsAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<GrantResponse>>($"{Root}/exercises/{exerciseId}/grants", ct);

    /// <summary>Grants another creator a permission (Owner ⊃ Write ⊃ Execute).</summary>
    public Task<GrantResponse> AddGrantAsync(int exerciseId, AddGrantDto dto, CancellationToken ct = default) =>
        Http.PostAsync<GrantResponse>($"{Root}/exercises/{exerciseId}/grants", dto, ct);

    /// <summary>Revokes a permission. The last owner cannot be removed (<c>409 last_owner</c>).</summary>
    public Task RemoveGrantAsync(int exerciseId, int creatorId, GrantPermission permission, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/exercises/{exerciseId}/grants/{creatorId}/{permission}", null, ct);

    // ---------------------------------------------------------------- Interest taxonomy

    /// <summary>
    /// The shared interest/style taxonomy. An agent should <b>read it before tagging</b>: it is
    /// the same vocabulary that child profiles draw from – only matches within it make an image discoverable.
    /// </summary>
    public Task<IReadOnlyList<InterestTagResponse>> ListInterestTagsAsync(string? search = null,
        InterestFacet? facet = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<InterestTagResponse>>(
            $"{Root}/interest-tags" + PuglingHttp.Query(("search", search), ("facet", facet)), ct);

    /// <summary>Creates a tag; a slug already taken returns the existing entry (idempotent).</summary>
    public Task<InterestTagResponse> CreateInterestTagAsync(CreateInterestTagDto dto, CancellationToken ct = default) =>
        Http.PostAsync<InterestTagResponse>($"{Root}/interest-tags", dto, ct);

    /// <summary>Updates label, facet, synonyms, or color. The slug remains immutable.</summary>
    public Task<InterestTagResponse> UpdateInterestTagAsync(int tagId, UpdateInterestTagDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<InterestTagResponse>($"{Root}/interest-tags/{tagId}", dto, ct);

    /// <summary>Deletes a tag along with its links to images and children.</summary>
    public Task DeleteInterestTagAsync(int tagId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/interest-tags/{tagId}", null, ct);

    // ---------------------------------------------------------------- Media store

    /// <summary>
    /// Assets of the media store. <paramref name="maxRating"/> applies the same suitability cutoff that
    /// the later automatic per-child selection enforces hard – so an agent sees what a child would get.
    /// </summary>
    public Task<IReadOnlyList<MediaAssetResponse>> ListMediaAsync(string? search = null, string[]? tags = null,
        ContentRating? maxRating = null, MediaKind? kind = null, CancellationToken ct = default) =>
        // Query() expands list values into repeated parameters (?tag=a&tag=b).
        Http.GetAsync<IReadOnlyList<MediaAssetResponse>>($"{Root}/media" + PuglingHttp.Query(
            ("search", search), ("tag", tags), ("maxRating", maxRating), ("kind", kind)), ct);

    /// <summary>An asset by id.</summary>
    public Task<MediaAssetResponse> GetMediaAsync(int assetId, CancellationToken ct = default) =>
        Http.GetAsync<MediaAssetResponse>($"{Root}/media/{assetId}", ct);

    /// <summary>An asset by stable key.</summary>
    public Task<MediaAssetResponse> GetMediaByKeyAsync(string key, CancellationToken ct = default) =>
        Http.GetAsync<MediaAssetResponse>($"{Root}/media/by-key/{key}", ct);

    /// <summary>Creates an asset – tags and variants may be supplied right away.</summary>
    public Task<MediaAssetResponse> CreateMediaAsync(CreateMediaAssetDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaAssetResponse>($"{Root}/media", dto, ct);

    /// <summary>
    /// Uploads an image file; the server generates the variants (Thumb/Card/Full) and the
    /// placeholder color itself. For an AI agent, the convenient path: it supplies the generated bytes and
    /// doesn't need to worry about scaling or a reachable URL.
    /// </summary>
    /// <param name="content">The image file (PNG/JPEG/WebP/…).</param>
    /// <param name="fileName">File name – only relevant for logging/debugging.</param>
    /// <param name="description">What is depicted; doubles as alt text. Required.</param>
    /// <param name="tags">Tags from the shared taxonomy.</param>
    /// <param name="rating">Suitability; the strictest level if not specified.</param>
    /// <param name="origin">Origin; for generated images <see cref="MediaOrigin.Generated"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<MediaAssetResponse> UploadMediaAsync(ReadOnlyMemory<byte> content, string fileName,
        string description, IEnumerable<string>? tags = null, ContentRating? rating = null,
        MediaOrigin? origin = null, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var file = new ReadOnlyMemoryContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(description), "description");
        // Multipart has no lists - the server expects the keywords as one comma-separated line.
        if (tags is not null) form.Add(new StringContent(string.Join(",", tags)), "tags");
        if (rating is { } r) form.Add(new StringContent(r.ToString()), "rating");
        if (origin is { } o) form.Add(new StringContent(o.ToString()), "origin");

        return await Http.PostContentAsync<MediaAssetResponse>($"{Root}/media/upload", form, ct);
    }

    /// <summary>Updates an asset (partial); tags are added, not replaced.</summary>
    public Task<MediaAssetResponse> UpdateMediaAsync(int assetId, UpdateMediaAssetDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<MediaAssetResponse>($"{Root}/media/{assetId}", dto, ct);

    /// <summary>Deletes an asset along with its variants and tag links.</summary>
    public Task DeleteMediaAsync(int assetId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/media/{assetId}", null, ct);

    /// <summary>Links an asset with tags (create-if-missing).</summary>
    public Task<MediaAssetResponse> TagMediaAsync(int assetId, TagMediaDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaAssetResponse>($"{Root}/media/{assetId}/tags", dto, ct);

    /// <summary>Unlinks a tag from the asset (the tag itself remains in the catalog).</summary>
    public Task UntagMediaAsync(int assetId, int tagId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/media/{assetId}/tags/{tagId}", null, ct);

    /// <summary>The variants of an asset.</summary>
    public Task<IReadOnlyList<MediaVariantResponse>> ListMediaVariantsAsync(int assetId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaVariantResponse>>($"{Root}/media/{assetId}/variants", ct);

    /// <summary>Adds a variant after the fact; (purpose, format) must be free on the asset.</summary>
    public Task<MediaVariantResponse> AddMediaVariantAsync(int assetId, CreateMediaVariantDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaVariantResponse>($"{Root}/media/{assetId}/variants", dto, ct);

    /// <summary>Updates a variant (partial).</summary>
    public Task<MediaVariantResponse> UpdateMediaVariantAsync(int assetId, int variantId,
        UpdateMediaVariantDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<MediaVariantResponse>($"{Root}/media/{assetId}/variants/{variantId}", dto, ct);

    /// <summary>Deletes a variant. The asset remains.</summary>
    public Task DeleteMediaVariantAsync(int assetId, int variantId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/media/{assetId}/variants/{variantId}", null, ct);

    /// <summary>Where an asset is assigned – read before deleting it (deleting is not blocked).</summary>
    public Task<IReadOnlyList<MediaUsage>> GetMediaUsageAsync(int assetId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaUsage>>($"{Root}/media/{assetId}/usage", ct);

    // ---------------------------------------------------------------- Image ⇢ carrier assignment

    /// <summary>
    /// The images of a vocabulary-store entry – the default assignment, it applies in every exercise using this word.
    /// <b>Multiple is the normal case</b>: only the selection makes per-child individualization possible.
    /// </summary>
    public Task<IReadOnlyList<MediaLinkResponse>> ListVocabularyMediaAsync(int vocabularyId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaLinkResponse>>($"{Root}/vocabulary/{vocabularyId}/media", ct);

    /// <summary>Links an image to the vocabulary entry (by id or key).</summary>
    public Task<MediaLinkResponse> LinkVocabularyMediaAsync(int vocabularyId, AddMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaLinkResponse>($"{Root}/vocabulary/{vocabularyId}/media", dto, ct);

    /// <summary>Updates the editorial rank of a vocabulary link.</summary>
    public Task<MediaLinkResponse> UpdateVocabularyMediaAsync(int vocabularyId, int linkId,
        UpdateMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<MediaLinkResponse>($"{Root}/vocabulary/{vocabularyId}/media/{linkId}", dto, ct);

    /// <summary>Unlinks a vocabulary link; the image remains in the store.</summary>
    public Task UnlinkVocabularyMediaAsync(int vocabularyId, int linkId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/vocabulary/{vocabularyId}/media/{linkId}", null, ct);

    /// <summary>The cover images of an exercise (text/reading/sentence exercise without word reference).</summary>
    public Task<IReadOnlyList<MediaLinkResponse>> ListExerciseMediaAsync(int exerciseId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaLinkResponse>>($"{Root}/exercises/{exerciseId}/media", ct);

    /// <summary>Links a cover image to the exercise (write permission required).</summary>
    public Task<MediaLinkResponse> LinkExerciseMediaAsync(int exerciseId, AddMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaLinkResponse>($"{Root}/exercises/{exerciseId}/media", dto, ct);

    /// <summary>Unlinks a cover image from the exercise (write permission required).</summary>
    public Task UnlinkExerciseMediaAsync(int exerciseId, int linkId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/exercises/{exerciseId}/media/{linkId}", null, ct);

    /// <summary>The exercise-local override of an item (later overrides the store assignment).</summary>
    public Task<IReadOnlyList<MediaLinkResponse>> ListItemMediaAsync(int exerciseId, int itemId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MediaLinkResponse>>($"{Root}/exercises/{exerciseId}/items/{itemId}/media", ct);

    /// <summary>Sets a different image for this item without altering the store (write permission required).</summary>
    public Task<MediaLinkResponse> LinkItemMediaAsync(int exerciseId, int itemId, AddMediaLinkDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MediaLinkResponse>($"{Root}/exercises/{exerciseId}/items/{itemId}/media", dto, ct);

    /// <summary>Removes the override – afterward the image from the store applies again (write permission required).</summary>
    public Task UnlinkItemMediaAsync(int exerciseId, int itemId, int linkId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/exercises/{exerciseId}/items/{itemId}/media/{linkId}", null, ct);

    private static string ExercisePath(int subjectId, int chapterId, string authoringRoute) =>
        $"{Root}/subjects/{subjectId}/chapters/{chapterId}/{authoringRoute}";

    private static string ItemsPath(int subjectId, int chapterId, int exerciseId) =>
        $"{ExercisePath(subjectId, chapterId, "vocabulary")}/{exerciseId}/items";
}
