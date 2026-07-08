using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Absicherung des Versionierungs-Gerüsts (URL-Segment /api/v1/…) und des einheitlichen
/// Fehlerschemas (RFC-konformes application/problem+json statt nackter Strings).
/// </summary>
public class ApiVersioningTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task V1_IstErreichbar_UnbekannteVersion_Wird_Abgelehnt()
    {
        var father = await TestApi.FatherAsync(factory);

        // Deklarierte Version funktioniert.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync("/api/v1/creator/subjects")).StatusCode);

        // Nicht deklarierte Version → wird abgewiesen (kein passender Controller → 404).
        var v2 = await father.GetAsync("/api/v2/learn/subjects");
        Assert.Equal(HttpStatusCode.NotFound, v2.StatusCode);
    }

    [Fact]
    public async Task FachFehler_LiefertStrukturiertesProblemDetails()
    {
        var father = await TestApi.FatherAsync(factory);

        // Leerer Name → 400 mit strukturiertem ProblemDetails-Body (nicht nacktem String).
        var res = await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Name is required.", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("title", out _)); // RFC-7807-Felder vorhanden
    }
}
