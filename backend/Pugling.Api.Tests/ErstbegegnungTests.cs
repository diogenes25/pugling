using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-419: a card the child has never seen is shown with its solution first (an introduction) and asked for in the
/// next round - first contact is a property of the card, not of the day's stage.
/// </summary>
public class ErstbegegnungTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string, string)[] ThreeWords = [("la ville", "die Stadt"), ("la rue", "die Straße"), ("le magasin", "das Geschäft")];

    private async Task<(HttpClient Child, int PlanId, int PositionId)> SetupAsync(TestStage stage, bool introduced)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage, introduced: introduced);
        return (await TestApi.ChildAsync(factory), planId, positionId);
    }

    private static async Task<int> StartAsync(HttpClient child, int planId, int positionId, string mode = "Lern") =>
        (await (await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

    private static async Task<List<JsonElement>> CardsAsync(HttpClient child, int planId, int positionId, int sessionId) =>
        (await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;

    private PositionItemProgress Progress(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
    }

    private int PointsOf(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var title = db.PlanPositions.Where(p => p.Id == positionId).Select(p => p.StudyPlan!.Title + " · " + p.Exercise!.Title).Single();
        return db.ChildPointsEntries.Where(e => e.Reason.StartsWith(title)).Sum(e => e.Amount);
    }

    /// <summary>
    /// B-432 AK 5: the introduction day is only spared the penalty, it does not count as met. After introducing every word
    /// without a test the day overview still shows the goal open and no goal reward is booked. Green on the original code
    /// (intent statement); red when the exception moves into <c>IsGoalMetAsync</c> instead of the penalty settlement.
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_NachDemEinfuehren_BleibtDasZielOffen()
    {
        var (child, planId, positionId) = await SetupAsync(TestStage.FreeText, introduced: false);
        var sessionId = await StartAsync(child, planId, positionId);
        for (var i = 0; i < ThreeWords.Length; i++)
            Assert.True((await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, i, wasKnown: true)).IsSuccessStatusCode);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(ThreeWords.Length, db.PositionItemProgress.Count(p => p.PlanPositionId == positionId && p.IntroducedAt == today));
        }

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        var position = overview.GetProperty("today").GetProperty("positions").EnumerateArray()
            .Single(p => p.GetProperty("positionId").GetInt32() == positionId);
        JsonAssert.False(position, "goalMet");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.False(db.PositionGoalRewards.Any(r => r.PlanPositionId == positionId));
        }
    }

    // AK 1: a never-seen atom comes as a display card on a typed stage - with the solution - and earns nothing,
    // while its review introduces it for today.
    [Fact]
    public async Task NieGesehenesWort_KommtAufTippstufe_AlsKennenlernkarte_MitLoesung_UndOhnePunkte()
    {
        var (child, planId, positionId) = await SetupAsync(TestStage.FreeText, introduced: false);
        var sessionId = await StartAsync(child, planId, positionId);

        var cards = await CardsAsync(child, planId, positionId, sessionId);
        Assert.Equal(3, cards.Count);
        Assert.All(cards, c =>
        {
            JsonAssert.True(c, "introduction");
            JsonAssert.True(c, "displayOnly");
            Assert.Equal((int)TestStage.ShowBoth, c.GetProperty("stage").GetInt32());
        });
        Assert.Equal("die Stadt", cards.Single(c => c.GetProperty("itemIndex").GetInt32() == 0).GetProperty("reveal").GetString());

        // The /next path decides the same way.
        var next = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
        JsonAssert.True(next.GetProperty("card"), "introduction");

        // Even the right answer typed onto an introduction earns nothing: it was shown, not asked.
        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "die Stadt"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        // The next card of this round is still an introduction.
        JsonAssert.True(outcome.GetProperty("next"), "introduction");
        Assert.Equal(0, PointsOf(positionId));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prog = Progress(positionId, 0);
        Assert.Equal(today, prog.IntroducedAt);
        Assert.Equal(today, prog.DueOn);
        Assert.Null(prog.LastReviewedAt); // risk 3: the introduction must not block today's graded review
    }

    // AK 2, since B-476: the second round on the same day asks the introduced words - on the lowest rung of the
    // ladder, turned over and judged, not typed. The claim moves the box but pays nothing; the new-content points wait
    // for the first checked hit (KartenstufeTests).
    [Fact]
    public async Task ZweiteRunde_FragtDieEingefuehrtenWoerter_Ab_AlsUmdrehkarte()
    {
        var (child, planId, positionId) = await SetupAsync(TestStage.FreeText, introduced: false);
        var first = await StartAsync(child, planId, positionId);
        foreach (var i in new[] { 0, 1, 2 })
            await TestApi.PositionReviewAsync(child, planId, positionId, first, i, wasKnown: true);

        var second = await StartAsync(child, planId, positionId);
        var cards = await CardsAsync(child, planId, positionId, second);
        Assert.Equal(3, cards.Count); // all three are due again today
        Assert.All(cards, c =>
        {
            JsonAssert.False(c, "introduction");
            JsonAssert.False(c, "displayOnly");
            Assert.Equal((int)TestStage.SelfAssess, c.GetProperty("stage").GetInt32());
        });

        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, second, 0, wasKnown: true))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(2, outcome.GetProperty("box").GetInt32());
        Assert.Equal(0, PointsOf(positionId));
    }

    // AK 3: an introduced atom is never shown as an introduction on a typed stage - existing plans play as before.
    [Fact]
    public async Task EingefuehrtesWort_KommtAufTippstufe_WieBisher()
    {
        var (child, planId, positionId) = await SetupAsync(TestStage.FreeText, introduced: true);
        var sessionId = await StartAsync(child, planId, positionId);

        Assert.All(await CardsAsync(child, planId, positionId, sessionId), c =>
        {
            JsonAssert.False(c, "introduction");
            JsonAssert.False(c, "displayOnly");
        });
        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "die Stadt"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, outcome.GetProperty("awarded").GetInt32());
    }

    // AK 4: a display stage of the day stays what it was - every card display-only, none an introduction.
    [Fact]
    public async Task AnzeigestufeDesTages_BleibtUnveraendert_UndIstKeineEinfuehrung()
    {
        var (child, planId, positionId) = await SetupAsync(TestStage.ShowBoth, introduced: false);
        var sessionId = await StartAsync(child, planId, positionId);

        Assert.All(await CardsAsync(child, planId, positionId, sessionId), c =>
        {
            JsonAssert.False(c, "introduction");
            JsonAssert.True(c, "displayOnly");
        });
    }

    // The info mode records nothing, so it cannot introduce - its cards stay at the day's stage.
    [Fact]
    public async Task InfoModus_KenntKeineEinfuehrung()
    {
        var (child, planId, positionId) = await SetupAsync(TestStage.FreeText, introduced: false);
        var sessionId = await StartAsync(child, planId, positionId, mode: "Info");

        Assert.All(await CardsAsync(child, planId, positionId, sessionId), c => JsonAssert.False(c, "introduction"));
    }

    // A type without a display stage has no way to show a card ungraded - it plays at the day's stage as before.
    [Fact]
    public async Task TypOhneAnzeigestufe_KenntKeineEinfuehrung()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father, ("2 + 3", 5));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, 0, introduced: false);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await StartAsync(child, planId, positionId);

        Assert.All(await CardsAsync(child, planId, positionId, sessionId), c =>
        {
            JsonAssert.False(c, "introduction");
            JsonAssert.False(c, "displayOnly");
        });
    }

    // Review finding: in a round mixing new and known atoms the introductions come first - an introduction is never a
    // hit, so one between two asked cards would break the combo. Serial order would otherwise ask item 0 first. This
    // test holds the order that protects the combo, not the combo itself.
    [Fact]
    public async Task GemischteRunde_ZeigtErstDieNeuen_DannDasBekannte()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial, comboThreshold: 0, introduced: false);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.PositionItemProgress.Add(new PositionItemProgress
            {
                PlanPositionId = positionId,
                ItemIndex = 0,
                IntroducedAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            });
            db.SaveChanges();
        }
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await StartAsync(child, planId, positionId);

        var cards = await CardsAsync(child, planId, positionId, sessionId);
        Assert.Equal([1, 2, 0], cards.Select(c => c.GetProperty("itemIndex").GetInt32()));
        Assert.Equal([true, true, false], cards.Select(c => c.GetProperty("introduction").GetBoolean()));
    }

    // Review finding: an offline client may answer a card other than the cursor's; the next card is then the atom it
    // just introduced, and that one must come asked, not shown again.
    [Fact]
    public async Task AntwortAusserDerReihe_FolgekarteIstDasEbenEingefuehrteWort_UndWirdAbgefragt()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial, introduced: false);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await StartAsync(child, planId, positionId);

        // The cursor stands on item 0; the client answers item 1, and the cursor moves on to item 1.
        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, wasKnown: true))
            .Content.ReadFromJsonAsync<JsonElement>();
        var next = outcome.GetProperty("next");
        Assert.Equal(1, next.GetProperty("itemIndex").GetInt32());
        JsonAssert.False(next, "introduction");
    }

    // B-433: the history entries of one atom of the position, oldest first.
    private List<ItemReviewEvent> HistoryOf(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var itemId = db.ExerciseItems
            .Where(i => i.ExerciseId == db.PlanPositions.Where(p => p.Id == positionId).Select(p => p.ExerciseId).Single())
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.Id).Skip(itemIndex).Select(i => i.Id).First();
        return [.. db.ItemReviewEvents.Where(e => e.PlanPositionId == positionId && e.ItemId == itemId).OrderBy(e => e.Id)];
    }

    private void SetProgress(int positionId, int itemIndex, Action<PositionItemProgress> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var prog = db.PositionItemProgress.SingleOrDefault(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
        if (prog is null) db.PositionItemProgress.Add(prog = new PositionItemProgress { PlanPositionId = positionId, ItemIndex = itemIndex });
        change(prog);
        db.SaveChanges();
    }

    private void SetMaxBox(int positionId, int maxBox)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.PlanPositions.Single(p => p.Id == positionId).MaxBox = maxBox;
        db.SaveChanges();
    }

    // B-433 AK 1: the supervisor clicking through an introduction card of the child's round introduces nothing - the child
    // still gets the card as an introduction afterwards. Red if the role check at the first-contact stamp is missing.
    [Fact]
    public async Task VaterRunde_FuehrtKeinWortEin_DasKindBekommtDieKennenlernkarteWeiterhin()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial, introduced: false);

        var fatherSession = await StartAsync(father, planId, positionId);
        JsonAssert.True((await CardsAsync(father, planId, positionId, fatherSession))[0], "introduction");
        (await TestApi.PositionReviewAsync(father, planId, positionId, fatherSession, 0)).EnsureSuccessStatusCode();
        Assert.Null(Progress(positionId, 0).IntroducedAt);

        var child = await TestApi.ChildAsync(factory);
        var cards = await CardsAsync(child, planId, positionId, await StartAsync(child, planId, positionId));
        JsonAssert.True(cards.Single(c => c.GetProperty("itemIndex").GetInt32() == 0), "introduction");
    }

    // B-433 AK 3: the child holds an introduction card, the atom is introduced in a second session meanwhile, and the
    // card's answerless "next" arrives: no verdict - box, review count, due date and the day's review stamp stay, no
    // history entry, the cursor moves on. Red if decision 2 is missing: on the ladder the empty claim stamps the day's
    // review and books "wrong" into the history; with a top box of 1 the card is asked typed and the review counts.
    [Theory]
    [InlineData(5)]
    [InlineData(1)]
    public async Task AntwortloseBewertung_EinerHeuteAnderswoEingefuehrtenKarte_IstKeinUrteil(int maxBox)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial, introduced: false);
        SetMaxBox(positionId, maxBox);
        var child = await TestApi.ChildAsync(factory);

        var held = await StartAsync(child, planId, positionId);
        JsonAssert.True((await CardsAsync(child, planId, positionId, held))[0], "introduction");
        var other = await StartAsync(child, planId, positionId);
        (await TestApi.PositionReviewAsync(child, planId, positionId, other, 0)).EnsureSuccessStatusCode();
        var before = Progress(positionId, 0);
        var historyBefore = HistoryOf(positionId, 0).Count;

        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, held, 0))
            .Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("next").GetProperty("itemIndex").GetInt32());
        var after = Progress(positionId, 0);
        Assert.Equal(before.Box, after.Box);
        Assert.Equal(0, after.ReviewCount);
        Assert.Equal(before.DueOn, after.DueOn);
        Assert.Null(after.LastReviewedAt);
        Assert.Equal(historyBefore, HistoryOf(positionId, 0).Count);
    }

    // B-433 AK 4: an answerless review stays a wrong answer when the atom was introduced on an earlier day or was
    // already checked today - the exemption is the introduction day's unchecked atom only. Red if decision 2 is widened
    // to every empty answer (then no "wrong" lands in the history).
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    public async Task AntwortloseBewertung_AufFrueherEingefuehrtesOderGeprueftesWort_BleibtFalsch(int introducedDaysAgo,
        int reviewCount)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial, introduced: false);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        SetProgress(positionId, 0, p =>
        {
            p.IntroducedAt = today.AddDays(introducedDaysAgo);
            p.ReviewCount = reviewCount;
            p.DueOn = today;
        });
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await StartAsync(child, planId, positionId);

        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0))
            .Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.False(outcome, "wasCorrect");
        var history = HistoryOf(positionId, 0);
        Assert.False(Assert.Single(history).WasCorrect);
        Assert.NotNull(Progress(positionId, 0).LastReviewedAt);
    }
}
