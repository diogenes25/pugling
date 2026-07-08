using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pugling.Api.Tests;

/// <summary>
/// Identitäts-Ebene (Phase 2): ein Konto trägt mehrere Rollen; Login über Vater/Sohn und über die Konto-Id;
/// das Ensure/Backfill ist idempotent (kein zweites Konto bei erneutem Login).
/// </summary>
public class IdentityAccountTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    private static async Task<JsonElement> MeAsync(HttpClient c)
    {
        var res = await c.GetAsync("/api/v1/auth/me");
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task VaterToken_TraegtCreatorUndSupervisorRolle_UndErreichtBeideEbenen()
    {
        var father = await TestApi.FatherAsync(_factory);

        var me = await MeAsync(father);
        var roles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Creator", roles);
        Assert.Contains("Supervisor", roles);
        Assert.Contains("Vater", roles); // Alias bleibt erhalten
        Assert.Equal(1, me.GetProperty("fatherId").GetInt32());
        Assert.True(me.GetProperty("accountId").GetInt32() > 0);

        // Ein und dasselbe Token erreicht die Creator-Ebene UND die Supervisor-Ebene.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync("/api/v1/creator/subjects")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync("/api/v1/supervisor/children")).StatusCode);
    }

    [Fact]
    public async Task SohnToken_TraegtStudentRolle()
    {
        var child = await TestApi.ChildAsync(_factory);
        var me = await MeAsync(child);
        var roles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Student", roles);
        Assert.Contains("Sohn", roles);
        Assert.Equal(1, me.GetProperty("childId").GetInt32());
    }

    [Fact]
    public async Task KontoLogin_MitKontoId_LiefertMehrrollenToken()
    {
        var father = await TestApi.FatherAsync(_factory);
        var accountId = (await MeAsync(father)).GetProperty("accountId").GetInt32();

        var anon = _factory.CreateClient();
        var res = await anon.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = "0000" });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        anon.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var roles = (await MeAsync(anon)).GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Creator", roles);
        Assert.Contains("Supervisor", roles);
    }

    [Fact]
    public async Task KontoLogin_MitFalscherPin_Ist401()
    {
        var father = await TestApi.FatherAsync(_factory);
        var accountId = (await MeAsync(father)).GetProperty("accountId").GetInt32();

        var anon = _factory.CreateClient();
        var res = await anon.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = "9999" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task WiederholterLogin_ErzeugtKeinZweitesKonto()
    {
        var first = (await MeAsync(await TestApi.FatherAsync(_factory))).GetProperty("accountId").GetInt32();
        var second = (await MeAsync(await TestApi.FatherAsync(_factory))).GetProperty("accountId").GetInt32();
        Assert.Equal(first, second); // EnsureForFatherAsync ist idempotent
    }
}
