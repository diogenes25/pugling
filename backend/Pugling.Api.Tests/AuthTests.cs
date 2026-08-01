using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Integration tests for the PIN login and the self-info lookup.</summary>
public class AuthTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task LoginFather_MitSeedZugangsdaten_LiefertToken()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 1, pin = "0000" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("Supervisor", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task LoginFather_MitFalscherPin_Liefert401()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 1, pin = "9999" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task LoginAdult_MitNichtNumerischerId_LiefertSauberesEnglischesProblem()
    {
        // A regression guard for the InvalidModelStateResponseFactory: an adultId that cannot be converted to
        // int ("1a") must (1) NOT leak the internal DTO type name, (2) NOT show the misleading "The dto field
        // is required." and (3) has to return an English message.
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = "1a", pin = "0000" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var raw = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Pugling.Api", raw);          // no type name leak
        Assert.DoesNotContain("could not be converted", raw); // no raw System.Text.Json message

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid request.", body.GetProperty("title").GetString());
        var errors = body.GetProperty("errors");
        Assert.False(errors.TryGetProperty("dto", out _));  // no misleading "field is required"
        Assert.Equal("The value is not of the expected type.",
            errors.GetProperty("adultId")[0].GetString());
    }

    [Fact]
    public async Task Me_OhneToken_Liefert401()
    {
        // Regression guard: /api/v1/auth/me was open for a while through a class-level [AllowAnonymous].
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
