using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Das <b>Lehrer-Konto</b>: ein Erwachsener, der Inhalte erstellt und kein Kind betreut.
///
/// Kein neuer Entitätstyp – die drei Ebenen sind Rollen, entkoppelt vom Login. Ein Vater-Konto trägt
/// Creator <b>und</b> Supervisor, ein Lehrer-Konto nur Creator. Alles hier Geprüfte folgt daraus, ohne dass
/// irgendwo eine Sonderregel für „Lehrer" steht: die Betreuungs-Endpunkte weisen ihn über ihr vorhandenes
/// <c>[Authorize(Roles = Roles.Supervisor)]</c> ab, und die Autoren-Endpunkte lassen ihn durch.
/// </summary>
public class TeacherAccountTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>Registriert ein Lehrer-Konto und liefert die Antwort samt eines angemeldeten Clients.</summary>
    private async Task<(JsonElement account, HttpClient client)> RegisterTeacherAsync(string name, string pin)
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/v1/creator/teacher-accounts",
            new { name, email = (string?)null, pin });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var account = await res.Content.ReadFromJsonAsync<JsonElement>();

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/father",
            new { fatherId = account.GetProperty("creatorId").GetInt32(), pin });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return (account, client);
    }

    [Fact]
    public async Task LehrerKonto_TraegtNurDieCreatorRolle()
    {
        var (account, client) = await RegisterTeacherAsync("Frau Berg", "1234");

        var roles = account.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Equal(["Creator"], roles);

        // Und das Token sagt dasselbe – die Rollen im JWT entstehen aus den Profilen, nicht aus einer Annahme.
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var tokenRoles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Equal(["Creator"], tokenRoles);
        Assert.Equal("Creator", me.GetProperty("role").GetString());
    }

    /// <summary>
    /// Die Zeile, die vorher log: <c>primaryRole</c> klappte jede Nicht-Student-Rolle auf
    /// <c>Supervisor</c> zusammen – ein Lehrer hätte die Vater-Oberfläche bekommen.
    /// </summary>
    [Fact]
    public async Task Login_MeldetCreatorAlsPrimaereEbene_UndBeimVaterWeiterhinSupervisor()
    {
        var (account, _) = await RegisterTeacherAsync("Herr Fels", "2345");

        var teacherLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/father",
            new { fatherId = account.GetProperty("creatorId").GetInt32(), pin = "2345" });
        Assert.Equal("Creator",
            (await teacherLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        // Gegenprobe: der geseedete Vater bleibt Supervisor.
        var fatherLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/father",
            new { fatherId = 1, pin = "0000" });
        Assert.Equal("Supervisor",
            (await fatherLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        // Auch der konto-zentrische Login urteilt gleich.
        var byAccount = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { accountId = account.GetProperty("accountId").GetInt32(), pin = "2345" });
        Assert.Equal("Creator",
            (await byAccount.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());
    }

    [Fact]
    public async Task Lehrer_DarfInhalteAnlegenUndBesitzenWieBisher()
    {
        var (_, teacher) = await RegisterTeacherAsync("Frau Stein", "3456");

        var subjectId = await TestApi.IdAsync(await teacher.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Lehrer-Fach {Guid.NewGuid():N}" }));
        var chapterId = await TestApi.IdAsync(await teacher.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await teacher.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new
            {
                title = "Lehrer-Material",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "meadow", back = "Wiese" } },
                },
            }));

        // Autorschaft und Owner-Recht hängen an derselben Id – darum funktionieren Rechtevergabe,
        // Freigabe und Rücknahme unverändert.
        var detail = await teacher.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        Assert.True(detail.GetProperty("isOwner").GetBoolean());

        var withdraw = await teacher.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false });
        Assert.Equal(HttpStatusCode.OK, withdraw.StatusCode);
    }

    /// <summary>
    /// Die harte Grenze: ohne Supervisor-Rolle ist der Betreuungs-Bereich zu. Nicht durch eine
    /// Lehrer-Sonderprüfung, sondern durch das Rollen-Attribut, das dort ohnehin steht.
    /// </summary>
    [Theory]
    [InlineData("/api/v1/supervisor/children")]
    [InlineData("/api/v1/supervisor/class-tests")]
    public async Task Lehrer_KommtNichtInDenBetreuungsBereich(string path)
    {
        var (_, teacher) = await RegisterTeacherAsync($"Grenzfall {Guid.NewGuid():N}"[..20], "4567");

        var res = await teacher.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    /// <summary>
    /// Die <b>lesende</b> Lehrplan-Liste ist bewusst nur <c>[Authorize]</c> – sie dient Vater und Sohn und
    /// trennt die Rollen inline (siehe CLAUDE.md). Ein Lehrer bekommt darum <c>200</c>, aber eine
    /// <b>leere</b> Liste: gefiltert wird über <c>SupervisorLinks</c>, und er betreut niemanden. Genau das
    /// ist hier die Zusage – ein <c>403</c> wäre schöner zu lesen, die Datenfrage ist aber die wichtigere.
    /// </summary>
    [Fact]
    public async Task Lehrer_SiehtInDerLehrplanListeNichts()
    {
        var (_, teacher) = await RegisterTeacherAsync("Frau Leer", "4321");

        var res = await teacher.GetAsync("/api/v1/supervisor/study-plans");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Empty((await res.Content.ReadFromJsonAsync<List<JsonElement>>())!);
    }

    [Fact]
    public async Task Lehrer_KannKeinKindAnlegenUndKeinenPlanBauen()
    {
        var (_, teacher) = await RegisterTeacherAsync("Frau Grenz", "5678");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await teacher.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Fremdkind", pin = "9999" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await teacher.PostAsJsonAsync("/api/v1/supervisor/study-plans",
                new { childId = 1, title = "Nicht meine Sache", durationDays = 10 })).StatusCode);
    }

    /// <summary>
    /// Das Anmelden darf keine Rolle nachreichen. <c>auth/father</c> ruft <c>EnsureForFatherAsync</c>, und
    /// würde das ein bestehendes Konto „vervollständigen", wäre der Lehrer nach dem ersten Login stiller
    /// Betreuer – die Trennung hätte sich selbst aufgehoben.
    /// </summary>
    [Fact]
    public async Task WiederholtesAnmelden_MachtDenLehrerNichtZumBetreuer()
    {
        var (account, _) = await RegisterTeacherAsync("Frau Dauer", "6789");
        var creatorId = account.GetProperty("creatorId").GetInt32();

        for (var i = 0; i < 3; i++)
        {
            var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/father",
                new { fatherId = creatorId, pin = "6789" });
            login.EnsureSuccessStatusCode();
            Assert.Equal("Creator",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());
        }

        var again = await _factory.CreateClient().GetAsync($"/api/v1/creator/teacher-accounts/{creatorId}");
        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);   // ohne Token kein Einblick
    }

    [Fact]
    public async Task FremdesLehrerKonto_IstNichtAbfragbar()
    {
        var (a, _) = await RegisterTeacherAsync("Frau Eins", "7891");
        var (_, clientB) = await RegisterTeacherAsync("Frau Zwei", "8912");

        var res = await clientB.GetAsync($"/api/v1/creator/teacher-accounts/{a.GetProperty("creatorId").GetInt32()}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task RegistrierungOhneNamen_WirdAbgewiesen()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/v1/creator/teacher-accounts",
            new { name = "  ", email = (string?)null, pin = "1111" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
