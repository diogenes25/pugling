using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The <b>teacher account</b>: an adult who creates content and supervises no child.
///
/// No new entity type – the three tiers are roles, decoupled from the login. A father account carries
/// Creator <b>and</b> Supervisor, a teacher account only Creator. Everything checked here follows from that,
/// without a special rule for "teacher" existing anywhere: the supervision endpoints turn them away via their
/// existing <c>[Authorize(Roles = Roles.Supervisor)]</c>, and the authoring endpoints let them through.
/// </summary>
public class TeacherAccountTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>Registers a teacher account and returns the response along with a logged-in client.</summary>
    private async Task<(JsonElement account, HttpClient client)> RegisterTeacherAsync(string name, string pin)
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/v1/creator/teacher-accounts",
            new { name, email = (string?)null, pin });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var account = await res.Content.ReadFromJsonAsync<JsonElement>();

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
            new { adultId = account.GetProperty("creatorId").GetInt32(), pin });
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
    /// The line that used to lie: <c>primaryRole</c> collapsed every non-Student role onto
    /// <c>Supervisor</c> – a teacher would have gotten the father UI.
    /// </summary>
    [Fact]
    public async Task Login_MeldetCreatorAlsPrimaereEbene_UndBeimVaterWeiterhinSupervisor()
    {
        var (account, _) = await RegisterTeacherAsync("Herr Fels", "2345");

        var teacherLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
            new { adultId = account.GetProperty("creatorId").GetInt32(), pin = "2345" });
        Assert.Equal("Creator",
            (await teacherLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        // Gegenprobe: der geseedete Vater bleibt Supervisor.
        var fatherLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
            new { adultId = 1, pin = "0000" });
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
    /// The hard boundary: without the supervisor role, the supervision area is closed. Not through a
    /// special teacher check, but through the role attribute that stands there anyway.
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
    /// The <b>reading</b> study plan list is deliberately only <c>[Authorize]</c> – it serves father and
    /// child and separates the roles inline (see CLAUDE.md). A teacher therefore gets <c>200</c>, but an
    /// <b>empty</b> list: filtering goes through <c>SupervisorLinks</c>, and they supervise nobody. That is
    /// exactly the guarantee here – a <c>403</c> would read nicer, but the data question is the more important one.
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
    /// Logging in must not add a role after the fact. <c>auth/adult</c> calls <c>EnsureForFatherAsync</c>,
    /// and if that were to "complete" an existing account, the teacher would become a silent supervisor
    /// after the first login – the separation would have undone itself.
    /// </summary>
    [Fact]
    public async Task WiederholtesAnmelden_MachtDenLehrerNichtZumBetreuer()
    {
        var (account, _) = await RegisterTeacherAsync("Frau Dauer", "6789");
        var creatorId = account.GetProperty("creatorId").GetInt32();

        for (var i = 0; i < 3; i++)
        {
            var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult",
                new { adultId = creatorId, pin = "6789" });
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
