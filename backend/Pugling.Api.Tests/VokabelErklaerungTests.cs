using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-583: a vocabulary item may carry an explanation why the answer is what it is. The creator writes and reads it back,
/// the child gets it with the review outcome - after the answer, on a miss and on a hit - and never before: not on a card,
/// not in a test. Each case names the change that makes it red.
/// </summary>
public class VokabelErklaerungTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Why = "Bahnhof ist männlich: der Bahnhof, la gare ist weiblich.";

    private sealed record Setup(HttpClient Father, string ItemsUrl, int ExerciseId);

    private async Task<Setup> ExerciseAsync(string direction = "front-to-back", string? explanation = Why)
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Erklär-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var url = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary";
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new
        {
            title = "Am Bahnhof",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                direction,
                sourceLang = "fr",
                targetLang = "de",
                // The German side is made unique: the learnt word is the answer on free text (B-597) and must stay as typed.
                items = new[] { new { front = "la gare", back = TestApi.UniqueName("der Bahnhof"), explanation } },
            },
        }));
        return new Setup(father, $"{url}/{exerciseId}/items", exerciseId);
    }

    private static async Task<JsonElement> FirstItemAsync(Setup s) =>
        (await s.Father.GetFromJsonAsync<JsonElement>(s.ItemsUrl))[0];

    /// <summary>
    /// AK 1: the inline form on create stores it, the item list reads it back; PATCH with empty clears, left out keeps, set
    /// overwrites; POST of a new item takes it. Red when the controller or the reconcile drops the field, or MapItem does
    /// not hand it out.
    /// </summary>
    [Fact]
    public async Task Creator_Schreibt_Und_Liest_Die_Erklaerung()
    {
        var s = await ExerciseAsync();
        var item = await FirstItemAsync(s);
        Assert.Equal(Why, item.GetProperty("explanation").GetString());
        var itemUrl = $"{s.ItemsUrl}/{item.GetProperty("id").GetInt32()}";

        (await s.Father.PatchAsJsonAsync(itemUrl, new { hint = "beginnt mit d" })).EnsureSuccessStatusCode();
        Assert.Equal(Why, (await FirstItemAsync(s)).GetProperty("explanation").GetString());
        (await s.Father.PatchAsJsonAsync(itemUrl, new { explanation = "Neu erklärt." })).EnsureSuccessStatusCode();
        Assert.Equal("Neu erklärt.", (await FirstItemAsync(s)).GetProperty("explanation").GetString());
        (await s.Father.PatchAsJsonAsync(itemUrl, new { explanation = "  " })).EnsureSuccessStatusCode();
        Assert.Equal(JsonValueKind.Null, (await FirstItemAsync(s)).GetProperty("explanation").ValueKind);

        var added = await (await s.Father.PostAsJsonAsync(s.ItemsUrl, new { front = TestApi.UniqueName("le train"), back = "der Zug", explanation = "Zug ist männlich." }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Zug ist männlich.", added.GetProperty("explanation").GetString());
    }

    private async Task<(HttpClient Child, int PlanId, int PositionId, int SessionId)> PlayAsync(Setup s)
    {
        var pin = "3" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = TestApi.UniqueName("Erklär-Kind"), pin }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, s.ExerciseId, (int)TestStage.FreeText, childId: childId);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        return (child, planId, positionId, await TestApi.StartPositionSessionAsync(child, planId, positionId));
    }

    /// <summary>
    /// AK 2 and 3: the review outcome carries the explanation on a miss and on a hit; the card, the session and the test
    /// never contain its text. Red when the resolver does not map the column (outcome null), or when someone puts the
    /// explanation on a card or a test DTO.
    /// </summary>
    [Fact]
    public async Task Das_Kind_Bekommt_Die_Erklaerung_Erst_Nach_Der_Antwort()
    {
        var s = await ExerciseAsync();
        var (child, planId, positionId, sessionId) = await PlayAsync(s);
        var practice = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}";

        Assert.DoesNotContain(Why, await child.GetStringAsync($"{practice}/cards"));
        Assert.DoesNotContain(Why, await child.GetStringAsync(practice));

        var wrong = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "falsch"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(wrong, "wasCorrect");
        Assert.Equal(Why, wrong.GetProperty("explanation").GetString());

        var tests = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var start = await child.PostAsJsonAsync(tests, new { });
        var startText = await start.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Why, startText);
        var attemptId = JsonDocument.Parse(startText).RootElement.GetProperty("attemptId").GetInt32();
        var submitted = await child.PostAsJsonAsync($"{tests}/{attemptId}/submit", new { answers = new[] { new { itemIndex = 0, givenAnswer = "der Bahnhof" } } });
        Assert.DoesNotContain(Why, await submitted.Content.ReadAsStringAsync());
    }

    /// <summary>AK 2: a hit gets it too. Red when only misses are explained.</summary>
    [Fact]
    public async Task Auch_Ein_Treffer_Bekommt_Die_Erklaerung()
    {
        var s = await ExerciseAsync();
        var (child, planId, positionId, sessionId) = await PlayAsync(s);

        var right = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "la gare"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(right, "wasCorrect");
        Assert.Equal(Why, right.GetProperty("explanation").GetString());
    }

    /// <summary>AK 4: asked the other way round (German → French) the same explanation comes back. Red when the swap clears it.</summary>
    [Fact]
    public async Task Umgekehrte_Richtung_Behaelt_Die_Erklaerung()
    {
        var s = await ExerciseAsync("back-to-front");
        var (child, planId, positionId, sessionId) = await PlayAsync(s);

        var wrong = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "falsch"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Why, wrong.GetProperty("explanation").GetString());
    }
}
