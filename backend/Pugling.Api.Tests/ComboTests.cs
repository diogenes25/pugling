using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft den serverseitig gezählten Combo-Bonus und dass er über die Plan-Einstellungen
/// (<c>ComboThreshold</c>/<c>ComboBonusPoints</c>) konfigurierbar bzw. abschaltbar ist.
/// </summary>
public class ComboTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<int> LeitnerPlanAsync(HttpClient father, int? threshold, int? bonus) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/study-plans", new
        {
            childId = 1,
            title = "Combo-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            useLeitner = true,
            comboThreshold = threshold,
            comboBonusPoints = bonus,
        }));

    private static async Task<(int session, List<int> contentIds)> StartSessionAsync(HttpClient child, int planId)
    {
        var plan = await (await child.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var ids = plan.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();
        var sid = (await (await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (sid, ids);
    }

    // Stufe 2 = SelfAssess (Selbsteinschätzung); ohne RequireTypedTest zählt das WasKnown-Flag voll.
    private static async Task<JsonElement> ReviewAsync(HttpClient child, int planId, int sid, int contentId) =>
        await (await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions/{sid}/review",
            new { contentId, stage = 2, wasKnown = true })).Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task ComboBonus_LautPlanEinstellung_BeiSchwelleErreicht()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father, threshold: 2, bonus: 7);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartSessionAsync(child, planId);

        var first = await ReviewAsync(child, planId, sid, ids[0]);
        Assert.Equal(1, first.GetProperty("combo").GetInt32());
        Assert.Equal(0, first.GetProperty("comboBonus").GetInt32());

        var second = await ReviewAsync(child, planId, sid, ids[1]);
        Assert.Equal(2, second.GetProperty("combo").GetInt32());
        Assert.Equal(7, second.GetProperty("comboBonus").GetInt32()); // Basis 7 × Meilenstein 1
    }

    [Fact]
    public async Task ComboBonus_WirdAlsEigenerPointKindGebucht()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father, threshold: 2, bonus: 7);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartSessionAsync(child, planId);

        await ReviewAsync(child, planId, sid, ids[0]);
        await ReviewAsync(child, planId, sid, ids[1]); // Schwelle 2 erreicht → Combo-Bonus

        // Der Vater sieht die Buchungen kategorisiert: der Bonus trägt Kind "Combo", nicht "Base".
        var points = await (await father.GetAsync("/api/fathers/1/children/1/points"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var combo = points.GetProperty("entries").EnumerateArray()
            .First(e => e.GetProperty("kind").GetString() == "Combo");
        Assert.Equal(7, combo.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task ComboBonus_AbgeschaltetBeiSchwelleNull()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father, threshold: 0, bonus: 7);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartSessionAsync(child, planId);

        await ReviewAsync(child, planId, sid, ids[0]);
        var second = await ReviewAsync(child, planId, sid, ids[1]);

        Assert.Equal(2, second.GetProperty("combo").GetInt32());
        Assert.Equal(0, second.GetProperty("comboBonus").GetInt32()); // Feature aus
    }
}
