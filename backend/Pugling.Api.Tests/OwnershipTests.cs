using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Integration tests for role and ownership boundaries (IDOR regression protection).</summary>
public class OwnershipTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Registers (anonymously) a second father and returns his id.</summary>
    private async Task<int> RegisterFatherAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = "Papa2", pin });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Sohn_DarfKeinenPlanAnlegen_403()
    {
        var child = await TestApi.ChildAsync(factory);

        var res = await child.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId = 1, title = "X", method = "Vocabulary", durationDays = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Vater_SiehtNurEigenenDatensatz()
    {
        await RegisterFatherAsync("2222"); // a second adult now exists …
        var father1 = await TestApi.FatherAsync(factory);

        var list = await (await father1.GetAsync("/api/v1/supervisor/adults")).Content.ReadFromJsonAsync<JsonElement>();

        // … the list still shows only the own record.
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(1, list[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Vater_KannAufFremdenVaterNichtZugreifen_403()
    {
        var id2 = await RegisterFatherAsync("2222");
        var father1 = await TestApi.FatherAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await father1.GetAsync($"/api/v1/supervisor/adults/{id2}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await father1.DeleteAsync($"/api/v1/supervisor/adults/{id2}")).StatusCode);
    }

    [Fact]
    public async Task Vater_KannFremdesKind_NichtSehen_404()
    {
        // The second adult creates a child …
        var id2 = await RegisterFatherAsync("2222");
        var father2 = await TestApi.FatherAsync(factory, id2, "2222");
        var child2 = await TestApi.IdAsync(await father2.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Kind2" }));

        // … the first adult must not see it (ChildOwnershipFilter → 404, no enumeration).
        var father1 = await TestApi.FatherAsync(factory);
        Assert.Equal(HttpStatusCode.NotFound, (await father1.GetAsync($"/api/v1/supervisor/children/{child2}")).StatusCode);
    }

    [Fact]
    public async Task FremderVater_KannPlanNichtSehen_403()
    {
        var father1 = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateEmptyPlanAsync(father1);

        var id2 = await RegisterFatherAsync("2222");
        var father2 = await TestApi.FatherAsync(factory, id2, "2222");

        Assert.Equal(HttpStatusCode.Forbidden, (await father2.GetAsync($"/api/v1/supervisor/study-plans/{planId}")).StatusCode);
    }

    [Fact]
    public async Task Sohn_KannPlanEinesAnderenKindes_NichtBenutzen_403()
    {
        // The adult creates a second child and seeds a position plan for it …
        var father = await TestApi.FatherAsync(factory);
        var otherChildId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Bruder" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess, childId: otherChildId);

        // … the seeded son (id 1) is a FOREIGN child for this plan: the child branch of
        // AuthAccess.OwnsPlanAsync compares plan.ChildId with the own cid → every access 403.
        // A regression guard against cross-child IDOR (so far only adult↔adult/child was covered).
        var child1 = await TestApi.ChildAsync(factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await child1.GetAsync($"/api/v1/supervisor/study-plans/{planId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await child1.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await child1.GetAsync($"/api/v1/student/study-plans/{planId}/positions/{positionId}/report")).StatusCode);
    }
}
