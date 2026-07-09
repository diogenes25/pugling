using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Integrationstests für den PIN-Login und die Selbstauskunft.</summary>
public class AuthTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task LoginFather_MitSeedZugangsdaten_LiefertToken()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "0000" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("Supervisor", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task LoginFather_MitFalscherPin_Liefert401()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "9999" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task LoginFather_MitNichtNumerischerId_LiefertSauberesEnglischesProblem()
    {
        // Regressionsschutz für die InvalidModelStateResponseFactory: Ein nicht in int konvertierbarer
        // fatherId ("1a") darf (1) NICHT den internen DTO-Typnamen leaken, (2) NICHT das irreführende
        // "The dto field is required." zeigen und (3) muss eine englische Meldung liefern.
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = "1a", pin = "0000" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var raw = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Pugling.Api", raw);          // kein Typnamen-Leak
        Assert.DoesNotContain("could not be converted", raw); // keine rohe System.Text.Json-Meldung

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid request.", body.GetProperty("title").GetString());
        var errors = body.GetProperty("errors");
        Assert.False(errors.TryGetProperty("dto", out _));  // kein irreführendes "field is required"
        Assert.Equal("The value is not of the expected type.",
            errors.GetProperty("fatherId")[0].GetString());
    }

    [Fact]
    public async Task Me_OhneToken_Liefert401()
    {
        // Regressionsschutz: /api/v1/auth/me war zwischenzeitlich durch [AllowAnonymous] auf Klassenebene offen.
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
