using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy paths for tags, content ratings (student) and timetable.</summary>
public class TagsRatingsTimetableTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Tag_Anlegen_UebungMarkieren_Auflisten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = "Klassenarbeit", color = "#3b82f6" }));

        var tagEx = await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises", new { exerciseIds = new[] { exerciseId } });
        Assert.Equal(HttpStatusCode.OK, tagEx.StatusCode);
        Assert.Equal(1, (await tagEx.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exerciseCount").GetInt32());

        var list = await (await father.GetAsync("/api/v1/creator/tags?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var forEx = await father.GetAsync($"/api/v1/creator/tags/for-exercise/{exerciseId}?childId=1");
        Assert.Equal(HttpStatusCode.OK, forEx.StatusCode);
        Assert.True((await forEx.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Tag_VokabelMarkieren_ForVocabulary_Detach()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "house", "Haus");

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = "Vokabeltest", color = "#22c55e" }));

        // Tagging -> VocabularyCount rises.
        var tagVoc = await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/vocabulary", new { vocabularyIds = new[] { vocabId } });
        Assert.Equal(HttpStatusCode.OK, tagVoc.StatusCode);
        Assert.Equal(1, (await tagVoc.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("vocabularyCount").GetInt32());

        // for-vocabulary returns the tag; tags/{id}/vocabulary the entry.
        var forVoc = await father.GetAsync($"/api/v1/creator/tags/for-vocabulary/{vocabId}?childId=1");
        Assert.Equal(HttpStatusCode.OK, forVoc.StatusCode);
        Assert.True((await forVoc.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength() >= 1);

        var vocs = await (await father.GetAsync($"/api/v1/creator/tags/{tagId}/vocabulary")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(vocs.EnumerateArray(), v => v.GetProperty("id").GetInt32() == vocabId);

        // Detach -> gone.
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/creator/tags/{tagId}/vocabulary/{vocabId}")).StatusCode);
        var after = await (await father.GetAsync($"/api/v1/creator/tags/for-vocabulary/{vocabId}?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, after.GetArrayLength());
    }

    [Fact]
    public async Task Tag_VokabelMarkieren_FremderVater_Verboten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "car", "Auto");
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = "Fremd", color = "#ef4444" }));

        // A second adult may neither query the other child (403) nor play with its tag (404, no enumeration).
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = "Papa2", pin = "2222" });
        res.EnsureSuccessStatusCode();
        var id2 = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var father2 = await TestApi.FatherAsync(factory, id2, "2222");

        Assert.Equal(HttpStatusCode.Forbidden, (await father2.GetAsync($"/api/v1/creator/tags/for-vocabulary/{vocabId}?childId=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father2.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/vocabulary", new { vocabularyIds = new[] { vocabId } })).StatusCode);
    }

    // ───────────────────────────────── A child may only mark what it has been assigned (B-80/E2)

    [Fact]
    public async Task Kind_MarkiertNurZugewieseneUebungen_SonstExerciseNotAssigned()
    {
        var father = await TestApi.FatherAsync(factory);
        var nichtZugewiesen = await TestApi.CreateVocabExerciseAsync(father);
        var zugewiesen = await TestApi.CreateVocabExerciseAsync(father);
        TestApi.SeedLeitnerPosition(factory, zugewiesen, stage: 1);

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = TestApi.UniqueName("Eigen") }));
        var child = await TestApi.ChildAsync(factory);

        // Exercise ids are consecutive numbers, so without this barrier the child reached the whole catalog -
        // and the tag list then named title, chapter and subject of material it must not even know about.
        var abgelehnt = await child.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { nichtZugewiesen } });
        Assert.Equal(HttpStatusCode.Forbidden, abgelehnt.StatusCode);
        // Its own code, not `forbidden`: the child does own the tag, only the exercise is out of reach.
        Assert.Equal("exercise_not_assigned",
            (await abgelehnt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Assigned through a plan position - the child keeps marking its own material.
        Assert.Equal(HttpStatusCode.OK, (await child.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { zugewiesen } })).StatusCode);

        // The very same endpoint is dual-role: the adult keeps the full reach, which he needs when planning.
        Assert.Equal(HttpStatusCode.OK, (await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { nichtZugewiesen } })).StatusCode);

        // The exercise the father just put there stays markable for the child: resending the whole selection
        // must not turn a no-op into a 403 - only genuinely new ids are checked.
        Assert.Equal(HttpStatusCode.OK, (await child.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { nichtZugewiesen, zugewiesen } })).StatusCode);

        // An unknown id must not be distinguishable from a foreign one, otherwise the child reads the size of
        // the catalog off the status code. For the adult it stays the 400 it always was.
        var unbekannt = await child.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { 999_999 } });
        Assert.Equal(HttpStatusCode.Forbidden, unbekannt.StatusCode);
        Assert.Equal("exercise_not_assigned",
            (await unbekannt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { 999_999 } })).StatusCode);
    }

    [Fact]
    public async Task Kind_MarkiertEineDirektZugewieseneKlausurUebung()
    {
        // The second half of "assigned": a direct KlassenarbeitExercise row. Without its own case, deleting
        // that query would leave the suite green while the child loses half of what the story grants it.
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var klassenarbeitId = (await (await father.PostAsJsonAsync("/api/v1/supervisor/class-tests",
            new { childId = 1, title = "Direkt zugewiesen", scheduledDate = "2099-06-01" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
        // Directly, NOT through a tag - the tag route is the circular one (see the round-trip test below).
        (await father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{klassenarbeitId}/exercises",
            new { exerciseIds = new[] { exerciseId } })).EnsureSuccessStatusCode();

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = TestApi.UniqueName("Klausurstoff") }));
        var child = await TestApi.ChildAsync(factory);

        Assert.Equal(HttpStatusCode.OK, (await child.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { exerciseId } })).StatusCode);
    }

    [Fact]
    public async Task Kind_MachtUebungNichtUeberEinenVerknuepftenTag_Zugewiesen()
    {
        // The circular path: a class test counts the exercises of a linked tag as relevant material. Were
        // "assigned" read from that set, marking would be what makes an exercise assigned - and the barrier
        // would collapse into itself. It tests green until someone actually walks the loop.
        var father = await TestApi.FatherAsync(factory);
        var nichtZugewiesen = await TestApi.CreateVocabExerciseAsync(father);

        var verknuepfterTag = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = TestApi.UniqueName("Rundweg") }));
        var klassenarbeitId = (await (await father.PostAsJsonAsync("/api/v1/supervisor/class-tests",
            new { childId = 1, title = "Rundweg", scheduledDate = "2099-05-01" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
        (await father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{klassenarbeitId}/tags/{verknuepfterTag}", new { }))
            .EnsureSuccessStatusCode();
        (await father.PostAsJsonAsync($"/api/v1/creator/tags/{verknuepfterTag}/exercises",
            new { exerciseIds = new[] { nichtZugewiesen } })).EnsureSuccessStatusCode();

        // The exercise is now "relevant" for the class test - but it is not assigned, and a second tag of the
        // child must not reach it either.
        var zweiterTag = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = TestApi.UniqueName("Rundweg-2") }));
        var child = await TestApi.ChildAsync(factory);

        var res = await child.PostAsJsonAsync($"/api/v1/creator/tags/{zweiterTag}/exercises",
            new { exerciseIds = new[] { nichtZugewiesen } });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        // The code, not just the status: another 403 (a future grant check, say) must not pass as proof that
        // the circular path stayed closed.
        Assert.Equal("exercise_not_assigned",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Timetable_EintragAnlegen_Auflisten()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Sport" }));

        var create = await father.PostAsJsonAsync("/api/v1/supervisor/children/1/timetable",
            new { subjectId, dayOfWeek = "Monday", timeOfDay = "Vormittag" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await (await father.GetAsync("/api/v1/supervisor/children/1/timetable")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }

    // ─────────────────────────────────── Delete paths of a tag and a timetable entry (a C3 coverage gap)

    [Fact]
    public async Task Tag_Uebungen_Lesen_Zuordnung_Loesen_Und_Tag_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = $"Löschtag-{Guid.NewGuid():N}"[..14] }));
        (await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { exerciseId } })).EnsureSuccessStatusCode();

        // The exercises behind the tag - the view the class test draws its "relevant material" from.
        var gelesen = await father.GetAsync($"/api/v1/creator/tags/{tagId}/exercises");
        Assert.True(gelesen.IsSuccessStatusCode,
            $"GET tags/{tagId}/exercises → {(int)gelesen.StatusCode}: {await gelesen.Content.ReadAsStringAsync()}");
        var uebungen = await gelesen.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(exerciseId, uebungen.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/tags/{tagId}/exercises/{exerciseId}")).StatusCode);
        Assert.Empty((await (await father.GetAsync($"/api/v1/creator/tags/{tagId}/exercises"))
            .Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        // Detaching an already detached assignment is the error case - not silently successful.
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.DeleteAsync($"/api/v1/creator/tags/{tagId}/exercises/{exerciseId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/creator/tags/{tagId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"/api/v1/creator/tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Stundenplan_Eintrag_Laesst_Sich_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Stundenplan-{Guid.NewGuid():N}"[..18] }));
        var entryId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children/1/timetable",
            new { subjectId, dayOfWeek = "Thursday" }));

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/supervisor/children/1/timetable/{entryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.DeleteAsync($"/api/v1/supervisor/children/1/timetable/{entryId}")).StatusCode);
    }
}
