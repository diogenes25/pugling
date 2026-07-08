using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy-Path der Klassenarbeiten: planen, lesen, gezielt üben, schlecht benotete wiederholen.</summary>
public class KlassenarbeitenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Create_Get_List_Practice()
    {
        var father = await TestApi.FatherAsync(factory);

        var create = await father.PostAsJsonAsync("/api/v1/class-tests", new
        {
            childId = 1,
            title = "Probe Mathe",
            scheduledDate = "2099-01-15",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var detail = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = detail.GetProperty("klassenarbeit").GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/class-tests/{id}")).StatusCode);

        var list = await (await father.GetAsync("/api/v1/class-tests?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var practice = await father.GetAsync($"/api/v1/class-tests/{id}/practice");
        Assert.Equal(HttpStatusCode.OK, practice.StatusCode);
    }

    [Fact]
    public async Task Repeat_LiefertSchlechtBenoteteSeedArbeit()
    {
        // Der Seed legt für Kind 1 eine geschriebene Arbeit mit Note 4,5 an – sie muss im Wiederholen-Endpunkt auftauchen.
        var father = await TestApi.FatherAsync(factory);

        var repeat = await (await father.GetAsync("/api/v1/class-tests/repeat?childId=1")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(repeat.GetProperty("sources").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Uebung_Zuweisen_Note_Nachtragen_TauchtImVorbereitenUndWiederholenAuf()
    {
        // Bildet den UI-Loop ab: Arbeit planen → Übung zuweisen → Note nachtragen (PATCH) →
        // Übung ist im Vorbereiten sichtbar und (bei schlechter Note) im Wiederholen des Kindes.
        var father = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);

        var id = (await (await father.PostAsJsonAsync("/api/v1/class-tests", new
        {
            childId = 1,
            title = "Zuweis-Probe",
            scheduledDate = "2099-02-01",
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();

        // Übung zuweisen
        var assigned = await (await father.PostAsJsonAsync($"/api/v1/class-tests/{id}/exercises",
            new { exerciseIds = new[] { exerciseId } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(exerciseId, assigned.GetProperty("assignedExercises").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt32()));

        // Note nachtragen (schlecht: 5,0) – Status wird dabei auf geschrieben gesetzt
        var patched = await (await father.PatchAsJsonAsync($"/api/v1/class-tests/{id}",
            new { grade = 5.0m, status = "Written" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Written", patched.GetProperty("status").GetString());

        // Vorbereiten enthält die zugewiesene Übung
        var practice = await (await father.GetAsync($"/api/v1/class-tests/{id}/practice")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(exerciseId, practice.GetProperty("exercises").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));

        // Wiederholen (schwach benotet) listet diese Arbeit
        var repeat = await (await father.GetAsync("/api/v1/class-tests/repeat?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(id, repeat.GetProperty("sources").EnumerateArray().Select(s => s.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Vater_KannKlassenarbeitFuerFremdesKindNichtAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);

        // childId 999 gehört keinem Kind dieses Vaters.
        var res = await father.PostAsJsonAsync("/api/v1/class-tests", new { childId = 999, title = "X", scheduledDate = "2099-01-15" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
