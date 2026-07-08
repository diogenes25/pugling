using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Ziel-/Punkte-Engine des Positions-Modells (Etappe 4): ein bestandener Positions-Test erfüllt das
/// Tagesziel der Position, bucht die Ziel-Punkte einmalig (<see cref="PointKind.Goal"/>) und lässt die
/// Tagesmission (<c>overview</c>) als erledigt gelten. Ein zweiter Abschluss zahlt nicht doppelt.
/// </summary>
public class PositionGoalOverviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task BestandenerPositionsTest_ErfuelltTagesziel_UndBuchtZielpunkteEinmalig()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Tagesmission vor dem Test: Ziel offen, Pflicht nicht erledigt.
        var before = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(before.GetProperty("today"), "dutyDone");

        // Test starten, alle Antworten korrekt einreichen.
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },   // hello → hallo
            new { itemIndex = 1, givenAnswer = "tschüss" }, // goodbye → tschüss
        };

        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(submit, "passed");

        // Ziel-Punkte einmalig gebucht (positionId-skopiert, da die Klassen-DB mit anderen Tests geteilt wird).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
            Assert.Equal(20, db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId).Sum(r => r.Points));
        }

        var after = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(after.GetProperty("today"), "dutyDone");

        // Zweiter bestandener Test am selben Tag → keine doppelten Ziel-Punkte (idempotent je Periode).
        var attempt2 = await (await child.PostAsJsonAsync(testsUrl, new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var attemptId2 = attempt2.GetProperty("attemptId").GetInt32();
        await child.PostAsJsonAsync($"{testsUrl}/{attemptId2}/submit", new { answers });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Idempotenz: trotz zweitem Versuch weiterhin nur 1 Belohnung für diese Position.
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
        }
    }

    /// <summary>
    /// Ein einmal erreichtes <b>Wochenziel</b> darf im Verlauf (<c>overview/progress</c>) genau einmal zählen –
    /// nicht an jedem Tag der Woche. Regression: die Belohnung trägt den Wochen-Montag als Perioden-Schlüssel,
    /// der Tages-Rollup muss deshalb über den echten Buchungstag summieren, sonst überhöht sich TotalPoints (bis 7×).
    /// </summary>
    [Fact]
    public async Task Wochenziel_WirdImVerlauf_NurEinmalGezaehlt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            cadence: GoalCadence.Weekly, pointsGoalMet: 20);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Wochenziel per bestandenem Test erfüllen.
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },   // hello → hallo
            new { itemIndex = 1, givenAnswer = "tschüss" }, // goodbye → tschüss
        };
        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(submit, "passed");

        // Für diese Position genau eine Belohnung über 20 (die Klassen-DB teilt sich mit anderen Tests → positions-skopiert prüfen).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
            Assert.Equal(20, db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId).Sum(r => r.Points));
        }

        // Verlauf über die gesamte Laufzeit: TotalPoints = 20 (nicht × Anzahl Wochentage im Plan).
        var progress = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview/progress"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, progress.GetProperty("totalPoints").GetInt32());
    }

    /// <summary>
    /// Der Verlauf (<c>overview/progress</c>) unterstützt Filter (Datumsbereich), Sortierung und Paging.
    /// Wichtig: Paging/Filter wirken nur auf die <c>days</c>-Liste; die Kennzahlen (<c>totalDays</c> etc.)
    /// bleiben über die gesamte Laufzeit stabil, und <c>X-Total-Count</c> spiegelt die <b>gefilterte</b> Gesamtzahl.
    /// </summary>
    [Fact]
    public async Task Verlauf_Progress_UnterstuetztFilterSortUndPaging()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, _) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText); // Plan: today..today+5 = 6 Tage
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/overview/progress";

        // Voller Verlauf: 6 Tage, X-Total-Count = 6.
        var full = await child.GetAsync(baseUrl);
        var fullBody = await full.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("6", full.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(6, fullBody.GetProperty("days").GetArrayLength());

        // Paging: take=2 → 2 Tage im Body, Header zählt weiterhin alle 6.
        var paged = await child.GetAsync($"{baseUrl}?take=2");
        var pagedBody = await paged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("6", paged.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(2, pagedBody.GetProperty("days").GetArrayLength());

        // Sortierung -day: erster Tag ist das Enddatum des Plans.
        var desc = await (await child.GetAsync($"{baseUrl}?sort=-day"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(desc.GetProperty("endDate").GetString(),
            desc.GetProperty("days")[0].GetProperty("day").GetString());

        // Filter from=Start+3 → nur die letzten 3 Tage; Kennzahlen bleiben über die volle Laufzeit.
        var start = DateOnly.Parse(fullBody.GetProperty("startDate").GetString()!);
        var filtered = await child.GetAsync($"{baseUrl}?from={start.AddDays(3):yyyy-MM-dd}");
        var filteredBody = await filtered.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3", filtered.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(3, filteredBody.GetProperty("days").GetArrayLength());
        Assert.Equal(6, filteredBody.GetProperty("totalDays").GetInt32());
    }
}
