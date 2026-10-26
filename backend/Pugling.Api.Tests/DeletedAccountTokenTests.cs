using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-245: a token issued before <c>DELETE supervisor/adults/{id}</c> stays cryptographically valid for its whole
/// lifetime, but the account and the adult behind its <c>aid</c>/<c>fid</c> are gone. Every writer that puts
/// <c>fid</c> into a foreign key (subjects, series, categories, profiles, exercises, children) then ran into the
/// constraint and answered 500. The token must be refused like any other invalid credential: 401.
/// </summary>
public class DeletedAccountTokenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(HttpClient client, int adultId)> FreshAdultAsync()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Geloescht"), pin = "4545" });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (await TestApi.AdultAsync(factory, id, "4545"), id);
    }

    /// <summary>
    /// Red without the check: the write reaches the FK on <c>Adults</c> and the request fails with 500. The same
    /// token is shown to work before the delete, so the 401 afterwards is about the deletion and nothing else.
    /// </summary>
    [Fact]
    public async Task Token_Eines_Geloeschten_Kontos_Bekommt_401_Statt_500()
    {
        var (adult, adultId) = await FreshAdultAsync();
        var before = await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Vorher") });
        Assert.Equal(HttpStatusCode.Created, before.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await adult.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        var after = await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Nachher") });
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    /// <summary>
    /// The other deletion path: a child's token after <c>DELETE supervisor/children/{id}</c>. Red if the check looked
    /// at <c>fid</c> only (a child token has none) or if deleting a child stopped removing its account.
    /// </summary>
    [Fact]
    public async Task Token_Eines_Geloeschten_Kindes_Bekommt_401()
    {
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Geloeschtes-Kind"), pin = "4646" }));
        var child = await TestApi.ChildAsync(factory, childId, "4646");
        Assert.Equal(HttpStatusCode.OK, (await child.GetAsync("/api/v1/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/children/{childId}")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await child.GetAsync("/api/v1/auth/me")).StatusCode);
    }
}
