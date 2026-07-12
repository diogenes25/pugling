using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// „Große Ziele" (Objectives, der kindgerechte OKR-Kern): der Vater setzt eine terminierte Klammer über
/// messbaren Etappen (Key Results), der Fortschritt wird live aus Lernstand + Klassenarbeits-Note berechnet.
/// Deckt ab: Anlage/Auswertung, die idempotente Belohnung (Etappen-Häppchen + Voll-Abschluss, Münzen bei
/// Committed / Gems bei Stretch), den Noten-Anker (ClassTestGrade) sowie Validierung/Rollen. Jeder Test nutzt
/// ein frisches Kind (isolierte Wallet), damit absolute Salden geprüft werden können.
/// </summary>
public class ObjectiveTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> SubjectAsync(HttpClient father, string name) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name }));

    private static async Task<int> FreshChildIdAsync(HttpClient father, string pin) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Ziel-Kind", pin }));

    private static string Url(int childId) => $"/api/v1/supervisor/children/{childId}/objectives";

    [Fact]
    public async Task Committed_ZahltEtappeUndAbschluss_InMuenzen_UndIstIdempotent()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Committed");
        var childId = await FreshChildIdAsync(father, "7101");

        // Ein sofort erreichbares Ziel: „höchstens 0 schwache Wörter" ist ohne Lernstand vakuär erfüllt.
        var created = await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Englisch sicher können",
            motivation = "Damit die nächste Arbeit sitzt.",
            kind = "Committed",
            rewardOnComplete = 20,
            rewardPerKeyResult = 5,
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        }));
        Assert.Equal("achieved", created.GetProperty("status").GetString());
        Assert.Equal(1, created.GetProperty("achievedCount").GetInt32());
        Assert.Equal(1, created.GetProperty("totalCount").GetInt32());
        Assert.False(created.GetProperty("rewarded").GetBoolean()); // bei Anlage noch nicht abgerechnet

        // Der Kind-Login rechnet die Belohnung nach: 5 (Etappe) + 20 (Abschluss) = 25 Münzen, keine Gems.
        var child = await TestApi.ChildAsync(factory, childId, "7101");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(25, wallet.GetProperty("coins").GetInt32());
        Assert.Equal(0, wallet.GetProperty("gems").GetInt32());

        // Genau zwei ObjectiveCoins-Buchungen (5 + 20).
        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        var objEntries = entries.EnumerateArray().Where(e => e.GetProperty("kind").GetString() == "ObjectiveCoins").ToList();
        Assert.Equal(2, objEntries.Count);
        Assert.Equal(25, objEntries.Sum(e => e.GetProperty("amount").GetInt32()));

        // Sohn-Sicht: erreicht + belohnt.
        var mine = await child.GetFromJsonAsync<List<JsonElement>>("/api/v1/student/me/objectives");
        var o = Assert.Single(mine!);
        Assert.True(o.GetProperty("rewarded").GetBoolean());
        Assert.Equal("achieved", o.GetProperty("status").GetString());

        // Zweiter Login → keine erneute Auszahlung (Unique-Index + Existenz-Check).
        var childAgain = await TestApi.ChildAsync(factory, childId, "7101");
        var wallet2 = await JsonAsync(await childAgain.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(25, wallet2.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task Stretch_ZahltNurErreichteEtappe_InGems_KeinAbschluss()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Stretch");
        var childId = await FreshChildIdAsync(father, "7102");

        // Zwei Etappen: eine sofort erfüllt (MaxWeakItems ≤ 0), eine unerreichbar (100 % beherrscht ohne Lernstand).
        var created = await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Extra-Dehnungsziel",
            kind = "Stretch",
            rewardOnComplete = 100,
            rewardPerKeyResult = 3,
            keyResults = new[]
            {
                new { subjectId, metric = "MaxWeakItems", targetValue = 0 },
                new { subjectId, metric = "MasteredPercent", targetValue = 100 },
            },
        }));
        Assert.Equal(1, created.GetProperty("achievedCount").GetInt32());
        Assert.Equal(2, created.GetProperty("totalCount").GetInt32());
        Assert.Equal("open", created.GetProperty("status").GetString());

        // Login zahlt nur das Etappen-Häppchen (3 Gems); der Abschluss-Batzen bleibt aus (nicht alle erreicht).
        var child = await TestApi.ChildAsync(factory, childId, "7102");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(3, wallet.GetProperty("gems").GetInt32());
        Assert.Equal(0, wallet.GetProperty("coins").GetInt32());

        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        var objEntries = entries.EnumerateArray().Where(e => e.GetProperty("kind").GetString() == "ObjectiveGems").ToList();
        Assert.Equal(3, Assert.Single(objEntries).GetProperty("amount").GetInt32());

        var mine = await child.GetFromJsonAsync<List<JsonElement>>("/api/v1/student/me/objectives");
        var o = Assert.Single(mine!);
        Assert.False(o.GetProperty("rewarded").GetBoolean());
        Assert.Equal("open", o.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ClassTestGrade_AlsAnker_WirdAusDerNoteErreicht()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Note");
        var childId = await FreshChildIdAsync(father, "7103");

        // Der Vater trägt eine 2,0 im Fach nach (Status wird dadurch „Written").
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        (await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId,
            subjectId,
            title = "Vokabeltest Unit 3",
            scheduledDate = today,
            grade = 2.0,
        })).EnsureSuccessStatusCode();

        // Ziel: Note ≤ 2,0 (Zielwert 20 = Note×10). Die 2,0 erfüllt es.
        var created = await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Gute Note schreiben",
            kind = "Committed",
            rewardOnComplete = 10,
            rewardPerKeyResult = 0,
            keyResults = new[] { new { subjectId, metric = "ClassTestGrade", targetValue = 20 } },
        }));
        Assert.Equal("achieved", created.GetProperty("status").GetString());
        Assert.Equal(20, created.GetProperty("keyResults")[0].GetProperty("currentValue").GetInt32());

        // Login schreibt den Abschluss-Batzen gut (10 Münzen; kein Etappen-Häppchen konfiguriert).
        var child = await TestApi.ChildAsync(factory, childId, "7103");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(10, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task Validierung_UndRollen()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Valid");
        var childId = await FreshChildIdAsync(father, "7104");

        // Leerer Titel → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "", kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        })).StatusCode);

        // ClassTestGrade mit Kapitel-Scope → 400 (Noten hängen am Fach).
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X", kind = "Committed",
            keyResults = new[] { new { subjectId, chapterId = 1, metric = "ClassTestGrade", targetValue = 20 } },
        })).StatusCode);

        // ClassTestGrade Zielnote außerhalb 10..60 → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X", kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "ClassTestGrade", targetValue = 5 } },
        })).StatusCode);

        // Prozent-Metrik über 100 → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X", kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "AvgMastery", targetValue = 150 } },
        })).StatusCode);

        // „Mindestens"-Metrik mit Zielwert 0 → 400 (wäre sonst sofort vakuär erfüllt = Gratis-Belohnung).
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X", kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MasteredPercent", targetValue = 0 } },
        })).StatusCode);

        // Der Sohn darf keine Ziele anlegen (nur Vater).
        var child = await TestApi.ChildAsync(factory, childId, "7104");
        Assert.Equal(HttpStatusCode.Forbidden, (await child.PostAsJsonAsync(Url(childId), new
        {
            title = "X", kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        })).StatusCode);

        // Fremdes/nicht existierendes Kind → 404 (Ownership-Filter).
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync(Url(999999))).StatusCode);
    }

    [Fact]
    public async Task Etappen_CrudUnterObjective()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-KR-Crud");
        var childId = await FreshChildIdAsync(father, "7105");

        var objectiveId = (await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Ziel mit Etappen", kind = "Committed", rewardOnComplete = 0, rewardPerKeyResult = 0,
        }))).GetProperty("id").GetInt32();

        var krUrl = $"{Url(childId)}/{objectiveId}/key-results";
        var kr = await JsonAsync(await father.PostAsJsonAsync(krUrl, new { subjectId, metric = "MaxWeakItems", targetValue = 0 }));
        var keyResultId = kr.GetProperty("id").GetInt32();
        Assert.Equal("achieved", kr.GetProperty("status").GetString());

        // Zielwert/Titel ändern (Scope bleibt fix).
        var patched = await JsonAsync(await father.PatchAsJsonAsync($"{krUrl}/{keyResultId}", new { metric = "MasteredPercent", targetValue = 80, title = "Beherrschen" }));
        Assert.Equal("MasteredPercent", patched.GetProperty("metric").GetString());
        Assert.Equal(80, patched.GetProperty("targetValue").GetInt32());
        Assert.Equal("open", patched.GetProperty("status").GetString());

        // Löschen.
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{krUrl}/{keyResultId}")).StatusCode);
        var afterDelete = await father.GetFromJsonAsync<JsonElement>($"{Url(childId)}/{objectiveId}");
        Assert.Equal(0, afterDelete.GetProperty("totalCount").GetInt32());
    }
}
