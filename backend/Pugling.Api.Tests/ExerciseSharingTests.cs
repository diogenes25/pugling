using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Material <b>zurückziehen</b> – die Gegenbewegung zum Veröffentlichen.
///
/// Nötig, weil Löschen bei einer benutzten Übung verweigert wird (der FK <c>PlanPosition→Exercise</c> ist
/// <c>Restrict</c>), und das ist richtig: laufende Pflichten dürfen nicht unter dem Kind wegbrechen. Ein
/// Creator – ein Lehrer oder eine KI-Creator-App – braucht trotzdem einen Weg, eigenes Material aus dem
/// Verkehr zu nehmen. Der Schalter ist <c>ExecutePublic</c>; er greift beim <b>Zuweisen</b>, nicht beim
/// Spielen.
/// </summary>
public class ExerciseSharingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private async Task<(HttpClient client, int fatherId)> NewFatherAsync(string name, string pin)
    {
        var id = await TestApi.IdAsync(await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/supervisor/fathers", new { name, pin }));
        return (await TestApi.FatherAsync(_factory, id, pin), id);
    }

    /// <summary>Legt eine gefüllte, öffentlich zuweisbare Vokabelübung an und liefert ihre Ids.</summary>
    private static async Task<(int subjectId, int chapterId, int exerciseId)> PublishVocabAsync(HttpClient creator)
    {
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Sharing-Fach {Guid.NewGuid():N}" }));
        var chapterId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new
            {
                title = "Veröffentlicht",
                orderIndex = 1,
                rewardPoints = 10,
                executePublic = true,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "cloud", back = "Wolke" } },
                },
            }));
        return (subjectId, chapterId, exerciseId);
    }

    private async Task<(int planId, HttpClient family)> FamilyWithPlanAsync(string name, string pin)
    {
        var (family, _) = await NewFatherAsync(name, pin);
        var childId = await TestApi.IdAsync(await family.PostAsJsonAsync(
            "/api/v1/supervisor/children", new { name = "Kind", pin = "7777" }));
        var planId = await TestApi.IdAsync(await family.PostAsJsonAsync(
            "/api/v1/supervisor/study-plans", new { childId, title = "Plan", durationDays = 10 }));
        return (planId, family);
    }

    [Fact]
    public async Task Zurueckziehen_StopptNeueZuweisungen_LaesstLaufendeUnberuehrt()
    {
        var (creator, _) = await NewFatherAsync("Zurückzieher", "1111");
        var (_, _, exerciseId) = await PublishVocabAsync(creator);

        // Familie A weist zu, WÄHREND die Übung öffentlich ist.
        var (planA, familyA) = await FamilyWithPlanAsync("Familie A", "2222");
        var posA = await familyA.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planA}/positions",
            new { exerciseId, cadence = "Daily" });
        Assert.Equal(HttpStatusCode.Created, posA.StatusCode);

        // Der Owner zieht zurück.
        var res = await creator.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("executePublic").GetBoolean());
        Assert.Equal(1, body.GetProperty("grantCount").GetInt32());   // nur noch der Owner darf zuweisen

        // Familie B kommt jetzt nicht mehr dran …
        var (planB, familyB) = await FamilyWithPlanAsync("Familie B", "3333");
        var posB = await familyB.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}/positions",
            new { exerciseId, cadence = "Daily" });
        // 403, nicht 400: die Ablehnung ist eine Rechtefrage (ApiErrors.ExerciseNotExecutable).
        Assert.Equal(HttpStatusCode.Forbidden, posB.StatusCode);
        Assert.Equal("exercise_not_executable",
            (await posB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // … und die Zusage, auf die es ankommt: die laufende Position von Familie A bleibt bestehen.
        var stillThere = await familyA.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/supervisor/study-plans/{planA}/positions");
        Assert.Single(stillThere!);
        Assert.Equal(exerciseId, stillThere![0].GetProperty("exerciseId").GetInt32());
    }

    [Fact]
    public async Task WiederFreigeben_MachtSieErneutZuweisbar()
    {
        var (creator, _) = await NewFatherAsync("Freigeber", "4444");
        var (_, _, exerciseId) = await PublishVocabAsync(creator);
        (await creator.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false })).EnsureSuccessStatusCode();

        var (planId, family) = await FamilyWithPlanAsync("Familie C", "5555");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await family.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
                new { exerciseId, cadence = "Daily" })).StatusCode);

        (await creator.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created,
            (await family.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
                new { exerciseId, cadence = "Daily" })).StatusCode);
    }

    /// <summary>
    /// Ein ausdrückliches Recht schlägt die Rücknahme – genau dafür ist das RWX-Modell da: der Owner nimmt
    /// Material aus dem allgemeinen Verkehr, kann es aber weiter gezielt weitergeben.
    /// </summary>
    [Fact]
    public async Task ZurueckgezogenAberMitExecuteRecht_BleibtZuweisbar()
    {
        var (creator, _) = await NewFatherAsync("Owner mit Kreis", "6666");
        var (_, _, exerciseId) = await PublishVocabAsync(creator);
        var (planId, family) = await FamilyWithPlanAsync("Eingeweihte Familie", "7777");
        var familyFatherId = (await family.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("fatherId").GetInt32();

        (await creator.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false })).EnsureSuccessStatusCode();
        (await creator.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants",
            new { creatorId = familyFatherId, permission = "Execute" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Created,
            (await family.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
                new { exerciseId, cadence = "Daily" })).StatusCode);
    }

    [Fact]
    public async Task NurDerOwnerDarfUmschalten()
    {
        var (creator, _) = await NewFatherAsync("Owner", "8888");
        var (_, _, exerciseId) = await PublishVocabAsync(creator);
        var (stranger, strangerId) = await NewFatherAsync("Fremder", "9911");

        // Auch ein Write-Grantee darf Inhalte pflegen, aber nicht über die Weitergabe entscheiden.
        (await creator.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants",
            new { creatorId = strangerId, permission = "Write" })).EnsureSuccessStatusCode();

        var res = await stranger.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("not_owner", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
