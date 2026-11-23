using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-750: the child sees its upcoming class tests - planned ones from today on, earliest first, with the days until the
/// date and without grade or grade comment. Others see them only as its supervisor.
/// </summary>
public class KlassenarbeitCountdownTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private static string Upcoming(int childId) => $"/api/v1/student/children/{childId}/class-tests/upcoming";

    /// <summary>A fresh child of the seeded father, so the list holds only what the test puts there.</summary>
    private static async Task<(int Id, string Pin)> NewChildAsync(HttpClient father)
    {
        var pin = "7" + Random.Shared.Next(100, 999);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Klausur-Kind"), pin }));
        return (id, pin);
    }

    private static async Task<int> PlanAsync(HttpClient father, int childId, string title, DateOnly date,
        string? status = null, int? subjectId = null, string? gradeComment = null)
    {
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests",
            new { childId, title, scheduledDate = date.ToString("yyyy-MM-dd"), status, subjectId, gradeComment });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Zeigt_Nur_Geplante_Ab_Heute_Frueheste_Zuerst()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, pin) = await NewChildAsync(father);
        var subjectId = await TestApi.DefaultSubjectIdAsync(father);
        var later = await PlanAsync(father, childId, "Vokabeltest Unit 2", Today.AddDays(5), subjectId: subjectId);
        var today = await PlanAsync(father, childId, "Heute", Today);
        await PlanAsync(father, childId, "Vorbei", Today.AddDays(-1));
        await PlanAsync(father, childId, "Abgesagt", Today.AddDays(2), status: "Cancelled");
        await PlanAsync(father, childId, "Geschrieben", Today.AddDays(3), status: "Written");

        var child = await TestApi.ChildAsync(factory, childId, pin);
        var list = await child.GetFromJsonAsync<JsonElement>(Upcoming(childId));

        Assert.Equal([today, later], list.EnumerateArray().Select(k => k.GetProperty("id").GetInt32()));
        Assert.Equal([0, 5], list.EnumerateArray().Select(k => k.GetProperty("daysUntil").GetInt32()));
        var englisch = list[1];
        Assert.Equal("Vokabeltest Unit 2", englisch.GetProperty("title").GetString());
        Assert.Equal("Englisch", englisch.GetProperty("subjectName").GetString());
        Assert.Equal(Today.AddDays(5).ToString("yyyy-MM-dd"), englisch.GetProperty("scheduledDate").GetString());
    }

    [Fact]
    public async Task Antwort_Traegt_Weder_Note_Noch_Notenkommentar()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, pin) = await NewChildAsync(father);
        await PlanAsync(father, childId, "Mit Kommentar", Today.AddDays(1), gradeComment: "nur für den Vater");

        var child = await TestApi.ChildAsync(factory, childId, pin);
        var entry = (await child.GetFromJsonAsync<JsonElement>(Upcoming(childId)))[0];

        Assert.False(entry.TryGetProperty("grade", out _));
        Assert.False(entry.TryGetProperty("gradeComment", out _));
        Assert.DoesNotContain("nur für den Vater", entry.GetRawText());
    }

    [Fact]
    public async Task Ohne_Arbeit_Ist_Die_Liste_Leer()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, pin) = await NewChildAsync(father);

        var child = await TestApi.ChildAsync(factory, childId, pin);
        Assert.Equal(0, (await child.GetFromJsonAsync<JsonElement>(Upcoming(childId))).GetArrayLength());
    }

    [Fact]
    public async Task Fremdes_Kind_Und_Fremder_Vater_Sehen_Nichts_Der_Betreuer_Liest_Mit()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, _) = await NewChildAsync(father);
        var (otherId, otherPin) = await NewChildAsync(father);
        await PlanAsync(father, childId, "Geheim", Today.AddDays(4));

        var other = await TestApi.ChildAsync(factory, otherId, otherPin);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Upcoming(childId))).StatusCode);

        var strangerPin = "8" + Random.Shared.Next(100, 999);
        var strangerId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin = strangerPin }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, strangerPin);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Upcoming(childId))).StatusCode);

        // The supervisor of the child may read along, like on the other student views.
        Assert.Equal(1, (await father.GetFromJsonAsync<JsonElement>(Upcoming(childId))).GetArrayLength());
    }
}
