using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-435: a position scoped to "new" contents shows a word on its introduction day and asks it in the follow-up round
/// of that same day; from the next day on the word is review material and leaves the scope - unless it still needs its
/// learn card and is not imprinted yet, then it stays until it is (B-653). "Old" is unchanged.
/// </summary>
public class UmfangNeuTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string, string)[] ThreeWords = [("la ville", "die Stadt"), ("la rue", "die Straße"), ("le magasin", "das Geschäft")];

    private async Task<(HttpClient Child, int PlanId, int PositionId)> SetupAsync(ItemScope scope, bool useLeitner = true)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            useLeitner: useLeitner, orderStrategy: PracticeOrder.Serial, introduced: false);
        using (var s = factory.Services.CreateScope())
        {
            var db = s.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.PlanPositions.Single(p => p.Id == positionId).Scope = scope;
            db.SaveChanges();
        }
        return (await TestApi.ChildAsync(factory), planId, positionId);
    }

    private static async Task<List<JsonElement>> NewRoundAsync(HttpClient child, int planId, int positionId)
    {
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        return (await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards"))!;
    }

    private static int[] Indices(List<JsonElement> cards) =>
        [.. cards.Select(c => c.GetProperty("itemIndex").GetInt32()).Order()];

    // Plays an introduction round and clicks through the given cards only, so the rest stays never-introduced.
    private static async Task IntroduceAsync(HttpClient child, int planId, int positionId, params int[] indices)
    {
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        // B-643: the learn card recalled right from memory - a word only seen would come back as a learn card.
        foreach (var i in indices)
            (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, i, givenAnswer: ThreeWords[i].Item1,
                wasKnown: true)).EnsureSuccessStatusCode();
    }

    // Runs the body the given number of days later on the frozen test clock and puts the clock back afterwards.
    private async Task OnDayAsync(int days, Func<Task> body)
    {
        factory.Clock.FreezeNow();
        factory.Clock.Advance(TimeSpan.FromDays(days));
        try { await body(); }
        finally { factory.Clock.Advance(TimeSpan.FromDays(-days)); }
    }

    // AK 1: after the introduction round the follow-up round of the same day asks the introduced words. Red if "new"
    // means "never introduced" again - then the follow-up round is empty.
    [Fact]
    public async Task Neu_Mit_Leitner_Fragt_Die_Heute_Eingefuehrten_Woerter_In_Der_Folgerunde_Ab()
    {
        var (child, planId, positionId) = await SetupAsync(ItemScope.New);
        await IntroduceAsync(child, planId, positionId, 0, 1, 2);

        var cards = await NewRoundAsync(child, planId, positionId);
        Assert.Equal(new[] { 0, 1, 2 }, Indices(cards));
        Assert.All(cards, c => JsonAssert.False(c, "introduction"));
    }

    // AK 2: the next day the words introduced yesterday are out of the scope, the never-introduced one is in. Red if the
    // condition widens to "introduced at any time".
    [Fact]
    public async Task Neu_Mit_Leitner_Laesst_Am_Folgetag_Nur_Die_Nie_Eingefuehrten_Im_Umfang()
    {
        var (child, planId, positionId) = await SetupAsync(ItemScope.New);
        await IntroduceAsync(child, planId, positionId, 0, 1);

        await OnDayAsync(1, async () =>
            Assert.Equal(new[] { 2 }, Indices(await NewRoundAsync(child, planId, positionId))));
    }

    // AK 3: without Leitner no review is ever counted, so "new" must end by the day: asked on the introduction day, gone
    // the day after. Red if the rule checks ReviewCount == 0 (then the words stay in the next day) or the old
    // "never introduced" (then today's follow-up round is empty).
    [Fact]
    public async Task Neu_Ohne_Leitner_Fragt_Am_Einfuehrungstag_Ab_Und_Laesst_Die_Woerter_Am_Folgetag_Los()
    {
        var (child, planId, positionId) = await SetupAsync(ItemScope.New, useLeitner: false);
        await IntroduceAsync(child, planId, positionId, 0, 1);

        var today = await NewRoundAsync(child, planId, positionId);
        Assert.Equal(new[] { 0, 1, 2 }, Indices(today));
        Assert.All(today.Where(c => c.GetProperty("itemIndex").GetInt32() < 2), c => JsonAssert.False(c, "introduction"));

        await OnDayAsync(1, async () =>
            Assert.Equal(new[] { 2 }, Indices(await NewRoundAsync(child, planId, positionId))));
    }

    // ---- B-653: an open learn card stays in the "new" scope until it is imprinted ----

    // Meets the given word on its learn card and gives up - introduced, not imprinted.
    private static async Task MeetOnlyAsync(HttpClient child, int planId, int positionId, int index)
    {
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, index, givenAnswer: null, wasKnown: false))
            .EnsureSuccessStatusCode();
    }

    // AK 1 and 2: a word met yesterday but not imprinted is a learn card today, still the day after, and leaves the
    // scope the day after it was imprinted. Red if "new" stays "never or today introduced" (word 0 missing on day 1),
    // red if only the day after the introduction is let in (word 0 missing on day 2), red if every introduced word is
    // let in (word 1, imprinted, comes back on day 1, and word 0 on day 3).
    [Fact]
    public async Task Neu_Bringt_Ein_Nicht_Eingepraegtes_Wort_Als_Lernkarte_Zurueck_Bis_Es_Sitzt()
    {
        var (child, planId, positionId) = await SetupAsync(ItemScope.New);
        await MeetOnlyAsync(child, planId, positionId, 0);
        await IntroduceAsync(child, planId, positionId, 1);

        await OnDayAsync(1, async () =>
        {
            var cards = await NewRoundAsync(child, planId, positionId);
            Assert.Equal(new[] { 0, 2 }, Indices(cards));
            JsonAssert.True(cards.Single(c => c.GetProperty("itemIndex").GetInt32() == 0), "introduction");
        });
        await OnDayAsync(2, async () =>
        {
            Assert.Equal(new[] { 0, 2 }, Indices(await NewRoundAsync(child, planId, positionId)));
            await IntroduceAsync(child, planId, positionId, 0);
        });
        await OnDayAsync(3, async () =>
            Assert.Equal(new[] { 2 }, Indices(await NewRoundAsync(child, planId, positionId))));
    }

    // AK 6: the day status counts the open word along with the round - both read the same selection. Red if the scope
    // rule for open learn cards sits outside DueItemIndicesAsync (the status would say 1).
    [Fact]
    public async Task Neu_Tagesstatus_Zaehlt_Die_Offene_Lernkarte_Mit()
    {
        var (child, planId, positionId) = await SetupAsync(ItemScope.New);
        await MeetOnlyAsync(child, planId, positionId, 0);
        await IntroduceAsync(child, planId, positionId, 1);

        await OnDayAsync(1, async () =>
        {
            var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
            var position = overview.GetProperty("today").GetProperty("positions").EnumerateArray()
                .Single(p => p.GetProperty("positionId").GetInt32() == positionId);
            Assert.Equal(2, position.GetProperty("dueCount").GetInt32());
        });
    }

    // AK 4: "old" stays "introduced at all" - a never-introduced word does not appear. Red if "old" is changed along.
    [Fact]
    public async Task Alt_Zeigt_Kein_Nie_Eingefuehrtes_Wort()
    {
        var (child, planId, positionId) = await SetupAsync(ItemScope.Old);
        using (var s = factory.Services.CreateScope())
        {
            var db = s.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.PositionItemProgress.Add(new PositionItemProgress
            {
                PlanPositionId = positionId,
                ItemIndex = 1,
                IntroducedAt = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-1),
                ImprintedAt = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(-1), // B-643: known
            });
            db.SaveChanges();
        }

        Assert.Equal(new[] { 1 }, Indices(await NewRoundAsync(child, planId, positionId)));
    }
}
