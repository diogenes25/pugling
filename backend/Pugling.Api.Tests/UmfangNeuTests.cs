using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-435: a position scoped to "new" contents shows a word on its introduction day and asks it in the follow-up round
/// of that same day; from the next day on the word is review material and leaves the scope. "Old" is unchanged.
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

    // Runs the body one day later on the frozen test clock and puts the clock back afterwards.
    private async Task OnNextDayAsync(Func<Task> body)
    {
        factory.Clock.FreezeNow();
        factory.Clock.Advance(TimeSpan.FromDays(1));
        try { await body(); }
        finally { factory.Clock.Advance(TimeSpan.FromDays(-1)); }
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

        await OnNextDayAsync(async () =>
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

        await OnNextDayAsync(async () =>
            Assert.Equal(new[] { 2 }, Indices(await NewRoundAsync(child, planId, positionId))));
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
