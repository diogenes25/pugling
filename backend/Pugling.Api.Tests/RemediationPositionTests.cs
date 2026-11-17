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
            Assert.Equal(t.EaseLadder.Count > 0, t.Manifest.Capabilities.Contains("remediation")));
        Assert.Contains(registry.All, t => t.EaseLadder.Count > 0);
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
}
