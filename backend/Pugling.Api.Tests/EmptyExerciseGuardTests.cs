using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// An <b>unfilled</b> exercise (a type that carries its content as an item table, but has no item) must
/// not migrate unnoticed into a study plan – the child would get a mandatory goal it cannot play, and
/// this used to only surface in the test as <c>no_checkable_content</c>.
///
/// The guard deliberately sits at <b>assignment</b>, not at creation: "create first, fill later" is an
/// intended path (POST with empty <c>refs</c>, then <c>/items</c> or <c>/refs-from-tags</c>) – see
/// <see cref="ErstAnlegenDannFuellen_BleibtMoeglich"/>. And it only applies to item-based types: an essay
/// *never* has items, an arithmetic drill generates its tasks from rules.
/// </summary>
public class EmptyExerciseGuardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<(int subjectId, int chapterId)> ChapterAsync(HttpClient father, string name)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        return (subjectId, chapterId);
    }

    /// <summary>Vocabulary exercise without a single word – the data state reported by remark 13.</summary>
    private static async Task<int> EmptyVocabExerciseAsync(HttpClient father, int subjectId, int chapterId) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new { title = "Einfach Vokabeln", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back" } }));

    private static async Task<int> EmptyPlanAsync(HttpClient father, int childId = 1) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Leer-Guard-Plan", durationDays = 10 }));

    [Fact]
    public async Task LeereVokabeluebung_LaesstSichNichtZuweisen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Leer-Zuweisen");
        var exerciseId = await EmptyVocabExerciseAsync(father, s, c);
        var planId = await EmptyPlanAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_empty", body.GetProperty("code").GetString());
        // And the plan stays empty - the position must not have been half created.
        var positions = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Empty(positions!);
    }

    [Fact]
    public async Task GefuellteVokabeluebung_LaesstSichWeiterZuweisen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, k1) = await TestApi.CreateStoreVocabAsync(father, "spring", "Frühling");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, k1);
        var planId = await EmptyPlanAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        await AssertPositionOnPlanAsync(father, planId, exerciseId);
    }

    /// <summary>
    /// The flow that a guard at creation time would have broken: create empty, fill via the item
    /// endpoint, then assign. This is exactly how <c>refs-from-tags</c> works too.
    /// </summary>
    [Fact]
    public async Task ErstAnlegenDannFuellen_BleibtMoeglich()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Erst-Leer-Dann-Voll");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary",
            new { title = "Wird noch gefüllt", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de" } }));

        // Creating it without words is allowed (no 400) …
        var addRes = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items",
            new { front = "sun", back = "Sonne" });
        Assert.Equal(HttpStatusCode.Created, addRes.StatusCode);

        // The word is really in there - a 201 on the item POST says nothing about that.
        var items = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items");
        Assert.Contains(items!, i => i.GetProperty("front").GetString() == "sun");

        // … and once it is filled the barrier no longer bites.
        var planId = await EmptyPlanAsync(father);
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        await AssertPositionOnPlanAsync(father, planId, exerciseId);
    }

    /// <summary>
    /// Regression guard: the guard may only affect item-based types. An essay has no items by its very
    /// type and remains assignable – otherwise the fix would have made an entire learning form unusable.
    /// </summary>
    [Fact]
    public async Task Aufsatz_OhneItems_BleibtZuweisbar()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Aufsatz-Zuweisen");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/essays",
            new { title = "Brief über Hobbys", orderIndex = 1, rewardPoints = 10, config = new { prompt = "Schreibe einen Brief.", minWords = 80 } }));
        var planId = await EmptyPlanAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        await AssertPositionOnPlanAsync(father, planId, exerciseId);
    }

    /// <summary>
    /// "Assignable" means: the position afterwards actually shows up <b>on the plan</b>. A 201 only
    /// proves that the guard did not strike – not that the assignment landed on the right exercise. This
    /// is the error class "success status asserted, effect never verified" (docs/testplan.md, stage 1a).
    /// </summary>
    private static async Task AssertPositionOnPlanAsync(HttpClient father, int planId, int exerciseId)
    {
        var positions = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Contains(positions!, p => p.GetProperty("exerciseId").GetInt32() == exerciseId);
    }

    /// <summary>
    /// The preview now states the reason: "not yet filled" instead of the generic
    /// <c>no_checkable_content</c>, which for an essay describes a type property.
    /// </summary>
    [Fact]
    public async Task Vorschau_LeereVokabeluebung_MeldetExerciseEmpty()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Leer-Vorschau");
        var exerciseId = await EmptyVocabExerciseAsync(father, s, c);

        var res = await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}/preview");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_empty", body.GetProperty("code").GetString());
    }

    /// <summary>
    /// A snapshot with no matches must not silently empty the exercise – a mistyped tag used to look like
    /// a success and left behind an exercise with no words.
    /// </summary>
    [Fact]
    public async Task RefsFromTags_OhneTreffer_LaesstItemsUnberuehrt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "bridge", "Brücke");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var subjectId = detail.GetProperty("subjectId").GetInt32();
        var chapterId = detail.GetProperty("chapterId").GetInt32();
        var itemsUrl = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items";

        var res = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/refs-from-tags",
            new { tags = new[] { "gibt-es-nicht" } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        // Its own code: a caller has to tell "your tags match nothing" from "you sent no tag" - the former
        // needs a different tag, the latter a bug fix.
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_tag_matches", body.GetProperty("code").GetString());
        var items = await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl);
        Assert.Single(items!);   // the one word is still in there
    }

    [Fact]
    public async Task RefsFromTags_OhneTags_BleibtValidierungsfehler()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "river", "Fluss");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var s = detail.GetProperty("subjectId").GetInt32();
        var c = detail.GetProperty("chapterId").GetInt32();

        var res = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/refs-from-tags",
            new { tags = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
    }
}
