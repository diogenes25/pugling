using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Kind-zentrische Drill-down-Sicht auf den Vokabel-Lernstand entlang der Katalog-Hierarchie
/// (Fach → Kapitel → Übung → Item). „Zugewiesen" wird aus den Lehrplänen abgeleitet – auch noch nicht
/// geübte Übungen erscheinen (Null-Fortschritt), Fortschritt kommt aus dem server-autoritativen Üben.
/// </summary>
public class ChildLearnProgressTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    // Legt Fach → Kapitel → eine Vokabelübung an und liefert alle Ids (der Katalog verrät sonst subject/chapter nicht).
    private static async Task<(int subjectId, int chapterId, int exerciseId)> VocabAsync(
        HttpClient father, string subjectName, string title, params (string Front, string Back)[] items)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = subjectName }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title,
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = items.Select(i => new { front = i.Front, back = i.Back }) },
            }));
        return (subjectId, chapterId, exerciseId);
    }

    // Legt eine weitere Vokabelübung in ein BESTEHENDES Fach/Kapitel und liefert ihre Id.
    private static async Task<int> VocabInAsync(HttpClient father, int subjectId, int chapterId, string title, params (string Front, string Back)[] items) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title,
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = items.Select(i => new { front = i.Front, back = i.Back }) },
            }));

    // Bündelt zwei bestehende Übungen als Positionen in EINEM aktiven Plan (ein aktiver Plan je Kind).
    private (int planId, int pos1, int pos2) SeedPlanWithTwoPositions(int exercise1, int exercise2, int childId = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = childId, Title = "Progress-Plan", StartDate = today, EndDate = today.AddDays(5), Active = true };
        var p1 = new PlanPosition { ExerciseId = exercise1, Order = 0, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true };
        var p2 = new PlanPosition { ExerciseId = exercise2, Order = 1, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true };
        plan.Positions.Add(p1);
        plan.Positions.Add(p2);
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return (plan.Id, p1.Id, p2.Id);
    }

    // Legt einen (aktiven oder inaktiven) Plan mit Positionen auf die gegebenen Übungen an; liefert die Plan-Id.
    private int SeedPlan(bool active, params int[] exerciseIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = 1, Title = "Flag-Plan", StartDate = today, EndDate = today.AddDays(5), Active = active };
        var order = 0;
        foreach (var id in exerciseIds)
            plan.Positions.Add(new PlanPosition { ExerciseId = id, Order = order++, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return plan.Id;
    }

    private void SetPlanActive(int planId, bool active)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = db.StudyPlans.Find(planId)!;
        plan.Active = active;
        db.SaveChanges();
    }

    [Fact]
    public async Task Hierarchie_AggregiertFortschritt_ZeigtAbdeckung_UndBlattItems()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Eindeutige Wörter, damit der pro-Kind geteilte Fortschritt/Store nicht mit anderen Tests kollidiert.
        var (subjectId, chapterId, ex1) = await VocabAsync(father, "Progress-Fach", "Geübt", ("quokka", "Kurzschwanzkänguru"), ("axolotl", "Axolotl"));
        var (_, _, ex2) = await VocabAsync(father, "Progress-Fach-B", "Ungeübt", ("pangolin", "Schuppentier"), ("tapir", "Tapir"));

        // Beide Übungen liegen im SELBEN Fach/Kapitel (ex2 nur für die Position umgehängt): wir nehmen ex1+ex2 in einen Plan.
        var (planId, pos1, _) = SeedPlanWithTwoPositions(ex1, ex2);
        var child = await TestApi.ChildAsync(_factory);

        // Nur ex1 üben: eine richtig, eine falsch → 2 von insgesamt 4 Items eingeführt.
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, pos1);
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 0, givenAnswer: "Kurzschwanzkänguru"); // richtig
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 1, givenAnswer: "daneben");             // falsch

        var basePath = "/api/v1/student/children/1/learn";

        // Fach-Liste: die beiden zugewiesenen Fächer erscheinen; das geübte Fach zeigt Abdeckung (Total > Introduced).
        var subjects = await father.GetFromJsonAsync<List<JsonElement>>($"{basePath}/subjects");
        var geubtesFach = subjects!.First(s => s.GetProperty("subjectId").GetInt32() == subjectId);
        var prog = geubtesFach.GetProperty("progress");
        Assert.Equal(1, geubtesFach.GetProperty("exerciseCount").GetInt32());
        Assert.Equal(2, prog.GetProperty("totalItems").GetInt32());       // beide Vokabeln der Übung
        Assert.Equal(2, prog.GetProperty("introducedItems").GetInt32());  // beide beantwortet
        Assert.True(prog.GetProperty("avgMasteryPercent").GetInt32() > 0);
        Assert.Equal(2, prog.GetProperty("seenCount").GetInt32());
        Assert.Equal(1, prog.GetProperty("correctCount").GetInt32());

        // Einzelnes Fach: identisches Aggregat.
        var subject = await father.GetFromJsonAsync<JsonElement>($"{basePath}/subjects/{subjectId}");
        Assert.Equal(2, subject.GetProperty("progress").GetProperty("totalItems").GetInt32());

        // Kapitel-Ebene.
        var chapters = await father.GetFromJsonAsync<List<JsonElement>>($"{basePath}/subjects/{subjectId}/chapters");
        Assert.Single(chapters!);
        Assert.Equal(chapterId, chapters![0].GetProperty("chapterId").GetInt32());

        // Übungs-Ebene: die geübte Übung mit Fortschritt.
        var exercises = await father.GetFromJsonAsync<List<JsonElement>>($"{basePath}/subjects/{subjectId}/chapters/{chapterId}/vocabulary");
        var ex1Row = exercises!.First(e => e.GetProperty("exerciseId").GetInt32() == ex1);
        Assert.Equal(2, ex1Row.GetProperty("progress").GetProperty("introducedItems").GetInt32());

        // Blatt-Ebene: Item-Lernstand, schwächste zuerst.
        var itemsRes = await father.GetAsync($"{basePath}/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{ex1}/items");
        itemsRes.EnsureSuccessStatusCode();
        var leaf = await itemsRes.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(2, leaf!.Count);
        Assert.Equal("2", itemsRes.Headers.GetValues("X-Total-Count").First());
        Assert.True(leaf![0].GetProperty("masteryPercent").GetInt32() <= leaf![1].GetProperty("masteryPercent").GetInt32());
    }

    [Fact]
    public async Task UngeübteAberZugewieseneÜbung_ErscheintMitNullFortschritt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, _, ex1) = await VocabAsync(father, "Null-Fach", "Geübt", ("okapi", "Okapi"));
        var (subjectId, chapterId, ex2) = await VocabAsync(father, "Null-Fach-B", "Nie geübt", ("numbat", "Ameisenbeutler"), ("dugong", "Dugong"));
        SeedPlanWithTwoPositions(ex1, ex2);

        var exercises = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/student/children/1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary");
        var ex2Row = exercises!.First(e => e.GetProperty("exerciseId").GetInt32() == ex2);
        var prog = ex2Row.GetProperty("progress");
        Assert.Equal(2, prog.GetProperty("totalItems").GetInt32());       // Übung hat 2 Items …
        Assert.Equal(0, prog.GetProperty("introducedItems").GetInt32());  // … aber nichts davon geübt
        Assert.Equal(0, prog.GetProperty("avgMasteryPercent").GetInt32());
        Assert.True(prog.GetProperty("lastActivityAt").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task NichtZugewiesenesFach_Und_NichtZugewieseneÜbung_Liefern404()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Fach mit Übung, aber KEIN Plan → dem Kind nicht zugewiesen.
        var (subjectId, chapterId, exerciseId) = await VocabAsync(father, "Waise-Fach", "Ohne Plan", ("caracal", "Karakal"));

        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/student/children/1/learn/subjects/{subjectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/student/children/1/learn/subjects/{subjectId}/chapters")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/student/children/1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items")).StatusCode);
    }

    [Fact]
    public async Task FremdesKind_Liefert404_SohnSiehtEigenen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, _, ex1) = await VocabAsync(father, "Ownership-Fach", "Geübt", ("serval", "Serval"));
        var (_, _, ex2) = await VocabAsync(father, "Ownership-Fach-B", "Ungeübt", ("gerenuk", "Giraffengazelle"));
        SeedPlanWithTwoPositions(ex1, ex2);

        // Fremdes/nicht existierendes Kind → Ownership-Filter liefert 404 (kein Enumerieren).
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync("/api/v1/student/children/999/learn/subjects")).StatusCode);

        // Der Sohn darf seinen eigenen Stand lesen.
        var child = await TestApi.ChildAsync(_factory);
        var self = await child.GetAsync("/api/v1/student/children/1/learn/subjects");
        self.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AbgehängterPlan_MachtÜbungInaktiv_FortschrittBleibt()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Beide Übungen im SELBEN Fach/Kapitel, damit die Vokabel-Liste beide zeigt.
        var (subjectId, chapterId, ex1) = await VocabAsync(father, "Retention-Fach", "Geübt", ("wombat", "Wombat"), ("kakapo", "Kakapo"));
        var ex2 = await VocabInAsync(father, subjectId, chapterId, "Ungeübt", ("quoll", "Beutelmarder"));
        var (planId, pos1, _) = SeedPlanWithTwoPositions(ex1, ex2);
        var child = await TestApi.ChildAsync(_factory);

        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, pos1);
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 0, givenAnswer: "Wombat");
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 1, givenAnswer: "Kakapo");

        var url = $"/api/v1/student/children/1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary";

        // Solange der Plan aktiv ist: ex1 aktiv, mit Fortschritt.
        var before = await father.GetFromJsonAsync<List<JsonElement>>(url);
        var ex1Before = before!.First(e => e.GetProperty("exerciseId").GetInt32() == ex1);
        Assert.True(ex1Before.GetProperty("active").GetBoolean());
        Assert.Equal(2, ex1Before.GetProperty("progress").GetProperty("introducedItems").GetInt32());

        // Plan deaktivieren → die Übung wird inaktiv, der Fortschritt bleibt erhalten (verschwindet nicht).
        SetPlanActive(planId, false);
        var after = await father.GetFromJsonAsync<List<JsonElement>>(url);
        var ex1After = after!.First(e => e.GetProperty("exerciseId").GetInt32() == ex1);
        Assert.False(ex1After.GetProperty("active").GetBoolean());
        Assert.Equal(2, ex1After.GetProperty("progress").GetProperty("introducedItems").GetInt32());
        // Die ungeübte Zweit-Übung bleibt als inaktiv sichtbar (0 % Fortschritt).
        var ex2After = after!.First(e => e.GetProperty("exerciseId").GetInt32() == ex2);
        Assert.False(ex2After.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task ActiveFilter_TrenntAktivVonInaktiv()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, chapterId, exAktiv) = await VocabAsync(father, "Filter-Fach", "Aktiv", ("dingo", "Dingo"));
        var exInaktiv = await VocabInAsync(father, subjectId, chapterId, "Inaktiv", ("bilby", "Bilby"));
        SeedPlan(active: true, exAktiv);
        SeedPlan(active: false, exInaktiv);

        var url = $"/api/v1/student/children/1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary";

        var aktiv = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?active=true");
        Assert.Contains(aktiv!, e => e.GetProperty("exerciseId").GetInt32() == exAktiv);
        Assert.DoesNotContain(aktiv!, e => e.GetProperty("exerciseId").GetInt32() == exInaktiv);

        var inaktiv = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?active=false");
        Assert.Contains(inaktiv!, e => e.GetProperty("exerciseId").GetInt32() == exInaktiv);
        Assert.DoesNotContain(inaktiv!, e => e.GetProperty("exerciseId").GetInt32() == exAktiv);
    }

    [Fact]
    public async Task SucheUndSortierung_AufÜbungen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, chapterId, exTiere) = await VocabAsync(father, "Sort-Fach", "Tiere", ("emu", "Emu"));
        var exFarben = await VocabInAsync(father, subjectId, chapterId, "Farben", ("mauve", "Malvenfarben"));
        SeedPlan(active: true, exTiere, exFarben);

        var url = $"/api/v1/student/children/1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary";

        // Suche nach Titel (Teilstring, case-insensitiv).
        var suche = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?search=tier");
        Assert.Single(suche!);
        Assert.Equal("Tiere", suche![0].GetProperty("title").GetString());

        // Sortierung nach Titel absteigend: "Tiere" vor "Farben".
        var desc = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?sort=title&dir=desc");
        var titlesDesc = desc!.Select(e => e.GetProperty("title").GetString()).ToList();
        Assert.True(titlesDesc.IndexOf("Tiere") < titlesDesc.IndexOf("Farben"));
    }
}
