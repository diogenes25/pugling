using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Self-service for one's own account (<c>PATCH auth/me</c>): name, email, PIN.
///
/// The trigger was the <b>teacher account</b> – it lacks the supervisor role, and so
/// <c>supervisor/adults/{id}</c> was closed to it: it could not change its own PIN. Because the
/// account does not belong to any tier (the same person operates it from every role), the route lives
/// under <c>auth/…</c> and applies to <b>both</b> adult kinds.
/// </summary>
public class AccountSelfServiceTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private HttpClient WithToken(string token)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<(int creatorId, HttpClient client)> NewTeacherAsync(string name, string pin)
    {
        var created = await _factory.CreateClient().PostAsJsonAsync("/api/v1/creator/teacher-accounts",
            new { name, email = (string?)null, pin });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("creatorId").GetInt32();

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult", new { adultId = id, pin });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        return (id, WithToken(token));
    }

    [Fact]
    public async Task Lehrer_AendertNamenUndEMail_UndBeidesGiltAuchFachlich()
    {
        var (creatorId, teacher) = await NewTeacherAsync("Frau Alt", "1212");
        var mail = $"neu-{Guid.NewGuid():N}@schule.example";

        var res = await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { name = "Frau Neu", email = mail });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Frau Neu", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());

        // The name hangs in TWO places: on the account (login) and on the domain row, where it appears as the
        // author. The counter-check runs through a creator endpoint, not through `auth/me`.
        var account = await teacher.GetFromJsonAsync<JsonElement>($"/api/v1/creator/teacher-accounts/{creatorId}");
        Assert.Equal("Frau Neu", account.GetProperty("name").GetString());
        Assert.Equal(mail, account.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Lehrer_AendertPin_UndMeldetSichDamitAn()
    {
        var (creatorId, teacher) = await NewTeacherAsync("Frau Schlüssel", "2323");

        (await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { pin = "9090" })).EnsureSuccessStatusCode();

        // The old PIN no longer applies …
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
                new { adultId = creatorId, pin = "2323" })).StatusCode);
        // … the new one does, and on both login paths (the hash is mirrored onto the account).
        var byFid = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
            new { adultId = creatorId, pin = "9090" });
        byFid.EnsureSuccessStatusCode();
        var accountId = (await (await WithToken((await byFid.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString()!).GetAsync("/api/v1/auth/me"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accountId").GetInt32();
        (await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { accountId, pin = "9090" })).EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Without the switch, a form with a cleared field would report "saved", and the old address would
    /// still be there – <c>null</c> means "not specified" in this API, not "clear".
    /// </summary>
    [Fact]
    public async Task EMailLeeren_BrauchtDenSchalter()
    {
        var (creatorId, teacher) = await NewTeacherAsync("Frau Post", "3434");
        var mail = $"weg-{Guid.NewGuid():N}@schule.example";
        (await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { email = mail })).EnsureSuccessStatusCode();

        // `null` leaves the address as it is.
        (await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { name = "Frau Post" })).EnsureSuccessStatusCode();
        var still = await teacher.GetFromJsonAsync<JsonElement>($"/api/v1/creator/teacher-accounts/{creatorId}");
        Assert.Equal(mail, still.GetProperty("email").GetString());

        (await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { clearEmail = true })).EnsureSuccessStatusCode();
        var gone = await teacher.GetFromJsonAsync<JsonElement>($"/api/v1/creator/teacher-accounts/{creatorId}");
        Assert.True(gone.GetProperty("email").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public async Task FremdeEMail_WirdAbgewiesen()
    {
        var mail = $"besetzt-{Guid.NewGuid():N}@schule.example";
        var (_, first) = await NewTeacherAsync("Frau Erst", "4545");
        (await first.PatchAsJsonAsync("/api/v1/auth/me", new { email = mail })).EnsureSuccessStatusCode();

        var (_, second) = await NewTeacherAsync("Frau Zweit", "5656");
        var res = await second.PatchAsJsonAsync("/api/v1/auth/me", new { email = mail });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("conflict", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task LeererName_WirdAbgewiesen()
    {
        var (_, teacher) = await NewTeacherAsync("Frau Leer", "6767");
        var res = await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { name = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>
    /// A child may not change its own PIN – it is the access the father grants. Otherwise the child
    /// would have escaped supervision, and via an endpoint called "my account" no less.
    /// </summary>
    [Fact]
    public async Task Kind_DarfSichNichtSelbstVerwalten()
    {
        var child = await TestApi.ChildAsync(_factory);
        var res = await child.PatchAsJsonAsync("/api/v1/auth/me", new { pin = "0001" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("forbidden", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Vater_KannSichEbenfallsSelbstVerwalten()
    {
        // The path applies to both kinds of adult - not only as a stopgap for the teacher.
        var father = await TestApi.AdultAsync(_factory);
        var res = await father.PatchAsJsonAsync("/api/v1/auth/me", new { name = "Papa" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Papa", body.GetProperty("name").GetString());
        Assert.Equal("Supervisor", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task OhneToken_KeinZugriff()
    {
        var res = await _factory.CreateClient().PatchAsJsonAsync("/api/v1/auth/me", new { name = "Niemand" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
