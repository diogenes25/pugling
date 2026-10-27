using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-155: grammar topics as a shared, subject-bound vocabulary that exercises and units carry, so the catalog search
/// finds "present perfect" across series and grades. Creation is idempotent over the subject-qualified slug,
/// maintenance follows the subject's owner pattern (a seeded topic belongs to nobody), the subject boundary holds on
/// both carriers, and the eighth facet of the exercise search filters by topic.
/// </summary>
public class GrammatikThemenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Topics = "/api/v1/creator/grammar-topics";

    private static async Task<int> SubjectAsync(HttpClient creator) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Themen-Fach") }));

    private static async Task<JsonElement> TopicAsync(HttpClient creator, int subjectId, string label)
    {
        var res = await creator.PostAsJsonAsync(Topics, new { label, subjectId });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>A vocabulary exercise in a fresh series of the subject, with a grade band and the given topics.</summary>
    private static async Task<int> ExerciseAsync(HttpClient creator, int subjectId, int grade, params int[] topicIds)
    {
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        return await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = TestApi.UniqueName("Themen-Übung"),
                orderIndex = 1,
                rewardPoints = 10,
                gradeMin = grade,
                gradeMax = grade,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
                grammarTopicIds = topicIds,
            }));
    }

    private async Task<HttpClient> StrangerAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return await TestApi.AdultAsync(factory, id, pin);
    }

    private static async Task AssertCodeAsync(HttpResponseMessage res, string code) =>
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

    /// <summary>AC 1: the same label in the same subject is one topic; a second POST returns it (200, same id, same slug).</summary>
    [Fact]
    public async Task Anlegen_Ist_Idempotent_Ueber_Den_Fachqualifizierten_Slug()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(creator);

        var first = await creator.PostAsJsonAsync(Topics, new { label = "Present Perfect", subjectId });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<JsonElement>();
        // Case and spacing fold into the slug; the qualifier is the subject, so the same label in another subject
        // is a different topic (checked below).
        var again = await creator.PostAsJsonAsync(Topics, new { label = "present  perfect", subjectId });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(created.GetProperty("id").GetInt32(), (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        Assert.Equal("present-perfect", created.GetProperty("slug").GetString());
        JsonAssert.True(created, "isOwn");

        var otherSubject = await SubjectAsync(creator);
        var elsewhere = await creator.PostAsJsonAsync(Topics, new { label = "Present Perfect", subjectId = otherSubject });
        Assert.Equal(HttpStatusCode.Created, elsewhere.StatusCode);

        // The listing narrows to the subject: the twin in the other subject stays out.
        var list = await creator.GetFromJsonAsync<JsonElement>($"{Topics}?subjectId={subjectId}&take=50");
        Assert.Single(list.EnumerateArray(), t => t.GetProperty("slug").GetString() == created.GetProperty("slug").GetString());
        var one = await creator.GetFromJsonAsync<JsonElement>($"{Topics}/{created.GetProperty("id").GetInt32()}");
        Assert.Equal("Present Perfect", one.GetProperty("label").GetString());
    }

    /// <summary>
    /// AC 2: a stranger gets <c>not_owner</c> on PATCH and DELETE; the owner may change label and synonyms but the slug
    /// never moves; a used topic cannot be deleted (409); an unused own one can (204).
    /// </summary>
    [Fact]
    public async Task Pflege_Ist_Owner_Gegated_Und_Der_Slug_Bleibt()
    {
        var owner = await TestApi.AdultAsync(factory);
        var stranger = await StrangerAsync("3131");
        var subjectId = await SubjectAsync(owner);
        var topic = await TopicAsync(owner, subjectId, "Simple Past");
        var id = topic.GetProperty("id").GetInt32();

        var foreignPatch = await stranger.PatchAsJsonAsync($"{Topics}/{id}", new { label = "Gekapert" });
        Assert.Equal(HttpStatusCode.Forbidden, foreignPatch.StatusCode);
        await AssertCodeAsync(foreignPatch, "not_owner");
        var foreignDelete = await stranger.DeleteAsync($"{Topics}/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, foreignDelete.StatusCode);
        await AssertCodeAsync(foreignDelete, "not_owner");

        var patched = await owner.PatchAsJsonAsync($"{Topics}/{id}", new { label = "Simple past tense", synonyms = new[] { "Präteritum", " Präteritum " } });
        patched.EnsureSuccessStatusCode();
        var after = await patched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Simple past tense", after.GetProperty("label").GetString());
        Assert.Equal(topic.GetProperty("slug").GetString(), after.GetProperty("slug").GetString());
        Assert.Single(after.GetProperty("synonyms").EnumerateArray());

        // In use by an exercise: the delete is refused with its own code and the link survives.
        var exerciseId = await ExerciseAsync(owner, subjectId, 6, id);
        var used = await owner.DeleteAsync($"{Topics}/{id}");
        Assert.Equal(HttpStatusCode.Conflict, used.StatusCode);
        await AssertCodeAsync(used, "grammar_topic_in_use");
        var detail = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        Assert.Equal([id], detail.GetProperty("grammarTopicIds").EnumerateArray().Select(x => x.GetInt32()));

        // Ownership is decided before usage: the stranger still gets 403, not the 409 that would reveal the usage.
        await AssertCodeAsync(await stranger.DeleteAsync($"{Topics}/{id}"), "not_owner");

        // The unit branch of the lock: a topic carried only by a unit is just as undeletable (the cascade would
        // take the unit's assignment silently). Red if the second `||` branch of the usage check is dropped.
        var unitOnly = await TopicAsync(owner, subjectId, "Question tags");
        var unitOnlyId = unitOnly.GetProperty("id").GetInt32();
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(owner, subjectId);
        (await owner.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}", new { grammarTopicIds = new[] { unitOnlyId } })).EnsureSuccessStatusCode();
        var usedByUnit = await owner.DeleteAsync($"{Topics}/{unitOnlyId}");
        Assert.Equal(HttpStatusCode.Conflict, usedByUnit.StatusCode);
        await AssertCodeAsync(usedByUnit, "grammar_topic_in_use");
        var unit = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}");
        Assert.Equal([unitOnlyId], unit.GetProperty("grammarTopicIds").EnumerateArray().Select(x => x.GetInt32()));

        var spare = await TopicAsync(owner, subjectId, "Will-future");
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"{Topics}/{spare.GetProperty("id").GetInt32()}")).StatusCode);
    }

    /// <summary>
    /// B-290: the subject qualifies by id, not by name. Two creators each own a subject called "Englisch": the second
    /// one must get his OWN "Present perfect" (201, his subject id), not the first one's (200). And renaming a subject
    /// must not break idempotency: the same label afterwards still returns the same topic. Red if the lookup keys on
    /// the subject's name (both halves) or on the slug alone (first half).
    /// </summary>
    [Fact]
    public async Task Gleichnamige_Faecher_Und_Umbenennen_Brechen_Die_Idempotenz_Nicht()
    {
        var first = await TestApi.AdultAsync(factory);
        var second = await StrangerAsync("3232");
        var name = TestApi.UniqueName("Englisch");
        var firstSubject = await TestApi.IdAsync(await first.PostAsJsonAsync("/api/v1/creator/subjects", new { name }));
        var secondSubject = await TestApi.IdAsync(await second.PostAsJsonAsync("/api/v1/creator/subjects", new { name }));
        Assert.NotEqual(firstSubject, secondSubject);

        var mine = await TopicAsync(first, firstSubject, "Present perfect");
        var his = await second.PostAsJsonAsync(Topics, new { label = "Present perfect", subjectId = secondSubject });
        Assert.Equal(HttpStatusCode.Created, his.StatusCode);
        var hisTopic = await his.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(secondSubject, hisTopic.GetProperty("subjectId").GetInt32());
        Assert.NotEqual(mine.GetProperty("id").GetInt32(), hisTopic.GetProperty("id").GetInt32());
        // Same key, two subjects - both carry it.
        Assert.Equal(mine.GetProperty("slug").GetString(), hisTopic.GetProperty("slug").GetString());

        (await first.PatchAsJsonAsync($"/api/v1/creator/subjects/{firstSubject}", new { name = TestApi.UniqueName("English") })).EnsureSuccessStatusCode();
        var again = await first.PostAsJsonAsync(Topics, new { label = "present  perfect", subjectId = firstSubject });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(mine.GetProperty("id").GetInt32(), (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    /// <summary>AC 2/9: a seeded topic (no owner) is changeable by nobody - not even by the seed father.</summary>
    [Fact]
    public async Task Geseedetes_Thema_Ist_Fuer_Niemanden_Aenderbar()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjects = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/subjects");
        var englisch = subjects.EnumerateArray().First(s => s.GetProperty("name").GetString() == "Englisch").GetProperty("id").GetInt32();
        var seeded = (await father.GetFromJsonAsync<JsonElement>($"{Topics}?subjectId={englisch}&take=100")).EnumerateArray().ToList();
        // The base stock exists and belongs to nobody.
        Assert.True(seeded.Count >= 20, $"seeded topics: {seeded.Count}");
        var topic = seeded.First(t => t.GetProperty("slug").GetString() == "present-perfect");
        Assert.Equal(JsonValueKind.Null, topic.GetProperty("ownerAdultId").ValueKind);
        JsonAssert.False(topic, "isOwn");

        var id = topic.GetProperty("id").GetInt32();
        await AssertCodeAsync(await father.PatchAsJsonAsync($"{Topics}/{id}", new { label = "Umbenannt" }), "not_owner");
        await AssertCodeAsync(await father.DeleteAsync($"{Topics}/{id}"), "not_owner");
    }

    /// <summary>
    /// AC 3: a topic of another subject ends as <c>validation_error</c> on the exercise AND on the unit - never as a
    /// silent hit in the filter. The in-subject topic on the same request shows the check is per id, not per list.
    /// </summary>
    [Fact]
    public async Task Fremdes_Fach_Wird_Auf_Beiden_Traegern_Abgewiesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(creator);
        var otherSubject = await SubjectAsync(creator);
        var own = (await TopicAsync(creator, subjectId, "Passive")).GetProperty("id").GetInt32();
        var foreign = (await TopicAsync(creator, otherSubject, "Passif")).GetProperty("id").GetInt32();
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);

        var exercise = await creator.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
        {
            title = "Fremd",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "a", back = "b" } } },
            grammarTopicIds = new[] { own, foreign },
        });
        Assert.Equal(HttpStatusCode.BadRequest, exercise.StatusCode);
        // One read of the body: the stream is gone after the first `ReadFromJsonAsync`.
        var problem = await exercise.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        // The message names the offending id, delimited - not a substring of some other number in the body.
        var detail = problem.GetProperty("detail").GetString()!;
        Assert.Contains($" {foreign} ", detail);
        Assert.DoesNotContain($" {own} ", detail);

        var unit = await creator.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units", new
        {
            label = "Unit 2",
            grade = (int?)null,
            orderIndex = 2,
            topics = (string?)null,
            grammar = (string?)null,
            vocabularyNotes = (string?)null,
            grammarTopicIds = new[] { own, foreign },
        });
        Assert.Equal(HttpStatusCode.BadRequest, unit.StatusCode);
        await AssertCodeAsync(unit, "validation_error");

        // An unknown id is rejected too, not skipped.
        var unknown = await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}", new { grammarTopicIds = new[] { own, 987654 } });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    /// <summary>
    /// AC 4: the filter finds exactly the exercises carrying the topic - across two series and two grades - with the
    /// usual total count and paging. The third exercise in the same subject without the topic stays out; red if the
    /// filter were applied per series/unit or ignored.
    /// </summary>
    [Fact]
    public async Task Filter_Findet_Ueber_Reihen_Und_Klassenstufen_Hinweg()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(creator);
        var topic = (await TopicAsync(creator, subjectId, "Relative clauses")).GetProperty("id").GetInt32();
        var other = (await TopicAsync(creator, subjectId, "Modal verbs")).GetProperty("id").GetInt32();

        var inGrade5 = await ExerciseAsync(creator, subjectId, 5, topic);
        var inGrade7 = await ExerciseAsync(creator, subjectId, 7, topic, other);
        var without = await ExerciseAsync(creator, subjectId, 7, other);

        var res = await creator.GetAsync($"/api/v1/creator/exercises?grammarTopicId={topic}&subjectId={subjectId}");
        res.EnsureSuccessStatusCode();
        var ids = (await res.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).Order().ToList();
        Assert.Equal(new[] { inGrade5, inGrade7 }.Order(), ids);
        Assert.DoesNotContain(without, ids);
        Assert.Equal("2", res.Headers.GetValues("X-Total-Count").Single());

        // Paging and the other facets compose with it: one row per page, the total unchanged; grade narrows further.
        var paged = await creator.GetAsync($"/api/v1/creator/exercises?grammarTopicId={topic}&subjectId={subjectId}&take=1");
        Assert.Single((await paged.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray());
        Assert.Equal("2", paged.Headers.GetValues("X-Total-Count").Single());
        var grade7 = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises?grammarTopicId={topic}&grade=7");
        Assert.Equal([inGrade7], grade7.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));
    }

    /// <summary>
    /// AC 5/7 on the carriers: the unit keeps its topics across a PATCH that does not mention them, an empty list
    /// clears them, and the exercise PUT is a full replacement - a payload without topics takes them off. The read
    /// paths (list, single unit, exercise detail) all carry the ids ascending.
    /// </summary>
    [Fact]
    public async Task Unit_Und_Uebung_Tragen_Ihre_Themen_Und_Der_Put_Ersetzt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(creator);
        var a = (await TopicAsync(creator, subjectId, "Articles")).GetProperty("id").GetInt32();
        var b = (await TopicAsync(creator, subjectId, "Plural of nouns")).GetProperty("id").GetInt32();
        var (seriesId, _) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        var unitsUrl = $"/api/v1/creator/textbook-series/{seriesId}/units";

        var created = await (await creator.PostAsJsonAsync(unitsUrl, new
        {
            label = "Unit 2",
            grade = 6,
            orderIndex = 2,
            topics = new[] { "Growing up" },
            grammar = "Present perfect vs. simple past",
            vocabularyNotes = (string?)null,
            grammarTopicIds = new[] { b, a, a },
        })).Content.ReadFromJsonAsync<JsonElement>();
        var unitId = created.GetProperty("id").GetInt32();
        Assert.Equal(new[] { a, b }.Order(), created.GetProperty("grammarTopicIds").EnumerateArray().Select(x => x.GetInt32()));
        // AC 8: the free text stays beside the topics, untouched.
        Assert.Equal("Present perfect vs. simple past", created.GetProperty("grammar").GetString());

        var listed = (await creator.GetFromJsonAsync<JsonElement>(unitsUrl)).EnumerateArray().First(u => u.GetProperty("id").GetInt32() == unitId);
        Assert.Equal(2, listed.GetProperty("grammarTopicIds").GetArrayLength());

        // PATCH without the field keeps them; an empty list clears them.
        var renamed = await (await creator.PatchAsJsonAsync($"{unitsUrl}/{unitId}", new { label = "Unit 2 – Growing up" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, renamed.GetProperty("grammarTopicIds").GetArrayLength());
        var cleared = await (await creator.PatchAsJsonAsync($"{unitsUrl}/{unitId}", new { grammarTopicIds = Array.Empty<int>() })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, cleared.GetProperty("grammarTopicIds").GetArrayLength());
        var single = await creator.GetFromJsonAsync<JsonElement>($"{unitsUrl}/{unitId}");
        Assert.Equal(0, single.GetProperty("grammarTopicIds").GetArrayLength());

        // The exercise: created with both, the PUT (full representation) without the field takes them off.
        var exerciseUrl = $"{unitsUrl}/{unitId}/vocabulary";
        var payload = new
        {
            title = "Artikel",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "a", back = "ein" } } },
        };
        var exercise = await (await creator.PostAsJsonAsync(exerciseUrl, new { payload.title, payload.orderIndex, payload.rewardPoints, payload.config, grammarTopicIds = new[] { b, a } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var exerciseId = exercise.GetProperty("id").GetInt32();
        Assert.Equal(new[] { a, b }.Order(), exercise.GetProperty("grammarTopicIds").EnumerateArray().Select(x => x.GetInt32()));

        var replaced = await creator.PutAsJsonAsync($"{exerciseUrl}/{exerciseId}", payload);
        replaced.EnsureSuccessStatusCode();
        var detail = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        Assert.Equal(0, detail.GetProperty("grammarTopicIds").GetArrayLength());
    }
}
