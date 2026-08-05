using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Coverage of the versioning scaffolding (URL segment /api/v1/…) and the unified
/// error schema (RFC-compliant application/problem+json instead of bare strings).
/// </summary>
public class ApiVersioningTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task V1_IstErreichbar_UnbekannteVersion_Wird_Abgelehnt()
    {
        var father = await TestApi.AdultAsync(factory);

        // A declared version works.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync("/api/v1/creator/subjects")).StatusCode);

        // An undeclared version → rejected (no matching controller → 404).
        var v2 = await father.GetAsync("/api/v2/learn/subjects");
        Assert.Equal(HttpStatusCode.NotFound, v2.StatusCode);
    }

    [Fact]
    public async Task FachFehler_LiefertStrukturiertesProblemDetails()
    {
        var father = await TestApi.AdultAsync(factory);

        // An empty name → 400 with a structured ProblemDetails body (not a bare string).
        var res = await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Name is required.", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("title", out _)); // RFC-7807-Felder vorhanden
    }
}
