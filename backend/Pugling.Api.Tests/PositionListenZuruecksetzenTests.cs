using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-547: the two list settings of a position (<c>boxIntervalDays</c>, <c>stageSchedule</c>) store <c>null</c> as
/// "method default". A PATCH with an empty list puts them back there; before, the empty list was stored and the
/// position never read "default" again. The switch fields <c>clearItemCount</c>/<c>clearGoalThreshold</c> are held by
/// the table in <see cref="PatchSemanticsTests"/>.
/// </summary>
public class PositionListenZuruecksetzenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>
    /// Both lists are set on create, then a PATCH sends both empty: the response reads <c>null</c> for both. Red when the
    /// controller stores the empty list (the response would read <c>[]</c>).
    /// </summary>
    [Fact]
    public async Task Leere_Liste_Setzt_Intervalle_Und_Stufenplan_Auf_Die_Vorgabe_Zurueck()
    {
        var father = await TestApi.AdultAsync(factory);
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new
            {
                exerciseId,
                boxIntervalDays = new[] { 0, 2, 5 },
                stageSchedule = new[] { new { dayNumber = 1, stage = 1 }, new { dayNumber = 3, stage = 4 } },
            }));

        var res = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{id}",
            new { boxIntervalDays = Array.Empty<int>(), stageSchedule = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var pos = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, pos.GetProperty("boxIntervalDays").ValueKind);
        Assert.Equal(JsonValueKind.Null, pos.GetProperty("stageSchedule").ValueKind);
    }

    /// <summary>
    /// Creating with empty lists stores the default too - the same reading as the PATCH, no third state. Red when the
    /// create path stores the empty list (the response would read <c>[]</c>).
    /// </summary>
    [Fact]
    public async Task Leere_Listen_Beim_Anlegen_Sind_Die_Vorgabe()
    {
        var father = await TestApi.AdultAsync(factory);
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, boxIntervalDays = Array.Empty<int>(), stageSchedule = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var pos = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, pos.GetProperty("boxIntervalDays").ValueKind);
        Assert.Equal(JsonValueKind.Null, pos.GetProperty("stageSchedule").ValueKind);
    }

    /// <summary>
    /// The counter-case: a PATCH that leaves the lists out keeps them. Red when "not specified" were read as "empty".
    /// </summary>
    [Fact]
    public async Task Ohne_Angabe_Bleiben_Die_Listen()
    {
        var father = await TestApi.AdultAsync(factory);
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, boxIntervalDays = new[] { 0, 2, 5 } }));

        var res = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{id}",
            new { penaltyCoins = 3 });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var pos = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal([0, 2, 5], pos.GetProperty("boxIntervalDays").EnumerateArray().Select(e => e.GetInt32()));
    }
}
