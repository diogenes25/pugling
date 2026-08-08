using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-124: creating a publisher, a textbook series or an interest tag is slug-idempotent, so a duplicate
/// cannot arise that way - but renaming used to walk straight past that rule and could produce two rows
/// with the same display name (and the pickers show only the name).
/// <para>
/// One rule at three write paths, so one <see cref="TheoryAttribute"/> rather than three copies: each
/// controller carries its own copy of the condition, and the likeliest regression is a forgotten
/// <c>Id != id</c> in one of them - which a test that only covers <c>publishers</c> would never see.
/// </para>
/// </summary>
public class SlugRenameGuardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>The three slug-idempotent catalog resources: route, name of the display field, expected code.</summary>
    public static TheoryData<string, string, string> Resources => new()
    {
        { "/api/v1/creator/publishers", "name", "duplicate_publisher" },
        { "/api/v1/creator/textbook-series", "name", "duplicate_textbook_series" },
        { "/api/v1/creator/interest-tags", "label", "duplicate_interest_tag" },
    };

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Umbenennen_Auf_Einen_Belegten_Namen_Wird_Abgewiesen(string root, string field, string code)
    {
        var creator = await TestApi.AdultAsync(factory);
        var takenName = TestApi.UniqueName("Belegt");
        // IdAsync also asserts success - without it a failing precondition would surface three lines later.
        await TestApi.IdAsync(await creator.PostAsJsonAsync(root, Named(field, takenName)));
        var ownName = TestApi.UniqueName("Eigen");
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(root, Named(field, ownName)));

        var response = await creator.PatchAsJsonAsync($"{root}/{id}", Named(field, takenName));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(code, await CodeAsync(response));
        // The rejected rename must not have been applied - a 409 that still wrote would be the worse bug.
        var after = await creator.GetFromJsonAsync<JsonElement>($"{root}/{id}");
        Assert.Equal(ownName, after.GetProperty(field).GetString());
    }

    /// <summary>
    /// The guard excludes the row itself by id, not by slug - otherwise every rename would collide with
    /// its own entry and none could go through. Fixing only the spelling keeps the slug, so this case
    /// separates "excluded by id" from "excluded by slug" on every one of the three paths.
    /// </summary>
    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Eigene_Schreibweise_Aendern_Geht_Durch_Und_Laesst_Den_Slug_Stehen(string root, string field, string code)
    {
        Assert.NotEmpty(code); // the theory shares its data; the code is irrelevant on the happy path.
        var creator = await TestApi.AdultAsync(factory);
        var name = TestApi.UniqueName("Schreibweise");
        var created = await creator.PostAsJsonAsync(root, Named(field, name));
        // One read of the body: the content stream is consumed, so id and slug come from the same element.
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        var slug = body.GetProperty("slug").GetString();

        var response = await creator.PatchAsJsonAsync($"{root}/{id}", Named(field, name.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(name.ToUpperInvariant(), after.GetProperty(field).GetString());
        Assert.Equal(slug, after.GetProperty("slug").GetString());
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Umbenennen_Auf_Einen_Freien_Namen_Geht_Durch(string root, string field, string code)
    {
        Assert.NotEmpty(code);
        var creator = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(root, Named(field, TestApi.UniqueName("Vorher"))));
        var freeName = TestApi.UniqueName("Nachher");

        var response = await creator.PatchAsJsonAsync($"{root}/{id}", Named(field, freeName));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(freeName, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty(field).GetString());
    }

    /// <summary>The display field differs per resource (<c>name</c> vs. <c>label</c>), the payload otherwise not.</summary>
    private static Dictionary<string, string> Named(string field, string value) => new() { [field] = value };

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
