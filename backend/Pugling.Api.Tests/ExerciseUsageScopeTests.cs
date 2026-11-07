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
        return await TestApi.AdultAsync(_factory, id, pin);
    }

    [Fact]
    public async Task FremdeVerwendung_WirdAlsZahlGenannt_UndBlocktDasLoeschen()
    {
        // Adult A creates the exercise (and is thereby its owner).
        var ownerPin = "3141";
        var owner = await NewFatherAsync("Übungs-Owner", ownerPin);
        var subjectId = await TestApi.IdAsync(await owner.PostAsJsonAsync("/api/v1/creator/subjects", new { name = $"Scope-Fach {Guid.NewGuid():N}" }));
        var seriesId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = $"Scope-Reihe {Guid.NewGuid():N}", subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
        var exerciseId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary",
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
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}");
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
        var father = await TestApi.AdultAsync(_factory);
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
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = $"Lehrer-Reihe {Guid.NewGuid():N}", subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary",
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
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        var detail = (await del.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()!;
        Assert.Contains("2 usages outside your care", detail);
    }

    [Fact]
    public async Task UnbenutzteUebung_MeldetNullUndLaesstSichLoeschen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "harbour", "Hafen");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var seriesId = detail.GetProperty("seriesId").GetInt32();
        var seriesUnitId = detail.GetProperty("seriesUnitId").GetInt32();

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Equal(0, usage.GetProperty("otherLearnersCount").GetInt32());

        var del = await father.DeleteAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    /// <summary>
    /// B-409, case 1: a goal milestone of an own child blocks deleting the exercise, so the usage display names it.
    /// Before, the display had no goal list and the panel promised "deleting is possible" while the delete was a 409.
    /// Red if the goal list is dropped or loses the exercise filter.
    /// </summary>
    [Fact]
    public async Task EigeneZielEtappe_WirdUnterVerwendungGenannt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exercise = await VocabExerciseAsync(father, "lighthouse", "Leuchtturm");
        var title = TestApi.UniqueName("Ziel");
        await GoalOnExerciseAsync(father, 1, exercise, title);

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exercise.Id}/usage");
        var goal = Assert.Single(usage.GetProperty("goals").EnumerateArray());
        Assert.Equal(title, goal.GetProperty("objectiveTitle").GetString());
        Assert.Equal("Etappe Leuchtturm", goal.GetProperty("keyResultTitle").GetString());
        Assert.Equal(1, goal.GetProperty("childId").GetInt32());
        Assert.False(string.IsNullOrEmpty(goal.GetProperty("childName").GetString()));
        Assert.Equal(0, usage.GetProperty("otherLearnersCount").GetInt32());

        // The comparison path: the delete lock counts the same milestone as "of yours".
        var del = await father.DeleteAsync(
            $"/api/v1/creator/textbook-series/{exercise.SeriesId}/units/{exercise.SeriesUnitId}/vocabulary/{exercise.Id}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        Assert.Contains("1 objective milestone of yours", (await del.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
    }

    /// <summary>
    /// B-409, case 2: the milestone of a child the caller does not supervise stays a number - no title, no name.
    /// Red if the goal list loses its supervision filter (then the foreign goal would be listed by name). The count of 1
    /// already held before B-409 (HiddenLearners included foreign milestones); here it only guards the number.
    /// </summary>
    [Fact]
    public async Task FremdeZielEtappe_BleibtEineZahl()
    {
        var owner = await NewFatherAsync("Ziel-Owner", "5151");
        var exercise = await VocabExerciseAsync(owner, "anchor", "Anker");
        var other = await NewFatherAsync("Fremder Ziel-Betreuer", "6161");
        var childId = await TestApi.IdAsync(await other.PostAsJsonAsync(
            "/api/v1/supervisor/children", new { name = "Zielkind", pin = "7777" }));
        await GoalOnExerciseAsync(other, childId, exercise, TestApi.UniqueName("Fremdziel"));

        var usage = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exercise.Id}/usage");
        Assert.Empty(usage.GetProperty("goals").EnumerateArray());
        Assert.Equal(1, usage.GetProperty("otherLearnersCount").GetInt32());

        // Counter-view: for the supervising adult it is an ordinary, named usage.
        var otherUsage = await other.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exercise.Id}/usage");
        Assert.Single(otherUsage.GetProperty("goals").EnumerateArray());
        Assert.Equal(0, otherUsage.GetProperty("otherLearnersCount").GetInt32());
    }

    /// <summary>
    /// B-409, case 3: a milestone on a sibling exercise of the same unit does not concern this exercise.
    /// Red if the goal list filters by unit instead of by exercise.
    /// </summary>
    [Fact]
    public async Task ZielEtappe_EinerAnderenUebung_ZaehltNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exercise = await VocabExerciseAsync(father, "harbour wall", "Hafenmauer");
        var sibling = await VocabExerciseAsync(father, "quay", "Kai", exercise);
        await GoalOnExerciseAsync(father, 1, sibling, TestApi.UniqueName("Nachbarziel"));

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exercise.Id}/usage");
        Assert.Empty(usage.GetProperty("goals").EnumerateArray());
        var siblingUsage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{sibling.Id}/usage");
        Assert.Single(siblingUsage.GetProperty("goals").EnumerateArray());
    }

    /// <summary>
    /// B-409, case 3b: a milestone on the exercise's whole unit (no exercise set) does not block deleting the exercise,
    /// so it is not a usage of it - the contract promises exactly this exclusion. Red if the goal list also takes the
    /// milestones of the exercise's unit.
    /// </summary>
    [Fact]
    public async Task ZielEtappe_AufDieGanzeUnit_IstKeineVerwendungDerUebung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exercise = await VocabExerciseAsync(father, "pier", "Mole");
        await GoalAsync(father, 1, exercise, TestApi.UniqueName("Unitziel"), exerciseId: null);

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exercise.Id}/usage");
        Assert.Empty(usage.GetProperty("goals").EnumerateArray());

        // The comparison path: deleting the exercise is not blocked by the unit milestone.
        var del = await father.DeleteAsync(
            $"/api/v1/creator/textbook-series/{exercise.SeriesId}/units/{exercise.SeriesUnitId}/vocabulary/{exercise.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    private sealed record ExerciseRef(int Id, int SubjectId, int SeriesId, int SeriesUnitId);

    /// <summary>A vocabulary exercise in a fresh unit, or - with <paramref name="sameUnitAs"/> - next to another one.</summary>
    private static async Task<ExerciseRef> VocabExerciseAsync(HttpClient creator, string front, string back, ExerciseRef? sameUnitAs = null)
    {
        int subjectId, seriesId, seriesUnitId;
        if (sameUnitAs is null)
        {
            subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
                "/api/v1/creator/subjects", new { name = TestApi.UniqueName("Ziel-Fach") }));
            (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        }
        else
        {
            (subjectId, seriesId, seriesUnitId) = (sameUnitAs.SubjectId, sameUnitAs.SeriesId, sameUnitAs.SeriesUnitId);
        }

        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary",
            new
            {
                title = $"Wörter {front}",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front, back } },
                },
            }));
        return new ExerciseRef(id, subjectId, seriesId, seriesUnitId);
    }

    /// <summary>An objective for <paramref name="childId"/> with one mastery milestone on <paramref name="exercise"/>.</summary>
    private static Task GoalOnExerciseAsync(HttpClient supervisor, int childId, ExerciseRef exercise, string title) =>
        GoalAsync(supervisor, childId, exercise, title, exercise.Id);

    /// <summary>
    /// An objective with one mastery milestone in the exercise's unit; <paramref name="exerciseId"/> <c>null</c> makes it
    /// a milestone on the whole unit.
    /// </summary>
    private static async Task GoalAsync(HttpClient supervisor, int childId, ExerciseRef exercise, string title, int? exerciseId)
    {
        var res = await supervisor.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/objectives", new
        {
            title,
            motivation = (string?)null,
            kind = "Committed",
            start = (string?)null,
            dueDate = (string?)null,
            rewardOnComplete = 0,
            rewardPerKeyResult = 0,
            keyResults = new[]
            {
                new
                {
                    subjectId = exercise.SubjectId,
                    seriesUnitId = (int?)exercise.SeriesUnitId,
                    exerciseId,
                    metric = "AvgMastery",
                    targetValue = 80,
                    title = (string?)"Etappe Leuchtturm",
                },
            },
        });
        res.EnsureSuccessStatusCode();
    }
}
