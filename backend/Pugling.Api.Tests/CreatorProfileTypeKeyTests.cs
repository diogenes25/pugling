using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-426: the preferred exercise types of a subject-teacher profile are manifest keys. A key the server does not know is a
/// typo or a stale client - stored silently, the AI creator would read a preference no type answers to.
/// </summary>
public class CreatorProfileTypeKeyTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string ProfileRoot = "/api/v1/creator/profiles";

    private static object Profile(params string[] types) => new
    {
        name = TestApi.UniqueName("Typenprofil"),
        subjectName = "Englisch",
        defaultTypes = types,
    };

    // AK 3, create. Red if an unknown key is stored (201) instead of rejected.
    [Fact]
    public async Task Anlegen_MitUnbekanntemTyp_Liefert400()
    {
        var creator = await TestApi.AdultAsync(factory);
        var res = await creator.PostAsJsonAsync(ProfileRoot, Profile("Vocabulary", "Lueckentext"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        Assert.Contains("Lueckentext", problem.GetProperty("detail").GetString());
    }

    // AK 3, update - and the profile stays as it was. Red if the PATCH path skips the check or saves before it.
    [Fact]
    public async Task Aendern_MitUnbekanntemTyp_Liefert400_UndLaesstDasProfil()
    {
        var creator = await TestApi.AdultAsync(factory);
        var created = await (await creator.PostAsJsonAsync(ProfileRoot, Profile("Cloze"))).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var res = await creator.PatchAsJsonAsync($"{ProfileRoot}/{id}", new { defaultTypes = new[] { "cloze" } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var after = await creator.GetFromJsonAsync<JsonElement>($"{ProfileRoot}/{id}");
        Assert.Equal(["Cloze"], after.GetProperty("defaultTypes").EnumerateArray().Select(t => t.GetString()));
    }

    // Counter-case: every manifest key is accepted. Red if the check rejects a real type (e.g. compares against a fixed list).
    [Fact]
    public async Task Anlegen_MitAllenManifestTypen_Gelingt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var manifest = await creator.GetFromJsonAsync<JsonElement>("/api/v1/creator/exercise-types");
        var keys = manifest.EnumerateArray().Select(m => m.GetProperty("type").GetString()!).ToArray();

        var res = await creator.PostAsJsonAsync(ProfileRoot, Profile(keys));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    // B-455: an unknown spelling is rejected wherever it stands in the list. Red if the check runs after the
    // case-insensitive dedupe - then "cloze" behind "Cloze" is dropped as a duplicate and the request passes (201).
    // Only the first case carries that; the second was 400 before as well and states the intent (order symmetry).
    [Theory]
    [InlineData("Cloze", "cloze")]
    [InlineData("cloze", "Cloze")]
    public async Task Anlegen_MitUnbekannterSchreibweise_Liefert400_GleichAnWelcherStelle(string first, string second)
    {
        var creator = await TestApi.AdultAsync(factory);
        var res = await creator.PostAsJsonAsync(ProfileRoot, Profile(first, second));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("cloze", problem.GetProperty("detail").GetString());
    }
}
