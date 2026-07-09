using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Kern-Szenario der geteilten Übungs-Bibliothek: Ein <b>Englischlehrer</b> (Vater-Account) erstellt Übungen
/// auf Niveau der 9. Klasse Gymnasium. Ein <b>anderer Vater</b> findet sie über den globalen Katalog, übernimmt
/// sie in einen eigenen Lehrplan und richtet eine individuelle Belohnung ein – darf sie aber nicht ändern/löschen.
/// </summary>
public class SharedLibraryScenarioTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int id, HttpClient client)> RegisterAndLoginAsync(string name, string pin)
    {
        var reg = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/fathers", new { name, pin });
        reg.EnsureSuccessStatusCode();
        var id = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (id, await TestApi.FatherAsync(factory, id, pin));
    }

    /// <summary>Legt als Lehrer Fach → Kapitel → eine 9.-Klasse-Gymnasium-Vokabelübung an; liefert die Ids.</summary>
    private static async Task<(int subjectId, int chapterId, int exerciseId)> CreateGrade9GymExerciseAsync(HttpClient teacher)
    {
        var subjectId = await TestApi.IdAsync(await teacher.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Englisch (geteilt)" }));
        var chapterId = await TestApi.IdAsync(await teacher.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 5 – Global challenges", orderIndex = 5 }));
        var exerciseId = await TestApi.IdAsync(await teacher.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Vocabulary: The environment",
                orderIndex = 1,
                rewardPoints = 15,
                gradeMin = 9,
                gradeMax = 10,
                schoolTypes = "Gymnasium",
                source = "Green Line 5",
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[]
                    {
                        new { front = "sustainability", back = "Nachhaltigkeit" },
                        new { front = "pollution", back = "Umweltverschmutzung" },
                    },
                },
            }));
        return (subjectId, chapterId, exerciseId);
    }

    [Fact]
    public async Task Lehrer_ErstelltUebung_TraegtAutorschaftUndIstEditierbar()
    {
        var (teacherId, teacher) = await RegisterAndLoginAsync("Herr Schmidt", "7777");
        var (subjectId, _, exerciseId) = await CreateGrade9GymExerciseAsync(teacher);

        // Katalog-Suche des Lehrers: Klasse 9 + Gymnasium + Fach → seine Übung mit Attribution + IsOwn.
        var hits = await (await teacher.GetAsync(
                $"/api/v1/creator/exercises?subjectId={subjectId}&grade=9&schoolType=Gymnasium"))
            .Content.ReadFromJsonAsync<JsonElement>();

        var mine = hits.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == exerciseId);
        Assert.Equal(teacherId, mine.GetProperty("authorFatherId").GetInt32());
        Assert.Equal("Herr Schmidt", mine.GetProperty("authorName").GetString());
        JsonAssert.True(mine, "isOwn");
    }

    [Fact]
    public async Task AndererVater_FindetUndUebernimmtUebung_KannSieAberNichtAendern()
    {
        // 1) Lehrer erstellt die Übung.
        var (teacherId, teacher) = await RegisterAndLoginAsync("Frau Meier", "7777");
        var (subjectId, chapterId, exerciseId) = await CreateGrade9GymExerciseAsync(teacher);

        // 2) Ein anderer Vater registriert sich, legt ein Kind an.
        var (_, other) = await RegisterAndLoginAsync("Papa Müller", "8888");
        var childId = await TestApi.IdAsync(await other.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Tom", grade = 9, schoolType = "Gymnasium" }));

        // 3) Der andere Vater findet die Lehrer-Übung im globalen Katalog – als fremd markiert.
        var hits = await (await other.GetAsync(
                $"/api/v1/creator/exercises?subjectId={subjectId}&grade=9&schoolType=Gymnasium"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var found = hits.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == exerciseId);
        Assert.Equal("Frau Meier", found.GetProperty("authorName").GetString());
        JsonAssert.False(found, "isOwn");

        // 4) Er darf sie NICHT ändern oder löschen (Schutz der fremden Autorenarbeit).
        var putBody = new
        {
            title = "Gehackt",
            orderIndex = 1,
            rewardPoints = 999,
            config = new { direction = "front-to-back", items = new[] { new { front = "x", back = "y" } } },
        };
        var put = await other.PutAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}", putBody);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);

        var del = await other.DeleteAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);

        // 5) Aber er darf sie in einen EIGENEN Lehrplan übernehmen (Katalog global nutzbar).
        var planId = await TestApi.IdAsync(await other.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Toms Englisch-Plan", method = "Vocabulary", durationDays = 14 }));
        var posRes = await other.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, useLeitner = true });
        Assert.Equal(HttpStatusCode.Created, posRes.StatusCode);
        var pos = await posRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(exerciseId, pos.GetProperty("exerciseId").GetInt32());

        // 6) Und er richtet in seinem Familien-Shop ein Angebot für sein Kind ein.
        var listingId = await TestApi.CreateShopListingAsync(other, "GAME-1", coinPrice: 300, unitsPerPurchase: 60,
            stock: 2, articleTitle: "Zockzeit", listingTitle: "1 Stunde Zocken", unitType: "Minute", actionType: "Zocken");
        Assert.True(listingId > 0);

        // 7) Der Lehrer selbst darf seine Übung weiterhin ändern.
        var teacherPut = await teacher.PutAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}",
            new
            {
                title = "Vocabulary: The environment (überarbeitet)",
                orderIndex = 1,
                rewardPoints = 18,
                gradeMin = 9,
                gradeMax = 10,
                schoolTypes = "Gymnasium",
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "waste", back = "Abfall" } } },
            });
        Assert.Equal(HttpStatusCode.OK, teacherPut.StatusCode);
        _ = teacherId;
    }

    [Fact]
    public async Task GeseedeteSystemUebung_IstFuerNiemandenEditierbar()
    {
        // Die geseedeten Katalog-Übungen (Englisch „Begrüßungen") haben keinen Autor → nicht editierbar.
        var (_, father) = await RegisterAndLoginAsync("Irgendwer", "8888");
        var hits = await (await father.GetAsync("/api/v1/creator/exercises?search=Begrüßungen"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var seeded = hits.EnumerateArray().First();
        Assert.True(seeded.GetProperty("authorFatherId").ValueKind == JsonValueKind.Null);

        var subjectId = seeded.GetProperty("subjectId").GetInt32();
        var chapterId = seeded.GetProperty("chapterId").GetInt32();
        var id = seeded.GetProperty("id").GetInt32();
        var del = await father.DeleteAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
    }
}
