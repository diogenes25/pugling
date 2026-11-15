using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-422: practice names a misspelled word like the test does (B-524) - the review stays wrong, the outcome carries
/// <c>misspelled</c> so the child sees it knew the word.
/// </summary>
public class UebenFalschGeschriebenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string, string)[] Words =
        [("die Klinik", "clinic"), ("der Rassismus", "racism"), ("rassistisch", "racist")];

    private async Task<JsonElement> ReviewAsync(int exerciseId, TestStage stage, int itemIndex, string givenAnswer,
        bool? wasKnown = null)
    {
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex, givenAnswer, wasKnown);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> VocabAsync() =>
        await TestApi.CreateVocabExerciseAsync(await TestApi.AdultAsync(factory), "de", "en", Words);

    // AK 1: "clinik" for "clinic" is marked misspelled and stays wrong - the box falls as for any miss. Red if Review does
    // not set the field, or if the near miss were counted as a hit.
    [Fact]
    public async Task Tippfehler_Wird_Gekennzeichnet_Und_Bleibt_Falsch()
    {
        var outcome = await ReviewAsync(await VocabAsync(), TestStage.FreeText, 0, "clinik");

        JsonAssert.False(outcome, "wasCorrect");
        JsonAssert.True(outcome, "misspelled");
        Assert.Equal(1, outcome.GetProperty("box").GetInt32());
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
    }

    // AK 2: "racist" for "racism" is the neighbouring word of the same exercise, not a slip. Red if practice measures
    // without the sibling atoms.
    [Fact]
    public async Task Nachbarwort_Der_Uebung_Ist_Kein_Tippfehler()
    {
        var outcome = await ReviewAsync(await VocabAsync(), TestStage.FreeText, 1, "racist");

        JsonAssert.False(outcome, "wasCorrect");
        JsonAssert.False(outcome, "misspelled");
    }

    // AK 3a: a self-assessed card is not typed - even with a stray answer text it is never marked. Red if the typed
    // condition is missing.
    [Fact]
    public async Task Selbsteinschaetzung_Ist_Nie_Falsch_Geschrieben()
    {
        var outcome = await ReviewAsync(await VocabAsync(), TestStage.SelfAssess, 0, "clinik", wasKnown: false);

        JsonAssert.False(outcome, "wasCorrect");
        JsonAssert.False(outcome, "misspelled");
    }

    // AK 3b: in set mode (an unordered list) a miss credits no entry, so there is no word to be misspelled. Red if the
    // set-mode condition is missing ("Bayrn" is one letter off "Bayern").
    [Fact]
    public async Task Mengen_Modus_Ist_Nie_Falsch_Geschrieben()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Erdkunde-Tippfehler") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/list", new
            {
                title = TestApi.UniqueName("Drei Bundesländer"),
                orderIndex = 1,
                rewardPoints = 15,
                config = new
                {
                    instruction = "Nenne drei Bundesländer.",
                    ordered = false,
                    items = new object[] { new { value = "Bayern" }, new { value = "Hessen" }, new { value = "Berlin" } },
                },
            }));

        var outcome = await ReviewAsync(exerciseId, TestStage.FreeText, 0, "Bayrn");

        JsonAssert.False(outcome, "wasCorrect");
        JsonAssert.False(outcome, "misspelled");
    }
}
