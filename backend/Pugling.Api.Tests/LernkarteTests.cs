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
    private async Task<Setup> SetupAsync(TestStage stage, string direction = "front-to-back", bool introduced = false,
        bool useLeitner = true)
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
            introduced: introduced, useLeitner: useLeitner);
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
    /// stage, the recall with what was written) and moves no box and counts no hit; the word counts as introduced. B-657
    /// replaces "books no coin": the first right recall from memory pays the new-content points (10) - nothing else, no
    /// combo. Red when the learn card is scored like a typed card (the box climbs), when it stops paying, or when only one
    /// of the two results is kept.
    /// </summary>
    [Fact]
    public async Task Lernkarte_Speichert_Beide_Ergebnisse_Und_Zahlt_Den_Neu_Bonus()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);

        var outcome = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: "la gare");

        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal(10, outcome.GetProperty("awarded").GetInt32());
        JsonAssert.False(outcome, "repeatsAtEnd");
        Assert.Equal(10, CoinsOf(s.ChildId));
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
    /// B-643 AK 4 (decision 8 replaces B-598 decision 7): a recall given up or wrong comes back at the end of the round as a
    /// learn card again - as often as it takes, a third time too; the round ends only when every word is recalled. The
    /// history still tells "given up" (no answer) from "wrong". Red with the old limit of one repeat (the third occurrence
    /// is missing and the round ends with word 0 unrecalled), without the repeat, or when a repeat came as another form.
    /// </summary>
    [Fact]
    public async Task Nicht_Erinnertes_Wort_Kommt_Wieder_Bis_Es_Sitzt()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);

        var first = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null);
        JsonAssert.True(first, "repeatsAtEnd");
        Assert.Equal("la gare", first.GetProperty("expected").GetString());
        var second = await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le tren");
        JsonAssert.False(second, "wasCorrect");
        JsonAssert.True(second, "repeatsAtEnd");

        var repeat = second.GetProperty("next");
        Assert.Equal(0, repeat.GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)TestStage.LearnCard, repeat.GetProperty("stage").GetInt32());
        var again = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null);
        JsonAssert.True(again, "repeatsAtEnd");
        var trainRight = await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");
        JsonAssert.False(trainRight, "repeatsAtEnd");

        // The third time word 0 comes back - still a learn card - and given up once more it comes a fourth time.
        var third = trainRight.GetProperty("next");
        Assert.Equal(0, third.GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)TestStage.LearnCard, third.GetProperty("stage").GetInt32());
        JsonAssert.True(await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null), "repeatsAtEnd");
        var last = await AnswerAsync(s, sessionId, 0, wasKnown: true, givenAnswer: "la gare");
        JsonAssert.True(last, "done");

        Assert.Equal([0, 1, 0, 1, 0, 0], Db(db => db.PracticeSessions.Single(p => p.Id == sessionId).Order));
        var recalls = Db(db => db.ItemReviewEvents.Where(e => e.ChildId == s.ChildId && e.StageValue == (int)TestStage.LearnCard)
            .OrderBy(e => e.Id).Select(e => e.GivenAnswer).ToList());
        Assert.Equal([null, "le tren", null, "le train", null, "la gare"], recalls);
    }

    /// <summary>
    /// B-686: the card no longer asks "did you know it?", so an answer arrives without <c>WasKnown</c>; giving up says
    /// <c>gaveUp</c>. Given up, the word still comes back at the end of the round and the history keeps the recall row
    /// alone; recalled, it pays as before. Red when <c>GaveUp</c> is not read as a verdict (the given-up word would not
    /// repeat and leave no history row), or when a self-assessment row is invented for an answer that did not send one.
    /// </summary>
    [Fact]
    public async Task Lernkarte_Ohne_Kannte_Ich_Frage_Wiederholt_Und_Speichert_Das_Erinnern()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);

        var res = await s.Child.PostAsJsonAsync($"{Base(s)}/{sessionId}/review", new { itemIndex = 0, gaveUp = true });
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        var givenUp = await res.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(givenUp, "repeatsAtEnd");
        var recalled = await AnswerAsync(s, sessionId, 1, wasKnown: null, givenAnswer: "le train");
        JsonAssert.True(recalled, "wasCorrect");
        Assert.Equal(10, recalled.GetProperty("awarded").GetInt32());

        Assert.Equal([0, 1, 0], Db(db => db.PracticeSessions.Single(p => p.Id == sessionId).Order));
        var history = Db(db => db.ItemReviewEvents.Where(e => e.ChildId == s.ChildId).OrderBy(e => e.Id)
            .Select(e => new { e.StageValue, e.GivenAnswer }).ToList());
        Assert.Equal([((int)TestStage.LearnCard, (string?)null), ((int)TestStage.LearnCard, "le train")],
            history.Select(h => (h.StageValue, h.GivenAnswer)).ToList());
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
    /// A learn card the father plays in the child's round introduces nothing (B-433: only the child's own encounter does)
    /// and pays nothing (B-657: the new-content points are for the child's own recall). Red when the learn card's review
    /// stamps the introduction for any caller, or when the father's preview books the child's new-content points.
    /// </summary>
    [Fact]
    public async Task Eine_Lernkarte_Des_Vaters_Fuehrt_Nichts_Ein_Und_Zahlt_Nichts()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync(Base(s), new { mode = "Lern" }));

        var res = await s.Father.PostAsJsonAsync($"{Base(s)}/{sessionId}/review",
            new { itemIndex = 0, wasKnown = true, givenAnswer = "la gare" });
        res.EnsureSuccessStatusCode();

        Assert.Equal(0, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("awarded").GetInt32());
        Assert.Equal(0, CoinsOf(s.ChildId));
        var prog = Db(db => db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == 0));
        Assert.Null(prog.IntroducedAt);
        Assert.Null(prog.NewBonusPaidAt);
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

    // ---- B-643: make it easier, imprinted only from memory, back until it sits ----

    private async Task<JsonElement> EaseAsync(Setup s, int sessionId, int itemIndex)
    {
        var res = await s.Child.PostAsJsonAsync($"{Base(s)}/{sessionId}/easier", new { itemIndex });
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private PositionItemProgress ProgressOf(Setup s, int itemIndex) =>
        Db(db => db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == itemIndex));

    /// <summary>
    /// B-643 AK 1: the recall of a learn card is made easier step by step - boxes, scramble, choice - and stays a learn card
    /// in its own form: the choice offers learnt words, and the eased card neither reveals nor reads the word aloud. Red
    /// if the learn card cannot be eased (409), if a press skips or repeats a rung, or if the choice falls back to the
    /// forward card's German meanings.
    /// </summary>
    [Fact]
    public async Task Lernkarte_Wird_Im_Erinnern_Stufe_Fuer_Stufe_Leichter()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);
        var card = await NextAsync(s, sessionId);
        JsonAssert.True(card, "canEase");

        var stages = new List<int>();
        JsonElement eased = default;
        for (var press = 0; press < 3; press++)
        {
            eased = await EaseAsync(s, sessionId, 0);
            stages.Add(eased.GetProperty("stage").GetInt32());
            JsonAssert.True(eased, "learnCard");
            JsonAssert.True(eased, "eased");
            JsonAssert.Null(eased, "reveal");
            JsonAssert.Null(eased, "revealSpeechLang");
        }
        Assert.Equal([(int)TestStage.LetterBoxes, (int)TestStage.LetterScramble, (int)TestStage.MultipleChoice], stages);
        Assert.Contains("la gare", eased.GetProperty("choices").EnumerateArray().Select(c => c.GetString()));
        Assert.Contains("le train", eased.GetProperty("choices").EnumerateArray().Select(c => c.GetString()));
        JsonAssert.False(eased, "canEase");
    }

    /// <summary>
    /// B-643 AK 2 + AK 3 (anti-cheat): an eased recall answered right - even sent as free text - shows right and the word,
    /// goes to the end of the round and imprints nothing; the free recall right imprints. Red if the review grades the
    /// client's form (imprinted), if the eased hit is not repeated, or if the free recall does not imprint. Before the build
    /// there was neither `easier` nor `ImprintedAt`.
    /// </summary>
    [Fact]
    public async Task Leichter_Erinnert_Praegt_Nicht_Ein_Frei_Erinnert_Schon()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);
        await EaseAsync(s, sessionId, 0);

        var eased = await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: "la gare");
        JsonAssert.True(eased, "wasCorrect");
        Assert.Equal("la gare", eased.GetProperty("expected").GetString());
        JsonAssert.True(eased, "repeatsAtEnd");
        Assert.Equal(0, eased.GetProperty("awarded").GetInt32());
        Assert.Null(ProgressOf(s, 0).ImprintedAt);

        var free = await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");
        JsonAssert.False(free, "repeatsAtEnd");
        Assert.NotNull(ProgressOf(s, 1).ImprintedAt);
        // The repeat at the end of the round is the whole learn card again, not the eased form.
        var repeat = free.GetProperty("next");
        Assert.Equal((int)TestStage.LearnCard, repeat.GetProperty("stage").GetInt32());
        JsonAssert.False(repeat, "eased");
    }

    /// <summary>
    /// B-643 AK 5, the next day: a round closed with a word not imprinted brings it back as a learn card - it was
    /// introduced, so the old rule asked it at its rung. Red with the introduction as the card selection's gate.
    /// </summary>
    [Fact]
    public async Task Nicht_Eingepraegtes_Wort_Kommt_Am_Naechsten_Tag_Als_Lernkarte()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);
        await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null);
        await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");
        // The ✕ ends the round here; then a day passes for both words.
        Db(db =>
        {
            foreach (var p in db.PositionItemProgress.Where(p => p.PlanPositionId == s.PositionId))
            {
                p.IntroducedAt = p.IntroducedAt!.Value.AddDays(-1);
                p.DueOn = p.IntroducedAt;
                if (p.ImprintedAt is not null) p.ImprintedAt = p.IntroducedAt;
            }
            return db.SaveChanges();
        });

        var cards = (await s.Child.GetFromJsonAsync<List<JsonElement>>($"{Base(s)}/{await StartAsync(s)}/cards"))!
            .ToDictionary(c => c.GetProperty("itemIndex").GetInt32());

        Assert.Equal((int)TestStage.LearnCard, cards[0].GetProperty("stage").GetInt32());
        JsonAssert.True(cards[0], "introduction");
        Assert.NotEqual((int)TestStage.LearnCard, cards[1].GetProperty("stage").GetInt32());
    }

    /// <summary>
    /// B-643 AK 5, the dose: six new words a day and two open learn cards from yesterday make two open and four new - not
    /// two plus six. Red if open learn cards do not count on the day's dose.
    /// </summary>
    [Fact]
    public async Task Offene_Lernkarten_Zaehlen_Auf_Die_Tagesdosis()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Dosis-Kind"), pin }));
        var words = Enumerable.Range(0, 10).Select(i => ($"mot{i}", TestApi.UniqueName($"Wort{i}"))).ToArray();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", words);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId,
            introduced: false);
        var yesterday = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-1);
        Db(db =>
        {
            db.PlanPositions.Single(p => p.Id == positionId).NewItemsPerDay = 6;
            foreach (var i in new[] { 0, 1 })
                db.PositionItemProgress.Add(new PositionItemProgress
                { PlanPositionId = positionId, ExerciseId = TestApi.ExerciseOf(db, positionId), ItemIndex = i, IntroducedAt = yesterday, DueOn = yesterday });
            return db.SaveChanges();
        });
        var child = await TestApi.ChildAsync(factory, childId, pin);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        var cards = (await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;

        Assert.Equal([0, 1, 2, 3, 4, 5], cards.Select(c => c.GetProperty("itemIndex").GetInt32()).Order());
        Assert.All(cards, c => Assert.Equal((int)TestStage.LearnCard, c.GetProperty("stage").GetInt32()));
    }

    /// <summary>
    /// B-643 AK 6: the golden ring counts imprinted words - a word given up is introduced but not imprinted - while a card
    /// without a learn card (the recognition card of a "both" word) counts once introduced. Red if the count reads the
    /// introduction (2) or leaves the recognition card out (0).
    /// </summary>
    [Fact]
    public async Task Ring_Zaehlt_Eingepraegte_Woerter()
    {
        var s = await SetupAsync(TestStage.FreeText, direction: "both");
        var sessionId = await StartAsync(s);
        var cards = (await s.Child.GetFromJsonAsync<List<JsonElement>>($"{Base(s)}/{sessionId}/cards"))!
            .ToDictionary(c => c.GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)TestStage.ShowBoth, cards[0].GetProperty("stage").GetInt32());

        (await s.Child.PostAsJsonAsync($"{Base(s)}/{sessionId}/review", new { itemIndex = 0 })).EnsureSuccessStatusCode();
        await AnswerAsync(s, sessionId, 1, wasKnown: false, givenAnswer: null);

        var overview = await s.Child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{s.PlanId}/overview");
        var position = overview.GetProperty("today").GetProperty("positions").EnumerateArray()
            .Single(p => p.GetProperty("positionId").GetInt32() == s.PositionId);
        Assert.Equal(2, position.GetProperty("introducedCount").GetInt32());
        Assert.Equal(1, position.GetProperty("imprintedCount").GetInt32());
    }

    // ---- B-643 review findings ----

    /// <summary>
    /// Review finding 1a (risk 4, a double send): the eased answer of the round's last word sent twice lands the second time
    /// on its own repeat - it must not imprint there; the repeat counts once the client reported it visible (B-530). Red
    /// without the echo check (the copy imprints the word) or if a visible repeat recalled right does not imprint.
    /// </summary>
    [Fact]
    public async Task Doppelt_Geschickte_Hilfe_Antwort_Praegt_Nicht_Ein()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);
        await AnswerAsync(s, sessionId, 0, wasKnown: true, givenAnswer: "la gare");
        await EaseAsync(s, sessionId, 1);

        await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");
        var echo = await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");

        Assert.Null(ProgressOf(s, 1).ImprintedAt);
        JsonAssert.True(echo, "repeatsAtEnd");
        (await s.Child.PostAsJsonAsync($"{Base(s)}/{sessionId}/shown", new { itemIndex = 1 })).EnsureSuccessStatusCode();
        await AnswerAsync(s, sessionId, 1, wasKnown: true, givenAnswer: "le train");
        Assert.NotNull(ProgressOf(s, 1).ImprintedAt);
    }

    /// <summary>
    /// Review finding 1b: an answer for a place the round has passed (a late copy) imprints nothing. Red if the recall
    /// counts for any position of the word.
    /// </summary>
    [Fact]
    public async Task Antwort_Fuer_Eine_Vergangene_Stelle_Praegt_Nicht_Ein()
    {
        var s = await SetupAsync(TestStage.FreeText);
        var sessionId = await StartAsync(s);
        await AnswerAsync(s, sessionId, 0, wasKnown: false, givenAnswer: null);

        await AnswerAsync(s, sessionId, 0, wasKnown: true, givenAnswer: "la gare");

        Assert.Null(ProgressOf(s, 0).ImprintedAt);
    }

    // A fresh child with ten words, a dose of six and two words met yesterday on a learn card but not imprinted.
    private async Task<(HttpClient Child, int ChildId, int PlanId, int PositionId)> DoseSetupAsync(
        ItemScope scope = ItemScope.All)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Dosis-Kind"), pin }));
        var words = Enumerable.Range(0, 10).Select(i => ($"mot{i}", TestApi.UniqueName($"Wort{i}"))).ToArray();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", words);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId,
            introduced: false);
        var yesterday = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-1);
        Db(db =>
        {
            var position = db.PlanPositions.Single(p => p.Id == positionId);
            position.NewItemsPerDay = 6;
            position.Scope = scope;
            foreach (var i in new[] { 0, 1 })
                db.PositionItemProgress.Add(new PositionItemProgress
                { PlanPositionId = positionId, ExerciseId = TestApi.ExerciseOf(db, positionId), ItemIndex = i, IntroducedAt = yesterday, DueOn = yesterday });
            return db.SaveChanges();
        });
        return (await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    // Plays a learn round: every learn card recalled right ("mot{i}").
    private static async Task PlayLearnRoundAsync(HttpClient child, int planId, int positionId)
    {
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        foreach (var card in (await child.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}/{sessionId}/cards"))!)
        {
            var i = card.GetProperty("itemIndex").GetInt32();
            (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, i, givenAnswer: $"mot{i}", wasKnown: true))
                .EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Review finding 2: the open learn cards keep their places for the whole day - imprinted in the first round, they do
    /// not free places for new words in a second round. Red if only words still open count (words 6 and 7 come as new).
    /// </summary>
    [Fact]
    public async Task Zweite_Runde_Am_Selben_Tag_Bringt_Keine_Neuen_Ueber_Die_Dosis()
    {
        var (child, _, planId, positionId) = await DoseSetupAsync();
        await PlayLearnRoundAsync(child, planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        var cards = (await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;

        Assert.DoesNotContain(cards, c => c.GetProperty("itemIndex").GetInt32() >= 6);
        Assert.Equal(6, Db(db => db.PositionItemProgress.Count(p => p.PlanPositionId == positionId && p.IntroducedAt != null)));
    }

    /// <summary>
    /// B-653 AK 4: under "new" too the two open learn cards from yesterday take places of the dose - two open and four
    /// new, not six new beside them. Red while "new" keeps yesterday's words out (cards 2..7, the dose leak), and red if
    /// the open words were let in without counting on the dose (cards 0..7).
    /// </summary>
    [Fact]
    public async Task Neu_Offene_Lernkarten_Zaehlen_Auf_Die_Tagesdosis()
    {
        var (child, _, planId, positionId) = await DoseSetupAsync(ItemScope.New);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        var cards = (await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;

        Assert.Equal([0, 1, 2, 3, 4, 5], cards.Select(c => c.GetProperty("itemIndex").GetInt32()).Order());
    }

    /// <summary>
    /// B-653 AK 4, second round: imprinted in the first round today, the two open words still hold their places - the
    /// second round brings no word beyond the dose. Red if the scope rule leaves out "imprinted today" (the two words drop
    /// out of the count, words 6 and 7 come as new).
    /// </summary>
    [Fact]
    public async Task Neu_Zweite_Runde_Am_Selben_Tag_Bringt_Keine_Neuen_Ueber_Die_Dosis()
    {
        var (child, _, planId, positionId) = await DoseSetupAsync(ItemScope.New);
        await PlayLearnRoundAsync(child, planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        var cards = (await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;

        Assert.DoesNotContain(cards, c => c.GetProperty("itemIndex").GetInt32() >= 6);
        Assert.Equal(6, Db(db => db.PositionItemProgress.Count(p => p.PlanPositionId == positionId && p.IntroducedAt != null)));
    }

    /// <summary>
    /// B-653 AK 3: on a "both" list under "new", the recognition card met yesterday needs no learn card and stays out the
    /// next day, while the reversed card met yesterday but not imprinted comes back. Red if the scope rule lets every
    /// not-imprinted word in without asking whether it needs a learn card (card 0 would stay forever).
    /// </summary>
    [Fact]
    public async Task Neu_Laesst_Eine_Erkennungskarte_Ohne_Lernkarte_Am_Folgetag_Draussen()
    {
        var s = await SetupAsync(TestStage.FreeText, direction: "both");
        var yesterday = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-1);
        Db(db =>
        {
            db.PlanPositions.Single(p => p.Id == s.PositionId).Scope = ItemScope.New;
            foreach (var i in new[] { 0, 1 })
                db.PositionItemProgress.Add(new PositionItemProgress
                { PlanPositionId = s.PositionId, ExerciseId = TestApi.ExerciseOf(db, s.PositionId), ItemIndex = i, IntroducedAt = yesterday, DueOn = yesterday });
            return db.SaveChanges();
        });

        var indices = (await s.Child.GetFromJsonAsync<List<JsonElement>>($"{Base(s)}/{await StartAsync(s)}/cards"))!
            .Select(c => c.GetProperty("itemIndex").GetInt32()).ToList();

        Assert.Contains(1, indices);
        Assert.DoesNotContain(0, indices);
    }

    // Takes the position's test and answers every item with its right answer in the stage's language.
    private async Task TakeTestAsync(Setup s)
    {
        var tests = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";
        var attempt = await (await s.Child.PostAsJsonAsync(tests, new { })).Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        for (var i = 0; i < attempt.GetProperty("totalItems").GetInt32(); i++)
        {
            var next = (await s.Child.GetFromJsonAsync<JsonElement>($"{tests}/{attemptId}/next")).GetProperty("item");
            var index = next.GetProperty("itemIndex").GetInt32();
            var answer = next.GetProperty("stage").GetInt32() == (int)TestStage.MultipleChoice
                ? next.GetProperty("choices").EnumerateArray().Select(c => c.GetString()!).Single(c => c.StartsWith(Words[index].Back))
                : Words[index].Front;
            (await s.Child.PostAsJsonAsync($"{tests}/{attemptId}/answer", new { itemIndex = index, givenAnswer = answer }))
                .EnsureSuccessStatusCode();
        }
    }

    private void IntroduceDaysAgo(Setup s, int days)
    {
        var day = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-days);
        Db(db =>
        {
            foreach (var i in Enumerable.Range(0, Words.Length))
                db.PositionItemProgress.Add(new PositionItemProgress
                { PlanPositionId = s.PositionId, ExerciseId = TestApi.ExerciseOf(db, s.PositionId), ItemIndex = i, IntroducedAt = day, DueOn = day });
            return db.SaveChanges();
        });
    }

    /// <summary>
    /// Review finding 4, derived from decision 1 ("written from memory, right, without help"): a word typed right in the
    /// test's free field is imprinted - past its ladder week the test asks in writing. Red if the test path never imprints
    /// (the word comes back as a learn card for good).
    /// </summary>
    [Fact]
    public async Task Frei_Getippt_Im_Test_Praegt_Ein()
    {
        var s = await SetupAsync(TestStage.FreeText);
        IntroduceDaysAgo(s, 8);

        await TakeTestAsync(s);

        Assert.NotNull(ProgressOf(s, 0).ImprintedAt);
        Assert.NotNull(ProgressOf(s, 1).ImprintedAt);
    }

    /// <summary>
    /// B-658: a test that asks cold words and gets them typed right introduces and imprints them in ONE request. Both
    /// steps look up the word's progress row; the second must find the row the first added but has not saved yet. Red if
    /// the lookup only asks the database - the submission then adds the row twice and ends in a 500 (unique index).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Kalter_Test_Frei_Getippt_Fuehrt_Ein_Und_Praegt_Ein(bool bulk)
    {
        // Without a card box the test asks every word at the position's own stage - a cold word, too, in the free field.
        var s = await SetupAsync(TestStage.FreeText, useLeitner: false);
        var tests = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";
        var start = await s.Child.PostAsJsonAsync(tests, new { });
        var attemptId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptId").GetInt32();

        if (bulk)
        {
            var answers = Words.Select((w, i) => new { itemIndex = i, givenAnswer = w.Front });
            var submit = await s.Child.PostAsJsonAsync($"{tests}/{attemptId}/submit", new { answers });
            Assert.True(submit.IsSuccessStatusCode, $"{(int)submit.StatusCode}: {await submit.Content.ReadAsStringAsync()}");
        }
        else
        {
            for (var i = 0; i < Words.Length; i++)
            {
                var index = (await s.Child.GetFromJsonAsync<JsonElement>($"{tests}/{attemptId}/next"))
                    .GetProperty("item").GetProperty("itemIndex").GetInt32();
                var res = await s.Child.PostAsJsonAsync($"{tests}/{attemptId}/answer", new { itemIndex = index, givenAnswer = Words[index].Front });
                Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
            }
        }

        Assert.All(Enumerable.Range(0, Words.Length), i =>
        {
            Assert.NotNull(ProgressOf(s, i).IntroducedAt);
            Assert.NotNull(ProgressOf(s, i).ImprintedAt);
        });
        Assert.Equal(Words.Length, Db(db => db.PositionItemProgress.Count(p => p.PlanPositionId == s.PositionId)));
    }

    /// <summary>
    /// The other half of finding 4: a word chosen right in the test (its ladder week asks a choice) is not written from
    /// memory and imprints nothing. Red if every right test answer imprinted.
    /// </summary>
    [Fact]
    public async Task Gewaehlt_Im_Test_Praegt_Nicht_Ein()
    {
        var s = await SetupAsync(TestStage.FreeText);
        IntroduceDaysAgo(s, 1);

        await TakeTestAsync(s);

        Assert.Null(ProgressOf(s, 0).ImprintedAt);
        Assert.Null(ProgressOf(s, 1).ImprintedAt);
    }

    /// <summary>
    /// Review finding 5 (story test plan, risk 1), turned by B-657: an open learn card coming back pays its new-content
    /// points when it is finally recalled - and only once per word, never "new" twice. The second recall below finds the
    /// word not imprinted again (reset by hand) but the bonus spent. Red if the learn card stopped paying, or paid again.
    /// </summary>
    [Fact]
    public async Task Wiederkehrende_Lernkarte_Zahlt_Den_Neu_Bonus_Nur_Einmal()
    {
        var s = await SetupAsync(TestStage.FreeText);
        IntroduceDaysAgo(s, 1);
        var before = CoinsOf(s.ChildId);

        var answer = await AnswerAsync(s, await StartAsync(s), 0, wasKnown: true, givenAnswer: "la gare");

        Assert.Equal(10, answer.GetProperty("awarded").GetInt32());
        Assert.Equal(before + 10, CoinsOf(s.ChildId));
        Assert.NotNull(ProgressOf(s, 0).NewBonusPaidAt);
        Assert.NotNull(ProgressOf(s, 0).ImprintedAt);

        Db(db =>
        {
            db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == 0).ImprintedAt = null;
            return db.SaveChanges();
        });
        var again = await AnswerAsync(s, await StartAsync(s), 0, wasKnown: true, givenAnswer: "la gare");
        Assert.Equal(0, again.GetProperty("awarded").GetInt32());
        Assert.Equal(before + 10, CoinsOf(s.ChildId));
    }

    /// <summary>
    /// A cold word typed right in a test asked in the free field (no Leitner, so no ladder) is introduced AND imprinted by
    /// the same answer. Both go through the same progress row, created and not yet saved: the second lookup must find the
    /// tracked one, or the save inserts the row twice and the submission fails with a 500 (UNIQUE PlanPositionId+ItemIndex).
    /// Red if <c>ProgressForAsync</c> only queries the database.
    /// </summary>
    [Fact]
    public async Task Kaltes_Wort_Frei_Getippt_Im_Sammel_Abgabe_Test_Wird_Einmal_Angelegt()
    {
        var s = await SetupAsync(TestStage.FreeText, useLeitner: false);
        var tests = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(tests, new { }), "attemptId");

        var submit = await s.Child.PostAsJsonAsync($"{tests}/{attemptId}/submit",
            new { answers = Words.Select((w, itemIndex) => new { itemIndex, givenAnswer = w.Front }) });

        Assert.True(submit.IsSuccessStatusCode, $"{(int)submit.StatusCode}: {await submit.Content.ReadAsStringAsync()}");
        Assert.NotNull(ProgressOf(s, 0).IntroducedAt);
        Assert.NotNull(ProgressOf(s, 0).ImprintedAt);
    }

    /// <summary>
    /// The step-wise twin of the case above: the same cold word answered right through <c>/answer</c>. Red if
    /// <c>ProgressForAsync</c> only queries the database (the answer fails with a 500).
    /// </summary>
    [Fact]
    public async Task Kaltes_Wort_Frei_Getippt_Im_Schritt_Test_Wird_Einmal_Angelegt()
    {
        var s = await SetupAsync(TestStage.FreeText, useLeitner: false);

        await TakeTestAsync(s);

        Assert.NotNull(ProgressOf(s, 0).ImprintedAt);
        Assert.NotNull(ProgressOf(s, 1).ImprintedAt);
    }
}
