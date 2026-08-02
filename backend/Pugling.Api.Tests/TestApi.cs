using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Shared helpers for the integration tests: login (father/child) and plan creation.</summary>
internal static class TestApi
{
    // Subject names are unique since the DB structure rebuild (the catalog is shared: "Englisch" may exist
    // only once). These helpers are called several times within the same test class - and thus against the
    // same DB; without names of their own the second call would collide with a 409. A counter instead of a
    // GUID, so that the names stay reproducible within one run.
    private static int _catalogSeq;

    private static string UniqueName(string prefix) =>
        $"{prefix} {Interlocked.Increment(ref _catalogSeq)}";

    private static async Task<string> TokenAsync(HttpClient c, string role, object dto)
    {
        var res = await c.PostAsJsonAsync($"/api/v1/auth/{role}", dto);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    /// <summary>Client with a father token (default: the seeded father, id 1 / PIN 0000).</summary>
    public static async Task<HttpClient> FatherAsync(WebApplicationFactory<Program> f, int id = 1, string pin = "0000")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, "adult", new { adultId = id, pin }));
        return c;
    }

    /// <summary>Client with a child token (default: the seeded child, id 1 / PIN 1111).</summary>
    public static async Task<HttpClient> ChildAsync(WebApplicationFactory<Program> f, int id = 1, string pin = "1111")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, "child", new { childId = id, pin }));
        return c;
    }

    /// <summary>Reads the <c>id</c> from a successful JSON response.</summary>
    public static Task<int> IdAsync(HttpResponseMessage res) => IdWithKeyAsync(res, "id");

    /// <summary>Reads an int property (e.g. <c>attemptId</c>) from a successful JSON response.</summary>
    public static async Task<int> IdWithKeyAsync(HttpResponseMessage res, string key)
    {
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty(key).GetInt32();
    }

    /// <summary>
    /// Creates (as the supervisor) an <b>empty</b> study plan container and returns its id. Content is added
    /// via positions (<see cref="SeedLeitnerPosition"/>) - the plan itself carries none.
    /// </summary>
    public static async Task<int> CreateEmptyPlanAsync(HttpClient father, int childId = 1)
    {
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId,
            title = "Test-Plan",
            durationDays = 5,
        });
        return await IdAsync(res);
    }

    /// <summary>Creates (as the supervisor) subject → chapter → an arithmetic exercise and returns their ids.</summary>
    /// <param name="father">Logged-in creator/supervisor client.</param>
    /// <param name="problems">
    /// Problems for the exercise; empty = the one default problem "7 × 6". Multiple are needed as soon as a
    /// hit rate between 0% and 100% needs to be tested.
    /// </param>
    public static async Task<(int subjectId, int chapterId, int exerciseId)> CreateArithmeticExerciseAsync(
        HttpClient father, params (string Prompt, int Answer)[] problems)
    {
        var tasks = problems.Length > 0 ? problems : [("7 × 6", 42)];
        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = UniqueName("Katalog-Test") }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel 1", orderIndex = 1 }));
        var exerciseId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic", new
            {
                title = "Kleines 1×1",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = tasks.Select(t => new { prompt = t.Prompt, answer = t.Answer, tolerance = 0 }) },
            }));
        return (subjectId, chapterId, exerciseId);
    }

    /// <summary>Creates (as the supervisor) a vocabulary exercise in the catalog and returns its id.</summary>
    public static async Task<int> CreateVocabExerciseAsync(HttpClient father, params (string Front, string Back)[] items)
    {
        var vocab = items.Length > 0 ? items : [("hello", "hallo"), ("goodbye", "tschüss")];
        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = UniqueName("Englisch-Pos") }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        return await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Begrüßungen",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = vocab.Select(i => new { front = i.Front, back = i.Back }) },
            }));
    }

    /// <summary>
    /// Creates (as the supervisor) a family shop article together with a listing (<c>ShopListing</c>) and
    /// returns the listing id. Shared setup helper for the shop tests (instead of posting article+listing by
    /// hand in every test). The article number is unique per father - it may repeat across multiple fathers.
    /// </summary>
    public static async Task<int> CreateShopListingAsync(HttpClient supervisor, string articleNumber,
        int coinPrice, int unitsPerPurchase, int stock, string articleTitle = "Test-Artikel",
        string listingTitle = "", int gemPrice = 0, string unitType = "Stueck", string actionType = "Sonstiges")
    {
        var articleId = await IdAsync(await supervisor.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
            new { articleNumber, title = articleTitle, unitType, actionType }));
        return await IdAsync(await supervisor.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { title = listingTitle, coinPrice, gemPrice, unitsPerPurchase, currentStock = stock, maxStock = stock }));
    }

    /// <summary>
    /// Creates (as the supervisor) a store vocabulary entry "simply" (auto key) and returns (id, key).
    /// <paramref name="translationAlternatives"/> declares further equally valid translations.
    /// </summary>
    public static async Task<(int id, string key)> CreateStoreVocabAsync(HttpClient father, string word, string translation,
        string src = "en", string tgt = "de", string[]? translationAlternatives = null)
    {
        var res = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = src, targetLanguage = tgt, word, translation, translationAlternatives });
        res.EnsureSuccessStatusCode();
        var v = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (v.GetProperty("id").GetInt32(), v.GetProperty("key").GetString()!);
    }

    /// <summary>Resolves a store key to its id (refs now reference by id).</summary>
    public static async Task<int> ResolveVocabIdAsync(HttpClient father, string key)
    {
        var list = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/vocabulary?search={Uri.EscapeDataString(key)}&take=500");
        return list!.First(v => v.GetProperty("key").GetString() == key).GetProperty("id").GetInt32();
    }

    /// <summary>Creates (as the supervisor) a vocabulary exercise that references store entries by id; returns its id.</summary>
    public static async Task<int> CreateVocabRefExerciseAsync(HttpClient father, params string[] keys)
    {
        var ids = new List<int>();
        foreach (var key in keys) ids.Add(await ResolveVocabIdAsync(father, key));

        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = UniqueName("Englisch-Ref") }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        return await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Vokabeln (Store)",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", refs = ids.Select(id => new { vocabularyId = id }) },
            }));
    }

    /// <summary>Seeds a plan container directly with one (Leitner) position on the exercise.</summary>
    public static (int planId, int positionId) SeedLeitnerPosition(WebApplicationFactory<Program> f, int exerciseId,
        int stage, int childId = 1, GoalCadence cadence = GoalCadence.Daily, int? goalThreshold = null,
        bool useLeitner = true, bool requireTypedTest = false, int pointsGoalMet = 20,
        int comboThreshold = 5, int comboBonusPoints = 5, int speedThresholdSeconds = 0, int speedBonusPoints = 0,
        PracticeOrder orderStrategy = PracticeOrder.WeakestFirst)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = childId, Title = "Positions-Plan", StartDate = today, EndDate = today.AddDays(5) };
        var pos = new PlanPosition
        {
            ExerciseId = exerciseId,
            Order = 0,
            Stage = stage,
            Cadence = cadence,
            GoalThreshold = goalThreshold,
            UseLeitner = useLeitner,
            RequireTypedTest = requireTypedTest,
            NewContentPoints = 10,
            PointsGoalMet = pointsGoalMet,
            ComboThreshold = comboThreshold,
            ComboBonusPoints = comboBonusPoints,
            SpeedThresholdSeconds = speedThresholdSeconds,
            SpeedBonusPoints = speedBonusPoints,
            OrderStrategy = orderStrategy,
        };
        plan.Positions.Add(pos);
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return (plan.Id, pos.Id);
    }

    /// <summary>Base URL of the position practice sessions.</summary>
    public static string PracticeBase(int planId, int positionId) =>
        $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";

    /// <summary>Starts a position practice session and returns its id.</summary>
    public static async Task<int> StartPositionSessionAsync(HttpClient child, int planId, int positionId) =>
        await IdAsync(await child.PostAsJsonAsync(PracticeBase(planId, positionId), new { }));

    /// <summary>Reviews a card server-side (typed via <paramref name="givenAnswer"/>, otherwise self-assessment).</summary>
    public static Task<HttpResponseMessage> PositionReviewAsync(HttpClient child, int planId, int positionId, int sessionId,
        int itemIndex, string? givenAnswer = null, bool? wasKnown = null) =>
        child.PostAsJsonAsync($"{PracticeBase(planId, positionId)}/{sessionId}/review",
            new { itemIndex, givenAnswer, wasKnown });
}
