using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Integration tests of the server-side anti-cheating guarantees in the position engine.</summary>
public class AntiCheatTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int planId, int positionId)> SetupAsync(int stage = (int)TestStage.SelfAssess)
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        return TestApi.SeedLeitnerPosition(factory, exerciseId, stage);
    }

    [Fact]
    public async Task Heartbeat_ClamptUebertriebeneSekunden()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        var sid = await TestApi.StartPositionSessionAsync(father, planId, positionId);

        var hb = await father.PostAsJsonAsync(
            $"{TestApi.PracticeBase(planId, positionId)}/{sid}/heartbeat", new { seconds = 1200, active = true });
        var session = await hb.Content.ReadFromJsonAsync<JsonElement>();

        // 1200 s would be 20 min; at most 120 s per heartbeat can be credited.
        Assert.Equal(120, session.GetProperty("activeSeconds").GetInt32());
    }

    [Fact]
    public async Task Heartbeat_InaktivOderNichtPositiv_RechnetKeineZeitAn()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        var sid = await TestApi.StartPositionSessionAsync(father, planId, positionId);
        var url = $"{TestApi.PracticeBase(planId, positionId)}/{sid}/heartbeat";

        // Paused (active:false): the seconds must not count despite being sent - otherwise time spent clicked
        // away/in the background would count as practice time (anti time cheat).
        var paused = await (await father.PostAsJsonAsync(url, new { seconds = 90, active = false }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, paused.GetProperty("activeSeconds").GetInt32());

        // A non-positive heartbeat (0 s) does not either: the condition Seconds > 0 drops it silently.
        var zero = await (await father.PostAsJsonAsync(url, new { seconds = 0, active = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, zero.GetProperty("activeSeconds").GetInt32());
    }

    [Fact]
    public async Task Sohn_KannTeststufeNichtWaehlen_FahrplanStufeErzwungen()
    {
        var (planId, positionId) = await SetupAsync(stage: (int)TestStage.SelfAssess);
        var child = await TestApi.ChildAsync(factory);

        // The child requests the free display stage "ShowBoth" (1) …
        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { stage = (int)TestStage.ShowBoth });
        res.EnsureSuccessStatusCode();
        var attempt = await res.Content.ReadFromJsonAsync<JsonElement>();

        // … but the position/schedule stage is enforced (SelfAssess = 2).
        Assert.Equal((int)TestStage.SelfAssess, attempt.GetProperty("stage").GetInt32());
    }

    [Fact]
    public async Task Sohn_KannFremdenTagNichtNachtragen_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { day = yesterday });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Vater_DarfFremdenTagNachtragen()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        var res = await father.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { day = yesterday });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        // The status alone was the assurance - and that was too little: shortening `var day = dto.Day ?? today`
        // to `var day = today` stayed green (docs/testplan.md, injection D11). Catching up is the way to heal a
        // missed mandatory period; if it landed on "today", yesterday's penalty would remain. So the booked day
        // has to be read back, not just the success.
        var attempt = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(yesterday, attempt.GetProperty("day").GetString());
    }

    [Fact]
    public async Task Sohn_KannInaktivenPlanNichtUeben_403()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // No cherry-picking: the child can no longer practice the deactivated plan.
        var child = await TestApi.ChildAsync(factory);
        var res = await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    /// <summary>
    /// The end of a session is not just a read: it books the position's goal points, and those do not check
    /// <c>Active</c> themselves. Without this guard a plan deactivated mid-round could still be cashed in.
    /// </summary>
    [Fact]
    public async Task Sohn_KannLaufendeSitzungAufInaktivemPlanNichtAbschliessen_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        // Start the session WHILE the plan is still playable.
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        var res = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Empty(db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId));
    }

    [Fact]
    public async Task Sohn_KannInaktivenPlanNichtTesten_403()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        var child = await TestApi.ChildAsync(factory);
        var res = await child.PostAsJsonAsync($"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Vater_DarfInaktivenPlanTrotzdemDurchspielen()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // The supervisor stays exempt for preview/catch-up - even with an inactive plan.
        var res = await father.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        // "Playing through" means getting cards: the 201 alone only proves that the session was created.
        // Without this line the test stayed green even if the plan barrier only bites during play - the
        // supervisor's preview would then be an empty shell (docs/testplan.md, stage 1a).
        var sid = await TestApi.IdAsync(res);
        var next = await father.GetFromJsonAsync<JsonElement>(
            $"{TestApi.PracticeBase(planId, positionId)}/{sid}/next");
        JsonAssert.False(next, "done");
        Assert.NotEqual(JsonValueKind.Null, next.GetProperty("card").ValueKind);
    }

    [Fact]
    public async Task Sohn_ListeZeigtInaktivenPlanNicht()
    {
        var (planId, _) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        var child = await TestApi.ChildAsync(factory);
        var plans = await (await child.GetAsync("/api/v1/supervisor/study-plans")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.DoesNotContain(plans.EnumerateArray(), p => p.GetProperty("id").GetInt32() == planId);
    }

    [Fact]
    public async Task Sohn_StudentPlanListe_ZeigtNurSpielbarenPlan()
    {
        var (planId, _) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        // A namespace-faithful discovery entry: the child finds its playable plan under student/.
        var plans = await (await child.GetAsync("/api/v1/student/study-plans")).Content.ReadFromJsonAsync<JsonElement>();
        var plan = Assert.Single(plans.EnumerateArray(), p => p.GetProperty("id").GetInt32() == planId);
        JsonAssert.True(plan, "isPlayable");
    }

    [Fact]
    public async Task Sohn_StudentPlanListe_ZeigtInaktivenPlanNicht()
    {
        var (planId, _) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // No cherry-picking: the deactivated plan does not show up in the child's discovery.
        var child = await TestApi.ChildAsync(factory);
        var plans = await (await child.GetAsync("/api/v1/student/study-plans")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.DoesNotContain(plans.EnumerateArray(), p => p.GetProperty("id").GetInt32() == planId);
    }

    [Fact]
    public async Task Vater_DarfStudentPlanListeNicht_403()
    {
        await SetupAsync();
        var father = await TestApi.FatherAsync(factory);

        // student/study-plans is child-only (a role gate); the supervisor reads plans under supervisor/.
        var res = await father.GetAsync("/api/v1/student/study-plans");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AktivierenEinesPlans_DeaktiviertAndereDesKindes()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planA, _) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess);
        var (planB, _) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess);

        // Both are seeded active directly; activating A must deactivate B (one active plan per child).
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planA}", new { active = true })).EnsureSuccessStatusCode();

        var b = await (await father.GetAsync($"/api/v1/supervisor/study-plans/{planB}")).Content.ReadFromJsonAsync<JsonElement>();
        var a = await (await father.GetAsync($"/api/v1/supervisor/study-plans/{planA}")).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(b, "active");
        JsonAssert.True(a, "active");

        // The IsPlayable affordance follows the anti-cheat rule: only the active plan (within its runtime) is playable.
        JsonAssert.True(a, "isPlayable");
        JsonAssert.False(b, "isPlayable");
    }

    [Fact]
    public async Task IsPlayable_False_WennAktivAberNochNichtGestartet()
    {
        var father = await TestApi.FatherAsync(factory);
        // An active plan whose runtime only starts in the future → not (yet) playable today.
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7).ToString("yyyy-MM-dd");
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId = 1, title = "Zukunfts-Plan", startDate = future, durationDays = 5 }));

        var plan = await (await father.GetAsync($"/api/v1/supervisor/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(plan, "active");
        JsonAssert.False(plan, "isPlayable");
    }

    [Fact]
    public async Task Sohn_KannMitOffenerSession_InaktivenPlanNichtWeiterUeben_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        // The session is started while the plan is still active …
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        // … then the supervisor deactivates the plan (or it expires).
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // Through the still open session the child must not keep scoring points.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, wasKnown: true);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Sohn_KannOffenenTestversuch_AufInaktivemPlanNichtAbschliessen_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        // The test attempt is started while the plan is active …
        var start = await child.PostAsJsonAsync($"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { });
        start.EnsureSuccessStatusCode();
        var attempt = await start.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();

        // … then the plan is deactivated.
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // Submitting (and scoring) the open attempt must fail (the plan check runs before any grading).
        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit",
            new { answers = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Hoerstufe_RueckwaertsRichtung_GibtKeineAudioquellePreis()
    {
        var father = await TestApi.FatherAsync(factory);
        var (id, key) = await TestApi.CreateStoreVocabAsync(father, "hello", "hallo");
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { pronunciationAudioUrl = "https://example.test/hello.mp3" })).EnsureSuccessStatusCode();

        // A reverse exercise: after the swap the (spoken) word is the solution. The listening stage must then
        // NOT include the audio source, otherwise it would speak the answer.
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Audio-Ref" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "U1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Rueckwaerts",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "back-to-front", refs = new[] { new { vocabularyId = id } } },
            }));

        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage=5");
        Assert.Equal(5, data.GetProperty("stage").GetInt32());
        var item = data.GetProperty("items").EnumerateArray().First();
        Assert.Equal(JsonValueKind.Null, item.GetProperty("audioUrl").ValueKind);
    }
}
