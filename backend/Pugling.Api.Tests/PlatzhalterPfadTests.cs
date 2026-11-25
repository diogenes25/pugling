using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-626 AK 5: a word-list entry asks only its core - the slots ("sth.", "sb.", "qc") may be left out - and every place
/// the child types grades it alike: practice, position test, bonus training, and the supervisor's preview. The rule
/// itself is unit-tested in <see cref="AnswerVariantsTests"/>; these cases pin that each path reaches it through the
/// resolver's word-list mark. Each path also plays a core that is too short ("point"), so a path that accepted
/// anything would not pass.
/// </summary>
public class PlatzhalterPfadTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Entry = "(to) point sth. out (to sb.)";
    private const string Meaning = "auf etw. hinweisen";
    private const string Core = "point out";
    private const string TooShort = "point";

    // A store entry in its own exercise, played as a daily position of child 1 at the given stage.
    private async Task<(HttpClient Father, int ExerciseId, int PlanId, int PositionId)> SetupAsync(TestStage stage)
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, Entry, TestApi.UniqueName(Meaning));
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage);
        return (father, exerciseId, planId, positionId);
    }

    /// <summary>Practice: red when the review grades against the printed entry only (no slot-free variant).</summary>
    [Theory]
    [InlineData(Core, true)]
    [InlineData(TooShort, false)]
    public async Task Ueben_Wertet_Den_Kern_Ohne_Platzhalter(string given, bool correct)
    {
        var (_, _, planId, positionId) = await SetupAsync(TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex: 0, givenAnswer: given);

        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correct, body.GetProperty("wasCorrect").GetBoolean());
    }

    /// <summary>Position test: red when the submit grades against the printed entry only.</summary>
    [Theory]
    [InlineData(Core, 1)]
    [InlineData(TooShort, 0)]
    public async Task Abschlusstest_Wertet_Den_Kern_Ohne_Platzhalter(string given, int correctItems)
    {
        var (_, _, planId, positionId) = await SetupAsync(TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = given } },
        });

        submit.EnsureSuccessStatusCode();
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correctItems, res.GetProperty("correctItems").GetInt32());
    }

    /// <summary>Supervisor preview ("Ausprobieren" grades like the child's test, B-597 decision 5).</summary>
    [Theory]
    [InlineData(Core, 1)]
    [InlineData(TooShort, 0)]
    public async Task Vorschau_Wertet_Den_Kern_Ohne_Platzhalter(string given, int correct)
    {
        var (father, exerciseId, _, _) = await SetupAsync(TestStage.FreeText);

        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = given, wasKnown = (bool?)null } },
            stage = (int)TestStage.FreeText,
        });

        res.EnsureSuccessStatusCode();
        Assert.Equal(correct, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correct").GetInt32());
    }

    /// <summary>
    /// Bonus training, typed form (after four completed runs): red when the run's grading skips the word-list mark.
    /// </summary>
    [Theory]
    [InlineData(Core, 1)]
    [InlineData(TooShort, 0)]
    public async Task Bonustraining_Wertet_Den_Kern_Ohne_Platzhalter(string given, int correct)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Platzhalter-Kind"), pin }));
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, Entry, TestApi.UniqueName(Meaning));
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId, cadence: GoalCadence.None);
        SeedCompletedRuns(childId, exerciseId);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var baseUrl = $"/api/v1/student/children/{childId}/extra-training";

        var startRes = await child.PostAsync($"{baseUrl}/{exerciseId}/runs", null);
        Assert.True(startRes.IsSuccessStatusCode, await startRes.Content.ReadAsStringAsync());
        var run = await startRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Typed", run.GetProperty("form").GetString());
        var answers = run.GetProperty("cards").EnumerateArray()
            .Select(c => new { itemIndex = c.GetProperty("itemIndex").GetInt32(), givenAnswer = given, wasKnown = (bool?)null })
            .ToArray();
        var submit = await child.PostAsJsonAsync($"{baseUrl}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers });

        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        Assert.Equal(correct, (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correct").GetInt32());
    }

    /// <summary>
    /// B-626 AK 2 at the card, plus risk 2 of the story: the letter-box card masks only the core, and the value the
    /// child app sends back - the boxes with their fixed characters, i.e. the whole entry - is right. Red when the
    /// vocabulary type builds the plain mask (every letter a box) or when a path strips the fixed accessories.
    /// </summary>
    [Fact]
    public async Task Kaestchen_Zeigen_Beiwerk_Fest_Und_Der_Ganze_Wert_Zaehlt()
    {
        var (_, _, planId, positionId) = await SetupAsync(TestStage.LetterBoxes);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var cards = await child.GetFromJsonAsync<JsonElement>(
            $"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards");
        var card = cards.EnumerateArray().Single();
        Assert.Equal("(to) _____ sth. ___ (to sb.)", card.GetProperty("answerPattern").GetString());

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex: 0, givenAnswer: Entry);
        res.EnsureSuccessStatusCode();
        Assert.True((await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
    }

    // Four runs completed yesterday make the next run the typed form (two self-assessed, two choices) - as in
    // ExtraTrainingTests.
    private void SeedCompletedRuns(int childId, int exerciseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var yesterday = factory.Clock.GetUtcNow().UtcDateTime.AddDays(-1);
        for (var i = 0; i < 4; i++)
        {
            db.ExtraTrainingRuns.Add(new ExtraTrainingRun
            {
                ChildId = childId,
                ExerciseId = exerciseId,
                Day = DateOnly.FromDateTime(yesterday),
                Pass = i + 1,
                Form = i < 2 ? ExtraForm.SelfAssess : ExtraForm.Choice,
                Stage = i < 2 ? (int)TestStage.SelfAssess : (int)TestStage.MultipleChoice,
                StartedAt = yesterday,
                CompletedAt = yesterday,
                ItemCount = 1,
            });
        }
        db.SaveChanges();
    }
}
