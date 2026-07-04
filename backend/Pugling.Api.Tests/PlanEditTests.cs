using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert die Plan-Bearbeitung ab, die die Plan-Detail-Oberfläche des Vaters nutzt:
/// verlängern/umbenennen/deaktivieren (PATCH) sowie Inhalte nachschieben/entfernen.
/// </summary>
public class PlanEditTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Vater_KannPlan_Umbenennen_Verlaengern_Deaktivieren()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father);

        var res = await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new
        {
            title = "Verlängerter Plan",
            endDate = "2027-01-31",
            active = false,
        });
        res.EnsureSuccessStatusCode();
        var plan = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Verlängerter Plan", plan.GetProperty("title").GetString());
        Assert.Equal("2027-01-31", plan.GetProperty("endDate").GetString());
        Assert.False(plan.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Vater_KannInhalt_Nachschieben_UndEntfernen()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father); // startet mit 2 Inhalten

        // Nachschieben (dritter geseedeter Vokabel-Key)
        var added = await (await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/items",
            new { contentKey = "en_goes_de_geht" })).Content.ReadFromJsonAsync<JsonElement>();
        var items = added.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);

        // Denselben Key erneut -> 400 (schon im Plan)
        var dup = await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/items", new { contentKey = "en_goes_de_geht" });
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);

        // Entfernen -> 204, danach wieder 2 Inhalte
        var itemId = items[^1].GetProperty("id").GetInt32();
        (await father.DeleteAsync($"/api/v1/study-plans/{planId}/items/{itemId}")).EnsureSuccessStatusCode();
        var after = await (await father.GetAsync($"/api/v1/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, after.GetProperty("items").GetArrayLength());
    }
}
