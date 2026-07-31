using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>Integration tests for the exercises' RWX grant model (Owner/Write/Execute) and the execute gate.</summary>
public class ExerciseGrantsTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<int> RegisterFatherAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = "Papa2", pin });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    /// <summary>Creates (as the creator) subject → chapter → vocabulary exercise and returns their ids (incl. execute visibility).</summary>
    private static async Task<(int subjectId, int chapterId, int exerciseId)> CreateVocabAsync(HttpClient father, bool executePublic = true)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Grant-Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", VocabPayload(executePublic)));
        return (subjectId, chapterId, exerciseId);
    }

    private static object VocabPayload(bool executePublic = true, string title = "Wörter") => new
    {
        title,
        orderIndex = 1,
        rewardPoints = 10,
        executePublic,
        config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "cat", back = "Katze" } } },
    };

    private static async Task AssertCodeAsync(HttpResponseMessage res, string code)
    {
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    /// <summary>Marks an adult as a platform admin (no API path – deliberately only via the DB, as in real operation).</summary>
    private void MakeAdmin(int supervisorId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.Adults.First(f => f.Id == supervisorId).IsAdmin = true;
        db.SaveChanges();
    }

    /// <summary>Removes all grants of an exercise - simulates an orphaned (ownerless) exercise (e.g. after a father is deleted).</summary>
    private void OrphanExercise(int exerciseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ExerciseGrants.RemoveRange(db.ExerciseGrants.Where(g => g.ExerciseId == exerciseId));
        db.SaveChanges();
    }

    private async Task<(HttpClient client, int id, int childId, int planId)> SecondFatherWithPlanAsync()
    {
        var id2 = await RegisterFatherAsync("2222");
        var f2 = await TestApi.FatherAsync(factory, id2, "2222");
        var childId = await TestApi.IdAsync(await f2.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "KindB" }));
        var planId = await TestApi.IdAsync(await f2.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "PlanB", durationDays = 5 }));
        return (f2, id2, childId, planId);
    }

    [Fact]
    public async Task OhneGrant_KannFremderCreatorNichtAendern_MitWriteGrantSchon()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await CreateVocabAsync(f1);
        var id2 = await RegisterFatherAsync("2222");
        var f2 = await TestApi.FatherAsync(factory, id2, "2222");
        var url = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}";

        // Ohne Recht: 403 not_author.
        var denied = await f2.PutAsJsonAsync(url, VocabPayload());
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await AssertCodeAsync(denied, "not_author");

        // Owner erteilt Write → B darf ändern.
        var grant = await f1.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Write" });
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await f2.PutAsJsonAsync(url, VocabPayload())).StatusCode);
    }

    [Fact]
    public async Task WriteGrantee_DarfWederLoeschenNochGranten_403NotOwner()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await CreateVocabAsync(f1);
        var id2 = await RegisterFatherAsync("2222");
        var f2 = await TestApi.FatherAsync(factory, id2, "2222");
        await f1.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Write" });

        var del = await f2.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
        await AssertCodeAsync(del, "not_owner");

        var grant = await f2.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Owner" });
        Assert.Equal(HttpStatusCode.Forbidden, grant.StatusCode);
        await AssertCodeAsync(grant, "not_owner");
    }

    [Fact]
    public async Task GrantIstIdempotent_UndListeZeigtOwner()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await CreateVocabAsync(f1);
        var id2 = await RegisterFatherAsync("2222");

        var g1 = await f1.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Write" });
        var g2 = await f1.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Write" });
        Assert.Equal(HttpStatusCode.Created, g1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, g2.StatusCode);

        var list = await f1.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/exercises/{exerciseId}/grants");
        // Genau ein Owner (Auto-Grant des Anlegers) + genau ein (nicht duplizierter) Write.
        Assert.Single(list!, g => g.GetProperty("permission").GetString() == "Owner");
        Assert.Single(list!, g => g.GetProperty("permission").GetString() == "Write");
    }

    [Fact]
    public async Task LetzterOwner_KannNichtEntferntWerden_409()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await CreateVocabAsync(f1);

        // f1 (Vater-Id 1) ist einziger Owner (Auto-Grant beim Anlegen).
        var del = await f1.DeleteAsync($"/api/v1/creator/exercises/{exerciseId}/grants/1/Owner");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        await AssertCodeAsync(del, "last_owner");
    }

    [Fact]
    public async Task ExecuteGate_BlocktNichtOeffentliche_UndExecuteGrantHebtAuf()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await CreateVocabAsync(f1, executePublic: false);
        var (f2, id2, _, planId) = await SecondFatherWithPlanAsync();
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var denied = await f2.PostAsJsonAsync(url, new { exerciseId });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await AssertCodeAsync(denied, "exercise_not_executable");

        await f1.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Execute" });
        Assert.Equal(HttpStatusCode.Created, (await f2.PostAsJsonAsync(url, new { exerciseId })).StatusCode);
    }

    [Fact]
    public async Task OeffentlicheUebung_BleibtFuerFremdeZuweisbar()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await CreateVocabAsync(f1); // executePublic default true
        var (f2, _, _, planId) = await SecondFatherWithPlanAsync();

        var ok = await f2.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

        // „Zuweisbar" heißt: die Position hängt danach auch am Plan und auf der richtigen Übung. Der 201
        // allein belegt nur, dass das Execute-Gate nicht zugeschlagen hat (docs/testplan.md, Etappe 1a).
        var positions = await f2.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Contains(positions!, p => p.GetProperty("exerciseId").GetInt32() == exerciseId);
    }

    [Fact]
    public async Task Admin_DarfFremdeUebungAendernUndLoeschen()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await CreateVocabAsync(f1);
        var adminId = await RegisterFatherAsync("9999");
        MakeAdmin(adminId);
        var admin = await TestApi.FatherAsync(factory, adminId, "9999");
        var url = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}";

        // Mit GEÄNDERTEM Titel schreiben und ihn zurücklesen: ein PUT, der die Übung unangetastet lässt,
        // antwortet ebenfalls mit 200. Vorher schickte der Test dieselben Werte, die schon drinstanden –
        // der Erfolgsstatus war zugesichert, der Effekt nie nachgelesen (docs/testplan.md, Etappe 1a).
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync(url, VocabPayload(title: "Vom Admin geändert"))).StatusCode);
        var changed = await admin.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal("Vom Admin geändert", changed.GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Admin_KannVerwaisteUebungBearbeiten_AutorNichtMehr()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await CreateVocabAsync(f1);
        OrphanExercise(exerciseId); // kein Owner mehr → für niemanden (außer Admin) editierbar
        var url = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}";

        // Selbst der ursprüngliche Autor hat ohne Grant kein Schreibrecht mehr.
        Assert.Equal(HttpStatusCode.Forbidden, (await f1.PutAsJsonAsync(url, VocabPayload())).StatusCode);

        var adminId = await RegisterFatherAsync("9999");
        MakeAdmin(adminId);
        var admin = await TestApi.FatherAsync(factory, adminId, "9999");
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync(url, VocabPayload(title: "Adoptiert"))).StatusCode);
        // Und die Änderung steht auch drin – sonst belegte der Test nur, dass der Admin nicht abgewiesen wird.
        Assert.Equal("Adoptiert", (await admin.GetFromJsonAsync<JsonElement>(url)).GetProperty("title").GetString());
    }

    /// <summary>
    /// The POST response must report the same <c>grantCount</c> as the subsequent GET. Previously
    /// <c>Create</c> appended the owner grant to the loaded navigation once more after saving, even though EF's
    /// relationship fixup had already added it there - the response reported 2 grants while the DB knew of 1.
    /// </summary>
    [Fact]
    public async Task CreateAntwort_ZaehltDenOwnerGrantNichtDoppelt()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Grant-Zaehlung {Guid.NewGuid():N}" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel 1", orderIndex = 1 }));

        var created = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", VocabPayload());
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("grantCount").GetInt32());

        // Gegenprobe über den Lesepfad: beide Sichten müssen sich einig sein.
        var detail = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises/{body.GetProperty("id").GetInt32()}");
        Assert.Equal(1, detail.GetProperty("grantCount").GetInt32());
    }

    [Fact]
    public async Task DetailResponse_ZeigtGrantCount()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await CreateVocabAsync(f1);
        var id2 = await RegisterFatherAsync("2222");
        await f1.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = id2, permission = "Write" });

        var detail = await f1.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        // 1 Owner (Auto-Grant des Anlegers) + 1 Write = 2.
        Assert.Equal(2, detail.GetProperty("grantCount").GetInt32());
        Assert.True(detail.GetProperty("isOwner").GetBoolean());
    }

    [Fact]
    public async Task VokabelListe_FiltertNachIsOwnUndIsOwner()
    {
        var f1 = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, ownExerciseId) = await CreateVocabAsync(f1);

        var id2 = await RegisterFatherAsync("2222");
        var f2 = await TestApi.FatherAsync(factory, id2, "2222");
        var foreignExerciseId = await TestApi.IdAsync(await f2.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", VocabPayload()));

        var baseUrl = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary";
        var ownDetail = await f1.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{ownExerciseId}");
        var ownCreatorId = ownDetail.GetProperty("authorAdultId").GetInt32();

        var ownOnlyBeforeGrant = await f1.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}?isOwn=true");
        Assert.Single(ownOnlyBeforeGrant!);
        Assert.Equal(ownExerciseId, ownOnlyBeforeGrant![0].GetProperty("id").GetInt32());

        var ownerOnlyBeforeGrant = await f1.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}?isOwner=true");
        Assert.Single(ownerOnlyBeforeGrant!);
        Assert.Equal(ownExerciseId, ownerOnlyBeforeGrant![0].GetProperty("id").GetInt32());

        // Nach Write-Grant darf f1 auch die fremde Übung ändern (isOwn=true), bleibt aber kein Owner.
        var grant = await f2.PostAsJsonAsync($"/api/v1/creator/exercises/{foreignExerciseId}/grants",
            new { creatorId = ownCreatorId, permission = "Write" });
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);

        var ownOnlyAfterGrant = await f1.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}?isOwn=true");
        Assert.Equal(2, ownOnlyAfterGrant!.Count);

        var ownerOnlyAfterGrant = await f1.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}?isOwner=true");
        Assert.Single(ownerOnlyAfterGrant!);
        Assert.Equal(ownExerciseId, ownerOnlyAfterGrant![0].GetProperty("id").GetInt32());
    }
}
