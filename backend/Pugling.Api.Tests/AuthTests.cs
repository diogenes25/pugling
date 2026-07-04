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
        Assert.Equal("Vater", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task LoginFather_MitFalscherPin_Liefert401()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "9999" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
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
