using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-524: the vocabulary test in the school's format. The answers are Friedrich's from a real test (11.11.2026);
/// the sentences are our own, so no textbook text lands in the repository.
/// <para>
/// Part C: a wrong typed answer that is the right word misspelled is marked as such - display only, the grading stays
/// binary (decision 2: the half point on paper is a personal compensation, not the format).
/// </para>
/// </summary>
public class SchulVokabeltestTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly AnswerGrader _grader = new();

    // The solutions of the real test that matter here: the neighbouring pair racism/racist decides decision 3.
    private static readonly string[] Solutions =
        ["opportunity", "clinic", "patients", "community", "racism", "racist", "height", "female", "peak"];

    private static IReadOnlyList<ContentItem> Items() =>
        [.. Solutions.Select((s, i) => new ContentItem(i, "…", s, [s]))];

    private bool Misspelled(string given, string solution)
    {
        var items = Items();
        return _grader.IsMisspelled(given, items.Single(i => i.Answer == solution), items);
    }

    // Friedrich's slips: one letter off each (k for c, a missing g, a missing e). Red if the distance or the threshold
    // for long words breaks - each of these would fall back to plain "wrong".
    [Theory]
    [InlineData("clinik", "clinic")]
    [InlineData("heiht", "height")]
    [InlineData("femal", "female")]
    [InlineData("oportunity", "opportunity")]
    [InlineData("comunity", "community")]
    public void Schreibfehler_Wird_Erkannt(string given, string solution) =>
        Assert.True(Misspelled(given, solution));

    // Two slips in a long word are still a slip ("opertunity": two substitutions). Red if long words get the short
    // threshold of 1.
    [Fact]
    public void Zwei_Fehler_Im_Langen_Wort_Sind_Noch_Ein_Schreibfehler() =>
        Assert.True(Misspelled("opertunity", "opportunity"));

    // Swapped neighbours count as one edit ("hieght"). Red if the transposition step of the distance is dropped:
    // plain Levenshtein makes it two, still within the long-word threshold - so the short word carries the case.
    [Fact]
    public void Vertauschte_Buchstaben_Zaehlen_Als_Ein_Fehler() =>
        Assert.True(Misspelled("paek", "peak"));

    // Decision 3: "racist" in the gap for "racism" is one letter off, but it is the other solution of the test - a
    // different word, not a spelling slip. Red if the comparison with the sibling solutions is dropped.
    [Fact]
    public void Nachbarwort_Ist_Kein_Schreibfehler()
    {
        Assert.False(Misspelled("racist", "racism"));
        Assert.False(Misspelled("racism", "racist"));
    }

    // Too far off, or short words with two slips: plain wrong. Red if the threshold for short words grows to 2.
    [Theory]
    [InlineData("pk", "peak")]
    [InlineData("hill", "peak")]
    [InlineData("hospital", "clinic")]
    [InlineData("", "clinic")]
    public void Zu_Weit_Weg_Ist_Falsch(string given, string solution) =>
        Assert.False(Misspelled(given, solution));

    // A right answer is never misspelled - case and a closing mark are forgiven by the grader already. Red if
    // IsMisspelled stops checking Matches first: "Clinic." is distance 0 after Normalize and would pass the threshold.
    [Fact]
    public void Richtig_Ist_Kein_Schreibfehler() =>
        Assert.False(Misspelled("Clinic.", "clinic"));

    // The way through the API: the child sits a cloze test, the result marks the slips, the score stays binary.
    // Red if the controller stops passing the flag (all false) or starts counting slips as right (score > 1/4).
    [Fact]
    public async Task Abschlusstest_Weist_Schreibfehler_Aus_Und_Wertet_Binaer()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await CreateClozeAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var res = await (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "clinik" },
                new { itemIndex = 1, givenAnswer = "racist" },
                new { itemIndex = 2, givenAnswer = "racist" },
                new { itemIndex = 3, givenAnswer = "dog" },
            },
        })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
        Assert.Equal(25, res.GetProperty("scorePercent").GetInt32());
        var flags = res.GetProperty("items").EnumerateArray()
            .ToDictionary(o => o.GetProperty("itemIndex").GetInt32(), o => o.GetProperty("misspelled").GetBoolean());
        Assert.True(flags[0]);   // clinik for clinic
        Assert.False(flags[1]);  // racist for racism: the neighbouring word
        Assert.False(flags[2]);  // racist for racist: right
        Assert.False(flags[3]);  // dog for height: not the word
    }

    // The supervisor's test mode shows the same category. Red if the preview stops computing it.
    [Fact]
    public async Task Vorschau_Weist_Schreibfehler_Aus()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await CreateClozeAsync(father);
        var res = await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 3, givenAnswer = "heiht", wasKnown = (bool?)null } },
            stage = (int)ClozeStage.FreeText,
        })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, res.GetProperty("correct").GetInt32());
        var outcome = res.GetProperty("items").EnumerateArray().Single(o => o.GetProperty("itemIndex").GetInt32() == 3);
        Assert.True(outcome.GetProperty("misspelled").GetBoolean());
    }

    private static async Task<int> CreateClozeAsync(HttpClient father)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Schultest") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze", new
            {
                title = TestApi.UniqueName("Schultest"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    text = "The {{1}} opens at eight. {{2}} has no place in our club. That joke was {{3}}. "
                        + "Her {{4}} helps her in volleyball.",
                    gaps = new[]
                    {
                        new { index = 1, answer = "clinic" },
                        new { index = 2, answer = "Racism" },
                        new { index = 3, answer = "racist" },
                        new { index = 4, answer = "height" },
                    },
                },
            }));
    }
}
