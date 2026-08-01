using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Usage display and deletion check must answer **the same** question consistently.
///
/// Previously they didn't: <c>usage</c> filtered by the creator's own children, while the deletion check looked
/// globally. If an exercise was sitting in the study plan of a child supervised by someone else, the display
/// reported "nowhere" and deletion still failed with <c>409</c> - the author had no way to find the reason
/// (remark 14, spotted on an exercise that sat in an active plan of a third account).
///
/// The setup here is exactly that constellation: father A **owns** the exercise, father B **supervises** the
/// child who has it in their plan.
/// </summary>
public class ExerciseUsageScopeTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private async Task<HttpClient> NewFatherAsync(string name, string pin)
    {
        var id = await TestApi.IdAsync(await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/supervisor/adults", new { name, pin }));
        return await TestApi.FatherAsync(_factory, id, pin);
    }

    [Fact]
    public async Task FremdeVerwendung_WirdAlsZahlGenannt_UndBlocktDasLoeschen()
    {
        // Adult A creates the exercise (and is thereby its owner).
        var ownerPin = "3141";
        var owner = await NewFatherAsync("Übungs-Owner", ownerPin);
        var subjectId = await TestApi.IdAsync(await owner.PostAsJsonAsync("/api/v1/creator/subjects", new { name = $"Scope-Fach {Guid.NewGuid():N}" }));
        var chapterId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new
            {
                title = "Geteilte Wörter",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "bridge", back = "Brücke" } },
                },
            }));

        // Adult B supervises a child of their own and takes the (publicly executable) exercise into their plan.
        var other = await NewFatherAsync("Fremder Betreuer", "2718");
        var childId = await TestApi.IdAsync(await other.PostAsJsonAsync(
            "/api/v1/supervisor/children", new { name = "Fremdkind", pin = "4444" }));
        var planId = await TestApi.IdAsync(await other.PostAsJsonAsync(
            "/api/v1/supervisor/study-plans", new { childId, title = "Fremder Plan", durationDays = 10 }));
        (await other.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" })).EnsureSuccessStatusCode();

        // From the owner's perspective: no usage of their OWN - but the foreign one is named as a number.
        var usage = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Empty(usage.GetProperty("plans").EnumerateArray());
        Assert.Empty(usage.GetProperty("classTests").EnumerateArray());
        Assert.Equal(1, usage.GetProperty("otherLearnersCount").GetInt32());

        // And the delete fails - with a message that names the same number instead of staying silent.
        var del = await owner.DeleteAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        var problem = await del.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_in_use", problem.GetProperty("code").GetString());
        var detail = problem.GetProperty("detail").GetString()!;
        Assert.Contains("1 usage outside your care", detail);
        // And their own side must not appear with it - the owner has no usage of their own here.
        Assert.DoesNotContain("of yours", detail);

        // The counter-check from adult B's perspective: for them it is a perfectly normal, visible usage.
        var otherUsage = await other.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Single(otherUsage.GetProperty("plans").EnumerateArray());
        Assert.Equal(0, otherUsage.GetProperty("otherLearnersCount").GetInt32());
    }

    [Fact]
    public async Task EigeneVerwendung_WirdWeiterhinBenanntUndNichtAlsFremdGezaehlt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "castle", "Burg");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/supervisor/study-plans", new { childId = 1, title = "Eigener Plan", durationDays = 10 }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" })).EnsureSuccessStatusCode();

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Single(usage.GetProperty("plans").EnumerateArray());
        // The core of the separation: the own usage must NOT be counted as "foreign".
        Assert.Equal(0, usage.GetProperty("otherLearnersCount").GetInt32());
    }

    /// <summary>
    /// The case the number actually exists for: a <b>creator without their own children</b> - a teacher or
    /// an AI creator app. Their two lists can never fill up, because they supervise nobody. What is counted
    /// are <b>children</b>, not usage spots: two positions in the same child's plan remain one user.
    /// </summary>
    [Fact]
    public async Task CreatorOhneKinder_ZaehltNutzendeKinder_NichtVerwendungsstellen()
    {
        var creator = await NewFatherAsync("Nur-Creator", "1618");
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = $"Lehrer-Fach {Guid.NewGuid():N}" }));
        var chapterId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new
            {
                title = "Lehrer-Material",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "island", back = "Insel" } },
                },
            }));
        // They supervise nobody - that is the precondition, not an intermediate state.
        Assert.Empty((await creator.GetFromJsonAsync<List<JsonElement>>("/api/v1/supervisor/children"))!);

        // One family takes the material into TWO plans of the same child.
        var family = await NewFatherAsync("Nutzende Familie", "2358");
        var childId = await TestApi.IdAsync(await family.PostAsJsonAsync(
            "/api/v1/supervisor/children", new { name = "Lernkind", pin = "5555" }));
        foreach (var title in new[] { "Plan A", "Plan B" })
        {
            var planId = await TestApi.IdAsync(await family.PostAsJsonAsync(
                "/api/v1/supervisor/study-plans", new { childId, title, durationDays = 10 }));
            (await family.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
                new { exerciseId, cadence = "Daily" })).EnsureSuccessStatusCode();
        }

        var usage = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Empty(usage.GetProperty("plans").EnumerateArray());
        // ONE child, although there are two usage sites - otherwise the number would mislead them.
        Assert.Equal(1, usage.GetProperty("otherLearnersCount").GetInt32());

        // The delete stays blocked and names the *sites* - that is where somebody would have to clean up.
        var del = await creator.DeleteAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        var detail = (await del.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()!;
        Assert.Contains("2 usages outside your care", detail);
    }

    [Fact]
    public async Task UnbenutzteUebung_MeldetNullUndLaesstSichLoeschen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "harbour", "Hafen");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var s = detail.GetProperty("subjectId").GetInt32();
        var c = detail.GetProperty("chapterId").GetInt32();

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Equal(0, usage.GetProperty("otherLearnersCount").GetInt32());

        var del = await father.DeleteAsync($"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }
}
