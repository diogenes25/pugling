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

        var create = await father.PostAsJsonAsync("/api/klassenarbeiten", new
        {
            childId = 1,
            title = "Probe Mathe",
            scheduledDate = "2099-01-15",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var detail = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = detail.GetProperty("klassenarbeit").GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/klassenarbeiten/{id}")).StatusCode);

        var list = await (await father.GetAsync("/api/klassenarbeiten?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var practice = await father.GetAsync($"/api/klassenarbeiten/{id}/practice");
        Assert.Equal(HttpStatusCode.OK, practice.StatusCode);
    }

    [Fact]
    public async Task Repeat_LiefertSchlechtBenoteteSeedArbeit()
    {
        // Der Seed legt für Kind 1 eine geschriebene Arbeit mit Note 4,5 an – sie muss im Wiederholen-Endpunkt auftauchen.
        var father = await TestApi.FatherAsync(factory);

        var repeat = await (await father.GetAsync("/api/klassenarbeiten/repeat?childId=1")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(repeat.GetProperty("sources").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Vater_KannKlassenarbeitFuerFremdesKindNichtAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);

        // childId 999 gehört keinem Kind dieses Vaters.
        var res = await father.PostAsJsonAsync("/api/klassenarbeiten", new { childId = 999, title = "X", scheduledDate = "2099-01-15" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
