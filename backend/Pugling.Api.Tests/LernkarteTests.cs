using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-598: a new word is met on a learn card - German shown, the learnt word uncovered and judged, copied, then recalled.
/// The card never pays and moves no box, stores both of its results, and comes back once at the end of the round when the
/// recall fails. Test, bonus training and free practice never play it. Case by case the change that turns it red is named.
/// </summary>
public class LernkarteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string Front, string Back)[] Words = [("la gare", "der Bahnhof"), ("le train", "der Zug")];

    private sealed record Setup(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId, int ExerciseId);

    // A fresh child per case: the cases read its learning history and its coins.
    private async Task<Setup> SetupAsync(TestStage stage, string direction = "front-to-back", bool introduced = false)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Lernkarten-Kind"), pin }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Französisch-Lernkarte") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = TestApi.UniqueName("Am Bahnhof"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction,
                    sourceLang = "fr",
                    targetLang = "de",
                    items = Words.Select(w => new { front = w.Front, back = TestApi.UniqueName(w.Back) }),
                },
            }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage, childId: childId,
            introduced: introduced);
        return new Setup(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId, exerciseId);
    }

    private static string Base(Setup s) => TestApi.PracticeBase(s.PlanId, s.PositionId);

    private static async Task<int> StartAsync(Setup s, string mode = "Lern") =>
        await TestApi.IdAsync(await s.Child.PostAsJsonAsync(Base(s), new { mode }));

    private static async Task<JsonElement> NextAsync(Setup s, int sessionId) =>
        (await s.Child.GetFromJsonAsync<JsonElement>($"{Base(s)}/{sessionId}/next")).GetProperty("card");

    private static async Task<JsonElement> AnswerAsync(Setup s, int sessionId, int itemIndex, bool? wasKnown, string? givenAnswer)
    {
        var res = await s.Child.PostAsJsonAsync($"{Base(s)}/{sessionId}/review", new { itemIndex, wasKnown, givenAnswer });
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private T Db<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private int CoinsOf(int childId) => Db(db => db.ChildPointsEntries.Where(e => e.ChildId == childId).AsEnumerable()
        .Where(e => PointKindCurrency.Of(e.Kind) == Currency.Coins).Sum(e => e.Amount));

    /// <summary>
    /// AK 2/3, the card's data: German as the prompt, the learnt word as the solution to uncover and copy, and the
    /// language to read that word aloud in (decision 10). Red without the reveal's speech language (no read-aloud button).
    /// </summary>
    [Fact]
    public async Task Lernkarte_Zeigt_Deutsch_Und_Bringt_Das_Fremdwort_Zum_Vorlesen_Mit()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var card = await NextAsync(s, await StartAsync(s));

        Assert.Equal((int)TestStage.LearnCard, card.GetProperty("stage").GetInt32());
        JsonAssert.True(card, "learnCard");
        Assert.Equal("la gare", card.GetProperty("reveal").GetString());
        Assert.Equal("fr", card.GetProperty("revealSpeechLang").GetString());
        // The prompt is German and never read aloud (B-252): the read-aloud button belongs to the learnt word.
        JsonAssert.Null(card, "speechLang");
    }

    /// <summary>
    /// AK 5: one call carries both results, the server keeps both in the history (the uncovering on the self-assessment
    /// stage, the recall with what was written) and books no coin, no hit and no box movement; the word counts as
    /// introduced. Red when the learn card is scored like a typed card (the box climbs, coins are booked) or when only one
    /// of the two results is kept.
    /// </summary>
    [Fact]
    public async Task Lernkarte_Speichert_Beide_Ergebnisse_Und_Zahlt_Nichts()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);

        var outcome = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: "la gare");

        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        JsonAssert.False(outcome, "repeatsAtEnd");
        Assert.Equal(0, CoinsOf(s.ChildId));
        var prog = Db(db => db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == 0));
        Assert.Equal(1, prog.Box);
        Assert.Equal(0, prog.ReviewCount);
        Assert.NotNull(prog.IntroducedAt);
        var history = Db(db => db.ItemReviewEvents.Where(e => e.ChildId == s.ChildId).OrderBy(e => e.Id)
            .Select(e => new { e.StageValue, e.WasCorrect, e.GivenAnswer }).ToList());
        Assert.Equal(2, history.Count);
        Assert.Equal(((int)TestStage.SelfAssess, false, (string?)null), (history[0].StageValue, history[0].WasCorrect, history[0].GivenAnswer));
        Assert.Equal(((int)TestStage.LearnCard, true, "la gare"), (history[1].StageValue, history[1].WasCorrect, history[1].GivenAnswer));
        // Neither row counts for the plan-wide mastery: there is no progress row for the word yet.
        Assert.Equal(0, Db(db => db.ItemProgress.Count(p => p.ChildId == s.ChildId)));
    }

    /// <summary>
    /// AK 4 + decisions 7/8: a recall given up comes back once at the end of the round as a learn card again; given up a
    /// second time it does not come a third time. The history tells "given up" (no answer) from "wrong" (an answer). Red
    /// without the repeat (two cards, then done), without the limit (a third time), or when the repeat came as another form.
    /// </summary>
    [Fact]
    public async Task Abbruch_Bringt_Die_Karte_Einmal_Ans_Rundenende()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);

        var first = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null);
        JsonAssert.True(first, "repeatsAtEnd");
        Assert.Equal("la gare", first.GetProperty("expected").GetString());
        var second = await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le tren");
        JsonAssert.False(second, "wasCorrect");
        JsonAssert.True(second, "repeatsAtEnd");
        JsonAssert.False(second, "done");

        // The two repeats, in the order they were appended, each a learn card again.
        var repeat = second.GetProperty("next");
        Assert.Equal(0, repeat.GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)TestStage.LearnCard, repeat.GetProperty("stage").GetInt32());
        var again = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null);
        JsonAssert.False(again, "repeatsAtEnd");
        Assert.Equal(1, again.GetProperty("next").GetProperty("itemIndex").GetInt32());
        var last = await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");
        JsonAssert.True(last, "done");

        Assert.Equal([0, 1, 0, 1], Db(db => db.PracticeSessions.Single(p => p.Id == sessionId).Order));
        var recalls = Db(db => db.ItemReviewEvents.Where(e => e.ChildId == s.ChildId && e.StageValue == (int)TestStage.LearnCard)
            .OrderBy(e => e.Id).Select(e => e.GivenAnswer).ToList());
        Assert.Equal([null, "le tren", null, "le train"], recalls);
    }

    /// <summary>
    /// Decision 9: the test never plays a learn card - a station whose day is the learn card is tested at its target, and
    /// so is a learn card the supervisor picks for a preview attempt. Red when the test takes the learn card as its stage.
    /// </summary>
    [Fact]
    public async Task Test_Spielt_Nie_Die_Lernkarte()
    {
        var s = await SetupAsync(TestStage.LearnCard);
        var tests = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

        var childStart = await s.Child.PostAsJsonAsync(tests, new { });
        var fatherStart = await s.Father.PostAsJsonAsync(tests, new { stage = (int)TestStage.LearnCard });
        Assert.Equal(System.Net.HttpStatusCode.Created, childStart.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Created, fatherStart.StatusCode);
        var childAttempt = await childStart.Content.ReadFromJsonAsync<JsonElement>();
        var fatherAttempt = await fatherStart.Content.ReadFromJsonAsync<JsonElement>();

        // The station's own stage is the learn card, so the target is the type's most telling stage: free text.
        Assert.Equal((int)TestStage.FreeText, childAttempt.GetProperty("stage").GetInt32());
        Assert.Equal((int)TestStage.FreeText, fatherAttempt.GetProperty("stage").GetInt32());
    }

    /// <summary>
    /// Review finding: a station shown on "both sides" whose schedule puts a learn-card day is tested at the type's most
    /// telling stage - and offered as testable on that day. Red when the target falls back to the display stage (the
    /// test answers 409 stage_not_testable while the overview offers it).
    /// </summary>
    [Fact]
    public async Task Lernkarten_Tag_Einer_Anzeige_Station_Ist_Testbar()
    {
        var s = await SetupAsync(TestStage.ShowBoth);
        Db(db =>
        {
            db.PlanPositions.Single(p => p.Id == s.PositionId).StageSchedule = [new StageStep(1, (int)TestStage.LearnCard)];
            return db.SaveChanges();
        });
        var tests = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

        var start = await s.Child.PostAsJsonAsync(tests, new { });

        Assert.Equal(System.Net.HttpStatusCode.Created, start.StatusCode);
        Assert.Equal((int)TestStage.FreeText, (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stage").GetInt32());
        var overview = await s.Child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{s.PlanId}/overview");
        var position = overview.GetProperty("today").GetProperty("positions").EnumerateArray()
            .Single(p => p.GetProperty("positionId").GetInt32() == s.PositionId);
        JsonAssert.True(position, "testable");
    }

    /// <summary>
    /// Review finding (the no-verdict rule of B-433 for the learn card): a learn card answered a second time - sent twice,
    /// or answered in a second tab after the first had learnt it - stays a learn card and neither scores nor moves the box,
    /// nor does it grow the round. Red when the late answer is graded at the word's rung (the box climbs, a mastery row
    /// appears) or when it appends a repeat although the round moved on.
    /// </summary>
    [Fact]
    public async Task Eine_Spaete_Lernkarten_Antwort_Zaehlt_Nicht()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var first = await StartAsync(s);
        var second = await StartAsync(s);

        await AnswerAsync(s, second, 0, wasKnown: true, givenAnswer: "la gare");
        var late = await AnswerAsync(s, first, 0, wasKnown: true, givenAnswer: "la gare");
        var again = await AnswerAsync(s, first, 0, wasKnown: true, givenAnswer: null);

        Assert.Equal(0, late.GetProperty("awarded").GetInt32());
        JsonAssert.False(again, "repeatsAtEnd");
        var prog = Db(db => db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == 0));
        Assert.Equal(1, prog.Box);
        Assert.Equal(0, Db(db => db.ItemProgress.Count(p => p.ChildId == s.ChildId)));
        Assert.Equal([0, 1], Db(db => db.PracticeSessions.Single(p => p.Id == first).Order));
    }

    /// <summary>
    /// A learn card the father plays in the child's round introduces nothing (B-433: only the child's own encounter does).
    /// Red when the learn card's review stamps the introduction for any caller.
    /// </summary>
    [Fact]
    public async Task Eine_Lernkarte_Des_Vaters_Fuehrt_Nichts_Ein()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync(Base(s), new { mode = "Lern" }));

        (await s.Father.PostAsJsonAsync($"{Base(s)}/{sessionId}/review",
            new { itemIndex = 0, wasKnown = true, givenAnswer = "la gare" })).EnsureSuccessStatusCode();

        Assert.Null(Db(db => db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == 0)).IntroducedAt);
    }

    /// <summary>
    /// Decision 9, the father's view of the check: a station on the learn card is checked in writing, because its test is.
    /// Red when the learn card counts as a self-assessed stage (the warning "self-assessment counts" would appear).
    /// </summary>
    [Fact]
    public async Task Lernkontrolle_Einer_Lernkarten_Station_Ist_Getippt()
    {
        var s = await SetupAsync(TestStage.LearnCard);

        var positions = await s.Father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/supervisor/study-plans/{s.PlanId}/positions");

        var check = positions!.Single(p => p.GetProperty("id").GetInt32() == s.PositionId).GetProperty("learningCheck");
        Assert.Equal("Typed", check.GetProperty("kind").GetString());
    }

    /// <summary>
    /// Decision 1 + 9: on a learn-card day a known word is learnt on the card too (shaky words), but free practice records
    /// nothing and shows it on "both sides" instead. The word sits in box 1, where the ladder would turn it over. Red when
    /// the ladder lowers the known word below the learn card, or when the info mode plays a learn card.
    /// </summary>
    [Fact]
    public async Task Lernkarten_Tag_Spielt_Auch_Bekannte_Woerter_Ausser_Im_Freien_Ueben()
    {
        var s = await SetupAsync(TestStage.LearnCard, introduced: true);
        Db(db =>
        {
            foreach (var p in db.PositionItemProgress.Where(p => p.PlanPositionId == s.PositionId)) p.Box = 1;
            return db.SaveChanges();
        });

        var learn = await NextAsync(s, await StartAsync(s));
        var info = (await s.Child.GetFromJsonAsync<List<JsonElement>>($"{Base(s)}/{await StartAsync(s, "Info")}/cards"))![0];

        Assert.Equal((int)TestStage.LearnCard, learn.GetProperty("stage").GetInt32());
        Assert.Equal((int)TestStage.ShowBoth, info.GetProperty("stage").GetInt32());
    }

    /// <summary>
    /// The learn card writes the learnt word (B-597): on a "both" list the forward card - the one that only recognises -
    /// meets its word on "both sides", the reversed one on the learn card. Red without the ceiling for the learn card.
    /// </summary>
    [Fact]
    public async Task Beide_Lernt_Nur_Die_Rueckwaertskarte_Auf_Der_Lernkarte()
    {
        var s = await SetupAsync(TestStage.FreeText, direction: "both");

        var cards = (await s.Child.GetFromJsonAsync<List<JsonElement>>($"{Base(s)}/{await StartAsync(s)}/cards"))!
            .ToDictionary(c => c.GetProperty("itemIndex").GetInt32());

        Assert.Equal((int)TestStage.ShowBoth, cards[0].GetProperty("stage").GetInt32());
        Assert.Equal((int)TestStage.LearnCard, cards[1].GetProperty("stage").GetInt32());
    }

    /// <summary>
    /// Decision 1: the learn card is offered in the father's stage list next to "both sides". Red when it is missing from
    /// the manifest (the father could not put it into a station's schedule).
    /// </summary>
    [Fact]
    public async Task Lernkarte_Steht_Im_Manifest_Neben_Beide_Zeigen()
    {
        var father = await TestApi.AdultAsync(factory);
        var manifest = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/exercise-types");
        var stages = manifest.EnumerateArray().Single(m => m.GetProperty("type").GetString() == "Vocabulary")
            .GetProperty("stages").EnumerateArray().Select(o => o.GetProperty("value").GetInt32()).ToList();
        Assert.Contains((int)TestStage.LearnCard, stages);
        Assert.Contains((int)TestStage.ShowBoth, stages);
    }
}
