using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The product goal as a test (B-451, B-04): a child practises French vocabulary every day for four weeks on ONE
/// mandatory position, and every day is a portion it can manage. The child walks the student API on a clock that is
/// advanced day by day - not the supervisor's backfill path, which runs under different rules.
/// </summary>
public class VierWochenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const int PoolSize = 120;
    private const int Dose = 8;
    private const string ChildPin = "2468";

    private static (string Front, string Back)[] Words(int count) =>
        [.. Enumerable.Range(0, count).Select(i => ($"mot {i}", $"Wort {i}"))];

    private sealed record Setup(HttpClient Father, int ChildId, int PlanId, int PositionId);

    private async Task<Setup> SetupAsync(int poolSize, int newItemsPerDay, int durationDays = 28)
    {
        // Near the real today: the seed dates its demo plans from the wall clock.
        factory.Clock.FreezeAt(new DateTimeOffset(DateTime.UtcNow.Date.AddHours(9), TimeSpan.Zero));
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Neuntklaessler"), grade = 9, pin = ChildPin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", Words(poolSize));
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Vier Wochen Französisch", durationDays }));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new
            {
                exerciseId,
                cadence = "Daily",
                stage = (int)TestStage.FreeText,
                useLeitner = true,
                requireTypedTest = true,
                penaltyCoins = 5,
                newItemsPerDay,
            }));
        return new Setup(father, childId, planId, positionId);
    }

    private static string TestBase(Setup s) => $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

    /// <summary>One learn round: introductions are looked at, asked cards are typed. Returns (introduced, asked).</summary>
    private static async Task<(int Introduced, int Asked)> PlayRoundAsync(HttpClient child, Setup s, HashSet<int> typedOnce)
    {
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(
            TestApi.PracticeBase(s.PlanId, s.PositionId), new { mode = "Lern" }));
        var cards = (await child.GetFromJsonAsync<List<JsonElement>>(
            $"{TestApi.PracticeBase(s.PlanId, s.PositionId)}/{sessionId}/cards"))!;
        int introduced = 0, asked = 0;
        foreach (var card in cards)
        {
            var index = card.GetProperty("itemIndex").GetInt32();
            if (card.GetProperty("introduction").GetBoolean())
            {
                introduced++;
                (await TestApi.PositionReviewAsync(child, s.PlanId, s.PositionId, sessionId, index, wasKnown: true))
                    .EnsureSuccessStatusCode();
                continue;
            }
            asked++;
            // Every sixth word is a hard one: wrong the first time it is asked, right from then on.
            var answer = index % 6 == 0 && !typedOnce.Contains(index) ? "faux" : $"Wort {index}";
            typedOnce.Add(index);
            (await TestApi.PositionReviewAsync(child, s.PlanId, s.PositionId, sessionId, index, givenAnswer: answer))
                .EnsureSuccessStatusCode();
        }
        return (introduced, asked);
    }

    /// <summary>The day's test, answered correctly card by card. Returns (items, passed).</summary>
    private static async Task<(int Items, bool Passed)> TakeTestAsync(HttpClient child, Setup s)
    {
        var start = await child.PostAsJsonAsync(TestBase(s), new { });
        start.EnsureSuccessStatusCode();
        var attempt = await start.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var total = attempt.GetProperty("totalItems").GetInt32();
        for (var i = 0; i < total; i++)
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{TestBase(s)}/{attemptId}/next");
            var index = next.GetProperty("item").GetProperty("itemIndex").GetInt32();
            (await child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/answer", new { itemIndex = index, givenAnswer = $"Wort {index}" }))
                .EnsureSuccessStatusCode();
        }
        var result = await (await child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        return (total, result.GetProperty("passed").GetBoolean());
    }

    private async Task<JsonElement> OverviewAsync(HttpClient child, Setup s) =>
        (await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{s.PlanId}/overview")).GetProperty("today");

    private int Penalties(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionGoalPenalties.Count(p => p.PlanPositionId == positionId);
    }

    [Fact]
    public async Task VierWochen_TaeglichEinePortion_ZielJedenTagErreichbar_EinAusgelassenerTagKostetEinmal()
    {
        const int skippedDay = 10;
        var s = await SetupAsync(PoolSize, Dose);
        var typedOnce = new HashSet<int>();
        var introducedTotal = 0;

        for (var day = 1; day <= 28; day++)
        {
            if (day > 1) factory.Clock.Advance(TimeSpan.FromDays(1));
            if (day == skippedDay) continue;

            // Logging in settles the closed periods - the path the penalty takes.
            var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);

            // Round 1 shows the day's new words and asks the due ones; round 2 asks what round 1 introduced or missed.
            var first = await PlayRoundAsync(child, s, typedOnce);
            var second = await PlayRoundAsync(child, s, typedOnce);
            var third = await PlayRoundAsync(child, s, typedOnce);

            var expectedNew = Math.Min(Dose, PoolSize - introducedTotal);
            Assert.Equal(expectedNew, first.Introduced);
            // The dose holds for the whole day, not per round.
            Assert.Equal(0, second.Introduced + third.Introduced);
            introducedTotal += first.Introduced;

            var asked = first.Asked + second.Asked + third.Asked;
            Assert.True(asked <= 70, $"Tag {day}: {asked} abgefragte Karten sind keine Tagesportion.");

            var (items, passed) = await TakeTestAsync(child, s);
            Assert.InRange(items, 1, 60);
            Assert.True(passed, $"Tag {day}: Test über {items} Wörter nicht bestanden.");

            var overview = await OverviewAsync(child, s);
            Assert.True(overview.GetProperty("dutyDone").GetBoolean(), $"Tag {day}: Pflicht nicht erfüllt.");
            Assert.Equal(introducedTotal, overview.GetProperty("positions")[0].GetProperty("introducedCount").GetInt32());
        }

        // 27 played days at 8 a day carry the whole pool of 120.
        Assert.Equal(PoolSize, introducedTotal);
        // Exactly the skipped day cost a penalty - no played day did.
        Assert.Equal(1, Penalties(s.PositionId));
    }

    [Fact]
    public async Task OhneDosis_KommtAmErstenTagDerGanzePool()
    {
        var s = await SetupAsync(poolSize: 20, newItemsPerDay: 0);
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        var (introduced, _) = await PlayRoundAsync(child, s, []);
        Assert.Equal(20, introduced);
    }

    [Fact]
    public async Task Dosis_TestPrueftDieTagesportion_NichtAllesEingefuehrte()
    {
        var s = await SetupAsync(poolSize: 30, newItemsPerDay: 5);
        var typedOnce = new HashSet<int>();
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);

        // Before anything is introduced the test checks the first dose, not the pool.
        var cold = await child.PostAsJsonAsync(TestBase(s), new { });
        Assert.Equal(5, (await cold.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalItems").GetInt32());

        // Day 1 and day 2 fully played: ten words introduced, all answered right, none due on day 3 but the new ones.
        for (var day = 1; day <= 3; day++)
        {
            if (day > 1)
            {
                factory.Clock.Advance(TimeSpan.FromDays(1));
                child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
            }
            await PlayRoundAsync(child, s, typedOnce);
            await PlayRoundAsync(child, s, typedOnce);
            await PlayRoundAsync(child, s, typedOnce);
        }
        var (items, _) = await TakeTestAsync(child, s);
        // 15 introduced by day 3. Met today: the five new ones and the five of day 1 (typed right on day 1, due again
        // after two days). The five of day 2 are not due before day 4.
        Assert.Equal(10, items);
    }

    [Fact]
    public async Task Dosis_WerNieUebt_WirdJedenTagAufMehrGeprueft_NichtAufDieselbenErstenWoerter()
    {
        var s = await SetupAsync(poolSize: 30, newItemsPerDay: 5);
        var sizes = new List<int>();
        for (var day = 1; day <= 3; day++)
        {
            if (day > 1) factory.Clock.Advance(TimeSpan.FromDays(1));
            var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
            var (items, _) = await TakeTestAsync(child, s);
            sizes.Add(items);
        }
        // The test introduces what it asked cold, and nothing but practice moves it out of "due": 5, 5+5, 10+5.
        Assert.Equal([5, 10, 15], sizes);
    }

    [Fact]
    public async Task Dosis_VorschauDesVaters_FuehrtNichtsEin()
    {
        var s = await SetupAsync(poolSize: 30, newItemsPerDay: 5);
        (await s.Father.PostAsJsonAsync(TestBase(s), new { })).EnsureSuccessStatusCode();
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        var (introduced, _) = await PlayRoundAsync(child, s, []);
        // Still the full first dose as introductions: the supervisor's attempt left the child's state alone.
        Assert.Equal(5, introduced);
    }

    [Fact]
    public async Task Dosis_Review_NurFuerKartenDerRunde()
    {
        var s = await SetupAsync(poolSize: 30, newItemsPerDay: 5);
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(
            TestApi.PracticeBase(s.PlanId, s.PositionId), new { mode = "Lern" }));

        var outside = await TestApi.PositionReviewAsync(child, s.PlanId, s.PositionId, sessionId, itemIndex: 20, wasKnown: true);
        Assert.Equal(HttpStatusCode.NotFound, outside.StatusCode);
        var inside = await TestApi.PositionReviewAsync(child, s.PlanId, s.PositionId, sessionId, itemIndex: 4, wasKnown: true);
        Assert.Equal(HttpStatusCode.OK, inside.StatusCode);
    }

    [Fact]
    public async Task Dosis_AufUngeordneterListe_WirdAbgelehnt()
    {
        var s = await SetupAsync(poolSize: 3, newItemsPerDay: 0);
        var subjectId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Erdkunde-Dosis") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(s.Father, subjectId);
        var listId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/list", new
            {
                title = "Bundesländer",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { instruction = "Nenne vier.", ordered = false, items = new[] { new { value = "Bayern" }, new { value = "Hessen" }, new { value = "Berlin" }, new { value = "Bremen" } } },
            }));
        var url = $"/api/v1/supervisor/study-plans/{s.PlanId}/positions";

        var withDose = await s.Father.PostAsJsonAsync(url, new { exerciseId = listId, useLeitner = true, newItemsPerDay = 2 });
        Assert.Equal(HttpStatusCode.BadRequest, withDose.StatusCode);
        var without = await s.Father.PostAsJsonAsync(url, new { exerciseId = listId, useLeitner = true });
        Assert.Equal(HttpStatusCode.Created, without.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public async Task Dosis_AusserhalbDerGrenzen_WirdAbgelehnt(int newItemsPerDay)
    {
        var s = await SetupAsync(poolSize: 3, newItemsPerDay: 0);
        var res = await s.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}", new { newItemsPerDay });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Dosis_OhneLeitner_WirdAbgelehnt_BeimAnlegenUndBeimAbschalten()
    {
        var s = await SetupAsync(poolSize: 3, newItemsPerDay: 4);
        var url = $"/api/v1/supervisor/study-plans/{s.PlanId}/positions";
        var exerciseId = await TestApi.CreateVocabExerciseAsync(s.Father, "fr", "de", Words(3));

        var create = await s.Father.PostAsJsonAsync(url, new { exerciseId, useLeitner = false, newItemsPerDay = 4 });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        // Switching the box off under a stored dose is the same contradiction, reached the other way.
        var patch = await s.Father.PatchAsJsonAsync($"{url}/{s.PositionId}", new { useLeitner = false });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
        // Both in one go is consistent again.
        var both = await s.Father.PatchAsJsonAsync($"{url}/{s.PositionId}", new { useLeitner = false, newItemsPerDay = 0 });
        Assert.Equal(HttpStatusCode.OK, both.StatusCode);
    }

    [Fact]
    public async Task Dosis_PatchSetztUndNullLaesstStehen()
    {
        var s = await SetupAsync(poolSize: 3, newItemsPerDay: 4);
        var url = $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}";
        var untouched = await (await s.Father.PatchAsJsonAsync(url, new { penaltyCoins = 3 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, untouched.GetProperty("newItemsPerDay").GetInt32());
        var cleared = await (await s.Father.PatchAsJsonAsync(url, new { newItemsPerDay = 0 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, cleared.GetProperty("newItemsPerDay").GetInt32());
    }
}
