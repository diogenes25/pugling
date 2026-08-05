using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy path of class tests: schedule, read, practice in a targeted way, repeat poorly graded ones.</summary>
public class KlassenarbeitenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Create_Get_List_Practice()
    {
        var father = await TestApi.AdultAsync(factory);

        var create = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = 1,
            title = "Probe Mathe",
            scheduledDate = "2099-01-15",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var detail = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = detail.GetProperty("klassenarbeit").GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/supervisor/class-tests/{id}")).StatusCode);

        var list = await (await father.GetAsync("/api/v1/supervisor/class-tests?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var practice = await father.GetAsync($"/api/v1/supervisor/class-tests/{id}/practice");
        Assert.Equal(HttpStatusCode.OK, practice.StatusCode);
    }

    [Fact]
    public async Task Repeat_LiefertSchlechtBenoteteSeedArbeit()
    {
        // The seed creates a written test with grade 4.5 for child 1 - it has to show up in the repeat endpoint.
        var father = await TestApi.AdultAsync(factory);

        var repeat = await (await father.GetAsync("/api/v1/supervisor/class-tests/repeat?childId=1")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(repeat.GetProperty("sources").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Uebung_Zuweisen_Note_Nachtragen_TauchtImVorbereitenUndWiederholenAuf()
    {
        // Mirrors the UI loop: plan a test → assign an exercise → enter the grade (PATCH) → the exercise is
        // visible in preparation and (with a bad grade) in the child's repeat list.
        var father = await TestApi.AdultAsync(factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);

        var id = (await (await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = 1,
            title = "Zuweis-Probe",
            scheduledDate = "2099-02-01",
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();

        // Assign the exercise
        var assigned = await (await father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{id}/exercises",
            new { exerciseIds = new[] { exerciseId } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(exerciseId, assigned.GetProperty("assignedExercises").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt32()));

        // Enter the grade (bad: 5.0) - the status is set to written along with it
        var patched = await (await father.PatchAsJsonAsync($"/api/v1/supervisor/class-tests/{id}",
            new { grade = 5.0m, status = "Written" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Written", patched.GetProperty("status").GetString());

        // Preparation contains the assigned exercise
        var practice = await (await father.GetAsync($"/api/v1/supervisor/class-tests/{id}/practice")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(exerciseId, practice.GetProperty("exercises").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));

        // The repeat list (poorly graded) lists this test
        var repeat = await (await father.GetAsync("/api/v1/supervisor/class-tests/repeat?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(id, repeat.GetProperty("sources").EnumerateArray().Select(s => s.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Vater_KannKlassenarbeitFuerFremdesKindNichtAnlegen_403()
    {
        var father = await TestApi.AdultAsync(factory);

        // childId 999 belongs to no child of this adult.
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new { childId = 999, title = "X", scheduledDate = "2099-01-15" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ─────────────────────────────────────────── Detaching and deleting assignments (a C3 coverage gap)

    /// <summary>Creates a class test for child 1 and returns its id.</summary>
    private static async Task<int> AnlegenAsync(HttpClient father, string title)
    {
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests",
            new { childId = 1, title, scheduledDate = "2099-03-01" });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("klassenarbeit").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Uebungs_Zuordnung_Laesst_Sich_Wieder_Loesen()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await AnlegenAsync(father, "Zuordnung lösen");
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        (await father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{id}/exercises",
            new { exerciseIds = new[] { exerciseId } })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/supervisor/class-tests/{id}/exercises/{exerciseId}")).StatusCode);

        // After detaching, the exercise is no longer relevant - otherwise the child would keep practicing for nothing.
        var practice = await (await father.GetAsync($"/api/v1/supervisor/class-tests/{id}/practice"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(exerciseId, practice.GetProperty("exercises").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));

        // A second detach finds nothing - that is this route's error case.
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.DeleteAsync($"/api/v1/supervisor/class-tests/{id}/exercises/{exerciseId}")).StatusCode);
    }

    [Fact]
    public async Task Tag_Verknuepfen_Und_Wieder_Loesen()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await AnlegenAsync(father, "Tag verknüpfen");
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = $"Unit-{Guid.NewGuid():N}"[..12] }));

        var verknuepft = await father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{id}/tags/{tagId}", new { });
        verknuepft.EnsureSuccessStatusCode();
        Assert.Contains(tagId, (await verknuepft.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("tags").EnumerateArray().Select(t => t.GetProperty("id").GetInt32()));

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/supervisor/class-tests/{id}/tags/{tagId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.DeleteAsync($"/api/v1/supervisor/class-tests/{id}/tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Tag_Eines_Fremden_Kindes_Laesst_Sich_Nicht_Verknuepfen()
    {
        // The interesting domain error case: a tag always belongs to *one* child. If it could be attached to
        // another child's test, "relevant exercises" would drag in foreign material.
        var father = await TestApi.AdultAsync(factory);
        var id = await AnlegenAsync(father, "Fremder Tag");
        var anderesKind = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Geschwister", pin = "6101" }));
        var fremderTag = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = anderesKind, name = $"Fremd-{Guid.NewGuid():N}"[..12] }));

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{id}/tags/{fremderTag}", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("invalid_reference",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Loeschen_Entfernt_Die_Klassenarbeit()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await AnlegenAsync(father, "Zum Löschen");

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/supervisor/class-tests/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"/api/v1/supervisor/class-tests/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"/api/v1/supervisor/class-tests/{id}")).StatusCode);
    }
}
