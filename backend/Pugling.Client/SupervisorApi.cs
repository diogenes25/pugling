namespace Pugling.Client;

/// <summary>
/// Typisierter Zugriff auf die Supervisor-Ebene (<c>api/v1/supervisor/…</c>): Kinder, Lehrpläne und ihre
/// Positionen, Lernziele/Objectives, Familien-Shop, Missionen und Klassenarbeiten – plus die Lese-Sichten,
/// auf denen Steuerungsentscheidungen beruhen. Das Konto hinter dem Client braucht die <b>Supervisor</b>-Rolle.
/// </summary>
public sealed class SupervisorApi(HttpClient http)
{
    private const string Root = "api/v1/supervisor";

    /// <summary>Der zugrunde liegende HttpClient – Ausweg für Endpunkte, die (noch) keinen Wrapper haben.</summary>
    public HttpClient Http { get; } = http;

    // ---------------------------------------------------------------- Kinder

    /// <summary>Alle betreuten Kinder.</summary>
    public Task<IReadOnlyList<ChildResponse>> ListChildrenAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChildResponse>>($"{Root}/children", ct);

    /// <summary>Ein Kind samt Kontostand (Münzen/Gems).</summary>
    public Task<ChildResponse> GetChildAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<ChildResponse>($"{Root}/children/{childId}", ct);

