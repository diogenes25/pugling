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

    // ---- Part B: the sentence form (decision 6) ----

    // A store-backed gap with a sentence form: the form is the solution, the base form alone is wrong. Red if the
    // resolver stops reading Form (then "patient" passes and "patients" fails).
    [Fact]
    public async Task Satzform_Schlaegt_Grundform_Aus_Dem_Speicher()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "patient", TestApi.UniqueName("Patient/in"));
        var exerciseId = await CreateClozeAsync(father, "The doctor sees forty {{1}} a day.",
            new { index = 1, answer = "", vocabKey = key, form = "patients" });

        var graded = await CheckAsync(father, exerciseId, "patient", "patients");
        Assert.Equal(new[] { false, true }, graded);
    }

    // Control for the case above: without a form the store word stays the solution. Red if Form were applied when
    // empty (the solution would turn blank and nothing would pass).
    [Fact]
    public async Task Ohne_Satzform_Bleibt_Das_Speicherwort_Die_Loesung()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "patient", TestApi.UniqueName("Patient/in"));
        var exerciseId = await CreateClozeAsync(father, "The {{1}} is asleep.",
            new { index = 1, answer = "", vocabKey = key, form = "  " });

        var graded = await CheckAsync(father, exerciseId, "patient");
        Assert.Equal(new[] { true }, graded);
    }

    // Without a store reference the form beats the inline answer too - "on every path", as the contract says. Red if
    // the provider for inline gaps ignores Form.
    [Fact]
    public async Task Satzform_Gilt_Auch_Ohne_Speicherverweis()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await CreateClozeAsync(father, "Parliament {{1}} the future of the club.",
            new { index = 1, answer = "debate", form = "debates" });

        var graded = await CheckAsync(father, exerciseId, "debate", "debates");
        Assert.Equal(new[] { false, true }, graded);
    }

    // ---- Part A: the cue box (decisions 4 and 5) ----

    // Two store-backed gaps and two distractors: every card carries the same sorted box of all four meanings, and no
    // card names its own. Red if the box is built per card, unsorted, without distractors, or if the hint leaks.
    [Fact]
    public async Task Wortkasten_Zeigt_Alle_Bedeutungen_Gleich_Und_Keinen_Hinweis()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, rassismus, gemeinschaft) = await CreateCueBoxClozeAsync(father);

        var data = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises/{exerciseId}/preview?stage={(int)ClozeStage.CueBoxFreeText}");
        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);

        var expected = new[] { gemeinschaft, "Gesundheit", rassismus, "rassistisch" }
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var item in items)
        {
            Assert.Equal(expected, item.GetProperty("cueBox").EnumerateArray().Select(m => m.GetString()).ToArray());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("hint").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("choices").ValueKind);
        }
    }

    // The same exercise on the old stage "translation + free text": no box, the hint stays at the gap. Red if the box
    // leaked onto every stage (it would replace the hint where the author chose the per-gap help).
    [Fact]
    public async Task Andere_Stufe_Behaelt_Den_Hinweis_An_Der_Luecke()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, rassismus, _) = await CreateCueBoxClozeAsync(father);

        var data = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises/{exerciseId}/preview?stage={(int)ClozeStage.TranslationFreeText}");
        var first = data.GetProperty("items").EnumerateArray().First();
        Assert.Equal(JsonValueKind.Null, first.GetProperty("cueBox").ValueKind);
        Assert.Equal(rassismus, first.GetProperty("hint").GetString());
    }

    // The child's exam card: the box rides along, the answer is graded like any free-text gap. Red if the test path
    // drops the facet (the child would see a bare gap and nothing to choose a meaning from).
    [Fact]
    public async Task Abschlusstest_Karte_Traegt_Den_Wortkasten()
    {
        var father = await TestApi.AdultAsync(factory);
        var (exerciseId, _, _) = await CreateCueBoxClozeAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.CueBoxFreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var next = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next");
        var item = next.GetProperty("item");
        Assert.Equal(4, item.GetProperty("cueBox").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("hint").ValueKind);
    }

    // Gap 1 "racism" and gap 2 "community" from the store, plus the distractors "rassistisch" and "Gesundheit".
    // The store translations carry a unique suffix (other tests share the store), so they are returned.
    private static async Task<(int exerciseId, string rassismus, string gemeinschaft)> CreateCueBoxClozeAsync(HttpClient father)
    {
        var rassismus = TestApi.UniqueName("Rassismus");
        var gemeinschaft = TestApi.UniqueName("Gemeinschaft");
        var (_, racism) = await TestApi.CreateStoreVocabAsync(father, "racism", rassismus);
        var (_, community) = await TestApi.CreateStoreVocabAsync(father, "community", gemeinschaft);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Wortkasten") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze", new
            {
                title = TestApi.UniqueName("Wortkasten"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    text = "{{1}} has no place in our {{2}}.",
                    gaps = new[]
                    {
                        new { index = 1, answer = "", vocabKey = racism },
                        new { index = 2, answer = "", vocabKey = community },
                    },
                    cueDistractors = new[] { "rassistisch", " Gesundheit ", "rassistisch" },
                },
            }));
        return (exerciseId, rassismus, gemeinschaft);
    }

    // One preview check per answer against gap 0; returns whether each was graded right.
    private static async Task<bool[]> CheckAsync(HttpClient father, int exerciseId, params string[] answers)
    {
        var results = new List<bool>();
        foreach (var given in answers)
        {
            var res = await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
            {
                answers = new[] { new { itemIndex = 0, givenAnswer = given, wasKnown = (bool?)null } },
                stage = (int)ClozeStage.FreeText,
            })).Content.ReadFromJsonAsync<JsonElement>();
            results.Add(res.GetProperty("correct").GetInt32() == 1);
        }
        return [.. results];
    }

    private static async Task<int> CreateClozeAsync(HttpClient father, string text, params object[] gaps)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Satzform") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var res = await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze", new
        {
            title = TestApi.UniqueName("Satzform"),
            orderIndex = 1,
            rewardPoints = 10,
            config = new { text, gaps },
        });
        return await TestApi.IdAsync(res);
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
