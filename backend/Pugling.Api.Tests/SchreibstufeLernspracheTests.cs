using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-597: wherever the child writes a word - letter boxes, letter tiles, free text, dictation - it writes the learnt word,
/// whatever direction the exercise asks in. Display and grading turn the card through the same function, and with "both"
/// the forward card of a word stops at the choice, because its reversed sibling is the one that is written.
/// </summary>
public class SchreibstufeLernspracheTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string Front, string Back)[] Words = [("walk", "gehen"), ("run", "rennen")];

    private sealed record Setup(HttpClient Child, int ChildId, int PlanId, int PositionId, int ExerciseId);

    // A fresh child per case: the cases move boxes and introduction dates of the child's own position.
    private async Task<Setup> SetupAsync(string direction, TestStage stage)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "5" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Schreib-Kind"), pin }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Schreiben") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = $"Bewegung {direction}",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction,
                    sourceLang = "en",
                    targetLang = "de",
                    items = Words.Select(w => new { front = w.Front, back = w.Back }),
                },
            }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage, childId: childId);
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId, exerciseId);
    }

    // Box and introduction date of every card - the inputs the ladder (B-476) and its calendar deadline read.
    private void SetProgress(int positionId, int box, int introducedDaysAgo)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        foreach (var prog in db.PositionItemProgress.Where(p => p.PlanPositionId == positionId))
        {
            prog.Box = box;
            prog.IntroducedAt = today.AddDays(-introducedDaysAgo);
            prog.DueOn = today;
        }
        db.SaveChanges();
    }

    private static string TestBase(Setup s) => $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

    private static async Task<List<JsonElement>> CardsAsync(Setup s)
    {
        var sessionId = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);
        return (await s.Child.GetFromJsonAsync<List<JsonElement>>(
            $"{TestApi.PracticeBase(s.PlanId, s.PositionId)}/{sessionId}/cards"))!;
    }

    private static async Task<bool> ReviewAsync(Setup s, int itemIndex, string given)
    {
        var sessionId = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);
        var outcome = await (await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, itemIndex,
            givenAnswer: given)).Content.ReadFromJsonAsync<JsonElement>();
        return outcome.GetProperty("wasCorrect").GetBoolean();
    }

    /// <summary>
    /// Decision 1: the writing stages are letter boxes, letter tiles, free text and the dictation - and since B-598 the learn
    /// card, which copies and recalls the learnt word; the choice taps and stays
    /// with the exercise's direction, as do the self-assessment and getting acquainted. Red for any stage moved across.
    /// </summary>
    [Theory]
    [InlineData(TestStage.ShowBoth, false)]
    [InlineData(TestStage.SelfAssess, false)]
    [InlineData(TestStage.MultipleChoice, false)]
    [InlineData(TestStage.LetterBoxes, true)]
    [InlineData(TestStage.LetterScramble, true)]
    [InlineData(TestStage.FreeText, true)]
    [InlineData(TestStage.Audio, true)]
    [InlineData(TestStage.LearnCard, true)]
    public void Schreibstufen_Sind_Kaestchen_Kacheln_Freitext_Und_Diktat(TestStage stage, bool writing) =>
        Assert.Equal(writing, VocabularyExerciseType.IsWritingStage(stage));

    /// <summary>
    /// AK 7: the hearing stage is offered as "Diktat" - the father picks it from the server manifest. Red if the label went
    /// back to "Hören → tippen", which read like a translation task.
    /// </summary>
    [Fact]
    public async Task Die_Hoerstufe_Heisst_Diktat()
    {
        var father = await TestApi.AdultAsync(factory);
        var manifest = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/exercise-types");
        var vocabulary = manifest.EnumerateArray().Single(m => m.GetProperty("type").GetString() == ExerciseTypeKeys.Vocabulary);
        Assert.Equal("Diktat", vocabulary.GetProperty("stages").EnumerateArray()
            .Single(s => s.GetProperty("value").GetInt32() == (int)TestStage.Audio).GetProperty("label").GetString());
    }

    /// <summary>
    /// AK 1 + AK 2 in practice: a front-to-back card on free text shows "gehen" and does not take "gehen"; its sibling
    /// takes "run". Red without the turn at the card (the prompt is "walk"), and red if only the display turns (then the
    /// German answer counts and the learnt one does not).
    /// </summary>
    [Fact]
    public async Task Ueben_Freitext_Zeigt_Deutsch_Und_Wertet_Nur_Das_Fremdwort()
    {
        var s = await SetupAsync("front-to-back", TestStage.FreeText);

        var card = (await CardsAsync(s)).Single(c => c.GetProperty("itemIndex").GetInt32() == 0);
        Assert.Equal("gehen", card.GetProperty("prompt").GetString());

        // Two words, one answer each: a miss drops its card to box 1, where the ladder would turn it over (B-476).
        Assert.False(await ReviewAsync(s, 0, "gehen"));
        Assert.True(await ReviewAsync(s, 1, "run"));
    }

    /// <summary>
    /// AK 1 + AK 2 in the test, both answer paths: the question shows "gehen", the one-at-a-time answer and the bulk
    /// submission grade "walk" right and "gehen" wrong, and the result names the question as asked. Red if the test path
    /// keeps the untouched card for display (prompt) or for either grading path (the counts).
    /// </summary>
    [Fact]
    public async Task Test_Freitext_Fragt_Und_Wertet_Das_Fremdwort()
    {
        var s = await SetupAsync("front-to-back", TestStage.FreeText);

        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(TestBase(s), new { }), "attemptId");
        var first = (await s.Child.GetFromJsonAsync<JsonElement>($"{TestBase(s)}/{attemptId}/next")).GetProperty("item");
        var firstIndex = first.GetProperty("itemIndex").GetInt32();
        Assert.Equal(Words[firstIndex].Back, first.GetProperty("prompt").GetString());
        (await s.Child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/answer",
            new { itemIndex = firstIndex, givenAnswer = Words[firstIndex].Front })).EnsureSuccessStatusCode();
        var result = await (await s.Child.PostAsJsonAsync($"{TestBase(s)}/{attemptId}/submit", new
        {
            // The bulk path for the other question, answered with the German word: wrong on a writing stage.
            answers = new[] { new { itemIndex = 1 - firstIndex, givenAnswer = Words[1 - firstIndex].Back } },
        })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, result.GetProperty("correctItems").GetInt32());
        var shown = result.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("itemIndex").GetInt32() == firstIndex);
        Assert.Equal(Words[firstIndex].Back, shown.GetProperty("prompt").GetString());
        Assert.Equal(Words[firstIndex].Front, shown.GetProperty("expected").GetString());
    }

    /// <summary>
    /// AK 4 over the calendar deadline: a "both" word introduced eight days ago is past its ladder week and asked at the
    /// target - the reversed card on free text, the forward card on the choice. Red without the ceiling (the forward card
    /// stands on free text and asks the very word its sibling asks).
    /// </summary>
    [Fact]
    public async Task Beide_Vorwaertskarte_Bleibt_Nach_Der_Frist_Auf_Der_Auswahl()
    {
        var s = await SetupAsync("both", TestStage.FreeText);
        SetProgress(s.PositionId, box: 1, introducedDaysAgo: 8);

        var cards = await CardsAsync(s);

        var forward = cards.Single(c => c.GetProperty("itemIndex").GetInt32() == 0);
        Assert.Equal((int)TestStage.MultipleChoice, forward.GetProperty("stage").GetInt32());
        Assert.Equal("walk", forward.GetProperty("prompt").GetString());
        Assert.Equal(JsonValueKind.Array, forward.GetProperty("choices").ValueKind);
        var reversed = cards.Single(c => c.GetProperty("itemIndex").GetInt32() == 1);
        Assert.Equal((int)TestStage.FreeText, reversed.GetProperty("stage").GetInt32());
        Assert.Equal("gehen", reversed.GetProperty("prompt").GetString());
    }

    /// <summary>
    /// AK 4 over the ladder: a card in the position's top box is asked at the target whatever the ladder says - the forward
    /// card still stops at the choice, and the review grades it as one ("gehen" chosen is right). Red without the ceiling
    /// in the practice stage (stage 4, and the review expects "walk").
    /// </summary>
    [Fact]
    public async Task Beide_Vorwaertskarte_Bleibt_Im_Obersten_Fach_Auf_Der_Auswahl()
    {
        var s = await SetupAsync("both", TestStage.FreeText);
        SetProgress(s.PositionId, box: 5, introducedDaysAgo: 1);

        var forward = (await CardsAsync(s)).Single(c => c.GetProperty("itemIndex").GetInt32() == 0);
        Assert.Equal((int)TestStage.MultipleChoice, forward.GetProperty("stage").GetInt32());
        Assert.True(await ReviewAsync(s, 0, "gehen"));
    }

    /// <summary>
    /// AK 4 in the test: each row is frozen at most at the choice for a forward card, and so is a stage the supervisor
    /// picks for a preview attempt. Red without the ceiling in <c>TestItemStage</c> (child: [4, 4]) or in the explicit
    /// stage of the supervisor (father: [4, 4]).
    /// </summary>
    [Fact]
    public async Task Beide_Vorwaertskarte_Steht_Im_Test_Hoechstens_Auf_Der_Auswahl()
    {
        var s = await SetupAsync("both", TestStage.FreeText);
        SetProgress(s.PositionId, box: 1, introducedDaysAgo: 8);
        var father = await TestApi.AdultAsync(factory);

        async Task<List<int>> RowStagesAsync(HttpClient client, object body)
        {
            var attemptId = await TestApi.IdWithKeyAsync(await client.PostAsJsonAsync(TestBase(s), body), "attemptId");
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            return [.. db.TestAttempts.Where(t => t.Id == attemptId).SelectMany(t => t.Results)
                .OrderBy(r => r.ItemIndex).Select(r => r.StageValue).Take(2)];
        }

        Assert.Equal([(int)TestStage.MultipleChoice, (int)TestStage.FreeText], await RowStagesAsync(s.Child, new { }));
        Assert.Equal([(int)TestStage.MultipleChoice, (int)TestStage.FreeText],
            await RowStagesAsync(father, new { stage = (int)TestStage.FreeText }));
    }

    /// <summary>
    /// AK 1 + AK 4 in the supervisor's preview, which shows each card as the child gets it: at free text the forward card of
    /// a "both" word comes as a choice, its reversed sibling in writing. Red without the ceiling in the preview's projection
    /// (the forward card would come without options).
    /// </summary>
    [Fact]
    public async Task Vorschau_Zeigt_Die_Vorwaertskarte_Bei_Beiden_Als_Auswahl()
    {
        var s = await SetupAsync("both", TestStage.FreeText);
        var father = await TestApi.AdultAsync(factory);

        var items = (await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises/{s.ExerciseId}/preview?stage={(int)TestStage.FreeText}")).GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(JsonValueKind.Array, items[0].GetProperty("choices").ValueKind);
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("choices").ValueKind);
        Assert.Equal("gehen", items[1].GetProperty("prompt").GetString());
    }

    /// <summary>
    /// The father's report labels the cards (B-597): a front-to-back card is turned where the child writes, a forward
    /// "both" card is never written. Red if either flag is not carried from the resolved card into the report row.
    /// </summary>
    [Fact]
    public async Task Bericht_Beschriftet_Gedrehte_Und_Erkennungskarten()
    {
        var father = await TestApi.AdultAsync(factory);
        var single = await SetupAsync("front-to-back", TestStage.FreeText);
        var both = await SetupAsync("both", TestStage.FreeText);

        async Task<List<JsonElement>> RowsAsync(Setup s) =>
            [.. (await father.GetFromJsonAsync<JsonElement>(
                $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}/report")).GetProperty("items").EnumerateArray()];

        var singleRow = (await RowsAsync(single))[0];
        JsonAssert.True(singleRow, "turnedWhenWritten");
        JsonAssert.False(singleRow, "recognitionOnly");
        var bothRows = await RowsAsync(both);
        JsonAssert.True(bothRows[0], "recognitionOnly");
        JsonAssert.False(bothRows[0], "turnedWhenWritten");
        JsonAssert.False(bothRows[1], "recognitionOnly");
        JsonAssert.False(bothRows[1], "turnedWhenWritten");
    }
}
