using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-993: whitespace next to a slash is not part of the vocabulary - "turn sth. up / down" is "(to) turn sth. up/down",
/// and the whole entry "towards the station / towards Mr Bell" may be typed without the spaces. The rule sits in
/// <see cref="StageMechanics.Normalize"/> (pinned by the shared case list); these cases pin that practice, the position
/// test and the supervisor's preview reach it. Each path also plays the slash left out ("up down"), so a rule that read
/// the slash as a space, or a path that accepted anything, would not pass; dropping every space is caught by the case
/// list ("ice cream" / "icecream").
/// </summary>
public class SchraegstrichPfadTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Unspaced = "(to) turn sth. up/down";
    private const string Spaced = "towards the station / towards Mr Bell";

    private async Task<(HttpClient Father, int ExerciseId, int PlanId, int PositionId)> SetupAsync(string entry)
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, entry, TestApi.UniqueName("Schrägstrich"));
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        return (father, exerciseId, planId, positionId);
    }

    /// <summary>
    /// AK 1, practice: the true cases are red when Normalize keeps the spaces at the slash. The false case is green in
    /// the starting state on purpose (a statement of intent): it turns red only when the slash is read as a space.
    /// </summary>
    [Theory]
    [InlineData(Unspaced, "turn sth. up / down", true)]
    [InlineData(Unspaced, "turn up / down", true)]
    [InlineData(Unspaced, "turn up/ down", true)]
    [InlineData(Unspaced, "turn sth. up down", false)]
    [InlineData(Spaced, "towards the station/towards Mr Bell", true)]
    public async Task Ueben_Ueberhoert_Leerzeichen_Am_Schraegstrich(string entry, string given, bool correct)
    {
        var (_, _, planId, positionId) = await SetupAsync(entry);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex: 0, givenAnswer: given);

        res.EnsureSuccessStatusCode();
        Assert.Equal(correct, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
    }

    /// <summary>AK 1, position test: red when the submit compares with the spaces kept.</summary>
    [Theory]
    [InlineData("turn sth. up / down", 1)]
    [InlineData("turn sth. up down", 0)]
    public async Task Abschlusstest_Ueberhoert_Leerzeichen_Am_Schraegstrich(string given, int correctItems)
    {
        var (_, _, planId, positionId) = await SetupAsync(Unspaced);
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

    /// <summary>
    /// AK 2 in the supervisor's preview: the whole spaced entry typed without spaces counts (red without the rule). The
    /// half and the slash-free cases are green in the starting state on purpose (statements of intent): the half turns
    /// red if the rule moves before the meaning split of <see cref="AnswerVariants"/>, the last if the slash becomes a space.
    /// </summary>
    [Theory]
    [InlineData("towards the station/towards Mr Bell", 1)]
    [InlineData("towards Mr Bell", 1)]
    [InlineData("towards the station towards Mr Bell", 0)]
    public async Task Vorschau_Ueberhoert_Leerzeichen_Am_Schraegstrich(string given, int correct)
    {
        var (father, exerciseId, _, _) = await SetupAsync(Spaced);

        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = given, wasKnown = (bool?)null } },
            stage = (int)TestStage.FreeText,
        });

        res.EnsureSuccessStatusCode();
        Assert.Equal(correct, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correct").GetInt32());
    }
}
