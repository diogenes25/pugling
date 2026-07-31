namespace Pugling.Client;

/// <summary>
/// Typed access to the Supervisor tier (<c>api/v1/supervisor/…</c>): children, study plans and their
/// positions, learn goals/objectives, family shop, missions, and class tests – plus the read views
/// that supervision decisions rely on. The account behind the client needs the <b>Supervisor</b> role.
/// </summary>
public sealed class SupervisorApi(HttpClient http)
{
    private const string Root = "api/v1/supervisor";

    /// <summary>The underlying HttpClient – an escape hatch for endpoints that don't (yet) have a wrapper.</summary>
    public HttpClient Http { get; } = http;

    // ---------------------------------------------------------------- Kinder

    /// <summary>All supervised children.</summary>
    public Task<IReadOnlyList<ChildResponse>> ListChildrenAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChildResponse>>($"{Root}/children", ct);

    /// <summary>A child including account balance (coins/gems).</summary>
    public Task<ChildResponse> GetChildAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<ChildResponse>($"{Root}/children/{childId}", ct);

    /// <summary>Creates a child; the creating supervisor is linked automatically.</summary>
    public Task<ChildResponse> CreateChildAsync(CreateChildDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ChildResponse>($"{Root}/children", dto, ct);

    /// <summary>Updates a child (profile, PIN, interests – input for tailoring the study plan).</summary>
    public Task<ChildResponse> UpdateChildAsync(int childId, UpdateChildDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<ChildResponse>($"{Root}/children/{childId}", dto, ct);

    /// <summary>
    /// The child's <b>weighted</b> interests – referenced against the shared taxonomy and thus
    /// machine-evaluable, unlike the free-form <c>ChildResponse.Interests</c> (which remains the language
    /// of the AI creator). Negative weights are aversions.
    /// </summary>
    public Task<IReadOnlyList<ChildInterestResponse>> ListInterestsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChildInterestResponse>>($"{Root}/children/{childId}/interests", ct);

    /// <summary>Replaces the interests entirely; unknown tags are created.</summary>
    public Task<IReadOnlyList<ChildInterestResponse>> SetInterestsAsync(int childId,
        SetChildInterestsDto dto, CancellationToken ct = default) =>
        Http.PutAsync<IReadOnlyList<ChildInterestResponse>>($"{Root}/children/{childId}/interests", dto, ct);

    /// <summary>Sets/updates the weight of a single tag (upsert).</summary>
    public Task<ChildInterestResponse> SetInterestWeightAsync(int childId, int tagId,
        SetChildInterestWeightDto dto, CancellationToken ct = default) =>
        Http.PutAsync<ChildInterestResponse>($"{Root}/children/{childId}/interests/{tagId}", dto, ct);

    /// <summary>Removes an interest (the tag remains in the catalog).</summary>
    public Task RemoveInterestAsync(int childId, int tagId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/children/{childId}/interests/{tagId}", null, ct);

    /// <summary>The supervisors of a child (multiple supervisors per child are supported).</summary>
    public Task<IReadOnlyList<SupervisorLinkResponse>> ListSupervisorsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SupervisorLinkResponse>>($"{Root}/children/{childId}/supervisors", ct);

    /// <summary>Links another supervisor to the child.</summary>
    public Task<SupervisorLinkResponse> AddSupervisorAsync(int childId, AddSupervisorDto dto, CancellationToken ct = default) =>
        Http.PostAsync<SupervisorLinkResponse>($"{Root}/children/{childId}/supervisors", dto, ct);

    /// <summary>Points account and ledger history of a child.</summary>
    public Task<ChildPointsResponse> GetPointsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<ChildPointsResponse>($"{Root}/children/{childId}/points", ct);

    /// <summary>
    /// Books coins or gems by hand – "gifting" outside the app and at the same time the
    /// pressure valve against penalty debt.
    /// </summary>
    public Task<PointsEntryResponse> GrantPointsAsync(int childId, PointsEntryDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PointsEntryResponse>($"{Root}/children/{childId}/points", dto, ct);

    /// <summary>
    /// The textbooks of a child – together with grade level and school type the fixed learning
    /// material that a generated study plan or generated exercise must align with.
    /// </summary>
    public Task<IReadOnlyList<TextbookResponse>> ListTextbooksAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<TextbookResponse>>($"{Root}/children/{childId}/textbooks", ct);

    /// <summary>Creates a textbook.</summary>
    public Task<TextbookResponse> CreateTextbookAsync(int childId, CreateTextbookDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TextbookResponse>($"{Root}/children/{childId}/textbooks", dto, ct);

    /// <summary>
    /// Updates a textbook (partial) – the way to add the cataloged series and the current unit to
    /// the child afterward. Only this lets profile matching find the creator who knows this book.
    /// </summary>
    public Task<TextbookResponse> UpdateTextbookAsync(int childId, int textbookId, UpdateTextbookDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<TextbookResponse>($"{Root}/children/{childId}/textbooks/{textbookId}", dto, ct);

    /// <summary>Daily dashboard across all supervised children: who has completed their mandatory goal today?</summary>
    public Task<Dashboard> GetDailyOverviewAsync(DateOnly? date = null, CancellationToken ct = default) =>
        Http.GetAsync<Dashboard>($"{Root}/children/daily-overview" + PuglingHttp.Query(("date", date)), ct);

    // ---------------------------------------------------------------- Lehrpläne & Positionen

    /// <summary>Study plans, optionally filtered by a child.</summary>
    public Task<IReadOnlyList<PlanResponse>> ListPlansAsync(int? childId = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<PlanResponse>>($"{Root}/study-plans" + PuglingHttp.Query(("childId", childId)), ct);

    /// <summary>A study plan.</summary>
    public Task<PlanResponse> GetPlanAsync(int planId, CancellationToken ct = default) =>
        Http.GetAsync<PlanResponse>($"{Root}/study-plans/{planId}", ct);

    /// <summary>
    /// Creates a study plan (a pure container). Per child, exactly <b>one</b> active plan is playable
    /// within its runtime – the server enforces this.
    /// </summary>
    public Task<PlanResponse> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PlanResponse>($"{Root}/study-plans", dto, ct);

    /// <summary>Updates a study plan (e.g. toggling active).</summary>
    public Task<PlanResponse> UpdatePlanAsync(int planId, UpdatePlanDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<PlanResponse>($"{Root}/study-plans/{planId}", dto, ct);

    /// <summary>Deletes a study plan.</summary>
    public Task DeletePlanAsync(int planId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/study-plans/{planId}", null, ct);

    /// <summary>The positions of a study plan.</summary>
    public Task<IReadOnlyList<PositionResponse>> ListPositionsAsync(int planId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<PositionResponse>>($"{Root}/study-plans/{planId}/positions", ct);

    /// <summary>
    /// Appends a catalog exercise as a position: own mandatory goal (rhythm + threshold), points, and
    /// an optional coin penalty. Non-executable exercises are rejected with <c>403 exercise_not_executable</c>.
    /// </summary>
    public Task<PositionResponse> AddPositionAsync(int planId, CreatePositionDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PositionResponse>($"{Root}/study-plans/{planId}/positions", dto, ct);

    /// <summary>Updates a position (goal, points, Leitner settings).</summary>
    public Task<PositionResponse> UpdatePositionAsync(int planId, int positionId, UpdatePositionDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<PositionResponse>($"{Root}/study-plans/{planId}/positions/{positionId}", dto, ct);

    /// <summary>Removes a position.</summary>
    public Task DeletePositionAsync(int planId, int positionId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/study-plans/{planId}/positions/{positionId}", null, ct);

    // ---------------------------------------------------------------- Objectives (OKR)

    /// <summary>Objectives (a dated wrapper around several milestones) of a child.</summary>
    public Task<IReadOnlyList<ObjectiveResponse>> ListObjectivesAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ObjectiveResponse>>($"{Root}/children/{childId}/objectives", ct);

    /// <summary>Creates an objective – optionally along with its key results.</summary>
    public Task<ObjectiveResponse> CreateObjectiveAsync(int childId, CreateObjectiveRequest request,
        CancellationToken ct = default) =>
        Http.PostAsync<ObjectiveResponse>($"{Root}/children/{childId}/objectives", request, ct);

    /// <summary>Updates an objective.</summary>
    public Task<ObjectiveResponse> UpdateObjectiveAsync(int childId, int objectiveId, UpdateObjectiveRequest request,
        CancellationToken ct = default) =>
        Http.PatchAsync<ObjectiveResponse>($"{Root}/children/{childId}/objectives/{objectiveId}", request, ct);

    /// <summary>Appends a key result to an existing objective.</summary>
    public Task<KeyResultResponse> AddKeyResultAsync(int childId, int objectiveId, CreateKeyResultRequest request,
        CancellationToken ct = default) =>
        Http.PostAsync<KeyResultResponse>($"{Root}/children/{childId}/objectives/{objectiveId}/key-results", request, ct);

    // ---------------------------------------------------------------- Familien-Shop

    /// <summary>The article catalog of the supervisor.</summary>
    public Task<IReadOnlyList<ShopArticleDto>> ListShopArticlesAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ShopArticleDto>>($"{Root}/shop/articles", ct);

    /// <summary>Creates a shop article (what can exist at all).</summary>
    public Task<ShopArticleDto> CreateShopArticleAsync(CreateShopArticleDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ShopArticleDto>($"{Root}/shop/articles", dto, ct);

    /// <summary>The listings for an article.</summary>
    public Task<IReadOnlyList<ShopListingDto>> ListShopListingsAsync(int articleId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ShopListingDto>>($"{Root}/shop/articles/{articleId}/listings", ct);

    /// <summary>Creates a listing (price in coins/gems, stock, restock rule).</summary>
    public Task<ShopListingDto> CreateShopListingAsync(int articleId, CreateShopListingDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ShopListingDto>($"{Root}/shop/articles/{articleId}/listings", dto, ct);

    /// <summary>Updates a listing.</summary>
    public Task<ShopListingDto> UpdateShopListingAsync(int articleId, int listingId, UpdateShopListingDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<ShopListingDto>($"{Root}/shop/articles/{articleId}/listings/{listingId}", dto, ct);

    /// <summary>The inventory of a child (purchased, not-yet-redeemed articles).</summary>
    public Task<IReadOnlyList<InventoryItemDto>> ListInventoryAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<InventoryItemDto>>($"{Root}/children/{childId}/shop/inventory", ct);

    /// <summary>Open and completed activation requests of a child.</summary>
    public Task<IReadOnlyList<ActivationRequestDto>> ListActivationsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ActivationRequestDto>>($"{Root}/children/{childId}/shop/activations", ct);

    /// <summary>Approves an activation (issuing supervisor only).</summary>
    public Task<ActivationRequestDto> ApproveActivationAsync(int childId, int requestId, CancellationToken ct = default) =>
        Http.PostAsync<ActivationRequestDto>($"{Root}/children/{childId}/shop/activations/{requestId}/approve", null, ct);

    /// <summary>Rejects an activation.</summary>
    public Task<ActivationRequestDto> RejectActivationAsync(int childId, int requestId, CancellationToken ct = default) =>
        Http.PostAsync<ActivationRequestDto>($"{Root}/children/{childId}/shop/activations/{requestId}/reject", null, ct);

    // ---------------------------------------------------------------- Missionen & Auszeichnungen

    /// <summary>Missions of a child.</summary>
    public Task<IReadOnlyList<MissionDto>> ListMissionsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MissionDto>>($"{Root}/children/{childId}/missions", ct);

    /// <summary>Creates a mission.</summary>
    public Task<MissionDto> CreateMissionAsync(int childId, CreateMissionDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MissionDto>($"{Root}/children/{childId}/missions", dto, ct);

    /// <summary>Awards of a child.</summary>
    public Task<IReadOnlyList<AchievementDto>> ListAchievementsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<AchievementDto>>($"{Root}/children/{childId}/achievements", ct);

    /// <summary>Creates an award.</summary>
    public Task<AchievementDto> CreateAchievementAsync(int childId, CreateAchievementDto dto, CancellationToken ct = default) =>
        Http.PostAsync<AchievementDto>($"{Root}/children/{childId}/achievements", dto, ct);

    // ---------------------------------------------------------------- Klassenarbeiten

    /// <summary>Class tests, optionally filtered by a child.</summary>
    public Task<IReadOnlyList<KlassenarbeitResponse>> ListClassTestsAsync(int? childId = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<KlassenarbeitResponse>>($"{Root}/class-tests" + PuglingHttp.Query(("childId", childId)), ct);

    /// <summary>
    /// Schedules a class test. The response is the <b>detail</b> (test + assigned exercises) – the
    /// endpoint returns it this way so a caller doesn't need to reload after creating.
    /// </summary>
    public Task<KlassenarbeitDetail> CreateClassTestAsync(CreateClassTestDto dto, CancellationToken ct = default) =>
        Http.PostAsync<KlassenarbeitDetail>($"{Root}/class-tests", dto, ct);

    /// <summary>Class test with exercises and tags.</summary>
    public Task<KlassenarbeitDetail> GetClassTestAsync(int classTestId, CancellationToken ct = default) =>
        Http.GetAsync<KlassenarbeitDetail>($"{Root}/class-tests/{classTestId}", ct);

    /// <summary>Updates a class test (among other things, entering the grade afterward – the ungameable reality anchor).</summary>
    public Task<KlassenarbeitResponse> UpdateClassTestAsync(int classTestId, UpdateClassTestDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<KlassenarbeitResponse>($"{Root}/class-tests/{classTestId}", dto, ct);

    /// <summary>Assigns exercises to a class test (the execute gate applies as with positions).</summary>
    public Task<KlassenarbeitDetail> AssignClassTestExercisesAsync(int classTestId, AssignExercisesDto dto,
        CancellationToken ct = default) =>
        Http.PostAsync<KlassenarbeitDetail>($"{Root}/class-tests/{classTestId}/exercises", dto, ct);
}
