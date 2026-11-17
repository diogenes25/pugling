using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-478: two leftovers of the daily dose (B-04) found after acceptance - the "introduced" count after the father
/// shrinks the pool, and a dose that survives on a list the creator later switches to any order.
/// </summary>
public class DosisResteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string ChildPin = "1357";

    private async Task<(HttpClient Father, int ChildId, int PlanId)> PlanAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Dosis"), pin = ChildPin }));
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, subjectId = await TestApi.DefaultSubjectIdAsync(father), title = TestApi.UniqueName("Dosis-Plan"), durationDays = 7 }));
        return (father, childId, planId);
    }

    // Red if the count only caps at the pool size (seven introduced, two inside a pool of five -> 5, not 2) or counts the
    // boundary itself: index 5 is introduced and lies just outside the pool.
    [Fact]
    public async Task Eingefuehrt_Zaehlt_Nur_Woerter_Im_Pool()
    {
        var (father, childId, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "en", "de",
            [.. Enumerable.Range(0, 10).Select(i => ($"word {i}", $"Wort {i}"))]);
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily", useLeitner = true, newItemsPerDay = 3 }));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            foreach (var index in new[] { 0, 1, 5, 6, 7, 8, 9 })
                db.PositionItemProgress.Add(new PositionItemProgress { PlanPositionId = positionId, ItemIndex = index, IntroducedAt = yesterday });
            db.SaveChanges();
        }
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", new { itemCount = 5 }))
            .EnsureSuccessStatusCode();

        var child = await TestApi.ChildAsync(factory, childId, ChildPin);
        var today = (await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview")).GetProperty("today");
        var position = today.GetProperty("positions").EnumerateArray().Single(p => p.GetProperty("positionId").GetInt32() == positionId);
        Assert.Equal(5, position.GetProperty("poolSize").GetInt32());
        Assert.Equal(2, position.GetProperty("introducedCount").GetInt32());
    }

    // Red if the dose still applies once the list grades as a set: the test would ask only the dose (2 of 6), and a
    // right entry outside that hidden choice would count as wrong (B-04, decision 9).
    [Fact]
    public async Task Dosis_Gilt_Nicht_Mehr_Wenn_Die_Liste_Ungeordnet_Wird()
    {
        var (father, childId, planId) = await PlanAsync();
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Erdkunde-Dosis") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/list";
        object Body(bool ordered) => new
        {
            title = "Sechs Bundesländer",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                instruction = "Nenne sechs Bundesländer.",
                ordered,
                items = new[] { "Bayern", "Hessen", "Berlin", "Bremen", "Sachsen", "Hamburg" }.Select(v => new { value = v }).ToArray(),
            },
        };
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(basePath, Body(ordered: true)));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily", useLeitner = true, newItemsPerDay = 2, stage = (int)TestStage.FreeText }));

        // B-480: on the ordered list the dose is in force.
        Assert.Equal(2, (await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}"))
            .GetProperty("dailyDoseInForce").GetInt32());

        // The creator switches the assigned list to any order - the position keeps its stored dose.
        (await father.PutAsJsonAsync($"{basePath}/{exerciseId}", Body(ordered: false))).EnsureSuccessStatusCode();

        // The father can still edit the position as his form sends it (the stored dose along); only setting a new dose
        // on the set-graded list is refused.
        var positionUrl = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}";
        // B-480: the father reads the dose that rests - stored 2, in force 0 - and not a dose the play ignores.
        var position = await father.GetFromJsonAsync<JsonElement>(positionUrl);
        Assert.Equal(2, position.GetProperty("newItemsPerDay").GetInt32());
        Assert.Equal(0, position.GetProperty("dailyDoseInForce").GetInt32());
        (await father.PatchAsJsonAsync(positionUrl, new { penaltyCoins = 3, newItemsPerDay = 2 })).EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await father.PatchAsJsonAsync(positionUrl, new { newItemsPerDay = 3 })).StatusCode);


        var child = await TestApi.ChildAsync(factory, childId, ChildPin);

        // The practice round: every entry, not the two of a dose.
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { mode = "Lern" }));
        var cards = await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards");
        Assert.Equal(6, cards!.Count);
        (await child.PostAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/end", null)).EnsureSuccessStatusCode();

        // The test: the whole list, and starting it introduces nothing in advance (that is the dose's way).
        var start = await child.PostAsJsonAsync($"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { });
        start.EnsureSuccessStatusCode();
        Assert.Equal(6, (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalItems").GetInt32());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(0, db.PositionItemProgress.Count(p => p.PlanPositionId == positionId && p.IntroducedAt != null));
    }

    // B-480 point 3: switching the box off keeps PATCH semantics (the stored dose is not cleared silently), so the
    // refusal names the step that lifts it - and that step works, on create and on patch. Red if the message loses the
    // way out, if the patch reads the dose from the wrong side, or if the named request is refused too.
    [Fact]
    public async Task Dosis_Ohne_Karteikasten_Nennt_Den_Ausweg_Und_Er_Geht()
    {
        var (father, _, planId) = await PlanAsync();
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Dosis-Ausweg") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/list", new
            {
                title = "Vier Flüsse",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    instruction = "Nenne vier Flüsse.",
                    ordered = true,
                    items = new[] { "Rhein", "Elbe", "Main", "Weser" }.Select(v => new { value = v }).ToArray()
                },
            }));
        var positionsUrl = $"/api/v1/supervisor/study-plans/{planId}/positions";

        // Create: refused with the way out, and the way out is accepted.
        var refused = await father.PostAsJsonAsync(positionsUrl,
            new { exerciseId, cadence = "Daily", useLeitner = false, newItemsPerDay = 2, stage = (int)TestStage.FreeText });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("newItemsPerDay: 0", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
        (await father.PostAsJsonAsync(positionsUrl,
            new { exerciseId, cadence = "Daily", useLeitner = false, newItemsPerDay = 0, stage = (int)TestStage.FreeText })).EnsureSuccessStatusCode();

        // Patch: a position with box and dose; switching the box off alone is refused, the named request passes.
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(positionsUrl,
            new { exerciseId, cadence = "Daily", useLeitner = true, newItemsPerDay = 2, stage = (int)TestStage.FreeText }));
        var positionUrl = $"{positionsUrl}/{positionId}";
        var off = await father.PatchAsJsonAsync(positionUrl, new { useLeitner = false });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, off.StatusCode);
        Assert.Contains("newItemsPerDay: 0", (await off.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
        (await father.PatchAsJsonAsync(positionUrl, new { useLeitner = false, newItemsPerDay = 0 })).EnsureSuccessStatusCode();
        Assert.False((await father.GetFromJsonAsync<JsonElement>(positionUrl)).GetProperty("useLeitner").GetBoolean());
    }
}
