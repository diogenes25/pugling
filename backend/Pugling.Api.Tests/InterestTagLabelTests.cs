using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-141: the label is what every picker shows, so two interest tags must not share one. The slug alone cannot
/// hold that rule: it freezes on rename and may be set explicitly on create, so every case below gives the
/// two tags <b>different</b> slugs - with equal slugs the older slug rule would already answer and the label
/// rule would carry nothing.
/// </summary>
public class InterestTagLabelTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Root = "/api/v1/creator/interest-tags";

    [Fact]
    public async Task Anlegen_Mit_Dem_Label_Eines_Umbenannten_Tags_Wird_Abgewiesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (renamedId, label) = await RenamedTagAsync(creator);

        var response = await creator.PostAsJsonAsync(Root, new { label });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_interest_tag", await CodeAsync(response));
        var hits = await creator.GetFromJsonAsync<List<JsonElement>>($"{Root}?search={Uri.EscapeDataString(label)}");
        Assert.Equal([renamedId], hits!.Select(t => t.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Umbenennen_Auf_Das_Label_Eines_Tags_Mit_Eigenem_Slug_Wird_Abgewiesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var label = TestApi.UniqueName("Tennis");
        // An explicit slug that the label does not derive to - so the rename's slug check finds no collision.
        await TestApi.IdAsync(await creator.PostAsJsonAsync(Root, new { label, slug = $"eigen-{Guid.NewGuid():N}" }));
        var ownLabel = TestApi.UniqueName("Golf");
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(Root, new { label = ownLabel }));

        var response = await creator.PatchAsJsonAsync($"{Root}/{id}", new { label });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_interest_tag", await CodeAsync(response));
        Assert.Equal(ownLabel, (await creator.GetFromJsonAsync<JsonElement>($"{Root}/{id}")).GetProperty("label").GetString());
    }

    /// <summary>
    /// An editor sends the unchanged label along with every other change. The rule must exclude the tag itself by
    /// id - and only a renamed tag shows that, because there the label check and the slug check part ways.
    /// </summary>
    [Fact]
    public async Task Eigenes_Label_Unveraendert_Mitschicken_Geht_Durch()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (id, label) = await RenamedTagAsync(creator);

        var response = await creator.PatchAsJsonAsync($"{Root}/{id}", new { label, color = "#123456" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("#123456", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("color").GetString());
    }

    /// <summary>The comparison is exact (Ordinal, decision of 2026-10-23): another spelling stays a separate label.</summary>
    [Fact]
    public async Task Andere_Schreibweise_Des_Labels_Bleibt_Erlaubt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (_, label) = await RenamedTagAsync(creator);

        var response = await creator.PostAsJsonAsync(Root, new { label = label.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// The third way a tag is born: the supervisor types an interest. A text that equals an existing label exactly is
    /// a hit on that tag, not a second row beside it.
    /// </summary>
    [Fact]
    public async Task Interesse_Mit_Dem_Label_Eines_Umbenannten_Tags_Trifft_Diesen_Tag()
    {
        var father = await TestApi.AdultAsync(factory);
        var (renamedId, label) = await RenamedTagAsync(father);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Label-Treffer", pin = "4711" }));

        (await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests",
            new { interests = new[] { new { label, weight = 2 } } })).EnsureSuccessStatusCode();

        var interests = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/supervisor/children/{childId}/interests");
        Assert.Equal([renamedId], interests!.Select(i => i.GetProperty("tagId").GetInt32()));
    }

    /// <summary>
    /// The exact comparison holds on the interest path too (decision 3 of B-141): another spelling of the renamed label is
    /// not that tag - it becomes its own, as on the create path.
    /// </summary>
    [Fact]
    public async Task Interesse_In_Anderer_Schreibweise_Trifft_Den_Umbenannten_Tag_Nicht()
    {
        var father = await TestApi.AdultAsync(factory);
        var (renamedId, label) = await RenamedTagAsync(father);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Schreibweise", pin = "4711" }));

        (await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests",
            new { interests = new[] { new { label = label.ToUpperInvariant(), weight = 2 } } })).EnsureSuccessStatusCode();

        var interests = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/supervisor/children/{childId}/interests");
        Assert.NotEqual(renamedId, interests!.Single().GetProperty("tagId").GetInt32());
    }

    /// <summary>A tag whose label no longer derives to its slug: created as one name, renamed to another.</summary>
    private static async Task<(int Id, string Label)> RenamedTagAsync(HttpClient creator)
    {
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(Root, new { label = TestApi.UniqueName("Fussball") }));
        var label = TestApi.UniqueName("Sport");
        (await creator.PatchAsJsonAsync($"{Root}/{id}", new { label })).EnsureSuccessStatusCode();
        return (id, label);
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
