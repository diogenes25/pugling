using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifies the server-authoritative grading of a position's Leitner practice loop (<c>/review</c>):
/// the student submits the answer, the server checks it against the item's solution and awards points
/// accordingly. A faked "correct" is not possible, and the practice cards arrive solution-free.
/// </summary>
public class ReviewGradingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Exercise: hello→hallo, goodbye→tschüss. The schedule stage is selectable (free text=4 → a real server-side check).
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync(int stage = (int)TestStage.FreeText, bool requireTyped = false)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage, requireTypedTest: requireTyped);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private int BoxOf(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionItemProgress.SingleOrDefault(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex)?.Box ?? 1;
    }

    [Fact]
    public async Task RichtigeAntwort_WirdServerseitigGewertet_UndBringtPunkte()
    {
        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        // "hello" → translation "hallo"; normalization makes capitalization irrelevant.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, givenAnswer: "hallo");
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal("hallo", outcome.GetProperty("expected").GetString());
        Assert.True(outcome.GetProperty("awarded").GetInt32() > 0);
        Assert.Equal(2, outcome.GetProperty("box").GetInt32()); // box 1 → 2 after a correct answer
    }

    [Fact]
    public async Task FalscheAntwort_TrotzManipulationsversuch_BringtKeinePunkte()
    {
        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, givenAnswer: "falschlösung");
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("box").GetInt32()); // wrong → back to box 1
        Assert.Equal(0, outcome.GetProperty("combo").GetInt32());
    }

    [Fact]
    public async Task Selbsteinschaetzung_BeiRequireTypedTest_BringtKeinePunkte()
    {
        // Schedule stage SelfAssess (2), but RequireTypedTest → self-assessment does not count.
        var (planId, positionId, sid) = await SetupAsync(stage: (int)TestStage.SelfAssess, requireTyped: true);
        var child = await TestApi.ChildAsync(factory);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, wasKnown: true);
        res.EnsureSuccessStatusCode(); // the cursor moves on, but the card is not graded …
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32()); // … no points …
        Assert.Equal(0, outcome.GetProperty("comboBonus").GetInt32());

        Assert.Equal(1, BoxOf(positionId, 0)); // … and no box movement (stays box 1)
    }

    [Fact]
    public async Task Stufe_NichtVomClientWaehlbar_KeinDowngradeAufSelbsteinschaetzung()
    {
        var (planId, positionId, sid) = await SetupAsync(); // schedule stage free text (typed)
        var child = await TestApi.ChildAsync(factory);

        // A manipulation attempt: only wasKnown without a typed answer. The server enforces the free-text stage
        // and grades against the solution → without a givenAnswer simply wrong, no free points.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, wasKnown: true);
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
    }

    [Fact]
    public async Task Cards_LiefernKeineLoesung_FuerGetippteStufe()
    {
        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        var res = await child.GetAsync($"{TestApi.PracticeBase(planId, positionId)}/{sid}/cards");
        res.EnsureSuccessStatusCode();
        var cards = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEmpty(cards.EnumerateArray());
        foreach (var card in cards.EnumerateArray())
        {
            // Free-text stage: the prompt (the word) yes, the solution (reveal) no.
            Assert.False(string.IsNullOrEmpty(card.GetProperty("prompt").GetString()));
            Assert.Equal(JsonValueKind.Null, card.GetProperty("reveal").ValueKind);
        }
    }
}
