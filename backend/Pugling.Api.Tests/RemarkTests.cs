using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Test remarks (<c>api/v1/remarks</c>): capturing with a context snapshot, the back channel
/// (answer from Claude Code) and above all the <b>visibility separation</b> - a student may see neither
/// their supervisor's notes nor their answers, which carry code internals.
/// </summary>
public class RemarkTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private const string Url = "/api/v1/remarks";

    private static async Task<int> CreateAsync(HttpClient client, string text, object? context = null, object? extra = null)
    {
        var body = new Dictionary<string, object?> { ["text"] = text };
        if (context is not null) body["context"] = context;
        if (extra is not null)
            foreach (var p in extra.GetType().GetProperties()) body[p.Name] = p.GetValue(extra);

        var res = await client.PostAsJsonAsync(Url, body);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await TestApi.IdAsync(res);
    }

    [Fact]
    public async Task Erfassen_LiefertId_UndSpeichertKontext()
    {
        var father = await TestApi.AdultAsync(_factory);

        var res = await father.PostAsJsonAsync(Url, new
        {
            text = "  E-Mail-Adresse lässt sich nirgends ändern  ",
            category = "Question",
            context = new
            {
                route = "/vater/profil",
                appArea = "vater",
                childId = 1,
                contextJson = """{"filter":"none"}""",
                recentErrorsJson = """[{"method":"GET","path":"/api/v1/auth/me","status":200}]""",
            },
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();

        // The id is visible in the domain - it is the key for "answer question 123".
        Assert.True(dto.GetProperty("id").GetInt32() > 0);
        Assert.Equal("E-Mail-Adresse lässt sich nirgends ändern", dto.GetProperty("text").GetString());
        Assert.Equal("Question", dto.GetProperty("category").GetString());
        Assert.Equal("Open", dto.GetProperty("status").GetString());
        Assert.Equal("Supervisor", dto.GetProperty("authorRole").GetString());
        Assert.True(dto.GetProperty("isOwn").GetBoolean());

        var ctx = dto.GetProperty("context");
        Assert.Equal("/vater/profil", ctx.GetProperty("route").GetString());
        Assert.Equal("vater", ctx.GetProperty("appArea").GetString());
        Assert.Equal(1, ctx.GetProperty("childId").GetInt32());
        Assert.Contains("filter", ctx.GetProperty("contextJson").GetString());
        Assert.Contains("/api/v1/auth/me", ctx.GetProperty("recentErrorsJson").GetString());
    }

    [Fact]
    public async Task OhneKategorie_BleibtUnspecified()
    {
        var father = await TestApi.AdultAsync(_factory);
        var res = await father.PostAsJsonAsync(Url, new { text = "Nur schnell notiert" });
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();

        // Categorizing while capturing costs more than it yields - the skill fills that in later.
        Assert.Equal("Unspecified", dto.GetProperty("category").GetString());
    }

    [Fact]
    public async Task LeererText_IstValidierungsfehler()
    {
        var father = await TestApi.AdultAsync(_factory);
        var res = await father.PostAsJsonAsync(Url, new { text = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OhneToken_IstUnauthorized()
    {
        var anon = _factory.CreateClient();
        var res = await anon.PostAsJsonAsync(Url, new { text = "anonym" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Sohn_SiehtEigene_AberNichtDieDesVaters()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var supervisorId = await CreateAsync(father, "Interne Notiz mit Codebezug");
        var childId = await CreateAsync(child, "Die Karte lädt langsam");

        // The child sees only its own remark - the supervisor's answers carry file/line references that have
        // no business in a child's app.
        var list = await child.GetFromJsonAsync<JsonElement>(Url);
        var ids = list.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(childId, ids);
        Assert.DoesNotContain(supervisorId, ids);

        // Direct access is closed too - and as a 404, not a 403 (no existence leak).
        var direct = await child.GetAsync($"{Url}/{supervisorId}");
        Assert.Equal(HttpStatusCode.NotFound, direct.StatusCode);
        var problem = await direct.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("remark_not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Vater_SiehtDieAnmerkungSeinesKindes()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var childRemark = await CreateAsync(child, "Der Shop-Kauf fühlt sich komisch an");

        var seen = await father.GetFromJsonAsync<JsonElement>($"{Url}/{childRemark}");
        Assert.Equal(childRemark, seen.GetProperty("id").GetInt32());
        Assert.Equal("Student", seen.GetProperty("authorRole").GetString());
        // Someone else's remark: visible, but not "own" - that is how the widget hides it.
        Assert.False(seen.GetProperty("isOwn").GetBoolean());
    }

    [Fact]
    public async Task FremderSupervisor_TauchtNichtInDerVorgabesichtAuf()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        // Adult 2 is the seeded teacher - they supervise no child.
        var teacher = await TestApi.AdultAsync(_factory, id: 2, pin: "9999");

        var fatherRemark = await CreateAsync(father, "Nur für Papa");
        var childRemark = await CreateAsync(child, "Nur für den Sohn");

        // The **default** stays narrow: own plus supervised accounts. The widget's list hangs on it.
        var list = await teacher.GetFromJsonAsync<JsonElement>(Url);
        var ids = list.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.DoesNotContain(fatherRemark, ids);
        Assert.DoesNotContain(childRemark, ids);

        // Targeted access to one id, by contrast, is **open** with `GlobalRead` switched on - that is exactly
        // what the switch is for: the skill answers remarks from every test account.
        (await teacher.GetAsync($"{Url}/{fatherRemark}")).EnsureSuccessStatusCode();

        // Without the switch the old world applies: invisible, and as a 404 instead of a 403 (no existence leak).
        var narrow = await FatherWithoutGlobalReadAsync(id: 2, pin: "9999");
        Assert.Equal(HttpStatusCode.NotFound, (await narrow.GetAsync($"{Url}/{fatherRemark}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await narrow.GetAsync($"{Url}/{childRemark}")).StatusCode);
    }

    [Fact]
    public async Task MineFilter_BlendetFremdeAus()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var own = await CreateAsync(father, "Eigene Beobachtung");
        var childRemark = await CreateAsync(child, "Beobachtung des Kindes");

        // Without a filter the supervisor sees both …
        var all = await father.GetFromJsonAsync<JsonElement>(Url);
        var allIds = all.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(own, allIds);
        Assert.Contains(childRemark, allIds);

        // … with mine=true only their own. That is the query behind the widget's list.
        var mine = await father.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        var mineIds = mine.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(own, mineIds);
        Assert.DoesNotContain(childRemark, mineIds);
    }

    [Fact]
    public async Task Antwort_BleibtAuchBeiZurueckgestellt_Erhalten()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Wie ändere ich meine E-Mail?");

        var res = await father.PatchAsJsonAsync($"{Url}/{id}", new
        {
            answer = "Geht über PATCH supervisor/adults/{id}; im Vater-Web fehlt das Formular (VaterProfil.tsx).",
            answeredBy = "claude-code",
            status = "Planned",
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Planned", dto.GetProperty("status").GetString());
        Assert.Contains("VaterProfil.tsx", dto.GetProperty("answer").GetString());
        Assert.Equal("claude-code", dto.GetProperty("answeredBy").GetString());
        Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("answeredAt").ValueKind);

        // The core: deferred does not mean "discarded" - the analysis stays as groundwork.
        var again = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Contains("VaterProfil.tsx", again.GetProperty("answer").GetString());
    }

    [Fact]
    public async Task Patch_NullLaesstWerteStehen_ClearLeertSie()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Ursprungstext", new { route = "/vater/shop", appArea = "vater", childId = 1 });
        await father.PatchAsJsonAsync($"{Url}/{id}", new { answer = "Erste Antwort", answeredBy = "claude-code" });

        // Change the status only: everything else is "not specified" and has to stay.
        var patched = await father.PatchAsJsonAsync($"{Url}/{id}", new { status = "Done" });
        var dto = await patched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Done", dto.GetProperty("status").GetString());
        Assert.Equal("Ursprungstext", dto.GetProperty("text").GetString());
        Assert.Equal("Erste Antwort", dto.GetProperty("answer").GetString());
        Assert.Equal(1, dto.GetProperty("context").GetProperty("childId").GetInt32());

        // Clearing happens only through the explicit switches.
        var cleared = await father.PatchAsJsonAsync($"{Url}/{id}", new { clearAnswer = true, clearChild = true });
        var after = await cleared.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, after.GetProperty("answer").ValueKind);
        Assert.Equal(JsonValueKind.Null, after.GetProperty("answeredAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, after.GetProperty("answeredBy").ValueKind);
        Assert.Equal(JsonValueKind.Null, after.GetProperty("context").GetProperty("childId").ValueKind);
    }

    [Fact]
    public async Task Patch_LeererText_IstValidierungsfehler()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Bleibt so");

        var res = await father.PatchAsJsonAsync($"{Url}/{id}", new { text = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var unchanged = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Bleibt so", unchanged.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Folgeanmerkung_VerweistAufDenVorgaenger()
    {
        var father = await TestApi.AdultAsync(_factory);
        var parent = await CreateAsync(father, "Warum gibt es kein E-Mail-Formular?");

        var res = await father.PostAsJsonAsync(Url, new
        {
            text = "Aufgabe: E-Mail-Formular im Vater-Profil ergänzen",
            category = "Ui",
            parentRemarkId = parent,
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(parent, dto.GetProperty("parentRemarkId").GetInt32());
    }

    [Fact]
    public async Task Folgeanmerkung_AufFremdenVorgaenger_WirdAbgelehnt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var teacher = await TestApi.AdultAsync(_factory, id: 2, pin: "9999");
        var foreign = await CreateAsync(father, "Fremde Anmerkung");

        // Otherwise the reference would allow inferring the existence of other people's entries.
        var res = await teacher.PostAsJsonAsync(Url, new { text = "Angehängt", parentRemarkId = foreign });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_reference", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Loeschen_EntferntNurEigenSichtbare()
    {
        var father = await TestApi.AdultAsync(_factory);
        var teacher = await TestApi.AdultAsync(_factory, id: 2, pin: "9999");
        var id = await CreateAsync(father, "Zum Löschen");

        Assert.Equal(HttpStatusCode.NotFound, (await teacher.DeleteAsync($"{Url}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{Url}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"{Url}/{id}")).StatusCode);
    }

    [Fact]
    public async Task UngueltigerKontext_VerhindertDasErfassenNicht()
    {
        var father = await TestApi.AdultAsync(_factory);

        // The widget sends context ids automatically - including ones read from the URL. If one points
        // nowhere (a deleted child, a typo in `/vater/kind/999`), that must NOT destroy the text: the
        // observation is the value, the reference is decoration.
        var res = await father.PostAsJsonAsync(Url, new
        {
            text = "Beobachtung mit veralteten Bezügen",
            context = new
            {
                route = "/vater/kind/999",
                appArea = "vater",
                childId = 999_999,
                exerciseId = 999_999,
                studyPlanId = 999_999,
                planPositionId = 999_999,
            },
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Beobachtung mit veralteten Bezügen", dto.GetProperty("text").GetString());

        // The dead references are dropped silently - route and text are preserved.
        var ctx = dto.GetProperty("context");
        Assert.Equal("/vater/kind/999", ctx.GetProperty("route").GetString());
        Assert.Equal(JsonValueKind.Null, ctx.GetProperty("childId").ValueKind);
        Assert.Equal(JsonValueKind.Null, ctx.GetProperty("exerciseId").ValueKind);
        Assert.Equal(JsonValueKind.Null, ctx.GetProperty("studyPlanId").ValueKind);
        Assert.Equal(JsonValueKind.Null, ctx.GetProperty("planPositionId").ValueKind);
    }

    [Fact]
    public async Task GueltigerKontext_BleibtErhalten()
    {
        var father = await TestApi.AdultAsync(_factory);
        // The counter-check to the test above: the validation must not throw away valid references with them.
        var id = await CreateAsync(father, "Mit echtem Kind-Bezug", new { route = "/vater/kind/1", appArea = "vater", childId = 1 });

        var dto = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal(1, dto.GetProperty("context").GetProperty("childId").GetInt32());
    }

    [Fact]
    public async Task Antwort_KorrigierenLaesstDenUrheberStehen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Frage mit zweistufiger Antwort");
        await father.PatchAsJsonAsync($"{Url}/{id}", new { answer = "Erste Fassung", answeredBy = "claude-code" });

        // Only improve the wording: `answeredBy` is "not specified", not "clear" - otherwise the export would
        // suddenly say "(unknown)".
        var res = await father.PatchAsJsonAsync($"{Url}/{id}", new { answer = "Zweite, genauere Fassung" });
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Zweite, genauere Fassung", dto.GetProperty("answer").GetString());
        Assert.Equal("claude-code", dto.GetProperty("answeredBy").GetString());
    }

    [Fact]
    public async Task FremderKontextBezug_WirdVerworfen_UndVerraetNichts()
    {
        var teacher = await TestApi.AdultAsync(_factory, id: 2, pin: "9999");

        // Child 1 exists but belongs to adult 1. If the id came back, the answer would be an oracle for which
        // ids exist - other people's child/plan ids could be tried out.
        var res = await teacher.PostAsJsonAsync(Url, new
        {
            text = "Notiz mit fremdem Kind-Bezug",
            context = new { route = "/vater/kind/1", appArea = "vater", childId = 1 },
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("context").GetProperty("childId").ValueKind);
    }

    [Fact]
    public async Task Sohn_SiehtDieAntwortAufSeineEigeneAnmerkungNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        var id = await CreateAsync(child, "Die Karte laedt langsam");

        await father.PatchAsJsonAsync($"{Url}/{id}", new
        {
            answer = "Ursache in SohnPractice.tsx:142 – Bild wird synchron geladen.",
            answeredBy = "claude-code",
        });

        // The supervisor sees the answer …
        var seenByFather = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Contains("SohnPractice.tsx", seenByFather.GetProperty("answer").GetString());

        // … the child does not: answers carry file/line references. Exactly the rationale that also makes the
        // export supervisor-only - without this filter the two would contradict each other.
        var seenByChild = await child.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal(JsonValueKind.Null, seenByChild.GetProperty("answer").ValueKind);
        Assert.Equal(JsonValueKind.Null, seenByChild.GetProperty("answeredBy").ValueKind);
        // The own remark itself stays visible - only the answer is filtered.
        Assert.Equal("Die Karte laedt langsam", seenByChild.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Export_LiefertMarkdownMitKontextUndAntwort()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Export-Beobachtung zum Shop", new
        {
            route = "/vater/shop",
            appArea = "vater",
            childId = 1,
            recentErrorsJson = """[{"kind":"http","method":"POST","path":"/api/v1/supervisor/shop","status":409,"code":"conflict"}]""",
        });
        await father.PatchAsJsonAsync($"{Url}/{id}", new
        {
            answer = "Reproduziert in ShopService.cs:120.",
            answeredBy = "claude-code",
            status = "Planned",
        });

        var res = await father.GetAsync($"{Url}/export");
        res.EnsureSuccessStatusCode();
        Assert.Equal("text/markdown", res.Content.Headers.ContentType?.MediaType);

        var md = await res.Content.ReadAsStringAsync();
        Assert.Contains("# Anmerkungen – Export", md);
        Assert.Contains($"## #{id}", md);
        Assert.Contains("eingeplant", md);
        Assert.Contains("Export-Beobachtung zum Shop", md);
        Assert.Contains("`/vater/shop`", md);
        Assert.Contains("Kind 1", md);
        // The error buffer travels along raw - the backend deliberately never interprets it in domain terms.
        Assert.Contains("\"code\":\"conflict\"", md);
        // The answer is half the value of the export: a deferred case carries its analysis with it.
        Assert.Contains("Reproduziert in ShopService.cs:120.", md);
        Assert.Contains("claude-code", md);
    }

    [Fact]
    public async Task Export_FiltertNachStatus()
    {
        var father = await TestApi.AdultAsync(_factory);
        var offen = await CreateAsync(father, "Bleibt offen");
        var erledigt = await CreateAsync(father, "Ist erledigt");
        await father.PatchAsJsonAsync($"{Url}/{erledigt}", new { status = "Done" });

        var md = await (await father.GetAsync($"{Url}/export?status=Open")).Content.ReadAsStringAsync();
        Assert.Contains($"## #{offen}", md);
        Assert.DoesNotContain($"## #{erledigt}", md);
        Assert.Contains("Filter: status=Open", md);
    }

    [Fact]
    public async Task Export_UeberlebtBacktickImKontext()
    {
        var father = await TestApi.AdultAsync(_factory);
        // The buffer comes from the frontend and can be filled freely through the API. An embedded ``` would
        // close a naive code block early and tear the document apart from there on.
        var id = await CreateAsync(father, "Mit Zaun im Kontext", new
        {
            route = "/vater",
            appArea = "vater",
            contextJson = """{"note":"``` fence ``` inside"}""",
        });

        var md = await (await father.GetAsync($"{Url}/export")).Content.ReadAsStringAsync();

        // The fence must be longer than the longest backtick run in the content (CommonMark).
        Assert.Contains("````json", md);
        Assert.Contains("``` fence ```", md);
        // And the structure that follows still stands: the entry is rendered completely.
        Assert.Contains($"## #{id}", md);
        Assert.Contains("Mit Zaun im Kontext", md);
    }

    [Fact]
    public async Task Export_IstFuerDenSohnGesperrt()
    {
        var child = await TestApi.ChildAsync(_factory);
        // Answers carry file/line references - code internals do not belong in a child's app.
        var res = await child.GetAsync($"{Url}/export");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Liste_FiltertUndPagt()
    {
        var father = await TestApi.AdultAsync(_factory);
        await CreateAsync(father, "Bug A", extra: new { category = "Bug" });
        await CreateAsync(father, "Bug B", extra: new { category = "Bug" });
        var idea = await CreateAsync(father, "Idee C", extra: new { category = "Idea" });

        var bugs = await father.GetAsync($"{Url}?mine=true&category=Bug");
        bugs.EnsureSuccessStatusCode();
        var bugList = await bugs.Content.ReadFromJsonAsync<JsonElement>();
        Assert.All(bugList.EnumerateArray(), r => Assert.Equal("Bug", r.GetProperty("category").GetString()));
        Assert.DoesNotContain(idea, bugList.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));

        // Paging reports the total in the header, before the body arrives.
        var paged = await father.GetAsync($"{Url}?mine=true&take=1");
        paged.EnsureSuccessStatusCode();
        Assert.True(paged.Headers.TryGetValues("X-Total-Count", out var totals));
        Assert.True(int.Parse(totals.First()) >= 3);
        var pageList = await paged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, pageList.GetArrayLength());
    }

    // ── History ───────────────────────────────────────────────────────────────────────────────────────

    private static async Task<JsonElement> CommentAsync(HttpClient client, int remarkId, string body,
        string? author = null, string? label = null)
    {
        var dto = new Dictionary<string, object?> { ["body"] = body };
        if (author is not null) dto["author"] = author;
        if (label is not null) dto["authorLabel"] = label;
        var res = await client.PostAsJsonAsync($"{Url}/{remarkId}/comments", dto);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Verlauf_IstChronologisch_UndZaehltAnDerAnmerkung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Der Löschen-Knopf ist nicht zu sehen");

        await CommentAsync(father, id, "Gemessen: die Tabelle braucht 868px.", author: "Assistant", label: "claude-code");
        await CommentAsync(father, id, "Passt, danke.");

        var thread = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}/comments");
        var bodies = thread.EnumerateArray().Select(c => c.GetProperty("body").GetString()).ToList();
        // Oldest first: a case reads chronologically, unlike the list of remarks.
        Assert.Equal(["Gemessen: die Tabelle braucht 868px.", "Passt, danke."], bodies);
        Assert.Equal("claude-code", thread[0].GetProperty("authorLabel").GetString());
        Assert.Equal("Assistant", thread[0].GetProperty("author").GetString());
        // Without a label the account's display name steps in - otherwise the export would say "Human".
        Assert.Equal("Papa", thread[1].GetProperty("authorLabel").GetString());
        Assert.Equal("Human", thread[1].GetProperty("author").GetString());

        // The count sits on the remark so that the list can show it without a second load.
        var one = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal(2, one.GetProperty("commentCount").GetInt32());
        var list = await father.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        var row = list.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == id);
        Assert.Equal(2, row.GetProperty("commentCount").GetInt32());
    }

    [Fact]
    public async Task MenschlicherBeitrag_HoltErledigteAnmerkungZurueck_AssistentNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Tags sollten sichtbar sein");

        (await father.PatchAsJsonAsync($"{Url}/{id}", new { status = "Done", answer = "Belegt: keine Tag-Spalte." }))
            .EnsureSuccessStatusCode();

        // Claude reports - that must not reopen the case, otherwise every implementation note would reopen
        // its own remark.
        await CommentAsync(father, id, "Gebaut: Spalte ergänzt.", author: "Assistant");
        var afterAssistant = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Done", afterAssistant.GetProperty("status").GetString());

        // The human follows up - that is the mechanic that puts the case back on the table.
        await CommentAsync(father, id, "Und die Kind-Tags?");
        var afterHuman = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Open", afterHuman.GetProperty("status").GetString());
        // The resolution stays: following up is not a withdrawal of the groundwork.
        Assert.Equal("Belegt: keine Tag-Spalte.", afterHuman.GetProperty("answer").GetString());
    }

    [Fact]
    public async Task MenschlicherBeitrag_LaesstOffeneAnmerkungInRuhe()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Noch offen");

        await CommentAsync(father, id, "Ergänzung: tritt nur im Firefox auf.");

        var after = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Open", after.GetProperty("status").GetString());
    }

    [Fact]
    public async Task EigenenBeitragEntfernen_FremdenNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var teacher = await TestApi.AdultAsync(_factory, id: 2, pin: "9999");
        var id = await CreateAsync(father, "Beitrag zurücknehmen");
        var comment = await CommentAsync(father, id, "Tippfehler-Beitrag");
        var commentId = comment.GetProperty("id").GetInt32();

        // The other supervisor does not see the remark at all.
        Assert.Equal(HttpStatusCode.NotFound,
            (await teacher.DeleteAsync($"{Url}/{id}/comments/{commentId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"{Url}/{id}/comments/{commentId}")).StatusCode);
        // Second attempt: gone is gone.
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.DeleteAsync($"{Url}/{id}/comments/{commentId}")).StatusCode);
    }

    [Fact]
    public async Task Sohn_SiehtDenVerlaufNicht_UndSchreibtKeinen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        var own = await CreateAsync(child, "Die Übung war doof");
        await CommentAsync(father, own, "Notiert.", author: "Assistant");

        // Entries carry the same code internals as answers - the same barrier.
        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync($"{Url}/{own}/comments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await child.PostAsJsonAsync($"{Url}/{own}/comments", new { body = "Warum?" })).StatusCode);

        // Even the count would disclose that the remark has been discussed.
        var mine = await child.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        var row = mine.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == own);
        Assert.Equal(0, row.GetProperty("commentCount").GetInt32());
    }

    [Fact]
    public async Task LeererBeitrag_IstValidierungsfehler()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Mit leerem Beitrag");

        var res = await father.PostAsJsonAsync($"{Url}/{id}/comments", new { body = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ── Reading across accounts (scope=all) ──────────────────────────────────────────────────────────
    //
    // The permission hangs on the switch `Remarks:GlobalRead` (on in development), NOT on a role.
    // Reason: testing constantly creates throwaway accounts, because some bugs only show up in a certain
    // constellation (a fresh adult without exercises reveals what never surfaces with the seeded one).
    // Flagging every such account first would be administration without any return.

    /// <summary>Registers a father and returns a client for it - a throwaway account with no special permissions.</summary>
    private async Task<HttpClient> FreshFatherAsync(string pin)
    {
        var id = await RegisterFatherAsync("Wegwerf-Vater", pin);
        return await TestApi.AdultAsync(_factory, id, pin);
    }

    private async Task<int> RegisterFatherAsync(string name, string pin)
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name, pin });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Registers a father <b>with</b> <c>Adult.IsAdmin</c> and returns its id - to prove that
    /// the role still holds as a break-glass, even when the switch is off.
    /// <para>
    /// Deliberately a <b>separate</b> account and not a seeded one: the factory is shared across the test class,
    /// and <c>Roles.Admin</c> also bypasses the RWX permissions on exercises. If Papa or the teacher were
    /// repurposed here, every other test would lose its assumption that "this account may not do that" -
    /// depending on the execution order. (That is exactly what happened on the first attempt.)
    /// </para>
    /// </summary>
    private async Task<int> RegisterAdminFatherAsync(string pin)
    {
        var id = await RegisterFatherAsync("Admin-Vater", pin);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var father = await db.Adults.FirstAsync(f => f.Id == id);
        father.IsAdmin = true;
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>
    /// Client against the <b>same database</b>, but with <c>GlobalRead</c> switched off - this is how a
    /// production instance behaves. The token is issued here so that the role claims are correct.
    /// </summary>
    private async Task<HttpClient> FatherWithoutGlobalReadAsync(int id = 1, string pin = "0000")
    {
        var narrow = _factory.WithWebHostBuilder(b => b.UseSetting("Remarks:GlobalRead", "false"));
        return await TestApi.AdultAsync(narrow, id, pin);
    }

    [Fact]
    public async Task JedesVaterKonto_LiestMitScopeAll_AlleAnmerkungen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var foreign = await CreateAsync(father, "Beobachtung aus einem anderen Konto");

        // A freshly registered throwaway account: no children, no exercises, NO admin flag.
        var fresh = await FreshFatherAsync("6101");

        // Without the parameter the view stays narrow - otherwise the widget's list would show other people's entries.
        var narrow = await fresh.GetFromJsonAsync<JsonElement>(Url);
        Assert.DoesNotContain(foreign, narrow.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));

        // With scope=all it sees everything - that is the view the follow-up skill needs.
        var all = await fresh.GetFromJsonAsync<JsonElement>($"{Url}?scope=all");
        Assert.Contains(foreign, all.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task FremdeAnmerkung_LaesstSichBeantwortenUndKommentieren()
    {
        var father = await TestApi.AdultAsync(_factory);
        var foreign = await CreateAsync(father, "Frage aus einem anderen Testkonto");
        var fresh = await FreshFatherAsync("6102");

        // A single id needs no scope parameter: that is exactly how the skill redeems "answer 123".
        var one = await fresh.GetFromJsonAsync<JsonElement>($"{Url}/{foreign}");
        Assert.Equal(foreign, one.GetProperty("id").GetInt32());
        // Someone else's, but visible - that is how the widget hides it from its own list.
        Assert.False(one.GetProperty("isOwn").GetBoolean());

        (await fresh.PatchAsJsonAsync($"{Url}/{foreign}",
            new { answer = "Belegt in VaterVocab.tsx:345.", answeredBy = "claude-code", status = "Done" }))
            .EnsureSuccessStatusCode();
        await CommentAsync(fresh, foreign, "Gebaut und geprüft.", author: "Assistant", label: "claude-code");

        // The owner sees the resolution and the history in their own view.
        var owner = await father.GetFromJsonAsync<JsonElement>($"{Url}/{foreign}");
        Assert.Equal("Belegt in VaterVocab.tsx:345.", owner.GetProperty("answer").GetString());
        var thread = await father.GetFromJsonAsync<JsonElement>($"{Url}/{foreign}/comments");
        Assert.Equal("Gebaut und geprüft.", thread[0].GetProperty("body").GetString());
    }

    [Fact]
    public async Task Sohn_LiestNiemalsAlleKonten_AuchNichtMitGlobalRead()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        var foreign = await CreateAsync(father, "Interne Notiz mit Codebezug");

        // The switch opens the view for adults, never for a child: answers and history carry file and line
        // references.
        var res = await child.GetAsync($"{Url}?scope=all");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("remark_scope_forbidden",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await child.GetAsync($"{Url}/{foreign}")).StatusCode);
    }

    [Fact]
    public async Task OhneGlobalRead_IstScopeAll_403_UndDieEngeSichtBleibt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var own = await CreateAsync(father, "Eigene Anmerkung bei abgeschaltetem Schalter");

        var narrow = await FatherWithoutGlobalReadAsync();

        var res = await narrow.GetAsync($"{Url}?scope=all");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("remark_scope_forbidden",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Without the parameter the same instance keeps working normally - the switch does not block the feature.
        var list = await narrow.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        Assert.Contains(own, list.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task OhneGlobalRead_TraegtDieAdminRolle_WeiterhinAlsBreakGlass()
    {
        var father = await TestApi.AdultAsync(_factory);
        var foreign = await CreateAsync(father, "Nur per Break-Glass erreichbar");

        var adminId = await RegisterAdminFatherAsync("6103");
        var adminNarrow = await FatherWithoutGlobalReadAsync(adminId, "6103");

        var all = await adminNarrow.GetFromJsonAsync<JsonElement>($"{Url}?scope=all");
        Assert.Contains(foreign, all.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Export_MitScopeAll_TraegtDenVerlauf_UndOhneGlobalRead_403()
    {
        var father = await TestApi.AdultAsync(_factory);
        var id = await CreateAsync(father, "Export mit Verlauf");
        await CommentAsync(father, id, "Umsetzungsnotiz für den Export.", author: "Assistant", label: "claude-code");

        // The own export carries the history - which is why a snapshot taken today still knows something about
        // yesterday; before, the implementation note overwrote the analysis.
        var own = await father.GetAsync($"{Url}/export");
        own.EnsureSuccessStatusCode();
        var markdown = await own.Content.ReadAsStringAsync();
        Assert.Contains("**Verlauf**", markdown);
        Assert.Contains("> Umsetzungsnotiz für den Export.", markdown);

        // Across accounts: allowed (switch on) and with the account named per entry, so that the repository
        // snapshot still shows whose observation a line is.
        var across = await father.GetAsync($"{Url}/export?scope=all");
        across.EnsureSuccessStatusCode();
        Assert.Contains("Konto", await across.Content.ReadAsStringAsync());

        var narrow = await FatherWithoutGlobalReadAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await narrow.GetAsync($"{Url}/export?scope=all")).StatusCode);
    }
}
