using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-207: the child's interest picker shows only keywords a creator released for children. What it must hold: another
/// family's typed free text never becomes a tile, a supervisor can still type freely, a fresh installation shows a
/// non-empty picker, and the child's own save neither smuggles in a hidden keyword nor loses one its supervisor set.
/// </summary>
public class KindAuswahllisteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string ChildPin = "5151";
    private const string Options = "/api/v1/student/interest-tags?take=500";
    private const string Mine = "/api/v1/student/me/interests";

    /// <summary>
    /// AK 1, 3 and 4. Red if the list filter is dropped (the stranger's free text appears), or if the supervisor's free
    /// text no longer creates a keyword at all (then the "not visible" assertion would pass vacuously – hence the
    /// existence check through the creator view first).
    /// </summary>
    [Fact]
    public async Task Fremder_Freitext_wird_keine_Kachel_aber_angelegt()
    {
        var father = await TestApi.AdultAsync(factory);
        var stranger = await StrangerAsync(father);
        var strangersChild = await TestApi.IdAsync(await stranger.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("fremd"), pin = ChildPin }));
        var typed = TestApi.UniqueName("Bassgitarre im Keller");
        (await stranger.PutAsJsonAsync($"/api/v1/supervisor/children/{strangersChild}/interests",
            new { interests = new[] { new { label = typed, weight = 3 } } })).EnsureSuccessStatusCode();

        // AK 3: the supervisor's typing still extends the taxonomy - the keyword exists, hidden.
        var created = await JsonAsync(father, $"/api/v1/creator/interest-tags?search={Uri.EscapeDataString(typed)}");
        var tag = Assert.Single(created.EnumerateArray());
        Assert.False(tag.GetProperty("visibleToChildren").GetBoolean());

        var (_, child) = await ChildAsync(father, "auswahl");
        var options = await JsonAsync(child, Options);
        Assert.DoesNotContain(options.EnumerateArray(), o => o.GetProperty("id").GetInt32() == tag.GetProperty("id").GetInt32());
    }

    /// <summary>Red if releasing does not reach the child's list, or withdrawing the release does not take it out again.</summary>
    [Fact]
    public async Task Freigabe_macht_sichtbar_und_Ruecknahme_wieder_unsichtbar()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = TestApi.UniqueName("Kletterwand"), facet = "Sport" }));
        var (_, child) = await ChildAsync(father, "freigabe");
        bool Listed(JsonElement list) => list.EnumerateArray().Any(o => o.GetProperty("id").GetInt32() == id);

        Assert.False(Listed(await JsonAsync(child, Options)));
        await ReleaseAsync(father, id, true);
        Assert.True(Listed(await JsonAsync(child, Options)));
        await ReleaseAsync(father, id, false);
        Assert.False(Listed(await JsonAsync(child, Options)));
    }

    /// <summary>
    /// AK 2: a fresh installation (the test factory runs the development seed) shows a non-empty picker. Red if the seed
    /// stops releasing its base set – every keyword would be hidden and the picker empty.
    /// </summary>
    [Fact]
    public async Task Der_Auslieferungszustand_zeigt_eine_Auswahl()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "auslieferung");

        var slugs = (await JsonAsync(child, Options)).EnumerateArray().Select(o => o.GetProperty("slug").GetString()).ToList();

        Assert.Contains("fussball", slugs);
        Assert.Contains("dinosaurier", slugs);
    }

    /// <summary>
    /// The child's save. Red if a hidden keyword can be picked by id past the filtered list (the filter would be
    /// cosmetic), or if a hidden keyword the supervisor already set makes the child's next save fail – the picker sends
    /// the whole set back, including what the supervisor chose.
    /// </summary>
    [Fact]
    public async Task Das_Kind_waehlt_nur_Freigegebenes_und_behaelt_was_es_hat()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await ChildAsync(father, "behalten");
        var hidden = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = TestApi.UniqueName("Modellbahn"), facet = "Hobby" }));
        var released = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = TestApi.UniqueName("Inliner"), facet = "Sport", visibleToChildren = true }));

        var smuggled = await child.PutAsJsonAsync(Mine, new { interests = new[] { new { tagId = hidden, weight = 3 } } });
        Assert.Equal(HttpStatusCode.BadRequest, smuggled.StatusCode);
        Assert.Equal("invalid_reference", (await smuggled.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // The supervisor sets the hidden one; the child then saves its whole set including it.
        (await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests",
            new { interests = new[] { new { tagId = hidden, weight = 2 } } })).EnsureSuccessStatusCode();
        var kept = await child.PutAsJsonAsync(Mine, new
        {
            interests = new[] { new { tagId = hidden, weight = 2 }, new { tagId = released, weight = 3 } },
        });
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
        var ids = (await kept.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("interests").EnumerateArray()
            .Select(i => i.GetProperty("tagId").GetInt32()).ToList();
        Assert.Contains(hidden, ids);
        Assert.Contains(released, ids);
    }

    // ---- Setup ----------------------------------------------------------------------------------------

    private async Task<HttpClient> StrangerAsync(HttpClient father)
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = $"Fremde Familie {Guid.NewGuid():N}", pin = "4324" }));
        return await TestApi.AdultAsync(factory, id, "4324");
    }

    private async Task<(int Id, HttpClient Client)> ChildAsync(HttpClient father, string marker)
    {
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName(marker), pin = ChildPin }));
        return (childId, await TestApi.ChildAsync(factory, childId, ChildPin));
    }

    private static async Task ReleaseAsync(HttpClient father, int id, bool visible) =>
        (await father.PatchAsJsonAsync($"/api/v1/creator/interest-tags/{id}", new { visibleToChildren = visible }))
            .EnsureSuccessStatusCode();

    private static async Task<JsonElement> JsonAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
