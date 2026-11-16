using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-472: a vocabulary exercise asked "both" ways plays every word twice - card 2i in the exercise's direction, card
/// 2i+1 the other way round - and everything that counts cards counts both.
/// </summary>
public class BeideRichtungenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string Front, string Back)[] Words =
        [("the city", "die Stadt"), ("the street", "die Straße"), ("the station", "der Bahnhof"), ("the school", "die Schule")];

    private async Task<(int ExerciseId, int UnitId)> ExerciseAsync(HttpClient father, string direction, int? unitId = null)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Richtung") }));
        var (seriesId, newUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var config = new Dictionary<string, object?>
        {
            ["direction"] = direction,
            ["sourceLang"] = "en",
            ["targetLang"] = "de",
            ["items"] = Words.Select(w => new { front = w.Front, back = w.Back }).ToArray(),
        };
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{newUnitId}/vocabulary",
            new { title = $"London {direction}", orderIndex = 1, rewardPoints = 10, config }));
        return (id, newUnitId);
    }

    private static async Task<JsonElement[]> PreviewItemsAsync(HttpClient father, int exerciseId, TestStage stage) =>
        [.. (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage={(int)stage}"))
            .GetProperty("items").EnumerateArray()];

    /// <summary>
    /// AK 1: each word comes twice, its two cards side by side. Red against the alternating reading of "both" (four cards,
    /// "die Straße" as the second prompt) and against "first the whole list forwards, then backwards" ("the street"
    /// second).
    /// </summary>
    [Fact]
    public async Task Jedes_Wort_kommt_in_beiden_Richtungen_direkt_hintereinander()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, _) = await ExerciseAsync(father, "both");

        var items = await PreviewItemsAsync(father, exerciseId, TestStage.FreeText);

        Assert.Equal(
            ["the city", "die Stadt", "the street", "die Straße", "the station", "der Bahnhof", "the school", "die Schule"],
            items.Select(i => i.GetProperty("prompt").GetString()));
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7], items.Select(i => i.GetProperty("itemIndex").GetInt32()));
    }

    /// <summary>
    /// AK 1, the other two directions stay as they were: one card per word. Red if the doubling leaks into them. On the
    /// choice, where the direction still decides what is shown (B-597: writing stages always ask the learnt word).
    /// </summary>
    [Theory]
    [InlineData("front-to-back", "the city")]
    [InlineData("back-to-front", "die Stadt")]
    public async Task Eine_Richtung_bleibt_eine_Karte_je_Wort(string direction, string firstPrompt)
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, _) = await ExerciseAsync(father, direction);

        var items = await PreviewItemsAsync(father, exerciseId, TestStage.MultipleChoice);

        Assert.Equal(Words.Length, items.Length);
        Assert.Equal(firstPrompt, items[0].GetProperty("prompt").GetString());
    }

    /// <summary>
    /// AK 2: the catalog counts the cards the child plays, so the father's "neue Inhalte je Tag" matches the number
    /// beside it. Red if <c>ItemCount</c> keeps counting rows for "both".
    /// </summary>
    [Fact]
    public async Task Der_Katalog_zaehlt_die_Karten_beider_Richtungen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, unitId) = await ExerciseAsync(father, "both");

        var rows = (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises?seriesUnitId={unitId}"))
            .EnumerateArray().ToList();

        Assert.Equal(2 * Words.Length, rows.Single(r => r.GetProperty("id").GetInt32() == exerciseId).GetProperty("itemCount").GetInt32());
    }

    /// <summary>
    /// AK 2 on the child's path: the daily dose counts cards, and because a word's two cards lie side by side, a dose of
    /// two brings one new word in both directions. Red against the alternating reading ("die Straße" second) and against
    /// "forwards first" ("the street" second); red as well if the dose started counting words (four cards).
    /// </summary>
    [Fact]
    public async Task Die_Tagesdosis_bringt_ein_neues_Wort_in_beiden_Richtungen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, _) = await ExerciseAsync(father, "both");
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId = 1, title = "Beide Richtungen", durationDays = 14 }));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily", stage = (int)TestStage.FreeText, useLeitner = true, newItemsPerDay = 2 }));
        var child = await TestApi.ChildAsync(factory);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(
            TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        var cards = (await child.GetFromJsonAsync<List<JsonElement>>(
            $"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;

        Assert.Equal(["the city", "die Stadt"], cards.Select(c => c.GetProperty("prompt").GetString()));
    }

    /// <summary>
    /// AK 2 on the test path: both cards of a word in one test write the word's cross-plan progress once, counted
    /// twice. Red as long as the second card of a word with no progress yet adds a second row for the same item - the
    /// unique index then turns the submission into a 500.
    /// </summary>
    [Fact]
    public async Task Ein_Test_mit_beiden_Karten_eines_neuen_Wortes_zaehlt_sie_in_einer_Zeile()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, _) = await ExerciseAsync(father, "both");
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "die Stadt" }, new { itemIndex = 1, givenAnswer = "the city" } },
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, submit.StatusCode);
        Assert.Equal(2, (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correctItems").GetInt32());
        var progress = (await child.GetFromJsonAsync<JsonElement>(
            $"/api/v1/student/children/1/vocabulary-progress?exerciseId={exerciseId}")).EnumerateArray().ToList();
        // Every card of the test is scored (an unanswered one as wrong), so all four words have a row - one each.
        Assert.Equal(Words.Length, progress.Count);
        var city = Assert.Single(progress, p => p.GetProperty("front").GetString() == "the city");
        Assert.Equal(2, city.GetProperty("seenCount").GetInt32());
        Assert.Equal(2, city.GetProperty("correctCount").GetInt32());
    }

    /// <summary>
    /// A planned exercise keeps its cards: switching into or out of "both" would make every saved card index point at
    /// another card, so it is locked like reordering the words. Turning one direction into the other keeps one card per
    /// word and stays allowed. Red if the lock only looks at items (the switch goes through and the preview has four
    /// cards), or if it locks every direction change (the second PUT fails).
    /// </summary>
    [Fact]
    public async Task Eine_verplante_Uebung_wechselt_nicht_in_oder_aus_beiden_Richtungen()
    {
        var father = await TestApi.AdultAsync(factory);

        async Task<HttpResponseMessage> PlannedSwitchAsync(string from, string to)
        {
            var (exerciseId, unitId) = await ExerciseAsync(father, from);
            TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
            var seriesId = (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}"))
                .GetProperty("seriesId").GetInt32();
            return await father.PutAsJsonAsync(
                $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary/{exerciseId}",
                new { title = $"London {from}", orderIndex = 1, rewardPoints = 10, config = new { direction = to, sourceLang = "en", targetLang = "de" } });
        }

        var blocked = await PlannedSwitchAsync("both", "front-to-back");
        Assert.Equal(System.Net.HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("exercise_items_locked", (await blocked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(System.Net.HttpStatusCode.OK, (await PlannedSwitchAsync("front-to-back", "back-to-front")).StatusCode);
    }

    /// <summary>
    /// The multiple-choice options of a card come from the language of its answer. With both directions in one list a
    /// neighbour's answer can be in the other language - and for card 2i the neighbour 2i+1 answers with the very word
    /// being asked. Red if <c>DistractorPool</c> takes every other card.
    /// </summary>
    [Fact]
    public async Task Ablenker_kommen_aus_der_Sprache_der_Antwort()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, _) = await ExerciseAsync(father, "both");
        var german = Words.Select(w => w.Back).ToHashSet();
        var english = Words.Select(w => w.Front).ToHashSet();

        var items = await PreviewItemsAsync(father, exerciseId, TestStage.MultipleChoice);

        foreach (var item in items)
        {
            var index = item.GetProperty("itemIndex").GetInt32();
            var choices = item.GetProperty("choices").EnumerateArray().Select(c => c.GetString()!).ToList();
            Assert.NotEmpty(choices);
            Assert.All(choices, c => Assert.Contains(c, index % 2 == 0 ? german : english));
        }
    }
}