    /// <summary>Legt ein Kind an; der anlegende Supervisor wird automatisch verknüpft.</summary>
    public Task<ChildResponse> CreateChildAsync(CreateChildDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ChildResponse>($"{Root}/children", dto, ct);

    /// <summary>Ändert ein Kind (Profil, PIN, Interessen – Futter für den Lehrplan-Zuschnitt).</summary>
    public Task<ChildResponse> UpdateChildAsync(int childId, UpdateChildDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<ChildResponse>($"{Root}/children/{childId}", dto, ct);

    /// <summary>
    /// Die <b>gewichteten</b> Interessen des Kindes – referenziert auf die geteilte Taxonomie und damit
    /// maschinell auswertbar, anders als das freie <c>ChildResponse.Interests</c> (das bleibt die Sprache
    /// des KI-Creators). Negative Gewichte sind Abneigungen.
    /// </summary>
    public Task<IReadOnlyList<ChildInterestResponse>> ListInterestsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ChildInterestResponse>>($"{Root}/children/{childId}/interests", ct);

    /// <summary>Ersetzt die Interessen vollständig; unbekannte Schlagworte werden angelegt.</summary>
    public Task<IReadOnlyList<ChildInterestResponse>> SetInterestsAsync(int childId,
        SetChildInterestsDto dto, CancellationToken ct = default) =>
        Http.PutAsync<IReadOnlyList<ChildInterestResponse>>($"{Root}/children/{childId}/interests", dto, ct);

    /// <summary>Setzt/ändert das Gewicht eines einzelnen Schlagworts (Upsert).</summary>
    public Task<ChildInterestResponse> SetInterestWeightAsync(int childId, int tagId,
        SetChildInterestWeightDto dto, CancellationToken ct = default) =>
        Http.PutAsync<ChildInterestResponse>($"{Root}/children/{childId}/interests/{tagId}", dto, ct);

    /// <summary>Entfernt ein Interesse (das Schlagwort bleibt im Katalog).</summary>
    public Task RemoveInterestAsync(int childId, int tagId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/children/{childId}/interests/{tagId}", null, ct);

    /// <summary>Die Betreuer eines Kindes (mehrere Supervisor je Kind sind vorgesehen).</summary>
    public Task<IReadOnlyList<SupervisorLinkResponse>> ListSupervisorsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<SupervisorLinkResponse>>($"{Root}/children/{childId}/supervisors", ct);

    /// <summary>Verknüpft einen weiteren Supervisor mit dem Kind.</summary>
    public Task<SupervisorLinkResponse> AddSupervisorAsync(int childId, AddSupervisorDto dto, CancellationToken ct = default) =>
        Http.PostAsync<SupervisorLinkResponse>($"{Root}/children/{childId}/supervisors", dto, ct);

    /// <summary>Punktekonto und Buchungshistorie eines Kindes.</summary>
    public Task<ChildPointsResponse> GetPointsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<ChildPointsResponse>($"{Root}/children/{childId}/points", ct);

    /// <summary>
    /// Bucht Münzen oder Gems von Hand – das „Verschenken" außerhalb der App und zugleich das
    /// Druckventil gegen Malus-Schulden.
    /// </summary>
    public Task<PointsEntryResponse> GrantPointsAsync(int childId, PointsEntryDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PointsEntryResponse>($"{Root}/children/{childId}/points", dto, ct);

    /// <summary>
    /// Die Lehrbücher eines Kindes – zusammen mit Klassenstufe und Schulart der feste Lernstoff,
    /// an dem sich ein generierter Lehrplan bzw. eine generierte Übung ausrichten muss.
    /// </summary>
    public Task<IReadOnlyList<TextbookResponse>> ListTextbooksAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<TextbookResponse>>($"{Root}/children/{childId}/textbooks", ct);

    /// <summary>Legt ein Lehrbuch an.</summary>
    public Task<TextbookResponse> CreateTextbookAsync(int childId, CreateTextbookDto dto, CancellationToken ct = default) =>
        Http.PostAsync<TextbookResponse>($"{Root}/children/{childId}/textbooks", dto, ct);

    /// <summary>Tagesdashboard über alle betreuten Kinder: wer hat heute seine Pflicht erledigt?</summary>
    public Task<Dashboard> GetDailyOverviewAsync(DateOnly? date = null, CancellationToken ct = default) =>
        Http.GetAsync<Dashboard>($"{Root}/children/daily-overview" + PuglingHttp.Query(("date", date)), ct);

    // ---------------------------------------------------------------- Lehrpläne & Positionen

    /// <summary>Lehrpläne, optional auf ein Kind gefiltert.</summary>
    public Task<IReadOnlyList<PlanResponse>> ListPlansAsync(int? childId = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<PlanResponse>>($"{Root}/study-plans" + PuglingHttp.Query(("childId", childId)), ct);

    /// <summary>Ein Lehrplan.</summary>
    public Task<PlanResponse> GetPlanAsync(int planId, CancellationToken ct = default) =>
        Http.GetAsync<PlanResponse>($"{Root}/study-plans/{planId}", ct);

    /// <summary>
    /// Legt einen Lehrplan an (reiner Container). Spielbar ist je Kind genau <b>ein</b> aktiver Plan
    /// innerhalb seiner Laufzeit – das erzwingt der Server.
    /// </summary>
    public Task<PlanResponse> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PlanResponse>($"{Root}/study-plans", dto, ct);

    /// <summary>Ändert einen Lehrplan (z. B. aktiv schalten).</summary>
    public Task<PlanResponse> UpdatePlanAsync(int planId, UpdatePlanDto dto, CancellationToken ct = default) =>
        Http.PatchAsync<PlanResponse>($"{Root}/study-plans/{planId}", dto, ct);

    /// <summary>Löscht einen Lehrplan.</summary>
    public Task DeletePlanAsync(int planId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/study-plans/{planId}", null, ct);

    /// <summary>Die Positionen eines Lehrplans.</summary>
    public Task<IReadOnlyList<PositionResponse>> ListPositionsAsync(int planId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<PositionResponse>>($"{Root}/study-plans/{planId}/positions", ct);

    /// <summary>
    /// Hängt eine Katalog-Übung als Position an: eigenes Pflichtziel (Rhythmus + Schwelle), Punkte und
    /// optionalen Münz-Malus. Nicht ausführbare Übungen werden mit <c>403 exercise_not_executable</c> abgelehnt.
    /// </summary>
    public Task<PositionResponse> AddPositionAsync(int planId, CreatePositionDto dto, CancellationToken ct = default) =>
        Http.PostAsync<PositionResponse>($"{Root}/study-plans/{planId}/positions", dto, ct);

    /// <summary>Ändert eine Position (Ziel, Punkte, Leitner-Einstellungen).</summary>
    public Task<PositionResponse> UpdatePositionAsync(int planId, int positionId, UpdatePositionDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<PositionResponse>($"{Root}/study-plans/{planId}/positions/{positionId}", dto, ct);

    /// <summary>Entfernt eine Position.</summary>
    public Task DeletePositionAsync(int planId, int positionId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/study-plans/{planId}/positions/{positionId}", null, ct);

    // ---------------------------------------------------------------- Lernziele & Objectives (OKR)

    /// <summary>Ergebnisziele eines Kindes auf Katalog-Scope (live ausgewertet).</summary>
    public Task<IReadOnlyList<LearnGoalResponse>> ListLearnGoalsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<LearnGoalResponse>>($"{Root}/children/{childId}/learn-goals", ct);

    /// <summary>Legt ein Lernziel an.</summary>
    public Task<LearnGoalResponse> CreateLearnGoalAsync(int childId, CreateLearnGoalRequest request,
        CancellationToken ct = default) =>
        Http.PostAsync<LearnGoalResponse>($"{Root}/children/{childId}/learn-goals", request, ct);

    /// <summary>Ändert ein Lernziel.</summary>
    public Task<LearnGoalResponse> UpdateLearnGoalAsync(int childId, int goalId, UpdateLearnGoalRequest request,
        CancellationToken ct = default) =>
        Http.PatchAsync<LearnGoalResponse>($"{Root}/children/{childId}/learn-goals/{goalId}", request, ct);

    /// <summary>Löscht ein Lernziel.</summary>
    public Task DeleteLearnGoalAsync(int childId, int goalId, CancellationToken ct = default) =>
        Http.SendAsync(HttpMethod.Delete, $"{Root}/children/{childId}/learn-goals/{goalId}", null, ct);

    /// <summary>Objectives (OKR-Klammer über den Lernzielen) eines Kindes.</summary>
    public Task<IReadOnlyList<ObjectiveResponse>> ListObjectivesAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ObjectiveResponse>>($"{Root}/children/{childId}/objectives", ct);

    /// <summary>Legt ein Objective an – optional gleich mit seinen Key Results.</summary>
    public Task<ObjectiveResponse> CreateObjectiveAsync(int childId, CreateObjectiveRequest request,
        CancellationToken ct = default) =>
        Http.PostAsync<ObjectiveResponse>($"{Root}/children/{childId}/objectives", request, ct);

    /// <summary>Ändert ein Objective.</summary>
    public Task<ObjectiveResponse> UpdateObjectiveAsync(int childId, int objectiveId, UpdateObjectiveRequest request,
        CancellationToken ct = default) =>
        Http.PatchAsync<ObjectiveResponse>($"{Root}/children/{childId}/objectives/{objectiveId}", request, ct);

    /// <summary>Hängt ein Key Result an ein bestehendes Objective.</summary>
    public Task<KeyResultResponse> AddKeyResultAsync(int childId, int objectiveId, CreateKeyResultRequest request,
        CancellationToken ct = default) =>
        Http.PostAsync<KeyResultResponse>($"{Root}/children/{childId}/objectives/{objectiveId}/key-results", request, ct);

    // ---------------------------------------------------------------- Familien-Shop

    /// <summary>Der Artikelkatalog des Supervisors.</summary>
    public Task<IReadOnlyList<ShopArticleDto>> ListShopArticlesAsync(CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ShopArticleDto>>($"{Root}/shop/articles", ct);

    /// <summary>Legt einen Shop-Artikel an (was es überhaupt geben kann).</summary>
    public Task<ShopArticleDto> CreateShopArticleAsync(CreateShopArticleDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ShopArticleDto>($"{Root}/shop/articles", dto, ct);

    /// <summary>Die Angebote zu einem Artikel.</summary>
    public Task<IReadOnlyList<ShopListingDto>> ListShopListingsAsync(int articleId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ShopListingDto>>($"{Root}/shop/articles/{articleId}/listings", ct);

    /// <summary>Legt ein Angebot an (Preis in Münzen/Gems, Bestand, Auffüll-Regel).</summary>
    public Task<ShopListingDto> CreateShopListingAsync(int articleId, CreateShopListingDto dto, CancellationToken ct = default) =>
        Http.PostAsync<ShopListingDto>($"{Root}/shop/articles/{articleId}/listings", dto, ct);

    /// <summary>Ändert ein Angebot.</summary>
    public Task<ShopListingDto> UpdateShopListingAsync(int articleId, int listingId, UpdateShopListingDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<ShopListingDto>($"{Root}/shop/articles/{articleId}/listings/{listingId}", dto, ct);

    /// <summary>Das Inventar eines Kindes (gekaufte, noch nicht eingelöste Artikel).</summary>
    public Task<IReadOnlyList<InventoryItemDto>> ListInventoryAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<InventoryItemDto>>($"{Root}/children/{childId}/shop/inventory", ct);

    /// <summary>Offene und erledigte Aktivierungsanfragen eines Kindes.</summary>
    public Task<IReadOnlyList<ActivationRequestDto>> ListActivationsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<ActivationRequestDto>>($"{Root}/children/{childId}/shop/activations", ct);

    /// <summary>Genehmigt eine Aktivierung (nur der ausstellende Supervisor).</summary>
    public Task<ActivationRequestDto> ApproveActivationAsync(int childId, int requestId, CancellationToken ct = default) =>
        Http.PostAsync<ActivationRequestDto>($"{Root}/children/{childId}/shop/activations/{requestId}/approve", null, ct);

    /// <summary>Lehnt eine Aktivierung ab.</summary>
    public Task<ActivationRequestDto> RejectActivationAsync(int childId, int requestId, CancellationToken ct = default) =>
        Http.PostAsync<ActivationRequestDto>($"{Root}/children/{childId}/shop/activations/{requestId}/reject", null, ct);

    // ---------------------------------------------------------------- Missionen & Auszeichnungen

    /// <summary>Missionen eines Kindes.</summary>
    public Task<IReadOnlyList<MissionDto>> ListMissionsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<MissionDto>>($"{Root}/children/{childId}/missions", ct);

    /// <summary>Legt eine Mission an.</summary>
    public Task<MissionDto> CreateMissionAsync(int childId, CreateMissionDto dto, CancellationToken ct = default) =>
        Http.PostAsync<MissionDto>($"{Root}/children/{childId}/missions", dto, ct);

    /// <summary>Auszeichnungen eines Kindes.</summary>
    public Task<IReadOnlyList<AchievementDto>> ListAchievementsAsync(int childId, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<AchievementDto>>($"{Root}/children/{childId}/achievements", ct);

    /// <summary>Legt eine Auszeichnung an.</summary>
    public Task<AchievementDto> CreateAchievementAsync(int childId, CreateAchievementDto dto, CancellationToken ct = default) =>
        Http.PostAsync<AchievementDto>($"{Root}/children/{childId}/achievements", dto, ct);

    // ---------------------------------------------------------------- Klassenarbeiten

    /// <summary>Klassenarbeiten, optional auf ein Kind gefiltert.</summary>
    public Task<IReadOnlyList<KlassenarbeitResponse>> ListClassTestsAsync(int? childId = null, CancellationToken ct = default) =>
        Http.GetAsync<IReadOnlyList<KlassenarbeitResponse>>($"{Root}/class-tests" + PuglingHttp.Query(("childId", childId)), ct);

    /// <summary>Plant eine Klassenarbeit.</summary>
    public Task<KlassenarbeitResponse> CreateClassTestAsync(CreateClassTestDto dto, CancellationToken ct = default) =>
        Http.PostAsync<KlassenarbeitResponse>($"{Root}/class-tests", dto, ct);

    /// <summary>Klassenarbeit mit Übungen und Tags.</summary>
    public Task<KlassenarbeitDetail> GetClassTestAsync(int classTestId, CancellationToken ct = default) =>
        Http.GetAsync<KlassenarbeitDetail>($"{Root}/class-tests/{classTestId}", ct);

    /// <summary>Ändert eine Klassenarbeit (u. a. die Note nachtragen – der ungameable Realitätsanker).</summary>
    public Task<KlassenarbeitResponse> UpdateClassTestAsync(int classTestId, UpdateClassTestDto dto,
        CancellationToken ct = default) =>
        Http.PatchAsync<KlassenarbeitResponse>($"{Root}/class-tests/{classTestId}", dto, ct);

    /// <summary>Weist einer Klassenarbeit Übungen zu (Execute-Gate gilt wie bei Positionen).</summary>
    public Task<KlassenarbeitDetail> AssignClassTestExercisesAsync(int classTestId, AssignExercisesDto dto,
        CancellationToken ct = default) =>
        Http.PostAsync<KlassenarbeitDetail>($"{Root}/class-tests/{classTestId}/exercises", dto, ct);
}
