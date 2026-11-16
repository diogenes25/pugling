using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-476: an introduced vocabulary card climbs a ladder with its Leitner box - box 1 is turned over and judged, box 2
/// offers a choice, from box 3 on it is asked at the position's stage. The position's stage is the ceiling.
/// </summary>
public class KartenstufeTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string, string)[] FourWords =
        [("la ville", "die Stadt"), ("la rue", "die Straße"), ("le magasin", "das Geschäft"), ("la gare", "der Bahnhof")];

    private sealed record Setup(HttpClient Child, int ChildId, int PlanId, int PositionId);

    // A fresh child per test: the points assertions read its ledger, and the default child is shared by the fixture.
    private async Task<Setup> SetupAsync(TestStage stage, bool useLeitner = true, bool requireTypedTest = false,
        PracticeOrder order = PracticeOrder.WeakestFirst, int? maxBox = null)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "4761";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Leiter-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", FourWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage, childId: childId,
            useLeitner: useLeitner, requireTypedTest: requireTypedTest, orderStrategy: order);
        if (maxBox is { } top) SetMaxBox(positionId, top);
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    private void SetMaxBox(int positionId, int maxBox)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.PlanPositions.Single(p => p.Id == positionId).MaxBox = maxBox;
        db.SaveChanges();
    }

    private void SetBoxes(int positionId, params int[] boxes)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        foreach (var prog in db.PositionItemProgress.Where(p => p.PlanPositionId == positionId))
            prog.Box = boxes[prog.ItemIndex];
        db.SaveChanges();
    }

    private PositionItemProgress Progress(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionItemProgress.AsEnumerable().Single(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
    }

    private int CoinsOf(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ChildPointsEntries.Where(e => e.ChildId == childId).AsEnumerable()
            .Where(e => PointKindCurrency.Of(e.Kind) == Currency.Coins).Sum(e => e.Amount);
    }

    private static async Task<int> StartAsync(Setup s, string mode = "Lern") =>
        await TestApi.IdAsync(await s.Child.PostAsJsonAsync(TestApi.PracticeBase(s.PlanId, s.PositionId), new { mode }));

    private static async Task<List<JsonElement>> CardsAsync(Setup s, int sessionId) =>
        (await s.Child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(s.PlanId, s.PositionId)}/{sessionId}/cards"))!;

    private static JsonElement CardOf(List<JsonElement> cards, int itemIndex) =>
        cards.Single(c => c.GetProperty("itemIndex").GetInt32() == itemIndex);

    private static int StageOf(List<JsonElement> cards, int itemIndex) =>
        CardOf(cards, itemIndex).GetProperty("stage").GetInt32();

    private static async Task<JsonElement> ReviewAsync(Setup s, int sessionId, int itemIndex, string? givenAnswer = null,
        bool? wasKnown = null) =>
        await (await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, itemIndex, givenAnswer, wasKnown))
            .Content.ReadFromJsonAsync<JsonElement>();

    // AK 1 + 2: box 1 turns the card over (solution revealed for the judgment), box 2 offers a choice that holds the
    // answer, box 3 and above ask at the target - typed, no solution on the card. Red if the ladder is skipped (all four
    // FreeText) or if a rung is mapped to the wrong box.
    [Fact]
    public async Task Fach_Bestimmt_Die_Kartenform_Bis_Zur_Zielstufe()
    {
        var s = await SetupAsync(TestStage.FreeText);
        SetBoxes(s.PositionId, 1, 2, 3, 5);
        var cards = await CardsAsync(s, await StartAsync(s));

        Assert.Equal((int)TestStage.SelfAssess, StageOf(cards, 0));
        Assert.Equal("die Stadt", CardOf(cards, 0).GetProperty("reveal").GetString());
        JsonAssert.False(CardOf(cards, 0), "displayOnly");

        Assert.Equal((int)TestStage.MultipleChoice, StageOf(cards, 1));
        Assert.Contains("die Straße", CardOf(cards, 1).GetProperty("choices").EnumerateArray().Select(c => c.GetString()));
        Assert.Equal(JsonValueKind.Null, CardOf(cards, 1).GetProperty("reveal").ValueKind);

        Assert.Equal((int)TestStage.FreeText, StageOf(cards, 2));
        Assert.Equal((int)TestStage.FreeText, StageOf(cards, 3));
        Assert.Equal(JsonValueKind.Null, CardOf(cards, 2).GetProperty("reveal").ValueKind);
    }

    // AK 3: the target is a ceiling. A position set to self-assessment never climbs into multiple choice, and one set to
    // multiple choice never climbs past it. Red if the ladder ignores the target.
    [Theory]
    [InlineData(TestStage.SelfAssess, 2, TestStage.SelfAssess)]
    [InlineData(TestStage.SelfAssess, 4, TestStage.SelfAssess)]
    [InlineData(TestStage.MultipleChoice, 1, TestStage.SelfAssess)]
    [InlineData(TestStage.MultipleChoice, 4, TestStage.MultipleChoice)]
    [InlineData(TestStage.LetterBoxes, 2, TestStage.MultipleChoice)]
    [InlineData(TestStage.LetterBoxes, 3, TestStage.LetterBoxes)]
    public async Task Zielstufe_Ist_Die_Obergrenze(TestStage target, int box, TestStage expected)
    {
        var s = await SetupAsync(target);
        SetBoxes(s.PositionId, box, box, box, box);
        var cards = await CardsAsync(s, await StartAsync(s));
        Assert.All(cards, c => Assert.Equal((int)expected, c.GetProperty("stage").GetInt32()));
    }

    // AK 4: "known" on the self-assessed rung moves the box and pays nothing - also under RequireTypedTest, which on a
    // position-wide self-assessment stage freezes the box. The review count stays, so the first checked hit is still
    // "new content". Red if the claim pays, if it does not move the box, or if it counts as a review.
    [Fact]
    public async Task Umdrehkarte_Gewusst_Steigt_Ein_Fach_Ohne_Muenzen_Auch_Mit_RequireTypedTest()
    {
        var s = await SetupAsync(TestStage.FreeText, requireTypedTest: true);
        SetBoxes(s.PositionId, 1, 1, 1, 1);
        var sessionId = await StartAsync(s);

        var outcome = await ReviewAsync(s, sessionId, 0, wasKnown: true);
        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(2, outcome.GetProperty("box").GetInt32());
        Assert.Equal(0, CoinsOf(s.ChildId));

        var prog = Progress(s.PositionId, 0);
        Assert.Equal(2, prog.Box);
        Assert.Equal(0, prog.ReviewCount);
        Assert.NotNull(prog.LastReviewedAt); // one climb per day: the same card does not climb twice today
    }

    // AK 4, second half: after the claim the card comes as a choice, and the first checked hit pays the new-content
    // points of the position (10 in the seed). Red if the claim had used up the "new" (ReviewCount bumped).
    [Fact]
    public async Task Erster_Gepruefter_Treffer_Nach_Der_Umdrehkarte_Zahlt_Die_Neu_Punkte()
    {
        var s = await SetupAsync(TestStage.FreeText);
        SetBoxes(s.PositionId, 1, 1, 1, 1);
        await ReviewAsync(s, await StartAsync(s), 0, wasKnown: true);

        // The next due day of box 2; the seeded plan runs five days.
        factory.Clock.FreezeNow();
        factory.Clock.Advance(TimeSpan.FromDays(2));
        try
        {
            var sessionId = await StartAsync(s);
            Assert.Equal((int)TestStage.MultipleChoice, StageOf(await CardsAsync(s, sessionId), 0));
            var outcome = await ReviewAsync(s, sessionId, 0, givenAnswer: "die Stadt");
            JsonAssert.True(outcome, "wasCorrect");
            Assert.Equal(10, outcome.GetProperty("awarded").GetInt32());
            Assert.Equal(3, outcome.GetProperty("box").GetInt32());
        }
        finally
        {
            factory.Clock.Advance(TimeSpan.FromDays(-2));
        }
    }

    // AK 5: a wrong choice drops the card to box 1, and the next round turns it over again. Red if a miss on the choice
    // rung kept the box or if box 1 were asked at the target.
    [Fact]
    public async Task Falsche_Auswahl_Faellt_Zurueck_Auf_Die_Umdrehkarte()
    {
        var s = await SetupAsync(TestStage.FreeText);
        SetBoxes(s.PositionId, 2, 3, 3, 3);
        var outcome = await ReviewAsync(s, await StartAsync(s), 0, givenAnswer: "die Straße");
        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(1, outcome.GetProperty("box").GetInt32());

        Assert.Equal((int)TestStage.SelfAssess, StageOf(await CardsAsync(s, await StartAsync(s)), 0));
    }

    // AK 6: the review is graded as the card's form, never as the day's. On box 1 a typed answer without a judgment is
    // no claim at all; on box 2 the picked choice is checked against the solution. Red if the review used the day's
    // stage (box 1 would then grade the typed text as right).
    [Fact]
    public async Task Review_Wertet_Die_Form_Der_Karte_Nicht_Die_Des_Tages()
    {
        var s = await SetupAsync(TestStage.FreeText);
        SetBoxes(s.PositionId, 1, 2, 3, 3);
        var sessionId = await StartAsync(s);

        var typedOnTurnOver = await ReviewAsync(s, sessionId, 0, givenAnswer: "die Stadt");
        JsonAssert.False(typedOnTurnOver, "wasCorrect");
        Assert.Equal(1, Progress(s.PositionId, 0).Box);

        var choice = await ReviewAsync(s, sessionId, 1, givenAnswer: "die Straße");
        JsonAssert.True(choice, "wasCorrect");
        Assert.True(choice.GetProperty("awarded").GetInt32() > 0);
    }

    // The follow-up card a review hands back follows the ladder as well (its box, not the day's stage). Red if the
    // review's `next` card skipped the ladder.
    [Fact]
    public async Task Folgekarte_Im_Review_Folgt_Der_Leiter()
    {
        var s = await SetupAsync(TestStage.FreeText, order: PracticeOrder.Serial);
        SetBoxes(s.PositionId, 3, 2, 3, 3);
        var sessionId = await StartAsync(s);
        var first = await ReviewAsync(s, sessionId, 0, givenAnswer: "die Stadt");
        var next = first.GetProperty("next");
        Assert.Equal(1, next.GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)TestStage.MultipleChoice, next.GetProperty("stage").GetInt32());
    }

    // Combo: unchecked turn-over cards come first in a mixed round, as introductions do (B-419) - a claim between two
    // checked hits would break the child's streak. Serial order puts the box-1 card last on its own; red if the
    // ladder's rung is not sorted to the front.
    [Fact]
    public async Task Umdrehkarten_Stehen_In_Der_Runde_Vorn()
    {
        var s = await SetupAsync(TestStage.FreeText, order: PracticeOrder.Serial);
        SetBoxes(s.PositionId, 3, 3, 3, 1);
        var cards = await CardsAsync(s, await StartAsync(s));
        Assert.Equal(3, cards[0].GetProperty("itemIndex").GetInt32());
        Assert.Equal([0, 1, 2], cards.Skip(1).Select(c => c.GetProperty("itemIndex").GetInt32()));
    }

    // AK 7: without Leitner no box ever moves, so the ladder would pin the card to its lowest rung - the day's stage
    // holds. The info mode records nothing and keeps the day's stage too. Red if either guard is dropped.
    [Theory]
    [InlineData(false, "Lern")]
    [InlineData(true, "Info")]
    public async Task Ohne_Leitner_Und_Im_Infomodus_Gilt_Die_Tagesstufe(bool useLeitner, string mode)
    {
        var s = await SetupAsync(TestStage.FreeText, useLeitner: useLeitner);
        SetBoxes(s.PositionId, 1, 2, 1, 2);
        var cards = await CardsAsync(s, await StartAsync(s, mode));
        Assert.All(cards, c => Assert.Equal((int)TestStage.FreeText, c.GetProperty("stage").GetInt32()));
    }

    // A card in the position's top box is asked at the target even when the ladder has rungs above it - with MaxBox 1
    // or 2 it could never climb further. Red if the ladder ignored MaxBox (box 1 would be turned over forever, box 2
    // a choice forever).
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public async Task Oberstes_Fach_Der_Position_Fragt_Die_Zielstufe(int maxBox, int box)
    {
        var s = await SetupAsync(TestStage.FreeText, maxBox: maxBox);
        SetBoxes(s.PositionId, box, box, box, box);
        var cards = await CardsAsync(s, await StartAsync(s));
        Assert.All(cards, c => Assert.Equal((int)TestStage.FreeText, c.GetProperty("stage").GetInt32()));
    }

    // The one-at-a-time path decides like /cards. Red if /next built its card from the day's stage.
    [Fact]
    public async Task Next_Liefert_Die_Karte_Auf_Ihrer_Sprosse()
    {
        var s = await SetupAsync(TestStage.FreeText, order: PracticeOrder.Serial);
        SetBoxes(s.PositionId, 2, 3, 3, 3);
        var sessionId = await StartAsync(s);
        var next = await s.Child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(s.PlanId, s.PositionId)}/{sessionId}/next");
        Assert.Equal(0, next.GetProperty("card").GetProperty("itemIndex").GetInt32());
        Assert.Equal((int)TestStage.MultipleChoice, next.GetProperty("card").GetProperty("stage").GetInt32());
    }

    private static string TestBase(Setup s) => $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

    // Walks a test from start to end and returns each question's (itemIndex, stage, choices), answering every one
    // correctly in its form (the choice or the typed solution). `client` starts it - the child or the supervisor.
    private async Task<(int AttemptId, List<(int Index, int Stage, bool HasChoices)> Questions)> TakeTestAsync(
        Setup s, HttpClient client, int? stage = null)
    {
        var start = await client.PostAsJsonAsync(TestBase(s), new { stage });
        start.EnsureSuccessStatusCode();
        var attempt = await start.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var questions = new List<(int, int, bool)>();
        for (var i = 0; i < attempt.GetProperty("totalItems").GetInt32(); i++)
        {
            var item = (await client.GetFromJsonAsync<JsonElement>($"{TestBase(s)}/{attemptId}/next")).GetProperty("item");
            var index = item.GetProperty("itemIndex").GetInt32();
            questions.Add((index, item.GetProperty("stage").GetInt32(), item.GetProperty("choices").ValueKind == JsonValueKind.Array));
            // A choice is answered with the German side, a writing stage with the learnt word (B-597).
            var stageOf = item.GetProperty("stage").GetInt32();
            var answer = stageOf == (int)TestStage.MultipleChoice ? FourWords[index].Item2 : FourWords[index].Item1;
            (await client.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/answer", new { itemIndex = index, givenAnswer = answer }))
                .EnsureSuccessStatusCode();
        }
        return (attemptId, questions);
    }

    private List<int> RowStages(int attemptId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return [.. db.TestAttempts.Where(t => t.Id == attemptId).SelectMany(t => t.Results).OrderBy(r => r.ItemIndex).Select(r => r.StageValue)];
    }

    // AK 8: the test follows the ladder but never asks a mere claim - box 1 and 2 are asked as a choice, box 3 and above
    // at the target. Each row keeps its stage, and the attempt passes when every question is answered right in its
    // form. Red if the test asked every word at the target (the risk the father decided against) or turned box 1 over.
    [Fact]
    public async Task Test_Fragt_Fach_1_Und_2_Als_Auswahl_Ab_Fach_3_Schriftlich()
    {
        var s = await SetupAsync(TestStage.FreeText, requireTypedTest: true);
        SetBoxes(s.PositionId, 1, 2, 3, 5);

        var (attemptId, questions) = await TakeTestAsync(s, s.Child);
        var byIndex = questions.ToDictionary(q => q.Index);
        Assert.Equal((int)TestStage.MultipleChoice, byIndex[0].Stage);
        Assert.True(byIndex[0].HasChoices);
        Assert.Equal((int)TestStage.MultipleChoice, byIndex[1].Stage);
        Assert.Equal((int)TestStage.FreeText, byIndex[2].Stage);
        Assert.False(byIndex[2].HasChoices);
        Assert.Equal((int)TestStage.FreeText, byIndex[3].Stage);
        Assert.Equal([6, 6, 4, 4], RowStages(attemptId));

        var result = await (await s.Child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(result, "passed");
        // The attempt still names the target: that is what the father set and what the result screen heads with.
        Assert.Equal((int)TestStage.FreeText, result.GetProperty("stage").GetInt32());

        // The learning history records each answer in the form it was given - two choices, two typed.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var history = db.ItemReviewEvents.Where(e => e.ChildId == s.ChildId && e.Source == ItemReviewSource.Test)
            .Select(e => e.StageValue).OrderBy(v => v).ToList();
        Assert.Equal([4, 4, 6, 6], history);
    }

    // AK 8: a word never seen before (here: nothing introduced, the test falls back to the whole pool) is asked as a
    // choice too. Red if a missing progress row were asked at the target.
    [Fact]
    public async Task Test_Fragt_Ein_Ungesehenes_Wort_Als_Auswahl()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", FourWords);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kalt-Kind"), pin = "4762" }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId,
            introduced: false);
        var s = new Setup(await TestApi.ChildAsync(factory, childId, "4762"), childId, planId, positionId);

        var (_, questions) = await TakeTestAsync(s, s.Child);
        Assert.Equal(4, questions.Count);
        Assert.All(questions, q => Assert.Equal((int)TestStage.MultipleChoice, q.Stage));
    }

    // AK 9: without Leitner no box moves, an unchecked target has no checked rung to raise to, and a stage the supervisor
    // picks for a preview holds for every question. The first and third rows go red if their guard is dropped. The
    // self-assessment row guards the ceiling instead: its own guard is an equivalent mutant (the ladder never climbs
    // above an unchecked target, so the loop returns the target anyway) - it goes red only if the ladder lost its cap.
    [Theory]
    [InlineData(TestStage.FreeText, false, null, TestStage.FreeText)]
    [InlineData(TestStage.SelfAssess, true, null, TestStage.SelfAssess)]
    [InlineData(TestStage.FreeText, true, TestStage.LetterBoxes, TestStage.LetterBoxes)]
    public async Task Test_Mit_Einer_Stufe_Fuer_Alle_Fragen(TestStage target, bool useLeitner, TestStage? picked, TestStage expected)
    {
        var s = await SetupAsync(target, useLeitner: useLeitner);
        SetBoxes(s.PositionId, 1, 2, 1, 2);
        var client = picked is null ? s.Child : await TestApi.AdultAsync(factory);
        var start = await client.PostAsJsonAsync(TestBase(s), new { stage = (int?)picked });
        start.EnsureSuccessStatusCode();
        var attemptId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptId").GetInt32();
        Assert.All(RowStages(attemptId), stage => Assert.Equal((int)expected, stage));
    }

    // AK 10: the child moves the box (no practice, or wrong on purpose, keeps a word in box 1), so the calendar ends the
    // ladder: seven days after its introduction a word is examined at the target whatever its box; six days is still a
    // choice. Red if the deadline is dropped (item 0 would be a choice) or off by one in either direction.
    [Fact]
    public async Task Test_Fragt_Nach_Sieben_Tagen_Schriftlich_Auch_Auf_Fach_1()
    {
        var s = await SetupAsync(TestStage.FreeText, requireTypedTest: true);
        SetBoxes(s.PositionId, 1, 1, 2, 3);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var rows = db.PositionItemProgress.Where(p => p.PlanPositionId == s.PositionId).ToList();
            rows.Single(p => p.ItemIndex == 0).IntroducedAt = today.AddDays(-7);
            rows.Single(p => p.ItemIndex == 1).IntroducedAt = today.AddDays(-6);
            rows.Single(p => p.ItemIndex == 2).IntroducedAt = today.AddDays(-30);
            db.SaveChanges();
        }

        var (attemptId, _) = await TakeTestAsync(s, s.Child);
        Assert.Equal([4, 6, 4, 4], RowStages(attemptId));
    }

    // AK 11, without a dose: a child that never practised meets the words cold in the test. The test introduces each
    // as it is answered, so they are a choice now - not in writing because the position joined the plan eight days
    // ago - and their week starts today: afterwards practice turns them over (box 1). The father's preview of the same
    // position, answered in full, introduces nothing. Red if only a dose let the test introduce (IntroducedAt stays
    // empty), if a cold word counted from any day before today (in writing), or if the preview moved the child's state.
    [Fact]
    public async Task Test_Fuehrt_Ungesehene_Woerter_Auch_Ohne_Dosis_Ein()
    {
        var s = await SetupNeverPractisedAsync(8, 8, dose: 0);
        var father = await TestApi.AdultAsync(factory);
        await TakeTestAsync(s, father);
        Assert.Empty(IntroducedOn(s.PositionId));

        var (attemptId, _) = await TakeTestAsync(s, s.Child);
        Assert.All(RowStages(attemptId), stage => Assert.Equal((int)TestStage.MultipleChoice, stage));
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        Assert.Equal([today, today, today, today], IntroducedOn(s.PositionId));
        Assert.All(await CardsAsync(s, await StartAsync(s)), c => Assert.Equal((int)TestStage.SelfAssess, c.GetProperty("stage").GetInt32()));
    }

    // Without a dose only an answer introduces, not the start: a start alone would count the whole pool as new words
    // (a NewWords mission paid for one click) and start the ladder of words the child never saw. One answer through the
    // step-wise path and one through a bulk submission introduce exactly those two words. Red if the start introduced
    // (four rows after the start), or if either answer path did not introduce its word.
    [Fact]
    public async Task Teststart_Allein_Fuehrt_Nichts_Ein_Erst_Die_Antwort()
    {
        var s = await SetupNeverPractisedAsync(8, 8, dose: 0);
        var start = await s.Child.PostAsJsonAsync(TestBase(s), new { });
        start.EnsureSuccessStatusCode();
        var attemptId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptId").GetInt32();
        Assert.Empty(IntroducedOn(s.PositionId));

        var first = (await s.Child.GetFromJsonAsync<JsonElement>($"{TestBase(s)}/{attemptId}/next")).GetProperty("item")
            .GetProperty("itemIndex").GetInt32();
        (await s.Child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/answer", new { itemIndex = first, givenAnswer = "x" }))
            .EnsureSuccessStatusCode();
        Assert.Equal([first], IntroducedIndices(s.PositionId));

        var other = Enumerable.Range(0, FourWords.Length).First(i => i != first);
        (await s.Child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/submit",
            new { answers = new[] { new { itemIndex = other, givenAnswer = "x", wasKnown = (bool?)null } } }))
            .EnsureSuccessStatusCode();
        Assert.Equal(new[] { first, other }.Order(), IntroducedIndices(s.PositionId));
    }

    private List<int> IntroducedIndices(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return [.. db.PositionItemProgress.Where(p => p.PlanPositionId == positionId && p.IntroducedAt != null)
            .OrderBy(p => p.ItemIndex).Select(p => p.ItemIndex)];
    }

    private List<DateOnly> IntroducedOn(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return [.. db.PositionItemProgress.Where(p => p.PlanPositionId == positionId && p.IntroducedAt != null)
            .OrderBy(p => p.ItemIndex).Select(p => p.IntroducedAt!.Value)];
    }

    // AK 11, under a daily dose (2 a day) and honestly behind it: the position joined the plan thirty days ago, but the
    // child has played only once, so words 2 and 3 meet the test cold. The test introduces them itself, so their week
    // starts today and they are a choice - not in writing at first sight because the dose would have introduced them
    // weeks ago (the dose never catches up a missed day, B-476 decision 7, "from the actual introduction"). Words 0 and 1,
    // introduced thirty days ago, are in writing. Red if a cold dose word counted from the day the dose would have
    // introduced it or from the position's day (words 2/3 in writing), or if the introduction were ignored (0/1 a choice).
    [Fact]
    public async Task Kaltes_Dosiswort_Zaehlt_Ab_Dem_Testtag_Auch_Hinter_Der_Dosis()
    {
        var s = await SetupNeverPractisedAsync(30, 30, dose: 2);
        var (_, questions) = await TakeTestAsync(s, s.Child);
        var stages = questions.ToDictionary(q => q.Index, q => q.Stage);
        Assert.Equal((int)TestStage.FreeText, stages[0]);
        Assert.Equal((int)TestStage.FreeText, stages[1]);
        Assert.Equal((int)TestStage.MultipleChoice, stages[2]);
        Assert.Equal((int)TestStage.MultipleChoice, stages[3]);
    }

    // Practice follows the deadline too (decision 7, revised): a word the test asks in writing must have been asked in
    // writing in practice. Seven days after its introduction a box-1 card is typed; six days in, it is still turned over.
    // Red if the deadline lived only in the test.
    [Theory]
    [InlineData(7, TestStage.FreeText)]
    [InlineData(6, TestStage.SelfAssess)]
    public async Task Ueben_Zieht_Mit_Nach_Sieben_Tagen_Geschrieben(int introducedDaysAgo, TestStage expected)
    {
        var s = await SetupAsync(TestStage.FreeText);
        SetBoxes(s.PositionId, 1, 1, 1, 1);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            foreach (var prog in db.PositionItemProgress.Where(p => p.PlanPositionId == s.PositionId))
                prog.IntroducedAt = today.AddDays(-introducedDaysAgo);
            db.SaveChanges();
        }
        var cards = await CardsAsync(s, await StartAsync(s));
        Assert.All(cards, c => Assert.Equal((int)expected, c.GetProperty("stage").GetInt32()));
    }

    // A child that never practised: nothing introduced, the plan started and the position was added the given days ago.
    // Under a dose, words 0 and 1 count as introduced on the position's day (one played day), words 2 and 3 stay cold.
    private async Task<Setup> SetupNeverPractisedAsync(int planDaysAgo, int positionDaysAgo, int dose)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", FourWords);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Nie-Kind"), pin = "4763" }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId,
            introduced: false);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.StudyPlans.Single(p => p.Id == planId).StartDate = today.AddDays(-planDaysAgo);
            var position = db.PlanPositions.Single(p => p.Id == positionId);
            position.CreatedAt = today.AddDays(-positionDaysAgo).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
            position.NewItemsPerDay = dose;
            if (dose > 0)
            {
                var added = today.AddDays(-positionDaysAgo);
                foreach (var i in new[] { 0, 1 })
                    db.PositionItemProgress.Add(new PositionItemProgress { PlanPositionId = positionId, ItemIndex = i, IntroducedAt = added });
            }
            db.SaveChanges();
        }
        return new Setup(await TestApi.ChildAsync(factory, childId, "4763"), childId, planId, positionId);
    }

    // AK 10: the week counts from the actual introduction, not from the day the position joined the plan - a word met
    // late gets its own week of ladder like any other (B-476 decision 7). A position eight days on the plan, its words
    // first practised yesterday: practice turns the box-1 cards over and the test asks them as a choice. Red if the
    // deadline counted from the position's day for an introduced word (both in writing).
    [Fact]
    public async Task Spaet_Eingefuehrtes_Wort_Bekommt_Seine_Woche()
    {
        var s = await SetupNeverPractisedAsync(8, 8, dose: 0);
        var yesterday = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-1);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            foreach (var i in Enumerable.Range(0, FourWords.Length))
                db.PositionItemProgress.Add(new PositionItemProgress { PlanPositionId = s.PositionId, ItemIndex = i, IntroducedAt = yesterday });
            db.SaveChanges();
        }

        Assert.All(await CardsAsync(s, await StartAsync(s)), c => Assert.Equal((int)TestStage.SelfAssess, c.GetProperty("stage").GetInt32()));
        var (attemptId, _) = await TakeTestAsync(s, s.Child);
        Assert.All(RowStages(attemptId), stage => Assert.Equal((int)TestStage.MultipleChoice, stage));
    }

    // The day logic runs on the server's clock, so a position is stamped from that clock, not the wall's - otherwise its
    // date would sit on another day in tests and near midnight. Red if CreatedAt fell back to the entity's
    // DateTime.UtcNow.
    [Fact]
    public async Task Position_Traegt_Das_Datum_Der_Serveruhr()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", FourWords);
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var past = factory.Clock.GetUtcNow().AddDays(-40);
        factory.Clock.FreezeAt(past);
        try
        {
            var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
                $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, cadence = "Daily", stage = 4 }));
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(DateOnly.FromDateTime(past.UtcDateTime),
                DateOnly.FromDateTime(db.PlanPositions.Single(p => p.Id == positionId).CreatedAt));
        }
        finally
        {
            factory.Clock.FreezeAt(past.AddDays(40));
        }
    }
}
