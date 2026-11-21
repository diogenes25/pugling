using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-607: a row's own hint is a clue to the German answer of the recognition card. Every card that asks the learnt word
/// is a swap and drops it, so the hint shows on the choice stage of "front-to-back" and "both" only. The editor's help
/// text (<c>FIELD_HELP.vocabularyHint</c>) promises exactly that; these tests pin the behaviour the text describes, so a
/// later change to <c>Swap</c> or to the written card cannot turn the text into a lie unnoticed.
/// </summary>
public class VokabelHinweisReichweiteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Hint = "beginnt mit S";

    /// <summary>Every stage the vocabulary type treats as writing: the answer is the learnt word.</summary>
    public static TheoryData<TestStage> WritingStages =>
        [TestStage.LetterBoxes, TestStage.LetterScramble, TestStage.FreeText, TestStage.Audio];

    private async Task<(HttpClient Father, int ExerciseId)> ExerciseAsync(string direction)
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Hinweis-Reichweite") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var config = new Dictionary<string, object?>
        {
            ["direction"] = direction,
            ["sourceLang"] = "en",
            ["targetLang"] = "de",
            ["items"] = new[]
            {
                new { front = "the city", back = "die Stadt", hint = Hint },
                new { front = "the street", back = "die Straße", hint = Hint },
            },
        };
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary",
            new { title = $"Hinweis {direction}", orderIndex = 1, rewardPoints = 10, config }));
        return (father, id);
    }

    private static async Task<string?[]> HintsAsync(HttpClient father, int exerciseId, TestStage stage) =>
        [.. (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage={(int)stage}"))
            .GetProperty("items").EnumerateArray()
            .Select(i => i.TryGetProperty("hint", out var h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null)];

    /// <summary>AK 3, choice: the recognition card carries the own hint. Red if the hint is dropped from the card.</summary>
    [Fact]
    public async Task Fremdwort_Deutsch_Zeigt_Den_Hinweis_Auf_Der_Auswahl()
    {
        var (father, id) = await ExerciseAsync("front-to-back");
        Assert.Equal<IEnumerable<string?>>([Hint, Hint], await HintsAsync(father, id, TestStage.MultipleChoice));
    }

    /// <summary>
    /// AK 3, writing: no hint on any writing stage. Red if <c>Swap</c> keeps the hint
    /// (<c>ExerciseContentProvider.Swap</c>) or the written card is built without the swap.
    /// </summary>
    [Theory]
    [MemberData(nameof(WritingStages))]
    public async Task Fremdwort_Deutsch_Zeigt_Beim_Schreiben_Keinen_Hinweis(TestStage stage)
    {
        var (father, id) = await ExerciseAsync("front-to-back");
        Assert.All(await HintsAsync(father, id, stage), Assert.Null);
    }

    /// <summary>AK 4: German to foreign word shows the hint on no stage at all. Red if <c>Swap</c> keeps the hint.</summary>
    [Theory]
    [MemberData(nameof(WritingStages))]
    [InlineData(TestStage.MultipleChoice)]
    public async Task Deutsch_Fremdwort_Zeigt_Nirgends_Einen_Hinweis(TestStage stage)
    {
        var (father, id) = await ExerciseAsync("back-to-front");
        var hints = await HintsAsync(father, id, stage);
        Assert.NotEmpty(hints);
        Assert.All(hints, Assert.Null);
    }

    /// <summary>
    /// AK 5, choice: "both" plays each word twice (card 2i forwards, 2i+1 backwards); the forward card carries the hint,
    /// the backward card never. Red if the backward card inherits it, or the forward card loses it on the choice.
    /// </summary>
    [Fact]
    public async Task Beides_Hinweis_Nur_Auf_Der_Vorwaertskarte_Der_Auswahl()
    {
        var (father, id) = await ExerciseAsync("both");
        Assert.Equal<IEnumerable<string?>>([Hint, null, Hint, null], await HintsAsync(father, id, TestStage.MultipleChoice));
    }

    /// <summary>
    /// AK 5, writing: on a writing stage of "both" the backward card (the written one) carries no hint, and the forward
    /// card is capped to the choice (<c>CardCeiling</c>, B-597) - so the hint it keeps is shown on a choice, never while
    /// writing. Red if the backward card inherits the hint, or the forward card is written and still carries it.
    /// </summary>
    [Theory]
    [MemberData(nameof(WritingStages))]
    public async Task Beides_Zeigt_Beim_Schreiben_Keinen_Hinweis(TestStage stage)
    {
        var (father, id) = await ExerciseAsync("both");
        var cards = (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{id}/preview?stage={(int)stage}"))
            .GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(4, cards.Length);
        foreach (var card in cards)
        {
            var hint = card.GetProperty("hint");
            if (hint.ValueKind != JsonValueKind.String)
                continue;
            // A card with a hint is a choice card: it offers options instead of asking the word to be written.
            Assert.Equal(Hint, hint.GetString());
            Assert.True(card.GetProperty("choices").ValueKind == JsonValueKind.Array && card.GetProperty("choices").GetArrayLength() > 0,
                $"card {card.GetProperty("itemIndex").GetInt32()} shows the hint on a writing stage");
        }
        Assert.All(cards.Where((_, i) => i % 2 == 1), c => Assert.NotEqual(JsonValueKind.String, c.GetProperty("hint").ValueKind));
    }
}
