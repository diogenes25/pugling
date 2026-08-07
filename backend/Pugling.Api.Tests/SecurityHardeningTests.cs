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

    /// <summary>
    /// B-48: the two anonymous registration endpoints carry the same throttle as the login. Without it a
    /// script could create accounts without limit, or squat the e-mail addresses of real people - the
    /// uniqueness check then rejects the person whose address it is.
    /// </summary>
    [Theory]
    [InlineData("/api/v1/supervisor/adults")]
    [InlineData("/api/v1/creator/teacher-accounts")]
    public async Task AnonymeRegistrierung_UeberschreitetRateLimit_Liefert429(string url)
    {
        using var limited = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:LoginEnabled", "true"));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            // Valid payloads on purpose: a rejected request must not be what stops the script - the
            // throttle has to bite on requests that would otherwise succeed.
            var res = await client.PostAsJsonAsync(url, new { name = $"Bot {i}", pin = "4711" });
            statuses.Add(res.StatusCode);
        }

        Assert.Equal(HttpStatusCode.Created, statuses[0]);          // the first registration goes through
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);  // the throttle bites within the same minute
    }

    /// <summary>
    /// B-100 (AC4): a token sitting in a shared/proxy cache is a real small hole - the three login actions
    /// and <c>auth/me</c> (the one GET that echoes identity) must never be cached.
    /// </summary>
    [Fact]
    public async Task Login_Und_Me_Tragen_CacheControlNoStore()
    {
        var father = await TestApi.AdultAsync(_factory);
        var login = await father.PostAsJsonAsync("/api/v1/auth/login", new { accountId = 1, pin = "0000" });
        Assert.True(login.Headers.CacheControl?.NoStore, "POST auth/login must send Cache-Control: no-store.");

        var me = await father.GetAsync("/api/v1/auth/me");
        Assert.True(me.Headers.CacheControl?.NoStore, "GET auth/me must send Cache-Control: no-store.");

        var badAdultLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 1, pin = "falsch" });
        Assert.True(badAdultLogin.Headers.CacheControl?.NoStore, "POST auth/adult must send Cache-Control: no-store even on failure.");

        var badChildLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/child", new { childId = 1, pin = "falsch" });
        Assert.True(badChildLogin.Headers.CacheControl?.NoStore, "POST auth/child must send Cache-Control: no-store even on failure.");
    }
}
