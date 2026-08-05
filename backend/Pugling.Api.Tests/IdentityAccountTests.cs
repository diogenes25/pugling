using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Identity layer (phase 2): one account carries multiple roles; login via father/child and via the
/// account id; the ensure/backfill is idempotent (no second account on repeated login).
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
        var father = await TestApi.AdultAsync(_factory);

        var me = await MeAsync(father);
        var roles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Creator", roles);
        Assert.Contains("Supervisor", roles);
        Assert.DoesNotContain("Vater", roles); // the alias is gone - tier roles only
        Assert.Equal(1, me.GetProperty("adultId").GetInt32());
        Assert.True(me.GetProperty("accountId").GetInt32() > 0);

        // One and the same token reaches the creator tier AND the supervisor tier.
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
        Assert.DoesNotContain("Sohn", roles); // the alias is gone - tier roles only
        Assert.Equal(1, me.GetProperty("childId").GetInt32());
    }

    [Fact]
    public async Task KontoLogin_MitKontoId_LiefertMehrrollenToken()
    {
        var father = await TestApi.AdultAsync(_factory);
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
        var father = await TestApi.AdultAsync(_factory);
        var accountId = (await MeAsync(father)).GetProperty("accountId").GetInt32();

        var anon = _factory.CreateClient();
        var res = await anon.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = "9999" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task WiederholterLogin_ErzeugtKeinZweitesKonto()
    {
        var first = (await MeAsync(await TestApi.AdultAsync(_factory))).GetProperty("accountId").GetInt32();
        var second = (await MeAsync(await TestApi.AdultAsync(_factory))).GetProperty("accountId").GetInt32();
        Assert.Equal(first, second); // EnsureForAdultAsync is idempotent
    }

    /// <summary>
    /// The account is the <b>mirror</b> of the domain row, not a second state: if the supervisor renames their
    /// child, the login's display name has to go along. It is what <c>POST auth/login</c> returns and what
    /// lands in the token as <c>ClaimTypes.Name</c> - the child's UI would otherwise have kept greeting the old
    /// name after the next login.
    /// </summary>
    [Fact]
    public async Task UmbenanntesKind_MeldetSichMitDemNeuenNamenAn()
    {
        var father = await TestApi.AdultAsync(_factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kind Alt", pin = "3131" }));
        var accountId = (await MeAsync(await TestApi.ChildAsync(_factory, childId, "3131")))
            .GetProperty("accountId").GetInt32();

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { name = "Kind Neu" }))
            .EnsureSuccessStatusCode();

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { accountId, pin = "3131" });
        login.EnsureSuccessStatusCode();
        Assert.Equal("Kind Neu",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());
    }

    /// <summary>
    /// "Exactly one of <c>AdultId</c>/<c>ChildId</c>" is what the comment on <c>AccountProfile</c> always
    /// claimed, without it being enforced anywhere - a profile with <b>both</b> targets would be one login with
    /// two identities behind it, one with <b>neither</b> a role pointing at nothing (and letting
    /// <c>AuthAccess</c> check silently into the void). The model stands right next to it: <c>MediaLink</c> and
    /// <c>ChildMediaPick</c> have long carried the same question as a check constraint.
    /// </summary>
    [Fact]
    public async Task ProfilOhneGenauEinZiel_WeistDieDatenbankAb()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();

        // Fresh rows without an account, so that only the XOR rule stands in the way and not the filtered
        // unique indexes on (role, profile).
        var adult = new Adult { Name = "Zwitter-Adult", Pin = "" };
        var child = new Child { Name = "Zwitter-Child", Pin = "" };
        var konto = new Account { DisplayName = "Zwitter", PinHash = "" };
        db.AddRange(adult, child, konto);
        await db.SaveChangesAsync();

        async Task VerweigertAsync(AccountProfile profil)
        {
            db.AccountProfiles.Add(profil);
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(profil).State = EntityState.Detached; // otherwise the next call tries both
        }

        await VerweigertAsync(new AccountProfile
        {
            AccountId = konto.Id,
            Role = ProfileRole.Creator,
            AdultId = adult.Id,
            ChildId = child.Id,
        });
        await VerweigertAsync(new AccountProfile
        {
            AccountId = konto.Id,
            Role = ProfileRole.Supervisor,
            AdultId = null,
            ChildId = null,
        });
    }
}
