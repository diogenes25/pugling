using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Kindübergreifendes Tages-Dashboard des Vaters: zeigt je Kind, ob das Tagessoll offen oder erledigt ist,
/// und aktualisiert sich, sobald das Kind seinen Positions-Test besteht.
/// </summary>
public class ChildrenDashboardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static JsonElement ChildRow(JsonElement dashboard, int childId) =>
        dashboard.GetProperty("children").EnumerateArray().First(c => c.GetProperty("childId").GetInt32() == childId);

    [Fact]
    public async Task Dashboard_SpiegeltTagessoll_VorUndNachTestabschluss()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        // Vorher: Kind sichtbar, Tagessoll vorhanden aber offen.
        var before = await (await father.GetAsync("/api/v1/children/daily-overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rowBefore = ChildRow(before, 1);
        Assert.True(rowBefore.GetProperty("goalsTotal").GetInt32() >= 1);
        Assert.False(rowBefore.GetProperty("dutyDone").GetBoolean());

        // Positions-Test bestehen → Tagesziel erfüllt, Ziel-Punkte gebucht.
        var testsUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/tests";
        var attempt = await (await child.PostAsJsonAsync(testsUrl, new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var answers = attempt.GetProperty("items").EnumerateArray().Select(i =>
        {
            var prompt = i.GetProperty("prompt").GetString();
            return new { itemIndex = i.GetProperty("itemIndex").GetInt32(), givenAnswer = prompt == "hello" ? "hallo" : "tschüss" };
        }).ToArray();
        await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers });

        // Nachher: Pflicht erledigt, Punkte des Tages sichtbar, als „geübt" markiert.
        var after = await (await father.GetAsync("/api/v1/children/daily-overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rowAfter = ChildRow(after, 1);
        Assert.True(rowAfter.GetProperty("dutyDone").GetBoolean());
        Assert.True(rowAfter.GetProperty("pointsToday").GetInt32() > 0);
        Assert.True(rowAfter.GetProperty("practiced").GetBoolean());
    }
}
