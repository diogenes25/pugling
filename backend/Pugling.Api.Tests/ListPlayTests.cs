using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Playing a list position (B-77). An unordered list is a <b>set</b>: naming its entries in any sequence is
/// right, and the catalog check has always graded it that way. The play path did not – it built one card per
/// entry and demanded entry N on card N, which made the seeded "16 federal states" unanswerable. Nobody had
/// ever played a list before, so no test saw it.
/// </summary>
public class ListPlayTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>The 16 states in seed order – the answers for the seeded exercise.</summary>
    private static readonly string[] Bundeslaender =
    [
        "Baden-Württemberg", "Bayern", "Berlin", "Brandenburg", "Bremen", "Hamburg", "Hessen",
        "Mecklenburg-Vorpommern", "Niedersachsen", "Nordrhein-Westfalen", "Rheinland-Pfalz", "Saarland",
        "Sachsen", "Sachsen-Anhalt", "Schleswig-Holstein", "Thüringen",
    ];

    /// <summary>Three entries – the smallest shape in which "set, not sequence" is visible.</summary>
    private static async Task<int> CreateListAsync(HttpClient father, bool ordered)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Erdkunde-Liste") }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Deutschland", orderIndex = 1 }));
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/list", new
            {
                // Not the seeded title: the seed test below looks its exercise up by name.
                title = TestApi.UniqueName("Drei Bundesländer (Fixture)"),
                orderIndex = 1,
                rewardPoints = 15,
                config = new
                {
                    instruction = "Nenne drei Bundesländer.",
                    ordered,
                    items = new object[]
                    {
                        new { value = "Bayern" },
                        new { value = "Hessen", alternatives = new[] { "Hesse" } },
                        new { value = "Berlin" },
                    },
                },
            }));
    }

    private static string TestUrl(int planId, int positionId) =>
        $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

    /// <summary>
    /// Sits the whole exam, answering with <paramref name="answers"/> in the given order, and returns the
    /// submission. The answers are deliberately NOT matched to the cards: which entry an answer credits is
    /// exactly what the server decides in set mode.
    /// </summary>
    private static async Task<JsonElement> SitExamAsync(HttpClient child, int planId, int positionId,
        bool anyOrder, params string[] answers)
    {
        var url = TestUrl(planId, positionId);
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(url, new { }), "attemptId");
        var first = true;
        foreach (var answer in answers)
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{url}/{attemptId}/next");
            var item = next.GetProperty("item");
            // E5 names the exam as the place where the rule matters most (no going back), so the exam CARD
            // carries its rule here and not only the practice card.
            if (first)
            {
                Assert.Equal(anyOrder, item.GetProperty("anyOrder").GetBoolean());
                Assert.Equal("List", item.GetProperty("type").GetString());
                first = false;
            }
            await child.PostAsJsonAsync($"{url}/{attemptId}/answer", new
            {
                itemIndex = item.GetProperty("itemIndex").GetInt32(),
                givenAnswer = answer,
                wasKnown = (bool?)null,
            });
        }
        var submit = await child.PostAsJsonAsync($"{url}/{attemptId}/submit", new { answers = (object?)null });
        submit.EnsureSuccessStatusCode();
        return await submit.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> CardsAsync(HttpClient child, int planId, int positionId)
    {
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        return await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
    }

    // ---- E1: the defect – every entry counts, whatever card it arrives on ---- ----

    /// <summary>
    /// The regression test of the story: the three states named in REVERSE order. Before B-77 exactly the one
    /// answer that happened to land on its own card counted (1 of 3); now all three do.
    /// </summary>
    [Fact]
    public async Task Klausur_UngeordneteListe_ZaehltJedeNennungUnabhaengigVonDerKarte()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateListAsync(father, ordered: false);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);

        var result = await SitExamAsync(child, planId, positionId, anyOrder: true, "Berlin", "Hesse", "Bayern");

        Assert.Equal(3, result.GetProperty("totalItems").GetInt32());
        Assert.Equal(3, result.GetProperty("correctItems").GetInt32());
        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());
        // The alternative counts as well, exactly as at the catalog endpoint (B-65).
        JsonAssert.Null(result, "wrongMentions");
        // E2: the outcomes are keyed by ENTRY, so each entry names itself as the expectation.
        var expected = result.GetProperty("items").EnumerateArray()
            .Select(o => o.GetProperty("expected").GetString()).ToList();
        Assert.Equal(["Bayern", "Berlin", "Hessen"], expected.Order());
    }

    // ---- E4: a repeat is a wrong mention ---- ----

    [Fact]
    public async Task Klausur_WiederholteNennung_ZaehltEinmalUndErscheintAlsFehlnennung()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateListAsync(father, ordered: false);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);

        var result = await SitExamAsync(child, planId, positionId, anyOrder: true, "Bayern", "Bayern", "Hessen");

        // R1: the wrong mention must NOT become a result row of entry 0 - the score stays over three entries.
        Assert.Equal(3, result.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, result.GetProperty("correctItems").GetInt32());
        Assert.Equal(67, result.GetProperty("scorePercent").GetInt32());
        Assert.Equal(["Bayern"], result.GetProperty("wrongMentions").EnumerateArray().Select(m => m.GetString()));
        // And the forgotten entry says so: named nothing, marked wrong (that is the lesson of "name them all").
        var berlin = result.GetProperty("items").EnumerateArray()
            .Single(o => o.GetProperty("expected").GetString() == "Berlin");
        JsonAssert.False(berlin, "wasCorrect");
        JsonAssert.Null(berlin, "givenAnswer");
    }

    // ---- E5/E6: the card says which rule applies ---- ----

    [Fact]
    public async Task Karte_WeistDieMengenRegelAus_GeordneteListeNicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var unordered = await CreateListAsync(father, ordered: false);
        var (planA, posA) = TestApi.SeedLeitnerPosition(_factory, unordered, (int)TestStage.FreeText);
        var setCards = await CardsAsync(child, planA, posA);
        Assert.All(setCards.EnumerateArray(), card => JsonAssert.True(card, "anyOrder"));

        var ordered = await CreateListAsync(father, ordered: true);
        var (planB, posB) = TestApi.SeedLeitnerPosition(_factory, ordered, (int)TestStage.FreeText);
        var sequenceCards = await CardsAsync(child, planB, posB);
        Assert.All(sequenceCards.EnumerateArray(), card => JsonAssert.False(card, "anyOrder"));
        // The ordered card is addressable through its item index - that IS the entry position (E6).
        Assert.Equal([0, 1, 2],
            sequenceCards.EnumerateArray().Select(c => c.GetProperty("itemIndex").GetInt32()).Order());
    }

    [Fact]
    public async Task Klausur_GeordneteListe_WertetWeiterPositionsgenau()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateListAsync(father, ordered: true);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);

        // Reverse order: with `Ordered` only the middle entry sits in its place - the exact opposite of the
        // unordered case above, and the reason both cases needed to stay separate.
        var result = await SitExamAsync(child, planId, positionId, anyOrder: false, "Berlin", "Hessen", "Bayern");

        Assert.Equal(1, result.GetProperty("correctItems").GetInt32());
        Assert.Equal(3, result.GetProperty("totalItems").GetInt32());
        JsonAssert.Null(result, "wrongMentions");
    }

    // ---- E3/E10: the practice round, per day ---- ----

    /// <summary>
    /// The practice round applies the same rule with the day as its period, and it takes no new state to do
    /// so: the anti-farming stamp on the progress row is the "already named" marker. A repeat therefore hits
    /// nothing – and then nothing moves (E10): no box, no due date, no solution to show.
    /// </summary>
    [Fact]
    public async Task Uebungsrunde_AntwortBestimmtDenEintrag_WiederholungBewegtNichts()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateListAsync(father, ordered: false);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        // Card 0 stands for entry 0 ("Bayern") - the answer names the LAST entry and is credited to it.
        var first = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "Berlin"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(first, "wasCorrect");
        Assert.Equal("Berlin", first.GetProperty("expected").GetString());
        Assert.Equal(2, first.GetProperty("box").GetInt32());

        // The same answer again: "Berlin" is spoken for today, so it credits nothing.
        var repeat = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, givenAnswer: "Berlin"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(repeat, "wasCorrect");
        JsonAssert.Null(repeat, "expected");
        Assert.Equal(0, repeat.GetProperty("box").GetInt32());
        JsonAssert.Null(repeat, "dueOn");

        // A still-open entry is credited normally afterwards - the round is not poisoned by the miss.
        var third = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 2, givenAnswer: "Hesse"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(third, "wasCorrect");
        Assert.Equal("Hessen", third.GetProperty("expected").GetString());
    }

    // ---- The case that motivated the story ---- ----

    /// <summary>
    /// The seeded weekly duty: 16 states, 90 % to pass. Before B-77 the child could know every one of them and
    /// still fail – on average exactly one answer landed on its own card. Played on an own position, because
    /// the seeded plan's ownership is the seed's business.
    /// </summary>
    [Fact]
    public async Task Geseedete16Bundeslaender_SindMitDerWochenpflichtBestehbar()
    {
        int exerciseId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var seeded = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e =>
                e.Type == ExerciseTypeKeys.List && e.Title == "Die 16 Bundesländer");
            Assert.NotNull(seeded);
            exerciseId = seeded.Id;
        }

        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            cadence: GoalCadence.Weekly, goalThreshold: 90, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);

        // Answered from Thüringen backwards: in seed order the answers would sit on their own cards and the
        // test would pass even under the old per-card grading - it has to discriminate, not just be green.
        var result = await SitExamAsync(child, planId, positionId, true, [.. Bundeslaender.Reverse()]);

        Assert.Equal(16, result.GetProperty("correctItems").GetInt32());
        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());
        JsonAssert.True(result, "passed");
        Assert.Equal(90, result.GetProperty("passPercent").GetInt32());
    }
}
