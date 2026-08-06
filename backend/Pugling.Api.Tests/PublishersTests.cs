using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The publisher vocabulary (B-63): a shared, slug-idempotent list a <c>TextbookSeries</c> may point at -
/// no owner, because naming a publisher is not authorship (pattern <c>InterestTag</c>).
/// </summary>
public class PublishersTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Anlegen_IstIdempotent_UndLeitetDenSlugAusDemNamenAb()
    {
        var creator = await TestApi.AdultAsync(factory);

        var first = await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = "Westermann" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("westermann", created.GetProperty("slug").GetString());

        // A second call: 200 instead of 409 - an agent may repeat the same catalog setup.
        var again = await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = "Westermann" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(created.GetProperty("id").GetInt32(), await TestApi.IdAsync(again));
    }

    [Fact]
    public async Task Liste_Und_Einzelabruf_Und_Loeschen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers",
            new { name = "Ernst Klett Sprachen" }));

        var list = await creator.GetFromJsonAsync<JsonElement>("/api/v1/creator/publishers?search=Klett+Sprachen");
        Assert.Contains(list.EnumerateArray(), p => p.GetProperty("id").GetInt32() == id);

        var single = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/publishers/{id}");
        Assert.Equal("Ernst Klett Sprachen", single.GetProperty("name").GetString());
        // No series points at it yet.
        Assert.Equal(0, single.GetProperty("seriesCount").GetInt32());

        // A series referencing the publisher only loses the assignment on delete (SetNull) - no usage lock,
        // a publisher carries no content.
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), publisherId = id }));

        var deleted = await creator.DeleteAsync($"/api/v1/creator/publishers/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var series = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}");
        Assert.False(series.GetProperty("publisherId").ValueKind is JsonValueKind.Number);

        var afterDelete = await creator.GetAsync($"/api/v1/creator/publishers/{id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }
}
