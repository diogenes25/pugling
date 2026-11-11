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
}
