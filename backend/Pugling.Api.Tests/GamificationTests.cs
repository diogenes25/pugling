using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Checks missions (time-bound goals) and awards (badges): server-side evaluation of the
/// metrics from the position engine, idempotent reward (exactly once per period/award)
/// and the child's view under <c>api/me/missions</c> resp. <c>api/me/achievements</c>.
/// </summary>
public class GamificationTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Combo off (threshold 0), so that only mission/achievement points show up.
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess, comboThreshold: 0);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private static Task ReviewAsync(HttpClient child, int planId, int positionId, int sid, int itemIndex) =>
        TestApi.PositionReviewAsync(child, planId, positionId, sid, itemIndex, wasKnown: true);

    private static async Task<int> CountPointReasonAsync(HttpClient child, string reason)
    {
        var entries = await (await child.GetAsync("/api/v1/student/me/points/entries")).Content.ReadFromJsonAsync<JsonElement>();
        return entries.EnumerateArray()
            .Count(e => e.GetProperty("reason").GetString() == reason);
    }

    [Fact]
    public async Task Mission_BeiZielerreichung_EinmaligBelohnt_UndAlsErfuelltSichtbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var missionTitle = "TEST Tagesziel 2 Treffer";
        await father.PostAsJsonAsync("/api/v1/supervisor/children/1/missions", new
        {
            title = missionTitle,
            metric = "CorrectReviews",
            target = 2,
            period = "Daily",
            rewardPoints = 25,
        });

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        await ReviewAsync(child, planId, positionId, sid, 1); // goal (2) reached → evaluation after the review

        var missions = await (await child.GetAsync("/api/v1/student/me/missions")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = missions.EnumerateArray().First(m => m.GetProperty("title").GetString() == missionTitle);
        JsonAssert.True(mine, "completed");

        // Rewarded exactly once - even after further hits (idempotent per day).
        await ReviewAsync(child, planId, positionId, sid, 0);
        Assert.Equal(1, await CountPointReasonAsync(child, $"Mission erfüllt: {missionTitle}"));
    }

    /// <summary>
    /// The <b>one-off mission</b> (<c>OneOff</c>) has no period - its entry therefore carries
    /// <c>PeriodStart = null</c>, and that NULL is the discriminator of the two filtered unique indexes.
    /// The case needs a test of its own, because SQLite treats NULLs as <b>distinct</b>: if the index on
    /// <c>(MissionId, Period) WHERE PeriodStart IS NULL</c> went away, any number of one-off rewards would be
    /// allowed without anything turning red.
    /// <para>
    /// That is why the test checks <b>both</b>: that the evaluation does not book twice (the existence check in
    /// the code) <i>and</i> that the database rejects a second entry (the hard guarantee). Checking only the
    /// first half is the failure class "rule tested, edge case open" - the test would stay green if exactly the
    /// safeguard it is meant to prove were missing.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Einmal_Mission_WirdNichtDoppeltBelohnt_UndDieDatenbankHaeltDagegen()
    {
        var father = await TestApi.FatherAsync(factory);
        var missionTitle = "TEST Einmal 1 Treffer";
        var missionId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children/1/missions", new
        {
            title = missionTitle,
            metric = "CorrectReviews",
            target = 1,
            period = "OneOff",
            rewardPoints = 40,
        }));

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        await ReviewAsync(child, planId, positionId, sid, 0); // goal (1) reached

        // Further hits pay nothing more - the existence check bites.
        await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(1, await CountPointReasonAsync(child, $"Mission erfüllt: {missionTitle}"));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var gebucht = await db.MissionAwards.AsNoTracking().SingleAsync(a => a.MissionId == missionId);
        Assert.Equal(MissionPeriod.OneOff, gebucht.Period);
        Assert.Null(gebucht.PeriodStart); // no period - and exactly for that reason NULL

        // And the database allows no second one, although both rows carry NULL.
        db.MissionAwards.Add(new MissionAward
        {
            MissionId = missionId,
            Period = MissionPeriod.OneOff,
            PeriodStart = null,
            Points = 40,
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Auszeichnung_BeiSchwelle_EinmaligVerliehen_UndAlsErreichtSichtbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var title = "TEST Badge 1 Treffer";
        await father.PostAsJsonAsync("/api/v1/supervisor/children/1/achievements", new
        {
            title,
            icon = "⭐",
            metric = "CorrectReviews",
            threshold = 1,
            rewardPoints = 33,
        });

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        await ReviewAsync(child, planId, positionId, sid, 0); // threshold (1) reached

        var listRes = await child.GetAsync("/api/v1/student/me/achievements");
        Assert.True(listRes.Headers.Contains("X-Total-Count")); // the list is paged
        var achievements = await listRes.Content.ReadFromJsonAsync<JsonElement>();
        var mine = achievements.EnumerateArray().First(a => a.GetProperty("title").GetString() == title);
        JsonAssert.True(mine, "earned");
        Assert.Equal("⭐", mine.GetProperty("icon").GetString());

        // The single view returns the same award; an unknown id → 404.
        var achievementId = mine.GetProperty("id").GetInt32();
        var single = await (await child.GetAsync($"/api/v1/student/me/achievements/{achievementId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(title, single.GetProperty("title").GetString());
        JsonAssert.True(single, "earned");
        Assert.Equal(HttpStatusCode.NotFound,
            (await child.GetAsync("/api/v1/student/me/achievements/999999")).StatusCode);

        // Granted exactly once, even after further hits.
        await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(1, await CountPointReasonAsync(child, $"Auszeichnung erreicht: {title}"));
    }

    [Fact]
    public async Task Mission_NeueWoerter_ZaehltErstmalsEingefuehrteInhalte()
    {
        var father = await TestApi.FatherAsync(factory);
        var missionTitle = "TEST 2 neue Wörter";
        await father.PostAsJsonAsync("/api/v1/supervisor/children/1/missions", new
        {
            title = missionTitle,
            metric = "NewWords",
            target = 2,
            period = "Daily",
            rewardPoints = 15,
        });

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        // Working on two so far unknown contents for the first time → 2 words introduced today (ProgressMetric.NewWords).
        await ReviewAsync(child, planId, positionId, sid, 0);
        await ReviewAsync(child, planId, positionId, sid, 1);

        var missions = await (await child.GetAsync("/api/v1/student/me/missions")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = missions.EnumerateArray().First(m => m.GetProperty("title").GetString() == missionTitle);
        JsonAssert.True(mine, "completed");
    }

    [Fact]
    public async Task Mission_GeuebteMinuten_ZaehltAktiveSekunden()
    {
        var father = await TestApi.FatherAsync(factory);
        var missionTitle = "TEST 1 Minute geübt";
        await father.PostAsJsonAsync("/api/v1/supervisor/children/1/missions", new
        {
            title = missionTitle,
            metric = "MinutesPracticed",
            target = 1,
            period = "Daily",
            rewardPoints = 10,
        });

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        // Credit 120 active seconds (= 2 minutes, ProgressMetric.MinutesPracticed = sum/60) …
        (await child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sid}/heartbeat",
            new { seconds = 120, active = true })).EnsureSuccessStatusCode();
        // … the mission evaluation runs when the session ends.
        (await child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sid}/end", new { })).EnsureSuccessStatusCode();

        var missions = await (await child.GetAsync("/api/v1/student/me/missions")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = missions.EnumerateArray().First(m => m.GetProperty("title").GetString() == missionTitle);
        JsonAssert.True(mine, "completed");
    }

    [Fact]
    public async Task Missionen_NurEigene_FremdesKindBekommt404()
    {
        var father = await TestApi.FatherAsync(factory);
        // Child 999 does not belong to the adult → the ChildOwnershipFilter returns 404 (no enumeration).
        var res = await father.GetAsync("/api/v1/supervisor/children/999/missions");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, res.StatusCode);
    }
}
