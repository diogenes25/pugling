using System.Net;
using System.Net.Http.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-125: the other half of what a reverse proxy hides. B-119 taught the app the real client address
/// (<c>X-Forwarded-For</c>); this covers the scheme. Behind a TLS-terminating front end - Azure App
/// Service - the hop Kestrel sees is plain HTTP, so every absolute URL the server builds from
/// <c>Request.Scheme</c> would say <c>http://</c> and send a client back down from HTTPS.
/// Asserting the <c>Location</c> header rather than the middleware options is deliberate: reading the
/// registered <c>ForwardedHeadersOptions</c> back out of DI would only restate the line under test.
/// </summary>
public class ForwardedProtoTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Hinter_Tls_Terminierung_Traegt_Der_Location_Header_Https()
    {
        var creator = await TestApi.AdultAsync(factory);
        creator.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        var response = await creator.PostAsJsonAsync("/api/v1/creator/publishers",
            new { name = TestApi.UniqueName("Verlag") });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.StartsWith("https://", location);
    }

    /// <summary>
    /// Without the header nothing changes - the scheme still comes from the connection. Guards against a
    /// "fix" that hard-codes HTTPS and would break local development over plain HTTP.
    /// </summary>
    [Fact]
    public async Task Ohne_Header_Bleibt_Das_Schema_Der_Verbindung()
    {
        var creator = await TestApi.AdultAsync(factory);

        var response = await creator.PostAsJsonAsync("/api/v1/creator/publishers",
            new { name = TestApi.UniqueName("Verlag") });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.StartsWith("http://", response.Headers.Location?.ToString());
    }
}
