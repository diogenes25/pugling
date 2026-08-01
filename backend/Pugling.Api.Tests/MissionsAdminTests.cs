using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Secures the father lifecycle for missions used by the reward UI (list → create → toggle
/// active/inactive → delete). The purchase/progress part is covered in <c>GamificationTests</c>; here
/// it's just the pure administrative verbs.
/// </summary>
public class MissionsAdminTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Vater_KannMission_Anlegen_Schalten_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Missions-Kind", pin = "8001" }));

        // Create
        var created = await (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/missions", new
        {
            title = "Tagesziel: 10 richtig",
            metric = "CorrectReviews",
            target = 10,
            period = "Daily",
            rewardPoints = 15,
        })).Content.ReadFromJsonAsync<JsonElement>();
        var missionId = created.GetProperty("id").GetInt32();
        JsonAssert.True(created, "active");

        // The list contains the mission
        var list = await (await father.GetAsync($"/api/v1/supervisor/children/{childId}/missions")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(missionId, list.EnumerateArray().Select(m => m.GetProperty("id").GetInt32()));

        // Deactivate (PATCH active=false)
        var patched = await (await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/missions/{missionId}", new { active = false }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(patched, "active");

        // Delete → afterwards a 404 on deleting again
        (await father.DeleteAsync($"/api/v1/supervisor/children/{childId}/missions/{missionId}")).EnsureSuccessStatusCode();
        var again = await father.DeleteAsync($"/api/v1/supervisor/children/{childId}/missions/{missionId}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Vater_KommtNichtAnMissionenFremderKinder_403Oder404()
    {
        // The ownership filter bites before the controller: a child not belonging to the adult yields neither
        // a list nor a creation - checked here through a non-existent child.
        var father = await TestApi.FatherAsync(factory);

        var res = await father.GetAsync("/api/v1/supervisor/children/999999/missions");

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────── Reading and deleting awards (a C3 coverage gap)

    [Fact]
    public async Task Auszeichnungen_Liste_Und_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Auszeichnungs-Kind", pin = "6301" }));
        var url = $"/api/v1/supervisor/children/{childId}/achievements";
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(url,
            new { title = "Hundert Wörter", metric = "NewWords", threshold = 100, rewardPoints = 50 }));

        var liste = await (await father.GetAsync(url)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(id, liste.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()));

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{url}/{id}")).StatusCode);
        Assert.Empty((await (await father.GetAsync(url)).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"{url}/{id}")).StatusCode);
    }
}
