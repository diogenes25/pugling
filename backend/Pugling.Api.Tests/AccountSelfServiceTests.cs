using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Selbstverwaltung des eigenen Kontos (<c>PATCH auth/me</c>): Name, E-Mail, PIN.
///
/// Der Anlass war das <b>Lehrer-Konto</b> – ihm fehlt die Supervisor-Rolle, und damit war
/// <c>supervisor/adults/{id}</c> für es verschlossen: es konnte seine eigene PIN nicht ändern. Weil das
/// Konto zu keiner Ebene gehört (derselbe Mensch bedient es aus jeder Rolle), liegt der Weg bei
/// <c>auth/…</c> und gilt für <b>beide</b> Erwachsenen-Arten.
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

        // Der Name hängt an ZWEI Stellen: am Konto (Login) und an der fachlichen Zeile, wo er als Autor
        // erscheint. Die Gegenprobe läuft über einen Creator-Endpunkt, nicht über `auth/me`.
        var account = await teacher.GetFromJsonAsync<JsonElement>($"/api/v1/creator/teacher-accounts/{creatorId}");
        Assert.Equal("Frau Neu", account.GetProperty("name").GetString());
        Assert.Equal(mail, account.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Lehrer_AendertPin_UndMeldetSichDamitAn()
    {
        var (creatorId, teacher) = await NewTeacherAsync("Frau Schlüssel", "2323");

        (await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { pin = "9090" })).EnsureSuccessStatusCode();

        // Die alte PIN gilt nicht mehr …
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
                new { adultId = creatorId, pin = "2323" })).StatusCode);
        // … die neue schon, und zwar auf beiden Login-Wegen (der Hash wird aufs Konto gespiegelt).
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
    /// Ohne den Schalter meldete ein Formular mit geleertem Feld „gespeichert", und die alte Adresse stünde
    /// weiter da – <c>null</c> heißt in dieser API „nicht angegeben", nicht „leeren".
    /// </summary>
    [Fact]
    public async Task EMailLeeren_BrauchtDenSchalter()
    {
        var (creatorId, teacher) = await NewTeacherAsync("Frau Post", "3434");
        var mail = $"weg-{Guid.NewGuid():N}@schule.example";
        (await teacher.PatchAsJsonAsync("/api/v1/auth/me", new { email = mail })).EnsureSuccessStatusCode();

        // `null` lässt die Adresse stehen.
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
    /// Ein Kind darf seine PIN nicht selbst umstellen – sie ist der Zugang, den der Vater vergibt. Sonst
    /// hätte sich das Kind der Aufsicht entzogen, und zwar über einen Endpunkt, der „mein Konto" heißt.
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
        // Der Weg gilt für beide Erwachsenen-Arten – nicht nur als Lückenfüller für den Lehrer.
        var father = await TestApi.FatherAsync(_factory);
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
