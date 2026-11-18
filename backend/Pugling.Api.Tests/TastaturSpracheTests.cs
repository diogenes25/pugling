using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-661: practice and exam cards name the language the child types in - the learnt language of the exercise's textbook
/// series - so the client can offer the special characters a phone keyboard may lack ("î"). Case by case the change that
/// turns it red is named.
/// </summary>
public class TastaturSpracheTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, int PlanId, int PositionId);

    private async Task<Setup> SetupAsync(string? seriesLanguage, string direction)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "5" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Tastatur-Kind"), pin }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Französisch-Tastatur") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId, sourceLanguage: seriesLanguage);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = TestApi.UniqueName("Essen"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction,
                    sourceLang = "fr",
                    targetLang = "de",
                    items = new[] { new { front = "dîner", back = TestApi.UniqueName("zu Abend essen") } },
                },
            }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, introduced: true);
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), planId, positionId);
    }

    private static async Task<JsonElement> FirstPracticeCardAsync(Setup s)
    {
        var baseUrl = TestApi.PracticeBase(s.PlanId, s.PositionId);
        var sessionId = await TestApi.IdAsync(await s.Child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        return (await s.Child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/next")).GetProperty("card");
    }

    /// <summary>
    /// The reversed direction asks the French word typed - the case the bar exists for - and carries no speech language
    /// (B-252 drops it on the swap). Red if the keyboard language is derived from <c>SpeechLang</c> instead of the series,
    /// or if the practice queries stop loading the series.
    /// </summary>
    [Fact]
    public async Task Uebungskarte_Nennt_Die_Lernsprache_Der_Reihe_Auch_Umgedreht()
    {
        var s = await SetupAsync("fr", "back-to-front");

        var card = await FirstPracticeCardAsync(s);

        Assert.Equal("fr", card.GetProperty("keyboardLang").GetString());
        Assert.Equal(JsonValueKind.Null, card.GetProperty("speechLang").ValueKind);
    }

    /// <summary>
    /// Decision 1: the bar stands in the exam too. Red if <c>TestItem</c> drops the field or the exam query stops loading
    /// the series.
    /// </summary>
    [Fact]
    public async Task Testfrage_Nennt_Die_Lernsprache_Der_Reihe()
    {
        var s = await SetupAsync("fr", "back-to-front");
        var baseUrl = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var next = await s.Child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next");

        Assert.Equal("fr", next.GetProperty("item").GetProperty("keyboardLang").GetString());
    }

    /// <summary>
    /// A series without a learnt language (a maths collection) names none, so no bar appears. Red if the server fills a
    /// default language.
    /// </summary>
    [Fact]
    public async Task Reihe_Ohne_Sprache_Nennt_Keine()
    {
        var s = await SetupAsync(null, "front-to-back");

        var card = await FirstPracticeCardAsync(s);

        Assert.Equal(JsonValueKind.Null, card.GetProperty("keyboardLang").ValueKind);
    }
}
