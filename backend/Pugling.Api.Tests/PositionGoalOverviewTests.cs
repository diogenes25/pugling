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
        var testsUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/tests";

        // Tagesmission vor dem Test: Ziel offen, Pflicht nicht erledigt.
        var before = await (await child.GetAsync($"/api/v1/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(before.GetProperty("today"), "dutyDone");

        // Test starten, alle Antworten korrekt einreichen.
        var attempt = await (await child.PostAsJsonAsync(testsUrl, new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var answers = attempt.GetProperty("items").EnumerateArray().Select(i =>
        {
            var prompt = i.GetProperty("prompt").GetString();
            return new { itemIndex = i.GetProperty("itemIndex").GetInt32(), givenAnswer = prompt == "hello" ? "hallo" : "tschüss" };
        }).ToArray();

        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(submit, "passed");

        // Ziel-Punkte einmalig gebucht (Kind = 20 = PointsGoalMet-Default), Tagesmission erledigt.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(20, db.ChildPoints.Where(e => e.ChildId == 1 && e.Kind == PointKind.Goal).Sum(e => e.Amount));
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
        }

        var after = await (await child.GetAsync($"/api/v1/study-plans/{planId}/overview"))
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
            Assert.Equal(20, db.ChildPoints.Where(e => e.ChildId == 1 && e.Kind == PointKind.Goal).Sum(e => e.Amount));
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
        var testsUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/tests";

        // Wochenziel per bestandenem Test erfüllen.
        var attempt = await (await child.PostAsJsonAsync(testsUrl, new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var answers = attempt.GetProperty("items").EnumerateArray().Select(i =>
        {
            var prompt = i.GetProperty("prompt").GetString();
            return new { itemIndex = i.GetProperty("itemIndex").GetInt32(), givenAnswer = prompt == "hello" ? "hallo" : "tschüss" };
        }).ToArray();
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
        var progress = await (await child.GetAsync($"/api/v1/study-plans/{planId}/overview/progress"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, progress.GetProperty("totalPoints").GetInt32());
    }
}
