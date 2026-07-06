using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert den Vater-Lebenszyklus für Missionen ab, den die Belohnungs-Oberfläche nutzt
/// (Liste → Anlegen → aktiv/inaktiv schalten → Löschen). Der Kauf-/Fortschritts-Teil ist in
/// <c>GamificationTests</c> abgedeckt; hier geht es um die reinen Verwaltungs-Verben.
/// </summary>
public class MissionsAdminTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Vater_KannMission_Anlegen_Schalten_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/children", new { name = "Missions-Kind", pin = "8001" }));

        // Anlegen
        var created = await (await father.PostAsJsonAsync($"/api/v1/children/{childId}/missions", new
        {
            title = "Tagesziel: 10 richtig",
            metric = "CorrectReviews",
            target = 10,
            period = "Daily",
            rewardPoints = 15,
        })).Content.ReadFromJsonAsync<JsonElement>();
        var missionId = created.GetProperty("id").GetInt32();
        JsonAssert.True(created, "active");

        // Liste enthält die Mission
        var list = await (await father.GetAsync($"/api/v1/children/{childId}/missions")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(missionId, list.EnumerateArray().Select(m => m.GetProperty("id").GetInt32()));

        // Deaktivieren (PATCH active=false)
        var patched = await (await father.PatchAsJsonAsync(
            $"/api/v1/children/{childId}/missions/{missionId}", new { active = false }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(patched, "active");

        // Löschen → danach 404 beim erneuten Löschen
        (await father.DeleteAsync($"/api/v1/children/{childId}/missions/{missionId}")).EnsureSuccessStatusCode();
        var again = await father.DeleteAsync($"/api/v1/children/{childId}/missions/{missionId}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Vater_KommtNichtAnMissionenFremderKinder_403Oder404()
    {
        // Der Ownership-Filter greift vor dem Controller: ein nicht (dem Vater) gehörendes Kind
        // liefert weder Liste noch Anlage – hier über ein nicht existierendes Kind geprüft.
        var father = await TestApi.FatherAsync(factory);

        var res = await father.GetAsync("/api/v1/children/999999/missions");

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }
}
