using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Auth;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-28: the login name is a small, dense id, so the PIN is the only barrier. A stored PIN is either empty ("login
/// disabled") or at least four characters, on every path that writes one; and an empty stored PIN never logs in,
/// not even with an empty input. Each boundary case uses 3 against 4 characters, so a rule "&lt; 5" or "&lt;= 4" fails.
/// </summary>
public class PinPolicyTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private static async Task AssertTooShortAsync(HttpResponseMessage res)
    {
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(res));
    }

    /// <summary>AC 1/2 on the three create paths: 3 characters refused, 4 and empty accepted.</summary>
    [Fact]
    public async Task Anlegen_Lehnt_Eine_Pin_Unter_Vier_Zeichen_Ab_Und_Nimmt_Vier_Oder_Leer()
    {
        var anonym = factory.CreateClient();
        var papa = await TestApi.AdultAsync(factory);
        var posts = new (HttpClient client, string url, Func<string, object> body)[]
        {
            (anonym, "/api/v1/supervisor/adults", pin => new { name = TestApi.UniqueName("Pin-Papa"), pin }),
            (anonym, "/api/v1/creator/teacher-accounts", pin => new { name = TestApi.UniqueName("Pin-Lehrerin"), email = (string?)null, pin }),
            (papa, "/api/v1/supervisor/children", pin => new { name = TestApi.UniqueName("Pin-Kind"), pin }),
        };
        foreach (var (client, url, body) in posts)
        {
            await AssertTooShortAsync(await client.PostAsJsonAsync(url, body("123")));
            Assert.True((await client.PostAsJsonAsync(url, body("1234"))).IsSuccessStatusCode, $"{url}: 4 characters");
            Assert.True((await client.PostAsJsonAsync(url, body(""))).IsSuccessStatusCode, $"{url}: empty = disabled");
        }
    }

    /// <summary>AC 1/2 on the three update paths.</summary>
    [Fact]
    public async Task Aendern_Lehnt_Eine_Pin_Unter_Vier_Zeichen_Ab_Und_Nimmt_Vier_Oder_Leer()
    {
        var created = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Pin-Aendern"), pin = "7070" });
        var adultId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var adult = await TestApi.AdultAsync(factory, adultId, "7070");
        var childId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Pin-Kind"), pin = "7171" }));

        var patches = new (string url, string kind)[]
        {
            ($"/api/v1/supervisor/children/{childId}", "child"),
            ($"/api/v1/supervisor/adults/{adultId}", "adult"),
            ("/api/v1/auth/me", "me"),
        };
        foreach (var (url, _) in patches)
        {
            await AssertTooShortAsync(await adult.PatchAsJsonAsync(url, new { pin = "123" }));
            Assert.True((await adult.PatchAsJsonAsync(url, new { pin = "7272" })).IsSuccessStatusCode, $"{url}: 4 characters");
        }
        // Empty last: it disables the adult's own login, and the token still has to work for the calls above.
        Assert.True((await adult.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { pin = "" })).IsSuccessStatusCode);
        Assert.True((await adult.PatchAsJsonAsync($"/api/v1/supervisor/adults/{adultId}", new { pin = "" })).IsSuccessStatusCode);
        Assert.True((await adult.PatchAsJsonAsync("/api/v1/auth/me", new { pin = "" })).IsSuccessStatusCode);
    }

    /// <summary>
    /// B-282 point 5: an empty PIN sent by PATCH really disables the login - each path on its own subject, so one path
    /// cannot pass on another's work. The old PIN logs in before the PATCH (the case can fail) and is refused after it.
    /// Red if a path accepts "" with 2xx but skips it (<c>if (!string.IsNullOrEmpty(dto.Pin)) SetPin(…)</c>).
    /// </summary>
    [Fact]
    public async Task Leere_Pin_Per_Patch_Sperrt_Den_Alten_Login()
    {
        var anonym = factory.CreateClient();
        async Task<int> NewAdultAsync() => (await (await anonym.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Pin-Leer"), pin = "8080" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Task<HttpResponseMessage> AdultLogin(int id) => anonym.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = id, pin = "8080" });

        // Child, via the supervising adult.
        var papaId = await NewAdultAsync();
        var papa = await TestApi.AdultAsync(factory, papaId, "8080");
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Pin-Leer-Kind"), pin = "8181" }));
        Task<HttpResponseMessage> ChildLogin() => anonym.PostAsJsonAsync("/api/v1/auth/child", new { childId, pin = "8181" });
        Assert.Equal(HttpStatusCode.OK, (await ChildLogin()).StatusCode);
        (await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { pin = "" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await ChildLogin()).StatusCode);

        // Adult, via supervisor/adults/{id}.
        var viaAdults = await NewAdultAsync();
        var self = await TestApi.AdultAsync(factory, viaAdults, "8080");
        Assert.Equal(HttpStatusCode.OK, (await AdultLogin(viaAdults)).StatusCode);
        (await self.PatchAsJsonAsync($"/api/v1/supervisor/adults/{viaAdults}", new { pin = "" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await AdultLogin(viaAdults)).StatusCode);

        // Adult, via auth/me.
        var viaMe = await NewAdultAsync();
        var me = await TestApi.AdultAsync(factory, viaMe, "8080");
        Assert.Equal(HttpStatusCode.OK, (await AdultLogin(viaMe)).StatusCode);
        (await me.PatchAsJsonAsync("/api/v1/auth/me", new { pin = "" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await AdultLogin(viaMe)).StatusCode);
    }

    /// <summary>
    /// AC 3: an empty stored PIN does not log in through any of the three logins - not with an empty input either.
    /// Red without the guard in <see cref="PinHasher.Verify"/>: the legacy plaintext branch compared "" with "".
    /// </summary>
    [Fact]
    public async Task Leere_Gespeicherte_Pin_Loggt_Nirgends_Ein()
    {
        var anonym = factory.CreateClient();
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Ohne-Pin"), pin = "" }));
        var adultRes = await anonym.PostAsJsonAsync("/api/v1/supervisor/adults", new { name = TestApi.UniqueName("Ohne-Pin"), pin = "" });
        var adultId = (await adultRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        int childAccountId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            childAccountId = await db.Accounts.Where(a => a.Profiles.Any(p => p.ChildId == childId)).Select(a => a.Id).SingleAsync();
        }

        foreach (var pin in new[] { "", "0000" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/child", new { childId, pin })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/login", new { accountId = childAccountId, pin })).StatusCode);
        }
    }

    [Fact]
    public void Verify_Lehnt_Eine_Leere_Gespeicherte_Pin_Ab()
    {
        Assert.False(PinHasher.Verify("", ""));
        // Counter-check: legacy plaintext still works for a non-empty stored PIN.
        Assert.True(PinHasher.Verify("0000", "0000"));
    }
}
