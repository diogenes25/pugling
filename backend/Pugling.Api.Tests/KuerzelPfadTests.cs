using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-990: a child who types the slot of a word-list entry in its own school abbreviation ("turn to smb" for
/// "(to) turn to sb.") knows the phrase. The rule sits in <see cref="AnswerGrader"/> (pinned case by case in
/// <see cref="AnswerGraderTests"/>); these cases pin that practice, the position test and the supervisor's preview reach
/// it the same way (decision 4). Each path also plays an abbreviation of the wrong kind, so a path that accepted anything
/// would not pass.
/// </summary>
public class KuerzelPfadTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Person = "(to) turn to sb.";
    private const string Thing = "(to) turn sth. up/down";

    private async Task<(HttpClient Father, int ExerciseId, int PlanId, int PositionId)> SetupAsync(string entry)
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, entry, TestApi.UniqueName("Kürzel"));
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        return (father, exerciseId, planId, positionId);
    }

    /// <summary>
    /// AK 1, practice: the true rows are red without the rule. The false rows are green in the starting state on purpose
    /// (statements of intent): they turn red only if the practice path forgives the wrong kind.
    /// </summary>
    [Theory]
    [InlineData(Person, "turn to smb", true)]
    [InlineData(Thing, "turn smt. up/down", true)]
    [InlineData(Person, "turn to smth", false)]
    public async Task Ueben_Wertet_Kuerzel_Des_Kindes(string entry, string given, bool correct)
    {
        var (_, _, planId, positionId) = await SetupAsync(entry);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex: 0, givenAnswer: given);

        res.EnsureSuccessStatusCode();
        Assert.Equal(correct, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
    }

    /// <summary>AK 1, position test - where Theodor's two cases fell: red when the submit compares without the rule.</summary>
    [Theory]
    [InlineData(Person, "turn to smb", 1)]
    [InlineData(Thing, "turn smt. up/down", 1)]
    [InlineData(Person, "turn to smth", 0)]
    public async Task Abschlusstest_Wertet_Kuerzel_Des_Kindes(string entry, string given, int correctItems)
    {
        var (_, _, planId, positionId) = await SetupAsync(entry);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = given } },
        });

        submit.EnsureSuccessStatusCode();
        Assert.Equal(correctItems, (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correctItems").GetInt32());
    }

    /// <summary>AK 1 in the supervisor's preview: red without the rule for the true row.</summary>
    [Theory]
    [InlineData("turn to smb.", 1)]
    [InlineData("turn to smth.", 0)]
    public async Task Vorschau_Wertet_Kuerzel_Des_Kindes(string given, int correct)
    {
        var (father, exerciseId, _, _) = await SetupAsync(Person);

        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = given, wasKnown = (bool?)null } },
            stage = (int)TestStage.FreeText,
        });

        res.EnsureSuccessStatusCode();
        Assert.Equal(correct, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correct").GetInt32());
    }
}
