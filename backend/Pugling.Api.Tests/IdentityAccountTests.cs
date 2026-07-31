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
        var father = await TestApi.FatherAsync(_factory);

        var me = await MeAsync(father);
        var roles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Creator", roles);
        Assert.Contains("Supervisor", roles);
        Assert.DoesNotContain("Vater", roles); // Alias entfernt – nur noch Ebenen-Rollen
        Assert.Equal(1, me.GetProperty("adultId").GetInt32());
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
        Assert.DoesNotContain("Sohn", roles); // Alias entfernt – nur noch Ebenen-Rollen
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
        Assert.Equal(first, second); // EnsureForAdultAsync ist idempotent
    }

    /// <summary>
    /// Das Konto ist die <b>Spiegelung</b> der fachlichen Zeile, nicht ein zweiter Datenstand: benennt der
    /// Vater sein Kind um, muss der Anzeigename des Logins mitgehen. Er ist das, was
    /// <c>POST auth/login</c> zurückgibt und was als <c>ClaimTypes.Name</c> im Token landet – die
    /// Sohn-Oberfläche hätte sonst nach dem nächsten Anmelden weiter den alten Namen begrüßt.
    /// </summary>
    [Fact]
    public async Task UmbenanntesKind_MeldetSichMitDemNeuenNamenAn()
    {
        var father = await TestApi.FatherAsync(_factory);
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
    /// „Genau eines von <c>AdultId</c>/<c>ChildId</c>" behauptete der Kommentar an
    /// <c>AccountProfile</c> seit immer, ohne dass es irgendwo durchgesetzt war – ein Profil mit
    /// <b>beiden</b> Zielen wäre ein Login mit zwei Identitäten dahinter, eines mit <b>keinem</b> eine
    /// Rolle, die auf nichts zeigt (und die <c>AuthAccess</c> stumm ins Leere prüfen lässt). Das Vorbild
    /// steht direkt daneben: <c>MediaLink</c> und <c>ChildMediaPick</c> tragen dieselbe Frage längst als
    /// Check-Constraint.
    /// </summary>
    [Fact]
    public async Task ProfilOhneGenauEinZiel_WeistDieDatenbankAb()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();

        // Frische Zeilen ohne Konto, damit nur die XOR-Regel im Weg steht und nicht die gefilterten
        // Unique-Indizes auf (Rolle, Profil).
        var adult = new Adult { Name = "Zwitter-Adult", Pin = "" };
        var child = new Child { Name = "Zwitter-Child", Pin = "" };
        var konto = new Account { DisplayName = "Zwitter", PinHash = "" };
        db.AddRange(adult, child, konto);
        await db.SaveChangesAsync();

        async Task VerweigertAsync(AccountProfile profil)
        {
            db.AccountProfiles.Add(profil);
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(profil).State = EntityState.Detached; // sonst versucht der nächste Aufruf beide
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
