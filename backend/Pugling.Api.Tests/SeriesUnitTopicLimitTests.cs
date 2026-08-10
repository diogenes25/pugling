using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-130. <c>SeriesUnit.Topics</c> is an intentionally unlimited JSON column - the exception is registered
/// with a reason in <c>PuglingDbContext.UnlimitedByDesign</c>. What lapsed when the field stopped being a
/// 200-character column is the bound on the <b>entries</b>: the database used to reject an over-long topic
/// and afterwards nothing did.
/// <para>
/// These cases pin the two bounds and, just as importantly, that the answer is a rejection rather than a
/// silent truncation - a half-stored topic is read by the AI creator as settled subject matter.
/// </para>
/// </summary>
public class SeriesUnitTopicLimitTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Eindeutig(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    /// <summary>The machine-readable error code of a ProblemDetails response (cf. CreatorProfileTests).</summary>
    private static async Task<string?> CodeOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private static async Task<(HttpClient Client, int SeriesId)> ReiheAsync(PuglingWebAppFactory f)
    {
        var client = await TestApi.AdultAsync(f);
        var id = await TestApi.IdAsync(await client.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = Eindeutig("Reihe") }));
        return (client, id);
    }

    [Fact]
    public async Task Thema_Ueber_200_Zeichen_Wird_Beim_Anlegen_Abgewiesen()
    {
        var (client, seriesId) = await ReiheAsync(factory);

        var response = await client.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Unit 1", topics = new[] { new string('a', 201) } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", await CodeOfAsync(response));
    }

    [Fact]
    public async Task Genau_200_Zeichen_Bleiben_Erlaubt()
    {
        var (client, seriesId) = await ReiheAsync(factory);

        var response = await client.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Unit 1", topics = new[] { new string('a', 200) } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Mehr_Als_50_Themen_Werden_Abgewiesen()
    {
        var (client, seriesId) = await ReiheAsync(factory);

        var response = await client.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Unit 1", topics = Enumerable.Range(1, 51).Select(i => $"Thema {i}").ToArray() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", await CodeOfAsync(response));
    }

    /// <summary>The PATCH is the second way in - it used to skip the same guard, so it carries its own case.</summary>
    [Fact]
    public async Task Auch_Das_Aendern_Weist_Ein_Zu_Langes_Thema_Ab()
    {
        var (client, seriesId) = await ReiheAsync(factory);
        var unitId = await TestApi.IdAsync(await client.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1" }));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}",
            new { topics = new[] { new string('b', 201) } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", await CodeOfAsync(response));
    }
}
