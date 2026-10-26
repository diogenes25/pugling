using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-230: the PIN lives on the login account only, and every login checks that one place. Before, the profile row
/// carried a second copy that each write path had to mirror; a forgotten mirror let one login accept a PIN the other
/// refused. Each case changes the PIN through one write path and then logs in BOTH ways - the role login
/// (<c>auth/adult</c>/<c>auth/child</c>) and the account login (<c>auth/login</c>) - with the new PIN (200) and the
/// old one (401). Red if a write path stops setting the account's PIN, or if a login reads anything else.
/// </summary>
public class PinSingleSourceTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<int> AccountOfAsync(int? adultId = null, int? childId = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return await db.Accounts.Where(a => a.Profiles.Any(p => (adultId != null && p.AdultId == adultId) || (childId != null && p.ChildId == childId)))
            .Select(a => a.Id).SingleAsync();
    }

    private async Task AssertLoginsAsync(string roleUrl, Func<string, object> roleBodyWith, int accountId, string newPin, string oldPin)
    {
        var anonym = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonym.PostAsJsonAsync(roleUrl, roleBodyWith(newPin))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonym.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = newPin })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync(roleUrl, roleBodyWith(oldPin))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = oldPin })).StatusCode);
    }

    private async Task<(HttpClient client, int adultId)> NewAdultAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Eine-Quelle"), pin });
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (await TestApi.AdultAsync(factory, id, pin), id);
    }

    /// <summary>
    /// The other half: a write WITHOUT a PIN keeps the stored one. Every name-only PATCH runs
    /// <c>AccountService.MirrorAsync</c>, and the seed backfill runs on every start - an "ensure" that rewrote the
    /// PIN of an existing account would blank every PIN on a rename or a restart. Red if <c>EnsureAsync</c> or
    /// <c>EnsureForChildAsync</c> touch the PIN of an account they found.
    /// </summary>
    [Fact]
    public async Task Ein_Patch_Ohne_Pin_Behaelt_Die_Pin()
    {
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Bleibt-Kind"), pin = "6700" }));
        (await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { name = TestApi.UniqueName("Umbenannt") }))
            .EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/child", new { childId, pin = "6700" })).StatusCode);

        var (adult, adultId) = await NewAdultAsync("6800");
        (await adult.PatchAsJsonAsync("/api/v1/auth/me", new { name = TestApi.UniqueName("Neu-Benannt") })).EnsureSuccessStatusCode();
        (await adult.PatchAsJsonAsync($"/api/v1/supervisor/adults/{adultId}", new { name = TestApi.UniqueName("Noch-Mal") })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin = "6800" })).StatusCode);
    }

    /// <summary>
    /// The seed's side of the same rule: the backfill runs on every start and passes the KNOWN seed PIN to
    /// <c>EnsureFor…Async</c>. For an account that already exists it must be ignored, or every restart would put
    /// 0000/1111/… back on accounts whose PIN was changed. Red if an ensure applies a passed PIN to an account it found.
    /// Fresh people, so no seeded PIN is touched (the seed theories below share this class).
    /// </summary>
    [Fact]
    public async Task Ensure_Mit_Pin_Aendert_Die_Pin_Eines_Bestehenden_Kontos_Nicht()
    {
        var (_, adultId) = await NewAdultAsync("6900");
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Ensure-Kind"), pin = "6910" }));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var accounts = scope.ServiceProvider.GetRequiredService<Pugling.Api.Auth.AccountService>();
            await accounts.EnsureForAdultAsync(await db.Adults.SingleAsync(a => a.Id == adultId), "9999");
            await accounts.EnsureForTeacherAsync(await db.Adults.SingleAsync(a => a.Id == adultId), "9999");
            await accounts.EnsureForChildAsync(await db.Children.SingleAsync(c => c.Id == childId), "9999");
            await db.SaveChangesAsync();
        }

        var anonym = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonym.PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin = "6900" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin = "9999" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonym.PostAsJsonAsync("/api/v1/auth/child", new { childId, pin = "6910" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/child", new { childId, pin = "9999" })).StatusCode);
    }

    /// <summary>
    /// B-230 AC 3: every seeded person logs in with its known PIN. The PINs come from a table in the seed, keyed by
    /// e-mail (adults) or name (children); a missing or mistyped key silently leaves that person with login disabled.
    /// </summary>
    [Theory]
    [InlineData("papa@example.com", "0000")]
    [InlineData("demo-vater@example.com", "0001")]
    [InlineData("englischlehrer@example.com", "9999")]
    public async Task Geseedeter_Erwachsener_Meldet_Sich_Mit_Seiner_Pin_An(string email, string pin)
    {
        int adultId;
        using (var scope = factory.Services.CreateScope())
            adultId = await scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Adults
                .Where(a => a.Email == email).Select(a => a.Id).SingleAsync();
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin })).StatusCode);
    }

    /// <summary>The seeded children (see the adults above).</summary>
    [Theory]
    [InlineData("Sohn", "1111")]
    [InlineData("Demo-Kind", "2222")]
    public async Task Geseedetes_Kind_Meldet_Sich_Mit_Seiner_Pin_An(string name, string pin)
    {
        int childId;
        using (var scope = factory.Services.CreateScope())
            childId = await scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Children
                .Where(c => c.Name == name).OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/child", new { childId, pin })).StatusCode);
    }

    [Fact]
    public async Task Pin_Des_Kindes_Aendern_Wirkt_In_Beiden_Logins()
    {
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Eine-Quelle-Kind"), pin = "6100" }));
        var accountId = await AccountOfAsync(childId: childId);

        (await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { pin = "6200" })).EnsureSuccessStatusCode();

        await AssertLoginsAsync("/api/v1/auth/child", pin => new { childId, pin }, accountId, "6200", "6100");
    }

    [Fact]
    public async Task Pin_Des_Erwachsenen_Aendern_Wirkt_In_Beiden_Logins()
    {
        var (adult, adultId) = await NewAdultAsync("6300");
        var accountId = await AccountOfAsync(adultId: adultId);

        (await adult.PatchAsJsonAsync($"/api/v1/supervisor/adults/{adultId}", new { pin = "6400" })).EnsureSuccessStatusCode();

        await AssertLoginsAsync("/api/v1/auth/adult", pin => new { adultId, pin }, accountId, "6400", "6300");
    }

    [Fact]
    public async Task Eigene_Pin_Ueber_Auth_Me_Aendern_Wirkt_In_Beiden_Logins()
    {
        var (adult, adultId) = await NewAdultAsync("6500");
        var accountId = await AccountOfAsync(adultId: adultId);

        (await adult.PatchAsJsonAsync("/api/v1/auth/me", new { pin = "6600" })).EnsureSuccessStatusCode();

        await AssertLoginsAsync("/api/v1/auth/adult", pin => new { adultId, pin }, accountId, "6600", "6500");
    }
}
