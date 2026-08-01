using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// End-to-end of the position-based final test (stage 3): content from the exercise config,
/// type-neutral scoring against the item solution, passing based on <see cref="PlanPosition.GoalThreshold"/>.
/// </summary>
public class PositionTestFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Test_AlleRichtig_Bestanden_100Prozent()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "hallo" },
                new { itemIndex = 1, givenAnswer = "tschüss" },
            },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, res.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, res.GetProperty("correctItems").GetInt32());
        Assert.Equal(100, res.GetProperty("scorePercent").GetInt32());
        JsonAssert.True(res, "passed");
    }

    [Fact]
    public async Task Test_HalbRichtig_UnterStandardgrenze_NichtBestanden()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hallo" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(80, res.GetProperty("passPercent").GetInt32());
        JsonAssert.False(res, "passed");
    }

    [Fact]
    public async Task Test_EigeneZielSchwelle_WirdRespektiert()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        // A position with a milder pass threshold (40 %): 50 % then suffice.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText, goalThreshold: 40);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hallo" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(40, res.GetProperty("passPercent").GetInt32());
        JsonAssert.True(res, "passed");
    }

    /*
     * Die Einheit der Schwelle ist auch bei einem Katalog-Check-Verfahren PROZENT – nicht die Anzahl
     * richtiger Aufgaben. Das stand einmal anders in der Doku (und der Seed setzte danach Trefferzahlen),
     * war aber nie implementiert: ein TestAttempt entsteht nur hier, und hier wird verglichen.
     *
     * Der Test nagelt beide Richtungen fest, weil die falsche Lesart lautlos schadet: als Prozentwert
     * gelesen macht eine „3" die Pflicht wirkungslos, statt sie zu verschärfen.
     */
    [Theory]
    [InlineData(3, true)]    // if the number were a hit count, 2 of 4 would be too few - as 3 % it suffices
    [InlineData(90, false)]  // 50 % miss a strict percent threshold
    public async Task Test_KatalogCheck_SchwelleIstProzent(int goalThreshold, bool expectPassed)
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(
            father, ("1 + 1", 2), ("2 + 2", 4), ("3 + 3", 6), ("4 + 4", 8));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, goalThreshold: goalThreshold, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        // Two of four correct = 50 %.
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "2" },
                new { itemIndex = 1, givenAnswer = "4" },
                new { itemIndex = 2, givenAnswer = "999" },
                new { itemIndex = 3, givenAnswer = "999" },
            },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, res.GetProperty("totalItems").GetInt32());
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(goalThreshold, res.GetProperty("passPercent").GetInt32());
        Assert.Equal(expectPassed, res.GetProperty("passed").GetBoolean());
    }

    /// <summary>
    /// The edge case of the pass threshold: a result <b>exactly at</b> the threshold counts as passed
    /// (<c>ScorePercent &gt;= passPercent</c>), not only above it.
    /// <para>
    /// Why this needs its own test: the other tests in this class check 100 against 80, 50 against 80,
    /// 50 against 40, 50 against 3, and 50 against 90 – always genuinely above or genuinely below. Turning
    /// <c>&gt;=</c> into a <c>&gt;</c> therefore stayed fully green (docs/testplan.md, injection D01 – the most
    /// expensive gap of the measurement). The bug costs real balance in <b>both</b> directions:
    /// <see cref="TestAttempt.Passed"/> decides, via <c>IsGoalMetAsync</c>, both the goal points
    /// (<see cref="PointKind.Goal"/>) and whether the coin penalty is skipped – a child with exactly the
    /// required rate would lose the reward <i>and</i> get the deduction.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Test_ErgebnisGenauAufDerSchwelle_IstBestanden()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        // A threshold of exactly 50 %: with two tasks one correct answer is exactly the bound.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            goalThreshold: 50);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hallo" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(50, res.GetProperty("passPercent").GetInt32());
        JsonAssert.True(res, "passed"); // exactly reached IS reached

        // And the money effect behind it: the daily goal is met and the goal points are booked.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
    }

    [Fact]
    public async Task Versuch_Wird_Einzeln_Mit_Ergebnissen_Gelesen()
    {
        // The single view of the attempt (a C3 coverage gap): it is the evaluation the child sees after
        // submitting - and the only place where the per-item results can be read.
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        // Before submitting, the attempt is already readable - only without a completion.
        var offen = await (await child.GetAsync($"{baseUrl}/{attemptId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(attemptId, offen.GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, offen.GetProperty("completedAt").ValueKind);

        (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hallo" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        })).EnsureSuccessStatusCode();

        var fertig = await (await child.GetAsync($"{baseUrl}/{attemptId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, fertig.GetProperty("completedAt").ValueKind);
        Assert.Equal(1, fertig.GetProperty("correctItems").GetInt32());
        Assert.Equal(2, fertig.GetProperty("results").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await child.GetAsync($"{baseUrl}/{attemptId + 999}")).StatusCode);
    }
}
