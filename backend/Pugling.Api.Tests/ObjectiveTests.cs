using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// "Big goals" (objectives, the child-friendly OKR core): the father sets a time-boxed bracket over
/// measurable milestones (key results), and progress is computed live from learning state + class test
/// grade. Covers: creation/evaluation, the idempotent reward (milestone chunk + full completion, coins
/// for Committed / gems for Stretch), the grade anchor (ClassTestGrade), and validation/roles. Every
/// test uses a fresh child (isolated wallet) so that absolute balances can be checked.
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

        // A goal that is reachable at once: "at most 0 weak words" is vacuously met without any learning state.
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
        Assert.False(created.GetProperty("rewarded").GetBoolean()); // not settled yet on creation

        // The child's login settles the reward: 5 (milestone) + 20 (completion) = 25 coins, no gems.
        var child = await TestApi.ChildAsync(factory, childId, "7101");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(25, wallet.GetProperty("coins").GetInt32());
        Assert.Equal(0, wallet.GetProperty("gems").GetInt32());

        // Exactly two ObjectiveCoins entries (5 + 20).
        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        var objEntries = entries.EnumerateArray().Where(e => e.GetProperty("kind").GetString() == "ObjectiveCoins").ToList();
        Assert.Equal(2, objEntries.Count);
        Assert.Equal(25, objEntries.Sum(e => e.GetProperty("amount").GetInt32()));

        // The child's view: reached + rewarded.
        var mine = await child.GetFromJsonAsync<List<JsonElement>>("/api/v1/student/me/objectives");
        var o = Assert.Single(mine!);
        Assert.True(o.GetProperty("rewarded").GetBoolean());
        Assert.Equal("achieved", o.GetProperty("status").GetString());

        // A second login → no second payout (unique index + existence check).
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

        // Two milestones: one met at once (MaxWeakItems ≤ 0), one unreachable (100 % mastered without any learning state).
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

        // The login pays only the milestone bite (3 gems); the completion chunk stays out (not all reached).
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

        // The supervisor enters a 2.0 in the subject (which makes the status "Written").
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        (await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId,
            subjectId,
            title = "Vokabeltest Unit 3",
            scheduledDate = today,
            grade = 2.0,
        })).EnsureSuccessStatusCode();

        // Goal: grade ≤ 2.0 (target value 20 = grade×10). The 2.0 meets it.
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

        // The login credits the completion chunk (10 coins; no milestone bite configured).
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

        // An empty title → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        })).StatusCode);

        // ClassTestGrade with a chapter scope → 400 (grades hang on the subject).
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X",
            kind = "Committed",
            keyResults = new[] { new { subjectId, chapterId = 1, metric = "ClassTestGrade", targetValue = 20 } },
        })).StatusCode);

        // A ClassTestGrade target outside 10..60 → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "ClassTestGrade", targetValue = 5 } },
        })).StatusCode);

        // A percent metric above 100 → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "AvgMastery", targetValue = 150 } },
        })).StatusCode);

        // An "at least" metric with target 0 → 400 (it would otherwise be vacuously met = a free reward).
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new
        {
            title = "X",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MasteredPercent", targetValue = 0 } },
        })).StatusCode);

        // The child may not create goals (supervisor only).
        var child = await TestApi.ChildAsync(factory, childId, "7104");
        Assert.Equal(HttpStatusCode.Forbidden, (await child.PostAsJsonAsync(Url(childId), new
        {
            title = "X",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        })).StatusCode);

        // Another/non-existent child → 404 (the ownership filter).
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
            title = "Ziel mit Etappen",
            kind = "Committed",
            rewardOnComplete = 0,
            rewardPerKeyResult = 0,
        }))).GetProperty("id").GetInt32();

        var krUrl = $"{Url(childId)}/{objectiveId}/key-results";
        var kr = await JsonAsync(await father.PostAsJsonAsync(krUrl, new { subjectId, metric = "MaxWeakItems", targetValue = 0 }));
        var keyResultId = kr.GetProperty("id").GetInt32();
        Assert.Equal("achieved", kr.GetProperty("status").GetString());

        // Change the target value/title (the scope stays fixed).
        var patched = await JsonAsync(await father.PatchAsJsonAsync($"{krUrl}/{keyResultId}", new { metric = "MasteredPercent", targetValue = 80, title = "Beherrschen" }));
        Assert.Equal("MasteredPercent", patched.GetProperty("metric").GetString());
        Assert.Equal(80, patched.GetProperty("targetValue").GetInt32());
        Assert.Equal("open", patched.GetProperty("status").GetString());

        // Delete.
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{krUrl}/{keyResultId}")).StatusCode);
        var afterDelete = await father.GetFromJsonAsync<JsonElement>($"{Url(childId)}/{objectiveId}");
        Assert.Equal(0, afterDelete.GetProperty("totalCount").GetInt32());
    }

    // ─────────────────────────────────── B-104: same-milestone duplicate reports 409, not a bare 500

    [Fact]
    public async Task Dublette_InnerhalbEinesPosts_Meldet409_UndLegtKeinZielAn()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Dublette-Inline");
        var childId = await FreshChildIdAsync(father, "7401");

        var res = await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Zwei gleiche Etappen",
            kind = "Committed",
            keyResults = new[]
            {
                new { subjectId, metric = "MaxWeakItems", targetValue = 0 },
                new { subjectId, metric = "MaxWeakItems", targetValue = 0 },
            },
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_key_result", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // No half-written goal: the list stays empty.
        Assert.Equal(0, (await JsonAsync(await father.GetAsync(Url(childId)))).GetArrayLength());
    }

    [Fact]
    public async Task Dublette_AlsZweiterKeyResultPost_Meldet409()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Dublette-Post");
        var childId = await FreshChildIdAsync(father, "7402");

        var objectiveId = (await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Ziel",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        }))).GetProperty("id").GetInt32();

        var res = await father.PostAsJsonAsync($"{Url(childId)}/{objectiveId}/key-results",
            new { subjectId, metric = "MaxWeakItems", targetValue = 0 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_key_result", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Still exactly one milestone - the conflicting POST did not get stored.
        var objective = await father.GetFromJsonAsync<JsonElement>($"{Url(childId)}/{objectiveId}");
        Assert.Equal(1, objective.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Dublette_UeberPatchAufBestehendenMeilenstein_Meldet409_UndLaesstIhnUnveraendert()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Dublette-Patch");
        var childId = await FreshChildIdAsync(father, "7403");

        var objectiveId = (await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Ziel",
            kind = "Committed",
            keyResults = new[]
            {
                new { subjectId, metric = "MaxWeakItems", targetValue = 0 },
                new { subjectId, metric = "MasteredPercent", targetValue = 80 },
            },
        }))).GetProperty("id").GetInt32();
        var krUrl = $"{Url(childId)}/{objectiveId}/key-results";
        var listBefore = await father.GetFromJsonAsync<JsonElement>($"{Url(childId)}/{objectiveId}");
        var targetKr = listBefore.GetProperty("keyResults").EnumerateArray()
            .Single(k => k.GetProperty("metric").GetString() == "MasteredPercent");
        var targetKrId = targetKr.GetProperty("id").GetInt32();

        // Shifting the second milestone's metric onto the first milestone's metric collides (same scope).
        var res = await father.PatchAsJsonAsync($"{krUrl}/{targetKrId}", new { metric = "MaxWeakItems", targetValue = 0 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_key_result", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Unchanged: still MasteredPercent, not MaxWeakItems.
        var unchanged = await father.GetFromJsonAsync<JsonElement>($"{Url(childId)}/{objectiveId}");
        var stillThere = unchanged.GetProperty("keyResults").EnumerateArray().Single(k => k.GetProperty("id").GetInt32() == targetKrId);
        Assert.Equal("MasteredPercent", stillThere.GetProperty("metric").GetString());
    }

    [Fact]
    public async Task Meilenstein_BehaeltEigeneMetrik_KeineSelbstkollision()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-KeineSelbstkollision");
        var childId = await FreshChildIdAsync(father, "7404");

        var objectiveId = (await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title = "Ziel",
            kind = "Committed",
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0 } },
        }))).GetProperty("id").GetInt32();
        var krUrl = $"{Url(childId)}/{objectiveId}/key-results";
        var keyResultId = (await father.GetFromJsonAsync<JsonElement>($"{Url(childId)}/{objectiveId}"))
            .GetProperty("keyResults")[0].GetProperty("id").GetInt32();

        // Re-sending its own metric/scope (only the target value changes) must not collide with itself.
        var res = await father.PatchAsJsonAsync($"{krUrl}/{keyResultId}", new { metric = "MaxWeakItems", targetValue = 1 });
        res.EnsureSuccessStatusCode();
        Assert.Equal(1, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("targetValue").GetInt32());
    }

    // ─────────────────────────────────── List, delete, the child's single view (C3 coverage gap)

    /// <summary>Creates an objective with one key result and returns its id.</summary>
    private static async Task<int> AnlegenAsync(HttpClient father, int childId, int subjectId, string title)
    {
        var created = await JsonAsync(await father.PostAsJsonAsync(Url(childId), new
        {
            title,
            kind = "Committed",
            rewardOnComplete = 20,
            rewardPerKeyResult = 5,
            keyResults = new[] { new { subjectId, metric = "MaxWeakItems", targetValue = 0, title = "Keine Wackelkandidaten" } },
        }));
        return created.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Liste_Zeigt_Die_Ziele_Des_Kindes_Und_Loeschen_Entfernt_Sie()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Liste");
        var childId = await FreshChildIdAsync(father, "7301");
        var ersterId = await AnlegenAsync(father, childId, subjectId, "Erstes Ziel");
        await AnlegenAsync(father, childId, subjectId, "Zweites Ziel");

        var liste = await JsonAsync(await father.GetAsync(Url(childId)));
        Assert.Equal(2, liste.GetArrayLength());

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{Url(childId)}/{ersterId}")).StatusCode);
        Assert.Equal(1, (await JsonAsync(await father.GetAsync(Url(childId)))).GetArrayLength());
        // Deleting twice is the route's error case - not silently successful.
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"{Url(childId)}/{ersterId}")).StatusCode);
    }

    [Fact]
    public async Task Sohn_Liest_Sein_Ziel_Einzeln_Ein_Deaktiviertes_Nicht()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await SubjectAsync(father, "Obj-Sohnsicht");
        var childId = await FreshChildIdAsync(father, "7302");
        var objectiveId = await AnlegenAsync(father, childId, subjectId, "Mein großes Ziel");

        var sohn = await TestApi.ChildAsync(factory, childId, "7302");
        var eigenes = await JsonAsync(await sohn.GetAsync($"/api/v1/student/me/objectives/{objectiveId}"));
        Assert.Equal("Mein großes Ziel", eigenes.GetProperty("title").GetString());

        // If the supervisor deactivates the goal it disappears from the child's view - congruent with the list,
        // so that the single view does not show what the list hides.
        (await father.PatchAsJsonAsync($"{Url(childId)}/{objectiveId}", new { active = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await sohn.GetAsync($"/api/v1/student/me/objectives/{objectiveId}")).StatusCode);
    }
}
