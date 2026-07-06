using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Geteilte Helfer für die Integrationstests: Login (Vater/Sohn) und Plan-Anlage.</summary>
internal static class TestApi
{
    private static async Task<string> TokenAsync(HttpClient c, string role, object dto)
    {
        var res = await c.PostAsJsonAsync($"/api/v1/auth/{role}", dto);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    /// <summary>Client mit Vater-Token (Default: der geseedete Papa, id 1 / PIN 0000).</summary>
    public static async Task<HttpClient> FatherAsync(WebApplicationFactory<Program> f, int id = 1, string pin = "0000")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, "father", new { fatherId = id, pin }));
        return c;
    }

    /// <summary>Client mit Sohn-Token (Default: der geseedete Sohn, id 1 / PIN 1111).</summary>
    public static async Task<HttpClient> ChildAsync(WebApplicationFactory<Program> f, int id = 1, string pin = "1111")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, "child", new { childId = id, pin }));
        return c;
    }

    /// <summary>Liest die <c>id</c> aus einer erfolgreichen JSON-Antwort.</summary>
    public static Task<int> IdAsync(HttpResponseMessage res) => IdWithKeyAsync(res, "id");

    /// <summary>Liest eine int-Property (z. B. <c>attemptId</c>) aus einer erfolgreichen JSON-Antwort.</summary>
    public static async Task<int> IdWithKeyAsync(HttpResponseMessage res, string key)
    {
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty(key).GetInt32();
    }

    /// <summary>Legt (als Vater) einen Vokabel-Lehrplan mit zwei Seed-Vokabeln an und liefert dessen Id.</summary>
    public static async Task<int> CreateVocabPlanAsync(HttpClient father, int childId = 1, bool dailyTestRequired = true)
    {
        var res = await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId,
            title = "Test-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            dailyTestRequired,
        });
        return await IdAsync(res);
    }

    /// <summary>Legt (als Vater) Fach → Kapitel → eine Rechen-Übung an und liefert deren Ids.</summary>
    public static async Task<(int subjectId, int chapterId, int exerciseId)> CreateArithmeticExerciseAsync(HttpClient father)
    {
        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Katalog-Test" }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "Kapitel 1", orderIndex = 1 }));
        var exerciseId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/arithmetic", new
            {
                title = "Kleines 1×1",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "7 × 6", answer = 42, tolerance = 0 } } },
            }));
        return (subjectId, chapterId, exerciseId);
    }

    /// <summary>Legt (als Vater) eine Vokabel-Übung im Katalog an und liefert ihre Id.</summary>
    public static async Task<int> CreateVocabExerciseAsync(HttpClient father, params (string Front, string Back)[] items)
    {
        var vocab = items.Length > 0 ? items : [("hello", "hallo"), ("goodbye", "tschüss")];
        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Englisch-Pos" }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        return await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Begrüßungen",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = vocab.Select(i => new { front = i.Front, back = i.Back }) },
            }));
    }

    /// <summary>Legt (als Vater) eine Store-Vokabel „einfach" an (Auto-Key) und liefert (id, key).</summary>
    public static async Task<(int id, string key)> CreateStoreVocabAsync(HttpClient father, string word, string translation,
        string src = "en", string tgt = "de")
    {
        var res = await father.PostAsJsonAsync("/api/v1/learn/vocabulary",
            new { sourceLanguage = src, targetLanguage = tgt, word, translation });
        res.EnsureSuccessStatusCode();
        var v = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (v.GetProperty("id").GetInt32(), v.GetProperty("key").GetString()!);
    }

    /// <summary>Löst einen Store-Key in seine Id auf (Refs referenzieren jetzt per Id).</summary>
    public static async Task<int> ResolveVocabIdAsync(HttpClient father, string key)
    {
        var list = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/learn/vocabulary?search={Uri.EscapeDataString(key)}&take=500");
        return list!.First(v => v.GetProperty("key").GetString() == key).GetProperty("id").GetInt32();
    }

    /// <summary>Legt (als Vater) eine Vokabel-Übung an, die Store-Einträge per Id referenziert; liefert deren Id.</summary>
    public static async Task<int> CreateVocabRefExerciseAsync(HttpClient father, params string[] keys)
    {
        var ids = new List<int>();
        foreach (var key in keys) ids.Add(await ResolveVocabIdAsync(father, key));

        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Englisch-Ref" }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        return await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Vokabeln (Store)",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", refs = ids.Select(id => new { vocabularyId = id }) },
            }));
    }

    /// <summary>Seedet direkt einen Plan-Container mit einer (Leitner-)Position auf die Übung.</summary>
    public static (int planId, int positionId) SeedLeitnerPosition(WebApplicationFactory<Program> f, int exerciseId,
        int stage, int childId = 1, GoalCadence cadence = GoalCadence.Daily, int? goalThreshold = null,
        bool useLeitner = true, bool requireTypedTest = false, int pointsGoalMet = 20,
        int comboThreshold = 5, int comboBonusPoints = 5, int speedThresholdSeconds = 0, int speedBonusPoints = 0)
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
        };
        plan.Positions.Add(pos);
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return (plan.Id, pos.Id);
    }

    /// <summary>Basis-URL der Positions-Übungssitzungen.</summary>
    public static string PracticeBase(int planId, int positionId) =>
        $"/api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions";

    /// <summary>Startet eine Positions-Übungssitzung und liefert ihre Id.</summary>
    public static async Task<int> StartPositionSessionAsync(HttpClient child, int planId, int positionId) =>
        await IdAsync(await child.PostAsJsonAsync(PracticeBase(planId, positionId), new { }));

    /// <summary>Bewertet eine Karte serverseitig (getippt via <paramref name="givenAnswer"/>, sonst Selbsteinschätzung).</summary>
    public static Task<HttpResponseMessage> PositionReviewAsync(HttpClient child, int planId, int positionId, int sessionId,
        int itemIndex, string? givenAnswer = null, bool? wasKnown = null) =>
        child.PostAsJsonAsync($"{PracticeBase(planId, positionId)}/{sessionId}/review",
            new { itemIndex, givenAnswer, wasKnown });
}
