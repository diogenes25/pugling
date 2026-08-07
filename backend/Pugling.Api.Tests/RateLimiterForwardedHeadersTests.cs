using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace Pugling.Api.Tests;

/// <summary>
/// B-119: the "login" rate-limiter policy partitions by <c>Connection.RemoteIpAddress</c>. Behind Azure App
/// Service, that address is always the loopback hop from IIS/ANCM - without honoring
/// <c>X-Forwarded-For</c>, every real client would share one partition. TestServer's own connection address
/// is loopback too, which happens to match exactly the scenario the fix targets (Program.cs only trusts the
/// header from a loopback hop) - no extra proxy configuration needed to prove it here.
/// </summary>
public class RateLimiterForwardedHeadersTests(RateLimitedFactory factory) : IClassFixture<RateLimitedFactory>
{
    /// <summary><c>FixedWindowRateLimiterOptions</c> for the "login" policy in Program.cs.</summary>
    private const int PermitLimit = 10;

    [Fact]
    public async Task Verschiedene_Forwarded_Ips_Bekommen_Getrennte_Partitionen()
    {
        var exhausted = await ExhaustLimitAsync(forwardedFor: "203.0.113.10");
        Assert.Equal(HttpStatusCode.TooManyRequests, exhausted);

        // A different forwarded client must NOT already be throttled by the first one's window - if the
        // partition key still fell back to the shared TestServer connection address, this would also 429.
        using var other = factory.CreateClient();
        other.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");
        var response = await other.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 999999, pin = "0000" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Fires <see cref="PermitLimit"/> + 1 requests under one forwarded address and returns the last status.</summary>
    private async Task<HttpStatusCode> ExhaustLimitAsync(string forwardedFor)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        HttpStatusCode last = HttpStatusCode.OK;
        for (var i = 0; i <= PermitLimit; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 999999, pin = "0000" });
            last = response.StatusCode;
        }
        return last;
    }
}

/// <summary>
/// The only factory in this suite that leaves the "login" rate limiter switched <b>on</b> - every other
/// factory disables it (<see cref="PuglingWebAppFactory"/>) because its many logins would otherwise share
/// one partition and 429 each other. This one exists exactly to exercise that partitioning.
/// </summary>
public sealed class RateLimitedFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder) =>
        builder.UseSetting("RateLimiting:LoginEnabled", "true");
}
