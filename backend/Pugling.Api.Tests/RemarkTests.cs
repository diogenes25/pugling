using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Test-Anmerkungen (<c>api/v1/remarks</c>): Erfassen samt Kontext-Schnappschuss, der Rückkanal
/// (Antwort aus Claude Code) und vor allem die <b>Sichtbarkeitstrennung</b> – ein Student darf weder
/// die Notizen seines Supervisors noch deren Antworten sehen, die tragen Code-Interna.
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
        var father = await TestApi.FatherAsync(_factory);

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

        // Die Id ist fachlich sichtbar – sie ist der Schlüssel für „Beantworte die Frage 123".
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
        var father = await TestApi.FatherAsync(_factory);
        var res = await father.PostAsJsonAsync(Url, new { text = "Nur schnell notiert" });
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();

        // Beim Erfassen zu kategorisieren kostet mehr, als es bringt – das zieht der Skill später nach.
        Assert.Equal("Unspecified", dto.GetProperty("category").GetString());
    }

    [Fact]
    public async Task LeererText_IstValidierungsfehler()
    {
        var father = await TestApi.FatherAsync(_factory);
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
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var supervisorId = await CreateAsync(father, "Interne Notiz mit Codebezug");
        var childId = await CreateAsync(child, "Die Karte lädt langsam");

        // Der Sohn sieht ausschließlich seine eigene Anmerkung – Antworten des Vaters tragen
        // Datei-/Zeilenverweise, die in einer Kinder-App nichts zu suchen haben.
        var list = await child.GetFromJsonAsync<JsonElement>(Url);
        var ids = list.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(childId, ids);
        Assert.DoesNotContain(supervisorId, ids);

        // Auch der direkte Zugriff ist zu – und zwar als 404, nicht als 403 (kein Existenz-Leak).
        var direct = await child.GetAsync($"{Url}/{supervisorId}");
        Assert.Equal(HttpStatusCode.NotFound, direct.StatusCode);
        var problem = await direct.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("remark_not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Vater_SiehtDieAnmerkungSeinesKindes()
    {
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var childRemark = await CreateAsync(child, "Der Shop-Kauf fühlt sich komisch an");

        var seen = await father.GetFromJsonAsync<JsonElement>($"{Url}/{childRemark}");
        Assert.Equal(childRemark, seen.GetProperty("id").GetInt32());
        Assert.Equal("Student", seen.GetProperty("authorRole").GetString());
        // Fremde Anmerkung: sichtbar, aber nicht „eigen" – das Widget blendet sie damit aus.
        Assert.False(seen.GetProperty("isOwn").GetBoolean());
    }

    [Fact]
    public async Task FremderSupervisor_TauchtNichtInDerVorgabesichtAuf()
    {
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        // Vater 2 ist der geseedete Lehrer – er betreut kein Kind.
        var teacher = await TestApi.FatherAsync(_factory, id: 2, pin: "9999");

        var fatherRemark = await CreateAsync(father, "Nur für Papa");
        var childRemark = await CreateAsync(child, "Nur für den Sohn");

        // Die **Vorgabe** bleibt eng: eigene plus betreute Konten. Daran hängt die Liste im Widget.
        var list = await teacher.GetFromJsonAsync<JsonElement>(Url);
        var ids = list.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.DoesNotContain(fatherRemark, ids);
        Assert.DoesNotContain(childRemark, ids);

        // Der gezielte Zugriff auf eine Id ist mit eingeschaltetem `GlobalRead` dagegen **offen** – genau
        // dafür ist der Schalter da: Der Skill beantwortet Anmerkungen aus jedem Testkonto.
        (await teacher.GetAsync($"{Url}/{fatherRemark}")).EnsureSuccessStatusCode();

        // Ohne den Schalter gilt die alte Welt: unsichtbar, und zwar als 404 statt 403 (kein Existenz-Leak).
        var narrow = await FatherWithoutGlobalReadAsync(id: 2, pin: "9999");
        Assert.Equal(HttpStatusCode.NotFound, (await narrow.GetAsync($"{Url}/{fatherRemark}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await narrow.GetAsync($"{Url}/{childRemark}")).StatusCode);
    }

    [Fact]
    public async Task MineFilter_BlendetFremdeAus()
    {
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);

        var own = await CreateAsync(father, "Eigene Beobachtung");
        var childRemark = await CreateAsync(child, "Beobachtung des Kindes");

        // Ohne Filter sieht der Vater beide …
        var all = await father.GetFromJsonAsync<JsonElement>(Url);
        var allIds = all.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(own, allIds);
        Assert.Contains(childRemark, allIds);

        // … mit mine=true nur die eigenen. Das ist die Abfrage hinter der Liste im Widget.
        var mine = await father.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        var mineIds = mine.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(own, mineIds);
        Assert.DoesNotContain(childRemark, mineIds);
    }

    [Fact]
    public async Task Antwort_BleibtAuchBeiZurueckgestellt_Erhalten()
    {
        var father = await TestApi.FatherAsync(_factory);
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

        // Der Kern: Zurückgestellt heißt nicht „verworfen" – die Analyse bleibt als Vorarbeit stehen.
        var again = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Contains("VaterProfil.tsx", again.GetProperty("answer").GetString());
    }

    [Fact]
    public async Task Patch_NullLaesstWerteStehen_ClearLeertSie()
    {
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Ursprungstext", new { route = "/vater/shop", appArea = "vater", childId = 1 });
        await father.PatchAsJsonAsync($"{Url}/{id}", new { answer = "Erste Antwort", answeredBy = "claude-code" });

        // Nur den Status ändern: alles andere ist „nicht angegeben" und muss stehen bleiben.
        var patched = await father.PatchAsJsonAsync($"{Url}/{id}", new { status = "Done" });
        var dto = await patched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Done", dto.GetProperty("status").GetString());
        Assert.Equal("Ursprungstext", dto.GetProperty("text").GetString());
        Assert.Equal("Erste Antwort", dto.GetProperty("answer").GetString());
        Assert.Equal(1, dto.GetProperty("context").GetProperty("childId").GetInt32());

        // Geleert wird nur über die ausdrücklichen Schalter.
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
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Bleibt so");

        var res = await father.PatchAsJsonAsync($"{Url}/{id}", new { text = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var unchanged = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Bleibt so", unchanged.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Folgeanmerkung_VerweistAufDenVorgaenger()
    {
        var father = await TestApi.FatherAsync(_factory);
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
        var father = await TestApi.FatherAsync(_factory);
        var teacher = await TestApi.FatherAsync(_factory, id: 2, pin: "9999");
        var foreign = await CreateAsync(father, "Fremde Anmerkung");

        // Sonst ließe sich über den Verweis auf die Existenz fremder Einträge schließen.
        var res = await teacher.PostAsJsonAsync(Url, new { text = "Angehängt", parentRemarkId = foreign });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_reference", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Loeschen_EntferntNurEigenSichtbare()
    {
        var father = await TestApi.FatherAsync(_factory);
        var teacher = await TestApi.FatherAsync(_factory, id: 2, pin: "9999");
        var id = await CreateAsync(father, "Zum Löschen");

        Assert.Equal(HttpStatusCode.NotFound, (await teacher.DeleteAsync($"{Url}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{Url}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"{Url}/{id}")).StatusCode);
    }

    [Fact]
    public async Task UngueltigerKontext_VerhindertDasErfassenNicht()
    {
        var father = await TestApi.FatherAsync(_factory);

        // Das Widget schickt Kontext-IDs automatisch mit – auch aus der URL gelesene. Zeigt eine ins
        // Leere (gelöschtes Kind, Tippfehler in `/vater/kind/999`), darf das den Text NICHT vernichten:
        // Die Beobachtung ist der Wert, der Bezug ist Beiwerk.
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

        // Die toten Bezüge sind still verworfen – Route und Text bleiben erhalten.
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
        var father = await TestApi.FatherAsync(_factory);
        // Gegenprobe zum Test oben: Die Prüfung darf gültige Bezüge nicht mit wegwerfen.
        var id = await CreateAsync(father, "Mit echtem Kind-Bezug", new { route = "/vater/kind/1", appArea = "vater", childId = 1 });

        var dto = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal(1, dto.GetProperty("context").GetProperty("childId").GetInt32());
    }

    [Fact]
    public async Task Antwort_KorrigierenLaesstDenUrheberStehen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Frage mit zweistufiger Antwort");
        await father.PatchAsJsonAsync($"{Url}/{id}", new { answer = "Erste Fassung", answeredBy = "claude-code" });

        // Nur den Wortlaut nachbessern: `answeredBy` ist „nicht angegeben", nicht „leeren" –
        // sonst stünde im Export plötzlich „(unbekannt)".
        var res = await father.PatchAsJsonAsync($"{Url}/{id}", new { answer = "Zweite, genauere Fassung" });
        var dto = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Zweite, genauere Fassung", dto.GetProperty("answer").GetString());
        Assert.Equal("claude-code", dto.GetProperty("answeredBy").GetString());
    }

    [Fact]
    public async Task FremderKontextBezug_WirdVerworfen_UndVerraetNichts()
    {
        var teacher = await TestApi.FatherAsync(_factory, id: 2, pin: "9999");

        // Kind 1 existiert, gehört aber Vater 1. Käme die Id zurück, wäre die Antwort ein Auskunftsdienst
        // darüber, welche Ids es gibt – fremde Kind-/Plan-Ids ließen sich durchprobieren.
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
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        var id = await CreateAsync(child, "Die Karte laedt langsam");

        await father.PatchAsJsonAsync($"{Url}/{id}", new
        {
            answer = "Ursache in SohnPractice.tsx:142 – Bild wird synchron geladen.",
            answeredBy = "claude-code",
        });

        // Der Vater sieht die Antwort …
        var seenByFather = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Contains("SohnPractice.tsx", seenByFather.GetProperty("answer").GetString());

        // … das Kind nicht: Antworten tragen Datei-/Zeilenverweise. Genau die Begründung, mit der auch
        // der Export Supervisor-only ist – ohne diesen Filter widerspräche sich beides.
        var seenByChild = await child.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal(JsonValueKind.Null, seenByChild.GetProperty("answer").ValueKind);
        Assert.Equal(JsonValueKind.Null, seenByChild.GetProperty("answeredBy").ValueKind);
        // Die eigene Anmerkung selbst bleibt sichtbar – nur die Antwort ist gefiltert.
        Assert.Equal("Die Karte laedt langsam", seenByChild.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Export_LiefertMarkdownMitKontextUndAntwort()
    {
        var father = await TestApi.FatherAsync(_factory);
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
        // Der Fehlerpuffer geht roh mit – das Backend interpretiert ihn bewusst nirgends fachlich.
        Assert.Contains("\"code\":\"conflict\"", md);
        // Die Antwort ist der halbe Wert des Exports: Ein zurückgestellter Fall trägt seine Analyse mit.
        Assert.Contains("Reproduziert in ShopService.cs:120.", md);
        Assert.Contains("claude-code", md);
    }

    [Fact]
    public async Task Export_FiltertNachStatus()
    {
        var father = await TestApi.FatherAsync(_factory);
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
        var father = await TestApi.FatherAsync(_factory);
        // Der Puffer kommt aus dem Frontend und lässt sich über die API frei befüllen. Ein eingebettetes
        // ``` würde einen naiven Code-Block vorzeitig schließen und das Dokument ab da zerlegen.
        var id = await CreateAsync(father, "Mit Zaun im Kontext", new
        {
            route = "/vater",
            appArea = "vater",
            contextJson = """{"note":"``` fence ``` inside"}""",
        });

        var md = await (await father.GetAsync($"{Url}/export")).Content.ReadAsStringAsync();

        // Der Zaun muss länger sein als die längste Backtick-Folge im Inhalt (CommonMark).
        Assert.Contains("````json", md);
        Assert.Contains("``` fence ```", md);
        // Und die nachfolgende Struktur steht noch: Der Eintrag ist vollständig gerendert.
        Assert.Contains($"## #{id}", md);
        Assert.Contains("Mit Zaun im Kontext", md);
    }

    [Fact]
    public async Task Export_IstFuerDenSohnGesperrt()
    {
        var child = await TestApi.ChildAsync(_factory);
        // Antworten tragen Datei-/Zeilenverweise – Code-Interna gehören nicht in eine Kinder-App.
        var res = await child.GetAsync($"{Url}/export");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Liste_FiltertUndPagt()
    {
        var father = await TestApi.FatherAsync(_factory);
        await CreateAsync(father, "Bug A", extra: new { category = "Bug" });
        await CreateAsync(father, "Bug B", extra: new { category = "Bug" });
        var idea = await CreateAsync(father, "Idee C", extra: new { category = "Idea" });

        var bugs = await father.GetAsync($"{Url}?mine=true&category=Bug");
        bugs.EnsureSuccessStatusCode();
        var bugList = await bugs.Content.ReadFromJsonAsync<JsonElement>();
        Assert.All(bugList.EnumerateArray(), r => Assert.Equal("Bug", r.GetProperty("category").GetString()));
        Assert.DoesNotContain(idea, bugList.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));

        // Paging meldet die Gesamtzahl im Header, bevor der Body kommt.
        var paged = await father.GetAsync($"{Url}?mine=true&take=1");
        paged.EnsureSuccessStatusCode();
        Assert.True(paged.Headers.TryGetValues("X-Total-Count", out var totals));
        Assert.True(int.Parse(totals.First()) >= 3);
        var pageList = await paged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, pageList.GetArrayLength());
    }

    // ── Verlauf ───────────────────────────────────────────────────────────────────────────────────────

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
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Der Löschen-Knopf ist nicht zu sehen");

        await CommentAsync(father, id, "Gemessen: die Tabelle braucht 868px.", author: "Assistant", label: "claude-code");
        await CommentAsync(father, id, "Passt, danke.");

        var thread = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}/comments");
        var bodies = thread.EnumerateArray().Select(c => c.GetProperty("body").GetString()).ToList();
        // Älteste zuerst: Ein Vorgang liest sich chronologisch, anders als die Liste der Anmerkungen.
        Assert.Equal(["Gemessen: die Tabelle braucht 868px.", "Passt, danke."], bodies);
        Assert.Equal("claude-code", thread[0].GetProperty("authorLabel").GetString());
        Assert.Equal("Assistant", thread[0].GetProperty("author").GetString());
        // Ohne Label springt der Anzeigename des Kontos ein – sonst stünde im Export „Human".
        Assert.Equal("Papa", thread[1].GetProperty("authorLabel").GetString());
        Assert.Equal("Human", thread[1].GetProperty("author").GetString());

        // Die Zahl liegt an der Anmerkung, damit die Liste sie ohne Nachladen zeigt.
        var one = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal(2, one.GetProperty("commentCount").GetInt32());
        var list = await father.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        var row = list.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == id);
        Assert.Equal(2, row.GetProperty("commentCount").GetInt32());
    }

    [Fact]
    public async Task MenschlicherBeitrag_HoltErledigteAnmerkungZurueck_AssistentNicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Tags sollten sichtbar sein");

        (await father.PatchAsJsonAsync($"{Url}/{id}", new { status = "Done", answer = "Belegt: keine Tag-Spalte." }))
            .EnsureSuccessStatusCode();

        // Claude berichtet – das darf den Stand nicht aufreißen, sonst öffnete jede Umsetzungsnotiz
        // die eigene Anmerkung wieder.
        await CommentAsync(father, id, "Gebaut: Spalte ergänzt.", author: "Assistant");
        var afterAssistant = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Done", afterAssistant.GetProperty("status").GetString());

        // Der Mensch hakt nach – das ist die Mechanik, die den Vorgang wieder auf den Tisch legt.
        await CommentAsync(father, id, "Und die Kind-Tags?");
        var afterHuman = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Open", afterHuman.GetProperty("status").GetString());
        // Die Auflösung bleibt stehen: Nachhaken ist keine Rücknahme der Vorarbeit.
        Assert.Equal("Belegt: keine Tag-Spalte.", afterHuman.GetProperty("answer").GetString());
    }

    [Fact]
    public async Task MenschlicherBeitrag_LaesstOffeneAnmerkungInRuhe()
    {
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Noch offen");

        await CommentAsync(father, id, "Ergänzung: tritt nur im Firefox auf.");

        var after = await father.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Open", after.GetProperty("status").GetString());
    }

    [Fact]
    public async Task EigenenBeitragEntfernen_FremdenNicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var teacher = await TestApi.FatherAsync(_factory, id: 2, pin: "9999");
        var id = await CreateAsync(father, "Beitrag zurücknehmen");
        var comment = await CommentAsync(father, id, "Tippfehler-Beitrag");
        var commentId = comment.GetProperty("id").GetInt32();

        // Der fremde Supervisor sieht die Anmerkung gar nicht.
        Assert.Equal(HttpStatusCode.NotFound,
            (await teacher.DeleteAsync($"{Url}/{id}/comments/{commentId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"{Url}/{id}/comments/{commentId}")).StatusCode);
        // Zweiter Versuch: weg ist weg.
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.DeleteAsync($"{Url}/{id}/comments/{commentId}")).StatusCode);
    }

    [Fact]
    public async Task Sohn_SiehtDenVerlaufNicht_UndSchreibtKeinen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        var own = await CreateAsync(child, "Die Übung war doof");
        await CommentAsync(father, own, "Notiert.", author: "Assistant");

        // Beiträge tragen dieselben Code-Interna wie Antworten – dieselbe Schranke.
        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync($"{Url}/{own}/comments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await child.PostAsJsonAsync($"{Url}/{own}/comments", new { body = "Warum?" })).StatusCode);

        // Schon die Anzahl wäre eine Auskunft darüber, dass über die Anmerkung gesprochen wurde.
        var mine = await child.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        var row = mine.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == own);
        Assert.Equal(0, row.GetProperty("commentCount").GetInt32());
    }

    [Fact]
    public async Task LeererBeitrag_IstValidierungsfehler()
    {
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Mit leerem Beitrag");

        var res = await father.PostAsJsonAsync($"{Url}/{id}/comments", new { body = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ── Kontenübergreifend lesen (scope=all) ──────────────────────────────────────────────────────────
    //
    // Die Berechtigung hängt am Schalter `Remarks:GlobalRead` (in der Entwicklung an), NICHT an einer Rolle.
    // Grund: Beim Testen entstehen ständig Wegwerf-Konten, weil sich manche Fehler nur in einer bestimmten
    // Konstellation zeigen (ein frischer Vater ohne Übungen deckt auf, was beim geseedeten Papa nie
    // auffällt). Jedes solche Konto erst mit einem Flag zu versehen wäre Verwaltungsarbeit ohne Gegenwert.

    /// <summary>Registriert einen Vater und liefert einen Client dafür – ein Wegwerf-Konto ohne Sonderrechte.</summary>
    private async Task<HttpClient> FreshFatherAsync(string pin)
    {
        var id = await RegisterFatherAsync("Wegwerf-Vater", pin);
        return await TestApi.FatherAsync(_factory, id, pin);
    }

    private async Task<int> RegisterFatherAsync(string name, string pin)
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name, pin });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Registriert einen Vater <b>mit</b> <c>Adult.IsAdmin</c> und liefert dessen Id – für den Nachweis, dass
    /// die Rolle als Break-Glass weiterhin trägt, auch wenn der Schalter aus ist.
    /// <para>
    /// Bewusst ein <b>eigenes</b> Konto und kein geseedetes: Die Factory ist über die Testklasse geteilt, und
    /// <c>Roles.Admin</c> umgeht auch die RWX-Rechte auf Übungen. Würde hier Papa oder der Lehrer umgewidmet,
    /// verlöre jeder andere Test seine Annahme „dieses Konto darf das nicht" – abhängig von der
    /// Ausführungsreihenfolge. (Genau das ist beim ersten Anlauf passiert.)
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
    /// Client gegen <b>dieselbe Datenbank</b>, aber mit abgeschaltetem <c>GlobalRead</c> – so verhält sich eine
    /// Produktions-Instanz. Das Token wird hier ausgestellt, damit die Rollen-Claims stimmen.
    /// </summary>
    private async Task<HttpClient> FatherWithoutGlobalReadAsync(int id = 1, string pin = "0000")
    {
        var narrow = _factory.WithWebHostBuilder(b => b.UseSetting("Remarks:GlobalRead", "false"));
        return await TestApi.FatherAsync(narrow, id, pin);
    }

    [Fact]
    public async Task JedesVaterKonto_LiestMitScopeAll_AlleAnmerkungen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var foreign = await CreateAsync(father, "Beobachtung aus einem anderen Konto");

        // Ein frisch registriertes Wegwerf-Konto: keine Kinder, keine Übungen, KEIN Admin-Flag.
        var fresh = await FreshFatherAsync("6101");

        // Ohne Parameter bleibt die Sicht eng – sonst zeigte die Liste im Widget fremde Einträge.
        var narrow = await fresh.GetFromJsonAsync<JsonElement>(Url);
        Assert.DoesNotContain(foreign, narrow.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));

        // Mit scope=all sieht es alles – das ist die Sicht, die der Nachbereitungs-Skill braucht.
        var all = await fresh.GetFromJsonAsync<JsonElement>($"{Url}?scope=all");
        Assert.Contains(foreign, all.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task FremdeAnmerkung_LaesstSichBeantwortenUndKommentieren()
    {
        var father = await TestApi.FatherAsync(_factory);
        var foreign = await CreateAsync(father, "Frage aus einem anderen Testkonto");
        var fresh = await FreshFatherAsync("6102");

        // Eine einzelne Id braucht den scope-Parameter nicht: Genau so löst der Skill „Beantworte 123" ein.
        var one = await fresh.GetFromJsonAsync<JsonElement>($"{Url}/{foreign}");
        Assert.Equal(foreign, one.GetProperty("id").GetInt32());
        // Fremd, aber sichtbar – das Widget blendet sie damit aus seiner eigenen Liste aus.
        Assert.False(one.GetProperty("isOwn").GetBoolean());

        (await fresh.PatchAsJsonAsync($"{Url}/{foreign}",
            new { answer = "Belegt in VaterVocab.tsx:345.", answeredBy = "claude-code", status = "Done" }))
            .EnsureSuccessStatusCode();
        await CommentAsync(fresh, foreign, "Gebaut und geprüft.", author: "Assistant", label: "claude-code");

        // Der Eigentümer sieht Auflösung und Verlauf in seiner eigenen Sicht.
        var owner = await father.GetFromJsonAsync<JsonElement>($"{Url}/{foreign}");
        Assert.Equal("Belegt in VaterVocab.tsx:345.", owner.GetProperty("answer").GetString());
        var thread = await father.GetFromJsonAsync<JsonElement>($"{Url}/{foreign}/comments");
        Assert.Equal("Gebaut und geprüft.", thread[0].GetProperty("body").GetString());
    }

    [Fact]
    public async Task Sohn_LiestNiemalsAlleKonten_AuchNichtMitGlobalRead()
    {
        var father = await TestApi.FatherAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        var foreign = await CreateAsync(father, "Interne Notiz mit Codebezug");

        // Der Schalter öffnet die Sicht für Erwachsene, nie für ein Kind: Antworten und Verlauf tragen
        // Datei- und Zeilenverweise.
        var res = await child.GetAsync($"{Url}?scope=all");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("remark_scope_forbidden",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await child.GetAsync($"{Url}/{foreign}")).StatusCode);
    }

    [Fact]
    public async Task OhneGlobalRead_IstScopeAll_403_UndDieEngeSichtBleibt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var own = await CreateAsync(father, "Eigene Anmerkung bei abgeschaltetem Schalter");

        var narrow = await FatherWithoutGlobalReadAsync();

        var res = await narrow.GetAsync($"{Url}?scope=all");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("remark_scope_forbidden",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Ohne den Parameter arbeitet dieselbe Instanz normal weiter – der Schalter sperrt nicht das Feature.
        var list = await narrow.GetFromJsonAsync<JsonElement>($"{Url}?mine=true");
        Assert.Contains(own, list.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task OhneGlobalRead_TraegtDieAdminRolle_WeiterhinAlsBreakGlass()
    {
        var father = await TestApi.FatherAsync(_factory);
        var foreign = await CreateAsync(father, "Nur per Break-Glass erreichbar");

        var adminId = await RegisterAdminFatherAsync("6103");
        var adminNarrow = await FatherWithoutGlobalReadAsync(adminId, "6103");

        var all = await adminNarrow.GetFromJsonAsync<JsonElement>($"{Url}?scope=all");
        Assert.Contains(foreign, all.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Export_MitScopeAll_TraegtDenVerlauf_UndOhneGlobalRead_403()
    {
        var father = await TestApi.FatherAsync(_factory);
        var id = await CreateAsync(father, "Export mit Verlauf");
        await CommentAsync(father, id, "Umsetzungsnotiz für den Export.", author: "Assistant", label: "claude-code");

        // Der eigene Export trägt den Verlauf – deshalb weiß ein Schnappschuss von heute noch etwas über
        // gestern; vorher überschrieb die Umsetzungsnotiz die Analyse.
        var own = await father.GetAsync($"{Url}/export");
        own.EnsureSuccessStatusCode();
        var markdown = await own.Content.ReadAsStringAsync();
        Assert.Contains("**Verlauf**", markdown);
        Assert.Contains("> Umsetzungsnotiz für den Export.", markdown);

        // Kontenübergreifend: erlaubt (Schalter an) und mit Konto-Angabe je Beitrag, damit im
        // Repo-Schnappschuss erkennbar bleibt, wessen Beobachtung eine Zeile ist.
        var across = await father.GetAsync($"{Url}/export?scope=all");
        across.EnsureSuccessStatusCode();
        Assert.Contains("Konto", await across.Content.ReadAsStringAsync());

        var narrow = await FatherWithoutGlobalReadAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await narrow.GetAsync($"{Url}/export?scope=all")).StatusCode);
    }
}
