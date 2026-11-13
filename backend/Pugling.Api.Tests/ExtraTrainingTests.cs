using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The bonus training: extra exercises on the current topic that pay coins beyond the day's duty - but only after it,
/// only for checked hits, once per exercise (vocabulary climbs a form per run instead) and within a daily cap.
/// </summary>
public class ExtraTrainingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string Front, string Back)[] Words =
        [("dog", "Hund"), ("cat", "Katze"), ("house", "Haus"), ("tree", "Baum")];

    private record Setup(HttpClient Father, HttpClient Child, int ChildId, int SeriesId, int UnitId, int VocabId);

    // A fresh child with one unit holding a vocabulary exercise of four words, assigned as a plan position.
    private async Task<Setup> SetupAsync(GoalCadence cadence = GoalCadence.None)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Bonus-Kind"), pin }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Bonus") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var vocabId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = "Tiere und Dinge",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = Words.Select(w => new { front = w.Front, back = w.Back })
                },
            }));
        TestApi.SeedLeitnerPosition(factory, vocabId, (int)TestStage.FreeText, childId: childId, cadence: cadence);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        return new Setup(father, child, childId, seriesId, unitId, vocabId);
    }

    private static async Task<int> AddArithmeticAsync(Setup s, params (string Prompt, int Answer)[] problems) =>
        await TestApi.IdAsync(await s.Father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{s.SeriesId}/units/{s.UnitId}/arithmetic", new
            {
                title = "Bonus-Rechnen",
                orderIndex = 2,
                rewardPoints = 10,
                config = new { problems = problems.Select(p => new { prompt = p.Prompt, answer = p.Answer, tolerance = 0 }) },
            }));

    private static string Base(Setup s) => $"/api/v1/student/children/{s.ChildId}/extra-training";

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    private static JsonElement PoolEntry(JsonElement overview, int exerciseId) =>
        overview.GetProperty("exercises").EnumerateArray().Single(e => e.GetProperty("exerciseId").GetInt32() == exerciseId);

    // Answers every vocabulary card right: typed/chosen by the word's meaning, self-assessed as known.
    private static object[] RightVocabAnswers(JsonElement run) =>
    [
        .. run.GetProperty("cards").EnumerateArray().Select(c => (object)new
        {
            itemIndex = c.GetProperty("itemIndex").GetInt32(),
            givenAnswer = Words.Single(w => w.Front == c.GetProperty("prompt").GetString()).Back,
            wasKnown = true,
        }),
    ];

    private int CoinsBooked(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ChildPointsEntries.Where(e => e.ChildId == childId && e.Kind == PointKind.ExtraTrainingCoins).Sum(e => e.Amount);
    }

    [Fact]
    public async Task Vor_Erfuellter_Tagespflicht_Ist_Das_Bonus_Training_Gesperrt()
    {
        var s = await SetupAsync(GoalCadence.Daily);

        var overview = await JsonAsync(await s.Child.GetAsync(Base(s)));
        JsonAssert.False(overview, "unlocked");
        Assert.Equal("duty_open", overview.GetProperty("lockReason").GetString());
        Assert.NotEmpty(overview.GetProperty("outstanding").EnumerateArray());

        await AssertProblemAsync(await s.Child.PostAsync($"{Base(s)}/{s.VocabId}/runs", null),
            HttpStatusCode.Conflict, "extra_training_locked");
    }

    [Fact]
    public async Task Vokabeln_Werden_Je_Durchgang_Schwerer_Und_Zahlen_Nur_Geprueft()
    {
        var s = await SetupAsync();
        var expected = new[] { ("SelfAssess", 0), ("SelfAssess", 0), ("Choice", 4), ("Choice", 4), ("Typed", 8), ("Typed", 8) };

        var total = 0;
        foreach (var (form, coins) in expected)
        {
            var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{s.VocabId}/runs", null));
            Assert.Equal(form, run.GetProperty("form").GetString());
            var card = run.GetProperty("cards")[0];
            // The meaning is shown on the self-assessed form only, the options on the choice form only.
            Assert.Equal(form == "SelfAssess", card.GetProperty("reveal").ValueKind == JsonValueKind.String);
            Assert.Equal(form == "Choice", card.GetProperty("choices").ValueKind == JsonValueKind.Array);

            var result = await JsonAsync(await s.Child.PostAsJsonAsync(
                $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers = RightVocabAnswers(run) }));
            Assert.Equal(4, result.GetProperty("correct").GetInt32());
            Assert.Equal(coins, result.GetProperty("coinsAwarded").GetInt32());
            total += coins;
        }

        Assert.Equal(total, CoinsBooked(s.ChildId));
    }

    [Fact]
    public async Task Eine_Uebung_Ohne_Vokabeln_Ist_Nach_Dem_Start_Verbraucht()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5), ("4 × 4", 16));

        var first = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var runId = first.GetProperty("runId").GetInt32();
        Assert.Equal("Typed", first.GetProperty("form").GetString());

        // Leaving and coming back resumes the same run instead of handing out a fresh one.
        var again = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        Assert.Equal(runId, again.GetProperty("runId").GetInt32());

        var answers = first.GetProperty("cards").EnumerateArray().Select(c => new
        {
            itemIndex = c.GetProperty("itemIndex").GetInt32(),
            givenAnswer = c.GetProperty("prompt").GetString() == "2 + 3" ? "5" : "16",
        }).ToArray();
        var result = await JsonAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{runId}/submit", new { answers }));
        Assert.Equal(2, result.GetProperty("correct").GetInt32());
        Assert.Equal(4, result.GetProperty("coinsAwarded").GetInt32());

        await AssertProblemAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{runId}/submit", new { answers }),
            HttpStatusCode.Conflict, "extra_run_completed");
        await AssertProblemAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null),
            HttpStatusCode.Conflict, "extra_exercise_done");
        JsonAssert.True(PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId), "done");
        Assert.Equal(4, CoinsBooked(s.ChildId));
    }

    [Fact]
    public async Task Eine_Uebung_Aus_Dem_Lernpfad_Ohne_Vokabeln_Fehlt_Im_Bonus_Pool()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5));
        TestApi.SeedLeitnerPosition(factory, mathId, 0, childId: s.ChildId, cadence: GoalCadence.None);

        var overview = await JsonAsync(await s.Child.GetAsync(Base(s)));
        var ids = overview.GetProperty("exercises").EnumerateArray().Select(e => e.GetProperty("exerciseId").GetInt32()).ToList();
        Assert.DoesNotContain(mathId, ids);
        Assert.Contains(s.VocabId, ids);
    }

    [Fact]
    public async Task Der_Tagesdeckel_Kappt_Die_Bonus_Muenzen()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("1 + 1", 2), ("2 + 2", 4), ("3 + 3", 6));
        using (var scope = factory.Services.CreateScope())
        {
            // 26 bonus coins already earned today by an earlier run, against the configured cap of 30.
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
            db.ExtraTrainingRuns.Add(new ExtraTrainingRun
            {
                ChildId = s.ChildId,
                ExerciseId = s.VocabId,
                Day = DateOnly.FromDateTime(now),
                Pass = 0,
                Form = ExtraForm.Typed,
                Stage = (int)TestStage.FreeText,
                StartedAt = now,
                CompletedAt = now,
                ItemCount = 13,
                CorrectCount = 13,
                CoinsAwarded = 26,
            });
            db.SaveChanges();
        }

        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var answers = run.GetProperty("cards").EnumerateArray().Select(c => new
        {
            itemIndex = c.GetProperty("itemIndex").GetInt32(),
            givenAnswer = (c.GetProperty("prompt").GetString()![0] - '0') * 2 + "",
        }).ToArray();
        var result = await JsonAsync(await s.Child.PostAsJsonAsync(
            $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers }));

        Assert.Equal(3, result.GetProperty("correct").GetInt32());
        Assert.Equal(4, result.GetProperty("coinsAwarded").GetInt32());
        JsonAssert.True(result, "capReached");
        Assert.Equal(30, result.GetProperty("coinsToday").GetInt32());
    }

    [Fact]
    public async Task Der_Vater_Sieht_Das_Bonus_Training_Spielt_Es_Aber_Nicht()
    {
        var s = await SetupAsync();

        var overview = await JsonAsync(await s.Father.GetAsync(Base(s)));
        JsonAssert.True(overview, "unlocked");
        Assert.Equal(HttpStatusCode.Forbidden, (await s.Father.PostAsync($"{Base(s)}/{s.VocabId}/runs", null)).StatusCode);
    }

    [Fact]
    public async Task Eine_Doppelte_Abgabe_Bucht_Nur_Einmal()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5));
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var url = $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit";
        var body = new { answers = new[] { new { itemIndex = 0, givenAnswer = "5" } } };

        // Two submits of the same run at once (double tap): one wins, the other loses with a conflict.
        // Statement of intent, not a red probe for the ordering: loading the child AFTER the run (the review finding this
        // guards) stayed green here 3 of 3 times - the in-process server does not interleave the two requests. The test
        // holds the outcome; the ordering itself is held by the comment at the top of ExtraTrainingService.SubmitAsync.
        var both = await Task.WhenAll(s.Child.PostAsJsonAsync(url, body), s.Child.PostAsJsonAsync(url, body));

        Assert.Single(both, r => r.IsSuccessStatusCode);
        Assert.Single(both, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(2, CoinsBooked(s.ChildId));
    }

    [Fact]
    public async Task Eine_Aufgesparte_Runde_Zahlt_Nicht_Waehrend_Die_Pflicht_Offen_Ist()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5));
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));

        // A new daily duty arrives before the run is handed in: the run started on an open day must wait for it.
        var otherVocab = await TestApi.CreateVocabExerciseAsync(s.Father);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var plan = db.StudyPlans.Single(p => p.ChildId == s.ChildId);
            db.PlanPositions.Add(new PlanPosition
            {
                StudyPlanId = plan.Id,
                ExerciseId = otherVocab,
                Order = 1,
                Stage = (int)TestStage.FreeText,
                Cadence = GoalCadence.Daily,
                PointsGoalMet = 20,
            });
            db.SaveChanges();
        }

        await AssertProblemAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "5" } } }), HttpStatusCode.Conflict, "extra_training_locked");
        Assert.Equal(0, CoinsBooked(s.ChildId));
    }

    [Fact]
    public async Task Eine_Fremde_Runde_Ist_Unbekannt()
    {
        var owner = await SetupAsync();
        var other = await SetupAsync();
        var mathId = await AddArithmeticAsync(owner, ("2 + 3", 5));
        var run = await JsonAsync(await owner.Child.PostAsync($"{Base(owner)}/{mathId}/runs", null));
        var body = new { answers = new[] { new { itemIndex = 0, givenAnswer = "5" } } };

        // Under the other child's own route: the run belongs to someone else.
        Assert.Equal(HttpStatusCode.NotFound, (await other.Child.PostAsJsonAsync(
            $"{Base(other)}/runs/{run.GetProperty("runId").GetInt32()}/submit", body)).StatusCode);
        Assert.Equal(0, CoinsBooked(owner.ChildId));
    }
}
