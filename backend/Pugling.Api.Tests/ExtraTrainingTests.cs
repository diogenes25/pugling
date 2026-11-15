using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The bonus training: extra exercises on the current topic that pay coins beyond the day's duty - but only after it,
/// only for checked hits, halved by every further run of the same exercise on the same day (B-538; vocabulary also climbs
/// a form per run) and within a daily cap.
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
        // The form climbs per completed run, whatever the day; the halving (B-538) counts the runs of the day only. Day one
        // ends after three runs (a fourth would be worth 0 % and does not start): the first choice run is the third of the
        // day and pays 4 × 1 × 25 % = 1. Day two starts at 100 % again: choice 4, then typed 4 × 3 × 50 % = 6 and 25 % = 3.
        var expected = new[]
        {
            ("SelfAssess", 0, 0), ("SelfAssess", 0, 0), ("Choice", 1, 0),
            ("Choice", 4, 1), ("Typed", 6, 1), ("Typed", 3, 1),
        };

        var total = 0;
        factory.Clock.FreezeNow();
        var advanced = false;
        try
        {
            foreach (var (form, coins, day) in expected)
            {
                if (day == 1 && !advanced)
                {
                    factory.Clock.Advance(TimeSpan.FromDays(1));
                    advanced = true;
                }
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
        }
        finally
        {
            // Rewind only what was advanced: a failure on day one must not leave the shared clock a day early.
            if (advanced) factory.Clock.Advance(TimeSpan.FromDays(-1));
        }

        Assert.Equal(total, CoinsBooked(s.ChildId));
    }

    /// <summary>
    /// B-538 AK 3, 4, 6 and 2: a non-vocabulary exercise stays in the pool after its run and pays 100, 50, 25 % of its typed
    /// hits (3 coins each) on runs one to three of the day; the fourth does not start (worth 0 today), and the next day the
    /// first run pays in full again. Red when the factor is fixed at 1 (6, 6, 6), when a played exercise is used up (the
    /// second start refused), when the pool does not report the next run's worth, or when the halving outlives the day.
    /// </summary>
    [Fact]
    public async Task Eine_Uebung_Ohne_Vokabeln_Bleibt_Im_Vorrat_Und_Halbiert_Sich_Je_Lauf()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5), ("4 × 4", 16));
        object[] Right(JsonElement run) =>
        [
            .. run.GetProperty("cards").EnumerateArray().Select(c => (object)new
            {
                itemIndex = c.GetProperty("itemIndex").GetInt32(),
                givenAnswer = c.GetProperty("prompt").GetString() == "2 + 3" ? "5" : "16",
            }),
        ];

        var first = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var runId = first.GetProperty("runId").GetInt32();
        Assert.Equal("Typed", first.GetProperty("form").GetString());
        // Leaving and coming back resumes the same run instead of handing out a fresh one.
        Assert.Equal(runId, (await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null))).GetProperty("runId").GetInt32());
        Assert.Equal(100, PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId).GetProperty("nextRunPercent").GetInt32());

        var paid = new List<int>();
        var percents = new List<int>();
        var started = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
            started.Add(run.GetProperty("runPercent").GetInt32());
            var result = await JsonAsync(await s.Child.PostAsJsonAsync(
                $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers = Right(run) }));
            Assert.Equal(2, result.GetProperty("correct").GetInt32());
            paid.Add(result.GetProperty("coinsAwarded").GetInt32());
            percents.Add(PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId).GetProperty("nextRunPercent").GetInt32());
        }
        Assert.Equal([6, 3, 1], paid);
        Assert.Equal([50, 25, 0], percents);
        // The run tells the child what it is worth before the first card (review finding: the hint promised 100 %).
        Assert.Equal([100, 50, 25], started);

        var entry = PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId);
        JsonAssert.True(entry, "done");
        // Played again - but it does not climb a form, so it still waits at a full cap (B-543).
        JsonAssert.False(entry, "repeatable");
        await AssertProblemAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null),
            HttpStatusCode.Conflict, "extra_exercise_done");
        Assert.Equal(10, CoinsBooked(s.ChildId));

        factory.Clock.FreezeNow();
        factory.Clock.Advance(TimeSpan.FromDays(1));
        try
        {
            Assert.Equal(100, PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId).GetProperty("nextRunPercent").GetInt32());
            var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
            var result = await JsonAsync(await s.Child.PostAsJsonAsync(
                $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers = Right(run) }));
            Assert.Equal(6, result.GetProperty("coinsAwarded").GetInt32());
        }
        finally
        {
            factory.Clock.Advance(TimeSpan.FromDays(-1));
        }
    }

    /// <summary>
    /// B-538, review finding: the halving counts the runs of the day the run is handed in, not of the day it started. One
    /// run completed on day one, a second started on day one (worth 50 % then) and handed in on day two pays 100 %. Red
    /// when the count reads the run's start day.
    /// </summary>
    [Fact]
    public async Task Ein_Am_Folgetag_Abgegebener_Lauf_Zaehlt_Die_Laeufe_Dieses_Tages()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5), ("4 × 4", 16));
        object[] Right(JsonElement run) =>
        [
            .. run.GetProperty("cards").EnumerateArray().Select(c => (object)new
            {
                itemIndex = c.GetProperty("itemIndex").GetInt32(),
                givenAnswer = c.GetProperty("prompt").GetString() == "2 + 3" ? "5" : "16",
            }),
        ];
        factory.Clock.FreezeNow();
        var first = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        await JsonAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{first.GetProperty("runId").GetInt32()}/submit",
            new { answers = Right(first) }));
        var second = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        Assert.Equal(50, second.GetProperty("runPercent").GetInt32());

        factory.Clock.Advance(TimeSpan.FromDays(1));
        try
        {
            var result = await JsonAsync(await s.Child.PostAsJsonAsync(
                $"{Base(s)}/runs/{second.GetProperty("runId").GetInt32()}/submit", new { answers = Right(second) }));
            Assert.Equal(6, result.GetProperty("coinsAwarded").GetInt32());
        }
        finally
        {
            factory.Clock.Advance(TimeSpan.FromDays(-1));
        }
    }

    /// <summary>
    /// B-538, stakeholder decision on review: vocabulary worth 0 % today still starts and pays nothing - as it may at a full
    /// cap (B-543, decision 5). Only a fixed-form exercise is refused, because its run would show the solutions. Red when
    /// the "done" lock covers vocabulary too.
    /// </summary>
    [Fact]
    public async Task Vokabeln_Bei_Null_Prozent_Duerfen_Weiter_Ohne_Muenzen()
    {
        var s = await SetupAsync();
        for (var i = 0; i < 3; i++)
        {
            var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{s.VocabId}/runs", null));
            await JsonAsync(await s.Child.PostAsJsonAsync(
                $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers = RightVocabAnswers(run) }));
        }
        var entry = PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), s.VocabId);
        Assert.Equal(0, entry.GetProperty("nextRunPercent").GetInt32());

        var fourth = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{s.VocabId}/runs", null));
        Assert.Equal(0, fourth.GetProperty("runPercent").GetInt32());
        var result = await JsonAsync(await s.Child.PostAsJsonAsync(
            $"{Base(s)}/runs/{fourth.GetProperty("runId").GetInt32()}/submit", new { answers = RightVocabAnswers(fourth) }));
        Assert.Equal(0, result.GetProperty("coinsAwarded").GetInt32());
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
            // 86 bonus coins already earned today by an earlier run, against the configured cap of 90 (B-538).
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
                ItemCount = 29,
                CorrectCount = 29,
                CoinsAwarded = 86,
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
        Assert.Equal(90, result.GetProperty("coinsToday").GetInt32());
    }

    // Books bonus coins as earned today by an earlier vocabulary run, so the cap can be filled without playing to it.
    private void SeedCoinsToday(Setup s, int coins)
    {
        using var scope = factory.Services.CreateScope();
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
            ItemCount = coins / 3,
            CorrectCount = coins / 3,
            CoinsAwarded = coins,
        });
        db.SaveChanges();
    }

    // Answers every "n + n" card of an arithmetic exercise right.
    private static object[] RightMathAnswers(JsonElement run) =>
    [
        .. run.GetProperty("cards").EnumerateArray().Select(c => (object)new
        {
            itemIndex = c.GetProperty("itemIndex").GetInt32(),
            givenAnswer = (c.GetProperty("prompt").GetString()![0] - '0') * 2 + "",
        }),
    ];

    // B-543 AK 1: at a full cap a one-shot exercise does not start, so it is not burnt for zero coins. Red if StartAsync
    // has no cap check (then 200 and the exercise is used up).
    [Fact]
    public async Task Bei_Vollem_Deckel_Startet_Eine_Einmal_Uebung_Nicht()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("1 + 1", 2), ("2 + 2", 4));
        SeedCoinsToday(s, 90);

        await AssertProblemAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null),
            HttpStatusCode.Conflict, "extra_cap_reached");
        var entry = PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId);
        JsonAssert.False(entry, "done");
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("openRunId").ValueKind);
    }

    // B-543 AK 2: the child's own cap of 0 means "pays nothing", so no fixed-form exercise starts. Red if the check reads
    // only the server default (90) or compares with > instead of >=.
    [Fact]
    public async Task Bei_Kind_Deckel_Null_Startet_Keine_Uebung_Fester_Form()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("1 + 1", 2));
        var patched = await JsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}",
            new { bonusDailyCoinCap = 0 }));
        Assert.Equal(0, patched.GetProperty("bonusDailyCoinCap").GetInt32());

        await AssertProblemAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null),
            HttpStatusCode.Conflict, "extra_cap_reached");
    }

    // B-543 AK 3: an open one-shot run is not resumed at a full cap either. Red if the check sits only before a new run
    // is created.
    [Fact]
    public async Task Eine_Offene_Einmal_Runde_Wird_Bei_Vollem_Deckel_Nicht_Fortgesetzt()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("1 + 1", 2));
        await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        SeedCoinsToday(s, 90);

        await AssertProblemAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null),
            HttpStatusCode.Conflict, "extra_cap_reached");
    }

    // B-543 AK 4: vocabulary may be played again and burns nothing, so it still starts at a full cap. Red if the check
    // does not tell repeatable exercises apart.
    [Fact]
    public async Task Eine_Vokabeluebung_Startet_Auch_Bei_Vollem_Deckel()
    {
        var s = await SetupAsync();
        SeedCoinsToday(s, 90);

        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{s.VocabId}/runs", null));
        Assert.Equal(s.VocabId, run.GetProperty("exerciseId").GetInt32());
    }

    // B-543 AK 5: a cap filled between start and submit leaves the one-shot run open and unpaid, without its solutions;
    // the same submit tomorrow pays in full. Red if the submit still caps and uses up (200 with 0 coins) or checks the
    // cap only after grading.
    [Fact]
    public async Task Eine_Bei_Vollem_Deckel_Abgegebene_Einmal_Runde_Zahlt_Morgen()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("1 + 1", 2), ("2 + 2", 4));
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var runId = run.GetProperty("runId").GetInt32();
        var body = new { answers = RightMathAnswers(run) };
        SeedCoinsToday(s, 90);

        var refused = await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{runId}/submit", body);
        var text = await refused.Content.ReadAsStringAsync();
        await AssertProblemAsync(refused, HttpStatusCode.Conflict, "extra_cap_reached");
        Assert.DoesNotContain("expected", text, StringComparison.OrdinalIgnoreCase);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Null(db.ExtraTrainingRuns.Single(r => r.Id == runId).CompletedAt);
        }
        Assert.Equal(0, CoinsBooked(s.ChildId));

        factory.Clock.FreezeNow();
        factory.Clock.Advance(TimeSpan.FromDays(1));
        try
        {
            var result = await JsonAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{runId}/submit", body));
            Assert.Equal(2, result.GetProperty("correct").GetInt32());
            Assert.Equal(6, result.GetProperty("coinsAwarded").GetInt32());
            Assert.Equal(6, CoinsBooked(s.ChildId));
        }
        finally
        {
            factory.Clock.Advance(TimeSpan.FromDays(-1));
        }
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
        Assert.Equal(3, CoinsBooked(s.ChildId));
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

    [Fact]
    public async Task Der_Vater_Setzt_Den_Tagesdeckel_Je_Kind()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("1 + 1", 2), ("2 + 2", 4), ("3 + 3", 6));
        var child = $"/api/v1/supervisor/children/{s.ChildId}";

        // Out of range is refused rather than clamped: an extra zero must not open the shop wide.
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Father.PatchAsJsonAsync(child, new { bonusDailyCoinCap = 5000 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Father.PatchAsJsonAsync(child, new { bonusDailyCoinCap = -1 })).StatusCode);

        var patched = await JsonAsync(await s.Father.PatchAsJsonAsync(child, new { bonusDailyCoinCap = 3 }));
        Assert.Equal(3, patched.GetProperty("bonusDailyCoinCap").GetInt32());
        Assert.Equal(3, (await JsonAsync(await s.Child.GetAsync(Base(s)))).GetProperty("dailyCoinCap").GetInt32());

        // Three typed hits would pay 9; the child's own cap of 3 cuts it, not the server default of 90.
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var answers = run.GetProperty("cards").EnumerateArray().Select(c => new
        {
            itemIndex = c.GetProperty("itemIndex").GetInt32(),
            givenAnswer = (c.GetProperty("prompt").GetString()![0] - '0') * 2 + "",
        }).ToArray();
        var result = await JsonAsync(await s.Child.PostAsJsonAsync(
            $"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers }));
        Assert.Equal(3, result.GetProperty("coinsAwarded").GetInt32());
        JsonAssert.True(result, "capReached");

        // Back to the default.
        var cleared = await JsonAsync(await s.Father.PatchAsJsonAsync(child, new { clearBonusDailyCoinCap = true }));
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("bonusDailyCoinCap").ValueKind);
        Assert.Equal(90, (await JsonAsync(await s.Child.GetAsync(Base(s)))).GetProperty("dailyCoinCap").GetInt32());
    }

    private void EditExercise(int exerciseId, Action<Exercise> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db.Exercises.Single(e => e.Id == exerciseId));
        db.SaveChanges();
    }

    private static object[] RightMathAnswers(JsonElement run, params (string Prompt, string Answer)[] key) =>
    [
        .. run.GetProperty("cards").EnumerateArray().Select(c => (object)new
        {
            itemIndex = c.GetProperty("itemIndex").GetInt32(),
            givenAnswer = key.Single(k => k.Prompt == c.GetProperty("prompt").GetString()).Answer,
        }),
    ];

    // B-545 AK 1: the author corrects a solution between start and submit - the run is discarded unpaid with 409, and
    // the one-shot exercise is open again. Red when the submit grades without comparing the fingerprint (200, paid
    // against a key the questions did not come from).
    [Fact]
    public async Task Eine_Geaenderte_Uebung_Verwirft_Die_Runde_Bei_Der_Abgabe_Unbezahlt()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5), ("4 × 4", 16));
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));

        EditExercise(mathId, e => e.ConfigJson = e.ConfigJson.Replace("16", "17", StringComparison.Ordinal));

        await AssertProblemAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit",
            new { answers = RightMathAnswers(run, ("2 + 3", "5"), ("4 × 4", "16")) }), HttpStatusCode.Conflict, "extra_run_stale");
        Assert.Equal(0, CoinsBooked(s.ChildId));
        var entry = PoolEntry(await JsonAsync(await s.Child.GetAsync(Base(s))), mathId);
        JsonAssert.False(entry, "done");
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("openRunId").ValueKind);
    }

    // B-545 AK 2: resuming an open run of a changed exercise starts a fresh one from the new content. Red when the start
    // resumes the old run without comparing (same run id, two cards).
    [Fact]
    public async Task Eine_Geaenderte_Uebung_Startet_Beim_Fortsetzen_Eine_Neue_Runde()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5), ("4 × 4", 16));
        var first = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));

        EditExercise(mathId, e => e.ConfigJson = e.ConfigJson.Replace("16", "17", StringComparison.Ordinal));

        var again = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        Assert.NotEqual(first.GetProperty("runId").GetInt32(), again.GetProperty("runId").GetInt32());
        var result = await JsonAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{again.GetProperty("runId").GetInt32()}/submit",
            new { answers = RightMathAnswers(again, ("2 + 3", "5"), ("4 × 4", "17")) }));
        Assert.Equal(2, result.GetProperty("correct").GetInt32());
    }

    // B-545 AK 3: a field outside grading (the item's hint) changes - the run is graded as usual. Red when the
    // fingerprint takes the hint (or image/audio) in: then the submit answers 409.
    [Fact]
    public async Task Ein_Geaenderter_Hinweis_Kostet_Die_Runde_Nicht()
    {
        var s = await SetupAsync();
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{s.VocabId}/runs", null));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.ExerciseItems.First(i => i.ExerciseId == s.VocabId).Hint = "ein Tier";
            db.SaveChanges();
        }

        var result = await JsonAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{run.GetProperty("runId").GetInt32()}/submit",
            new { answers = RightVocabAnswers(run) }));
        Assert.Equal(Words.Length, result.GetProperty("correct").GetInt32());
    }

    // B-545 AK 5: a run from before the column (no fingerprint) is graded as before, against the current content. Red
    // when a missing fingerprint counts as "changed" (409).
    [Fact]
    public async Task Eine_Bestandsrunde_Ohne_Fingerabdruck_Wird_Wie_Bisher_Gewertet()
    {
        var s = await SetupAsync();
        var mathId = await AddArithmeticAsync(s, ("2 + 3", 5), ("4 × 4", 16));
        var run = await JsonAsync(await s.Child.PostAsync($"{Base(s)}/{mathId}/runs", null));
        var runId = run.GetProperty("runId").GetInt32();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.ExtraTrainingRuns.Single(r => r.Id == runId).ContentHash = null;
            db.SaveChanges();
        }
        EditExercise(mathId, e => e.ConfigJson = e.ConfigJson.Replace("16", "17", StringComparison.Ordinal));

        var result = await JsonAsync(await s.Child.PostAsJsonAsync($"{Base(s)}/runs/{runId}/submit",
            new { answers = RightMathAnswers(run, ("2 + 3", "5"), ("4 × 4", "17")) }));
        Assert.Equal(2, result.GetProperty("correct").GetInt32());
    }
}
