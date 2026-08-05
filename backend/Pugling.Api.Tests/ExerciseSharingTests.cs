using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// <b>Retracting</b> material – the reverse move of publishing.
///
/// Necessary because deletion is refused for an exercise in use (the FK <c>PlanPosition→Exercise</c>
/// is <c>Restrict</c>), and that is correct: running mandatory goals must not collapse out from under
/// the child. A creator – a teacher or an AI creator app – still needs a way to take their own material
/// out of circulation. The switch is <c>ExecutePublic</c>; it takes effect on <b>assignment</b>, not on
/// play.
/// </summary>
public class ExerciseSharingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private async Task<(HttpClient client, int supervisorId)> NewFatherAsync(string name, string pin)
    {
        var id = await TestApi.IdAsync(await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/supervisor/adults", new { name, pin }));
        return (await TestApi.AdultAsync(_factory, id, pin), id);
    }

    /// <summary>Creates a populated, publicly assignable vocabulary exercise and returns its ids.</summary>
    private static async Task<(int subjectId, int seriesUnitId, int exerciseId)> PublishVocabAsync(HttpClient creator)
    {
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Sharing-Fach {Guid.NewGuid():N}" }));
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new
            {
                name = $"Sharing-Reihe {Guid.NewGuid():N}",
                publisher = (string?)null,
                subjectName = (string?)null,
                subjectId,
                schoolTypes = (string?)null,
                sourceLanguage = (string?)null,
                targetLanguage = (string?)null,
                notes = (string?)null,
            }));
        var seriesUnitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary",
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
        return (subjectId, seriesUnitId, exerciseId);
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

        // Family A assigns it WHILE the exercise is public.
        var (planA, familyA) = await FamilyWithPlanAsync("Familie A", "2222");
        var posA = await familyA.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planA}/positions",
            new { exerciseId, cadence = "Daily" });
        Assert.Equal(HttpStatusCode.Created, posA.StatusCode);

        // The owner withdraws it.
        var res = await creator.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("executePublic").GetBoolean());
        Assert.Equal(1, body.GetProperty("grantCount").GetInt32());   // only the owner may assign it now

        // Family B can no longer get at it …
        var (planB, familyB) = await FamilyWithPlanAsync("Familie B", "3333");
        var posB = await familyB.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}/positions",
            new { exerciseId, cadence = "Daily" });
        // 403, not 400: the rejection is a rights question (ApiErrors.ExerciseNotExecutable).
        Assert.Equal(HttpStatusCode.Forbidden, posB.StatusCode);
        Assert.Equal("exercise_not_executable",
            (await posB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // … and the promise that matters: family A's running position stays.
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
    /// An explicit permission overrides the retraction – that is exactly what the RWX model is for: the
    /// owner takes material out of general circulation but can still share it selectively.
    /// </summary>
    [Fact]
    public async Task ZurueckgezogenAberMitExecuteRecht_BleibtZuweisbar()
    {
        var (creator, _) = await NewFatherAsync("Owner mit Kreis", "6666");
        var (_, _, exerciseId) = await PublishVocabAsync(creator);
        var (planId, family) = await FamilyWithPlanAsync("Eingeweihte Familie", "7777");
        var familyFatherId = (await family.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("adultId").GetInt32();

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

        // A write grantee may maintain content too, but not decide about sharing.
        (await creator.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants",
            new { creatorId = strangerId, permission = "Write" })).EnsureSuccessStatusCode();

        var res = await stranger.PatchAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/sharing",
            new { executePublic = false });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("not_owner", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
