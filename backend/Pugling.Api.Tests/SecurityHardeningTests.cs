using System.Net;
using System.Net.Http.Json;
using Pugling.Api.Auth;

namespace Pugling.Api.Tests;

/// <summary>
/// Baseline hardening: PINs are stored hashed (with a plaintext fallback for legacy accounts), and
/// login is rate-limited against brute force.
/// </summary>
public class SecurityHardeningTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public void PinHasher_HashUndVerify_RoundTrip()
    {
        var hash = PinHasher.Hash("1234");
        Assert.NotEqual("1234", hash);                // not stored in clear text
        Assert.True(PinHasher.Verify("1234", hash));  // the right PIN
        Assert.False(PinHasher.Verify("0000", hash)); // the wrong PIN
    }

    [Fact]
    public void PinHasher_Verify_AkzeptiertAltKlartext()
    {
        // A plaintext PIN stored before the switch stays usable (nobody gets locked out).
        Assert.True(PinHasher.Verify("0000", "0000"));
        Assert.False(PinHasher.Verify("9999", "0000"));
    }

    [Fact]
    public async Task GeseederterLogin_FunktioniertMitGehashterPin()
    {
        // The seed hashes the PIN "0000"; the login must still go through.
        var father = await TestApi.AdultAsync(_factory);
        var res = await father.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_UeberschreitetRateLimit_Liefert429()
    {
        // Enable the rate limit deliberately (the default factory switches it off for the rest of the suite).
        using var limited = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:LoginEnabled", "true"));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 1, pin = "falsch" });
            statuses.Add(res.StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, statuses[0]);     // the first attempts are allowed (only the PIN is wrong)
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);  // the brute-force throttle bites
    }
}
