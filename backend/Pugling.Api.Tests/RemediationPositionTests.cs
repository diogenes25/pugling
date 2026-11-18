using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-635: a remediation position ("Nacharbeit"). Each round plays the tasks still open, in order, each as free text; the
/// child makes the current card easier one rung at a time, and a task is done only when typed freely right. Case by case
/// the change that turns it red is named.
/// </summary>
public class RemediationPositionTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Three tasks of a failed test: a phrase, a single word and a sentence word. Gap 1 has its own distractors, so every
    // rung of the ladder is a different form for it.
    private async Task<int> NacharbeitAsync(HttpClient father)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Nacharbeit") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze",
            new
            {
                title = TestApi.UniqueName("Nacharbeit Unit 1"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    text = "{{1}} | I fell {{2}} during the film. | Call me {{3}} you can.",
                    gaps = new object[]
                    {
                        new { index = 1, answer = "eastwards", prompt = "Richtung Osten", distractors = new[] { "westwards", "northwards" } },
                        new { index = 2, answer = "asleep", prompt = "Während des Films schlief ich ein." },
                        new { index = 3, answer = "as soon as", prompt = "Ruf mich an, sobald du kannst." },
                    },
                },
            }));
    }

    // The position as the father sets it up: seeded, then switched to remediation through the API (AK 1).
    private async Task<(HttpClient Father, int PlanId, int PositionId)> RemediationPositionAsync(bool useLeitner = false)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await NacharbeitAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.TranslationWordBank,
            useLeitner: useLeitner);
        var patch = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}",
            new { remediation = true });
        patch.EnsureSuccessStatusCode();
        return (father, planId, positionId);
    }

    private static async Task<(int SessionId, JsonElement Session)> StartAsync(HttpClient child, int planId, int positionId)
    {
        var response = await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (session.GetProperty("id").GetInt32(), session);
    }

    private static async Task<JsonElement> NextCardAsync(HttpClient child, int planId, int positionId, int sessionId) =>
        (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next"))
            .GetProperty("card");

    private static Task<HttpResponseMessage> EasierAsync(HttpClient child, int planId, int positionId, int sessionId,
        int itemIndex) =>
        child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/easier", new { itemIndex });

    private static async Task<JsonElement> ReviewAsync(HttpClient child, int planId, int positionId, int sessionId,
        int itemIndex, string answer) =>
        await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex, givenAnswer: answer))
            .Content.ReadFromJsonAsync<JsonElement>();

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private PositionItemProgress? Progress(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().PositionItemProgress.AsNoTracking()
            .SingleOrDefault(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
    }

    private int PointsEntries()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ChildPointsEntries.Count(e => e.ChildId == 1);
    }

    /// <summary>
    /// AK 1: the father switches remediation on through the position, and the response says so; a type without an ease
    /// ladder is refused. Red if the PATCH ignores the field (false in the response) or lets a vocabulary position on.
    /// </summary>
    [Fact]
    public async Task Vater_schaltet_Nacharbeit_ein_nur_fuer_einen_Typ_mit_Leiter()
    {
        var (father, planId, positionId) = await RemediationPositionAsync();
        var position = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}");
        JsonAssert.True(position, "remediation");

        var vocab = await TestApi.CreateVocabExerciseAsync(father, ("Haus", "house"));
        var (vocabPlan, vocabPos) = TestApi.SeedLeitnerPosition(factory, vocab, (int)TestStage.FreeText);
        var refused = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{vocabPlan}/positions/{vocabPos}",
            new { remediation = true });
        await AssertProblemAsync(refused, HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>
    /// AK 2: each task starts as free text; "easier" goes exactly one rung down - boxes, scramble, choice - and at the
    /// bottom it is refused. Red if the start stage is the day's stage (word bank), if a press skips or repeats a rung, or
    /// if the bottom lets a further press through.
    /// </summary>
    [Fact]
    public async Task Leichter_geht_eine_Stufe_je_Druck_bis_zur_Auswahl()
    {
        var (_, planId, positionId) = await RemediationPositionAsync();
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);

        var card = await NextCardAsync(child, planId, positionId, sessionId);
        Assert.Equal(0, card.GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)ClozeStage.TranslationFreeText, card.GetProperty("stage").GetInt32());
        JsonAssert.True(card, "canEase");
        JsonAssert.False(card, "eased");

        var stages = new List<int>();
        for (var press = 0; press < 3; press++)
        {
            var eased = await (await EasierAsync(child, planId, positionId, sessionId, 0)).Content.ReadFromJsonAsync<JsonElement>();
            stages.Add(eased.GetProperty("stage").GetInt32());
            JsonAssert.True(eased, "eased");
        }
        Assert.Equal([(int)ClozeStage.LetterBoxes, (int)ClozeStage.LetterScramble, (int)ClozeStage.Choice], stages);
        // The card the round now serves is the eased one - the stage lives on the session, not in the client.
        var again = await NextCardAsync(child, planId, positionId, sessionId);
        Assert.Equal((int)ClozeStage.Choice, again.GetProperty("stage").GetInt32());
        JsonAssert.False(again, "canEase");

        await AssertProblemAsync(await EasierAsync(child, planId, positionId, sessionId, 0),
            HttpStatusCode.Conflict, "cannot_ease");
    }

    /// <summary>
    /// Only the current card can be eased. Red if a press may name a card the round has not reached - it would keep the
    /// eased stage for a later card while the current one stays free.
    /// </summary>
    [Fact]
    public async Task Nur_die_aktuelle_Karte_wird_leichter()
    {
        var (_, planId, positionId) = await RemediationPositionAsync();
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);

        await AssertProblemAsync(await EasierAsync(child, planId, positionId, sessionId, 2),
            HttpStatusCode.Conflict, "cannot_ease");
    }

    /// <summary>
    /// AK 3 + AK 5 (anti-cheat) + AK 7: an eased card answered right - even sent as free text - shows right and the
    /// solution, pays nothing and leaves the task open; a free answer right finishes it. Red if Review grades the client's
    /// form instead of the session's stage (the eased task would be done) or if an eased hit pays. Before the build: no
    /// `easier`, no `taskDone`, no done date - red at the first press.
    /// </summary>
    [Fact]
    public async Task Leichter_geschaltet_erledigt_nie_frei_getippt_schon()
    {
        var (_, planId, positionId) = await RemediationPositionAsync(useLeitner: true);
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        var before = PointsEntries();

        (await EasierAsync(child, planId, positionId, sessionId, 0)).EnsureSuccessStatusCode();
        Assert.Equal(before, PointsEntries());
        var eased = await ReviewAsync(child, planId, positionId, sessionId, 0, "eastwards");

        JsonAssert.True(eased, "wasCorrect");
        Assert.Equal("eastwards", eased.GetProperty("expected").GetString());
        JsonAssert.False(eased, "taskDone");
        Assert.Equal(0, eased.GetProperty("awarded").GetInt32());
        Assert.Equal(before, PointsEntries());
        Assert.Null(Progress(positionId, 0)!.RemediationDoneAt);

        var free = await ReviewAsync(child, planId, positionId, sessionId, 1, "asleep");
        JsonAssert.True(free, "taskDone");
        Assert.NotNull(Progress(positionId, 1)!.RemediationDoneAt);
    }

    /// <summary>
    /// AK 4 + AK 6: the next round holds only the tasks still open - the eased one and the wrong one, not the one typed
    /// freely - and the counters per stage stay. Red if the round is built from the Leitner due list (all three again), if
    /// the done date is not read, or if a solution is not counted at its stage.
    /// </summary>
    [Fact]
    public async Task Naechste_Runde_bringt_nur_die_offenen_Aufgaben()
    {
        var (_, planId, positionId) = await RemediationPositionAsync();
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, first) = await StartAsync(child, planId, positionId);
        Assert.Equal(3, first.GetProperty("total").GetInt32());

        await ReviewAsync(child, planId, positionId, sessionId, 0, "eastwards");
        (await EasierAsync(child, planId, positionId, sessionId, 1)).EnsureSuccessStatusCode();
        await ReviewAsync(child, planId, positionId, sessionId, 1, "asleep");
        await ReviewAsync(child, planId, positionId, sessionId, 2, "as long as");

        var (secondId, second) = await StartAsync(child, planId, positionId);
        Assert.Equal(2, second.GetProperty("total").GetInt32());
        var state = second.GetProperty("remediation");
        Assert.Equal(2, state.GetProperty("round").GetInt32());
        Assert.Equal(1, state.GetProperty("done").GetInt32());
        Assert.Equal(3, state.GetProperty("total").GetInt32());
        Assert.Equal(1, (await NextCardAsync(child, planId, positionId, secondId)).GetProperty("itemIndex").GetInt32());

        Assert.Equal(new Dictionary<int, int> { [(int)ClozeStage.TranslationFreeText] = 1 }, Progress(positionId, 0)!.SolvedByStage);
        Assert.Equal(new Dictionary<int, int> { [(int)ClozeStage.LetterBoxes] = 1 }, Progress(positionId, 1)!.SolvedByStage);
        Assert.Null(Progress(positionId, 2)?.SolvedByStage);
    }

    /// <summary>
    /// Guard: a position without remediation plays as before - its day stage, no ease flag - and refuses "easier". Red if
    /// the remediation branch fires for every cloze position.
    /// </summary>
    [Fact]
    public async Task Ohne_Nacharbeit_bleibt_alles_wie_es_war()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await NacharbeitAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, session) = await StartAsync(child, planId, positionId);

        var card = await NextCardAsync(child, planId, positionId, sessionId);
        Assert.Equal((int)ClozeStage.FreeText, card.GetProperty("stage").GetInt32());
        JsonAssert.False(card, "canEase");
        Assert.Equal(JsonValueKind.Null, session.GetProperty("remediation").ValueKind);
        await AssertProblemAsync(await EasierAsync(child, planId, positionId, sessionId,
            card.GetProperty("itemIndex").GetInt32()), HttpStatusCode.Conflict, "cannot_ease");
    }

    /// <summary>
    /// AK 2, a rung the card cannot take is skipped (decision 3 of B-643, the same ladder): "doesn't she" cannot be
    /// scrambled (apostrophe), so from the letter boxes the next press goes to the choice, never to a scramble that would
    /// show the boxes again. Red if <c>EasierStage</c> takes the next rung without comparing the form.
    /// </summary>
    [Fact]
    public void Eine_Stufe_ohne_eigene_Form_wird_uebersprungen()
    {
        var type = new Pugling.Api.Exercises.ClozeExerciseType();
        var config = JsonSerializer.Serialize(new
        {
            text = "She likes tea, {{1}}? He is {{2}}. They {{3}}.",
            gaps = new object[]
            {
                new { index = 1, answer = "doesn't she" },
                new { index = 2, answer = "tired" },
                new { index = 3, answer = "left" },
            },
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var items = type.ItemsOf(config);

        Assert.Equal((int)ClozeStage.Choice,
            PositionPlayService.EasierStage(type, config, items, items[0], (int)ClozeStage.LetterBoxes));
        Assert.Equal((int)ClozeStage.LetterScramble,
            PositionPlayService.EasierStage(type, config, items, items[1], (int)ClozeStage.LetterBoxes));
        Assert.Null(PositionPlayService.EasierStage(type, config, items, items[1], (int)ClozeStage.Choice));
    }

    /// <summary>
    /// The manifest says which types can run as remediation (the father's form shows the switch from it), and it says the
    /// same as the rule the server checks: capability "remediation" exactly where the type has an ease ladder. Red if
    /// either side changes alone.
    /// </summary>
    [Fact]
    public void Manifest_nennt_Nacharbeit_genau_wo_eine_Leiter_ist()
    {
        using var scope = factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<Pugling.Api.Exercises.ExerciseTypeRegistry>();
        Assert.All(registry.All, t =>
            Assert.Equal(t.SupportsRemediation && t.EaseLadder.Count > 0, t.Manifest.Capabilities.Contains("remediation")));
        Assert.Contains(registry.All, t => t.SupportsRemediation);
    }

    /// <summary>
    /// Review finding 7 (anti-cheat across rounds): a task made easier, then the round quit and started again - the new
    /// start continues the open round of the day, so the eased card is still eased and a free answer does not finish it;
    /// an answer for a card the round has not reached is refused. Red if Start always opens a new round (the task comes
    /// back as free text and is done) or if Review accepts a card off the cursor.
    /// </summary>
    [Fact]
    public async Task Neustart_setzt_die_offene_Runde_fort_und_die_Hilfe_bleibt()
    {
        var (_, planId, positionId) = await RemediationPositionAsync();
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        (await EasierAsync(child, planId, positionId, sessionId, 0)).EnsureSuccessStatusCode();

        var (againId, _) = await StartAsync(child, planId, positionId);
        Assert.Equal(sessionId, againId);
        var card = await NextCardAsync(child, planId, positionId, againId);
        JsonAssert.True(card, "eased");
        await AssertProblemAsync(await TestApi.PositionReviewAsync(child, planId, positionId, againId, 2, givenAnswer: "as soon as"),
            HttpStatusCode.NotFound, "not_found");

        var answer = await ReviewAsync(child, planId, positionId, againId, 0, "eastwards");
        JsonAssert.False(answer, "taskDone");
        Assert.Null(Progress(positionId, 0)!.RemediationDoneAt);
    }

    /// <summary>
    /// Review finding 8: a single gap without distractors - its choice falls back to letter boxes, so from the scramble
    /// nothing easier is left. Red if <c>EasierStage</c> compares only with the current form (it would offer the choice,
    /// which shows the harder boxes again).
    /// </summary>
    [Fact]
    public void Leichter_fuehrt_nie_zurueck_auf_eine_schon_gezeigte_Form()
    {
        var type = new Pugling.Api.Exercises.ClozeExerciseType();
        var config = JsonSerializer.Serialize(new { text = "{{1}}", gaps = new object[] { new { index = 1, answer = "eastwards" } } },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var items = type.ItemsOf(config);

        Assert.Equal((int)ClozeStage.LetterScramble,
            PositionPlayService.EasierStage(type, config, items, items[0], (int)ClozeStage.LetterBoxes));
        Assert.Null(PositionPlayService.EasierStage(type, config, items, items[0], (int)ClozeStage.LetterScramble));
    }

    /// <summary>
    /// Only the child's own answer finishes a task (B-433): the father playing the round as a preview leaves it open. Red
    /// if the student check is dropped from the done rule.
    /// </summary>
    [Fact]
    public async Task Antwort_des_Vaters_erledigt_nichts()
    {
        var (father, planId, positionId) = await RemediationPositionAsync();
        var (sessionId, _) = await StartAsync(father, planId, positionId);

        var answer = await ReviewAsync(father, planId, positionId, sessionId, 0, "eastwards");

        JsonAssert.True(answer, "wasCorrect");
        JsonAssert.False(answer, "taskDone");
        Assert.Null(Progress(positionId, 0)?.RemediationDoneAt);
    }

    // ---- B-636: "daily, until done" with a completion bonus ---- ----

    // The remediation position as a duty "daily, until done": coins for the round, a penalty for a missed day, a bonus.
    private async Task<(HttpClient Father, int PlanId, int PositionId)> UntilDoneAsync(int bonus = 50, int penalty = 5)
    {
        var (father, planId, positionId) = await RemediationPositionAsync();
        var patch = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}",
            new { cadence = "UntilDone", completionBonus = bonus, penaltyCoins = penalty });
        patch.EnsureSuccessStatusCode();
        return (father, planId, positionId);
    }

    private async Task<T> WithServiceAsync<T>(Func<PuglingDbContext, PositionProgressService, Task<T>> body)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        return await body(sp.GetRequiredService<PuglingDbContext>(), sp.GetRequiredService<PositionProgressService>());
    }

    private DateOnly Today() =>
        DateOnly.FromDateTime(factory.Services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);

    private Task<bool> GoalMetAsync(int positionId, DateOnly day) => WithServiceAsync(async (db, progress) =>
    {
        var pos = await db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan).SingleAsync(p => p.Id == positionId);
        return await progress.IsGoalMetAsync(pos, pos.StudyPlan!, day);
    });

    // Plays one round to the end, answering every card freely with the answer given for its task.
    private static async Task PlayRoundAsync(HttpClient child, int planId, int positionId, params string[] answers)
    {
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        while (true)
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
            if (next.GetProperty("card").ValueKind == JsonValueKind.Null) break;
            var index = next.GetProperty("card").GetProperty("itemIndex").GetInt32();
            await ReviewAsync(child, planId, positionId, sessionId, index, answers[index]);
        }
    }

    /// <summary>
    /// AK 1 + AK 5: the father picks "daily, until done" and the bonus on a remediation position, and the response carries
    /// both; without remediation (on create and on patch) and with a bonus out of range it is refused. Red if the cadence is
    /// accepted on a position nothing can finish, if the bonus is not stored, or if the range check is dropped.
    /// </summary>
    [Fact]
    public async Task Bis_erledigt_nur_mit_Nacharbeit_und_Bonus_einstellbar()
    {
        var (father, planId, positionId) = await UntilDoneAsync(bonus: 40);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}";
        var position = await father.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal("UntilDone", position.GetProperty("cadence").GetString());
        Assert.Equal(40, position.GetProperty("completionBonus").GetInt32());

        await AssertProblemAsync(await father.PatchAsJsonAsync(url, new { remediation = false }),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await father.PatchAsJsonAsync(url, new { completionBonus = 5000 }),
            HttpStatusCode.BadRequest, "validation_error");

        var exerciseId = position.GetProperty("exerciseId").GetInt32();
        await AssertProblemAsync(await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
                new { exerciseId, cadence = "UntilDone" }),
            HttpStatusCode.BadRequest, "validation_error");

        // Created in one step, with remediation: the bonus is stored on create as well.
        var created = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "UntilDone", remediation = true, completionBonus = 15 });
        created.EnsureSuccessStatusCode();
        Assert.Equal(15, (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("completionBonus").GetInt32());
    }

    /// <summary>
    /// The reward lock (daily box, bonus training) treats "until done" as a daily duty like the server's day view: an open
    /// remediation gates the box although a met weekly goal stands beside it. Red if only <c>Daily</c> counts as daily
    /// (the weekly goal would gate alone and the box open with the remediation undone).
    /// </summary>
    [Fact]
    public void Nacharbeit_bis_erledigt_sperrt_die_Belohnungen_wie_ein_Tagesziel()
    {
        static PositionStatus Status(int id, GoalCadence cadence, bool met) =>
            new(id, 1, "x", "Cloze", "cloze", 0, cadence, ExerciseCheckMode.StudyPlanTest, false, true, met, met ? 1 : 0,
                0, 3, 20, 0, 0, 1, "", "", null, 1, "", "", 0);

        var gating = PositionProgressService.RewardObligations(
            [Status(1, GoalCadence.UntilDone, met: false), Status(2, GoalCadence.Weekly, met: true)]);
        Assert.Equal([1], gating.Select(s => s.PositionId));
    }

    /// <summary>
    /// AK 2: a round played through all its tasks meets the day's duty although tasks stay open; a round broken off does
    /// not. Red if the until-done branch falls through to the test rule (a cloze needs a passed test - never met) or to
    /// the 80 % threshold (the broken-off round at 2 of 3 would count).
    /// </summary>
    [Fact]
    public async Task Ganz_gespielte_Runde_erfuellt_die_Tagespflicht_auch_mit_offenen_Aufgaben()
    {
        var (father, planId, positionId) = await UntilDoneAsync();
        // A low threshold the round must ignore: at 50 % the broken-off round would count, and its bar would look full.
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}",
            new { goalThreshold = 50 })).EnsureSuccessStatusCode();
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        await ReviewAsync(child, planId, positionId, sessionId, 0, "westwards");
        await ReviewAsync(child, planId, positionId, sessionId, 1, "asleep");
        Assert.False(await GoalMetAsync(positionId, Today()));
        // The bar shows the part of the whole round (2 of 3), not 0 like a test goal and not full like the threshold.
        var progress = await WithServiceAsync(async (db, service) =>
        {
            var plan = await db.StudyPlans.SingleAsync(p => p.Id == planId);
            return (await service.ComputeDayAsync(plan, Today())).Positions.Single(s => s.PositionId == positionId).GoalProgress;
        });
        Assert.Equal(2 / 3.0, progress, 3);

        await ReviewAsync(child, planId, positionId, sessionId, 2, "as long as");
        Assert.True(await GoalMetAsync(positionId, Today()));
        Assert.Null(Progress(positionId, 0)?.RemediationDoneAt);

        // Ending the round books the day's goal points as the period of this cadence (review finding Y3).
        (await child.PostAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/end", null)).EnsureSuccessStatusCode();
        var reward = await WithServiceAsync((db, _) =>
            db.PositionGoalRewards.SingleAsync(r => r.PlanPositionId == positionId));
        Assert.Equal((GoalCadence.UntilDone, Today()), (reward.Cadence, reward.Day));
    }

    /// <summary>
    /// AK 3: a day without a round costs the penalty; once the last task is done, no later day costs anything, even when
    /// the settlement catches up only days later. Red if the settlement ignores the end (days after it are charged) or if
    /// the until-done duty is never charged at all.
    /// </summary>
    [Fact]
    public async Task Malus_fuer_den_Tag_ohne_Runde_nicht_nach_dem_Ende()
    {
        var (_, planId, positionId) = await UntilDoneAsync(penalty: 7);
        var today = Today();
        var child = await TestApi.ChildAsync(factory);

        // Only one card today (B-535: the first answer starts the penalty clock), so the round is not through and
        // tomorrow's settlement charges today.
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        await ReviewAsync(child, planId, positionId, sessionId, 0, "westwards");
        await WithServiceAsync((_, progress) => progress.SettleClosedPeriodsAsync(1, today.AddDays(1), CancellationToken.None));
        Assert.Equal([today], await PenaltyDaysAsync(positionId));

        // The open round continues behind the missed card; the next round brings it back and finishes it.
        await PlayRoundAsync(child, planId, positionId, "eastwards", "asleep", "as soon as");
        await PlayRoundAsync(child, planId, positionId, "eastwards", "asleep", "as soon as");
        await WithServiceAsync(async (db, _) =>
        {
            var pos = await db.PlanPositions.AsNoTracking().SingleAsync(p => p.Id == positionId);
            Assert.Equal(today, pos.CompletedOn);
            // Pretend the end lay in the past, so the days after it are closed periods for the settlement.
            await db.PlanPositions.Where(p => p.Id == positionId)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.CompletedOn, today.AddDays(-3)));
            await db.PositionGoalPenalties.Where(p => p.PlanPositionId == positionId).ExecuteDeleteAsync();
            return 0;
        });

        await WithServiceAsync((_, progress) => progress.SettleClosedPeriodsAsync(1, today.AddDays(5), CancellationToken.None));
        Assert.Empty(await PenaltyDaysAsync(positionId));
    }

    private Task<List<DateOnly>> PenaltyDaysAsync(int positionId) => WithServiceAsync((db, _) =>
        db.PositionGoalPenalties.Where(p => p.PlanPositionId == positionId).Select(p => p.PeriodStart).ToListAsync());

    /// <summary>
    /// AK 4: the answer that finishes the last task books the bonus once, with its own kind, and says so; a second run of
    /// the completion books nothing. On the last day the position still shows as met, from the next day on it has left the
    /// mission. Red if the completion is not triggered by the last answer, if the bonus books twice, if it is booked as a
    /// goal reward, or if the ended position stays in tomorrow's mission (or leaves today's early).
    /// </summary>
    [Fact]
    public async Task Letzte_Aufgabe_bucht_den_Bonus_einmal_und_beendet_die_Pflicht()
    {
        var (_, planId, positionId) = await UntilDoneAsync(bonus: 30);
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        var first = await ReviewAsync(child, planId, positionId, sessionId, 0, "eastwards");
        Assert.Equal(0, first.GetProperty("completionBonus").GetInt32());
        await ReviewAsync(child, planId, positionId, sessionId, 1, "asleep");
        var last = await ReviewAsync(child, planId, positionId, sessionId, 2, "as soon as");
        Assert.Equal(30, last.GetProperty("completionBonus").GetInt32());

        var again = await WithServiceAsync(async (db, progress) =>
        {
            var pos = await db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan).SingleAsync(p => p.Id == positionId);
            pos.CompletedOn = null; // the fast path off: only the unique index may stop the second booking
            return await progress.CompleteRemediationIfDoneAsync(pos, pos.StudyPlan!, Today(), CancellationToken.None);
        });
        Assert.Equal(0, again);
        var bonuses = await WithServiceAsync((db, _) => db.ChildPointsEntries
            .Where(e => e.ChildId == 1 && e.Kind == PointKind.RemediationBonus && e.Amount == 30).CountAsync());
        Assert.Equal(1, bonuses);

        var (todayIn, tomorrowIn) = await WithServiceAsync(async (db, progress) =>
        {
            var plan = await db.StudyPlans.SingleAsync(p => p.Id == planId);
            var todayView = await progress.ComputeDayAsync(plan, Today());
            var tomorrowView = await progress.ComputeDayAsync(plan, Today().AddDays(1));
            return (todayView.Positions.SingleOrDefault(s => s.PositionId == positionId),
                tomorrowView.Positions.SingleOrDefault(s => s.PositionId == positionId));
        });
        Assert.NotNull(todayIn);
        Assert.True(todayIn.GoalMet);
        Assert.Null(tomorrowIn);

        // An ended duty pays no goal points on a later day, though its rule reads "met" from the end on.
        var laterRewards = await WithServiceAsync(async (db, progress) =>
        {
            var plan = await db.StudyPlans.SingleAsync(p => p.Id == planId);
            await progress.EvaluateAndAwardAsync(plan, Today().AddDays(2));
            return await db.PositionGoalRewards.CountAsync(r => r.PlanPositionId == positionId && r.Day == Today().AddDays(2));
        });
        Assert.Equal(0, laterRewards);
    }

    // ---- B-637: the father sees the help per task ---- ----

    /// <summary>
    /// AK 1: the report of a remediation position names per task the German cue, whether it is done, and how often it was
    /// solved per stage - free text first, in ladder order, although the boxes came first in time. AK 3: the child cannot
    /// read it (it carries the solutions). Red if the counters or the done day are not projected, if the cue is the passage
    /// instead of the gap's prompt, if the stages come in insertion order, or if the role wall lets the child in.
    /// </summary>
    [Fact]
    public async Task Auswertung_zeigt_je_Aufgabe_Vorgabe_Erledigt_und_Stufen()
    {
        var (father, planId, positionId) = await RemediationPositionAsync();
        var child = await TestApi.ChildAsync(factory);
        var (sessionId, _) = await StartAsync(child, planId, positionId);
        await ReviewAsync(child, planId, positionId, sessionId, 0, "eastwards");
        (await EasierAsync(child, planId, positionId, sessionId, 1)).EnsureSuccessStatusCode();
        await ReviewAsync(child, planId, positionId, sessionId, 1, "asleep");
        await ReviewAsync(child, planId, positionId, sessionId, 2, "as long as");
        var (secondId, _) = await StartAsync(child, planId, positionId);
        await ReviewAsync(child, planId, positionId, secondId, 1, "asleep");

        var url = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}/report";
        var report = await father.GetFromJsonAsync<JsonElement>(url);
        JsonAssert.True(report, "remediation");
        var items = report.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("remediation")).ToList();

        Assert.Equal("Richtung Osten", items[0].GetProperty("cue").GetString());
        Assert.Equal(Today().ToString("yyyy-MM-dd"), items[0].GetProperty("doneAt").GetString());
        Assert.Equal(Today().ToString("yyyy-MM-dd"), items[1].GetProperty("doneAt").GetString());
        Assert.Equal(JsonValueKind.Null, items[2].GetProperty("doneAt").ValueKind);
        Assert.Empty(items[2].GetProperty("solvedByStage").EnumerateArray());

        var task2 = items[1].GetProperty("solvedByStage").EnumerateArray()
            .Select(c => (c.GetProperty("stage").GetInt32(), c.GetProperty("count").GetInt32())).ToList();
        Assert.Equal([((int)ClozeStage.TranslationFreeText, 1), ((int)ClozeStage.LetterBoxes, 1)], task2);

        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync(url)).StatusCode);
    }

    /// <summary>Guard: a position without remediation carries no remediation part in its report. Red if it is always set.</summary>
    [Fact]
    public async Task Auswertung_ohne_Nacharbeit_ohne_Nacharbeitsteil()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await NacharbeitAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.TranslationWordBank);
        var report = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}/report");
        JsonAssert.False(report, "remediation");
        Assert.All(report.GetProperty("items").EnumerateArray(),
            i => Assert.Equal(JsonValueKind.Null, i.GetProperty("remediation").ValueKind));
    }

    /// <summary>
    /// Migration probe finding: a remediation finished before B-636 (no end stamp), then switched to "until done" - no
    /// answer is left to finish it, so the switch stamps the end at once, without a bonus. Red if the stamp is dropped (the
    /// duty would charge its penalty forever) or if finishing a plain daily remediation pays the bonus.
    /// </summary>
    [Fact]
    public async Task Schon_fertige_Nacharbeit_endet_beim_Umstellen_ohne_Bonus()
    {
        var (father, planId, positionId) = await RemediationPositionAsync();
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}";
        // The bonus is set while the position is still a plain daily goal: finishing then pays nothing (only "until done" does).
        (await father.PatchAsJsonAsync(url, new { completionBonus = 77 })).EnsureSuccessStatusCode();
        var child = await TestApi.ChildAsync(factory);
        await PlayRoundAsync(child, planId, positionId, "eastwards", "asleep", "as soon as");
        // Finished before B-636 existed: the real database holds such positions without an end stamp (migration probe).
        await WithServiceAsync((db, _) => db.PlanPositions.Where(p => p.Id == positionId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.CompletedOn, (DateOnly?)null)));

        var patched = await (await father.PatchAsJsonAsync(url, new { cadence = "UntilDone" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(Today().ToString("yyyy-MM-dd"), patched.GetProperty("completedOn").GetString());
        Assert.Equal(0, await WithServiceAsync((db, _) =>
            db.ChildPointsEntries.CountAsync(e => e.ChildId == 1 && e.Kind == PointKind.RemediationBonus && e.Amount == 77)));
    }

    /// <summary>
    /// An ended remediation is no duty any more, so it cannot carry the streak: a child whose only duty it was has no
    /// complete day after the end. Red if the duty state counts the ended position as a met duty - the streak would then
    /// grow every day without playing. Own child, so no other plan of the fixture decides the day.
    /// </summary>
    [Fact]
    public async Task Beendete_Nacharbeit_traegt_die_Serie_nicht_weiter()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Serie"), pin = "4711" }));
        var exerciseId = await NacharbeitAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.TranslationWordBank,
            childId: childId, useLeitner: false);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}",
            new { remediation = true, cadence = "UntilDone" })).EnsureSuccessStatusCode();
        var child = await TestApi.ChildAsync(factory, childId, "4711");
        await PlayRoundAsync(child, planId, positionId, "eastwards", "asleep", "as soon as");

        var (today, dayAfter) = await WithServiceAsync(async (_, progress) =>
            (await progress.ChildStreakAsync(childId, Today(), 10, CancellationToken.None),
             await progress.ChildStreakAsync(childId, Today().AddDays(1), 10, CancellationToken.None)));
        Assert.Equal(1, today);
        Assert.Equal(0, dayAfter);
    }

    /// <summary>
    /// Review finding Y1 + G2: the pool shrinks past the last open task without the father's form (here the item count is
    /// set directly) - the next round start finds nothing open and stamps the end, without a bonus; the report shows no
    /// remediation part for the task outside the pool. Red if the start leaves the end unstamped (the duty would charge its
    /// penalty every day with nothing to play) or if the report lists the task outside the pool as open.
    /// </summary>
    [Fact]
    public async Task Geschrumpfter_Pool_beendet_die_Nacharbeit_beim_Start()
    {
        var (father, planId, positionId) = await UntilDoneAsync(bonus: 33);
        var child = await TestApi.ChildAsync(factory);
        await PlayRoundAsync(child, planId, positionId, "eastwards", "asleep", "as long as");
        await WithServiceAsync((db, _) => db.PlanPositions.Where(p => p.Id == positionId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ItemCount, (int?)2)));

        var (_, session) = await StartAsync(child, planId, positionId);

        Assert.Equal(0, session.GetProperty("total").GetInt32());
        var pos = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}");
        Assert.Equal(Today().ToString("yyyy-MM-dd"), pos.GetProperty("completedOn").GetString());
        Assert.Equal(0, await WithServiceAsync((db, _) =>
            db.ChildPointsEntries.CountAsync(e => e.Kind == PointKind.RemediationBonus && e.Amount == 33)));
        var report = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}/report");
        Assert.Equal(JsonValueKind.Null, report.GetProperty("items")[2].GetProperty("remediation").ValueKind);
    }

    /// <summary>
    /// Review finding Y2 (B-538 rule): an ended remediation has no duty, so a Leitner hit on a task added after the end pays
    /// no coins. Red if the coin rule keeps reading only the cadence (then the hit pays its base points).
    /// </summary>
    [Fact]
    public async Task Nach_dem_Ende_zahlt_die_Position_keine_Muenzen()
    {
        var (father, planId, positionId) = await RemediationPositionAsync(useLeitner: true);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}";
        (await father.PatchAsJsonAsync(url, new { cadence = "UntilDone", itemCount = 2 })).EnsureSuccessStatusCode();
        var child = await TestApi.ChildAsync(factory);
        await PlayRoundAsync(child, planId, positionId, "eastwards", "asleep");
        Assert.Equal(Today(), Progress(positionId, 1)!.RemediationDoneAt);
        // A third task joins after the end (the item count grows behind the form), and the end lies a day back - on the
        // last day itself the duty still runs: the end stays final.
        await WithServiceAsync((db, _) => db.PlanPositions.Where(p => p.Id == positionId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ItemCount, (int?)3).SetProperty(p => p.CompletedOn, Today().AddDays(-1))));
        var coinsBefore = await WithServiceAsync((db, _) =>
            db.ChildPointsEntries.CountAsync(e => e.ChildId == 1 && e.Kind == PointKind.Base));

        var (sessionId, _) = await StartAsync(child, planId, positionId);
        var answer = await ReviewAsync(child, planId, positionId, sessionId, 2, "as soon as");

        JsonAssert.True(answer, "wasCorrect");
        Assert.Equal(0, answer.GetProperty("awarded").GetInt32());
        Assert.Equal(coinsBefore, await WithServiceAsync((db, _) =>
            db.ChildPointsEntries.CountAsync(e => e.ChildId == 1 && e.Kind == PointKind.Base)));
    }
}
