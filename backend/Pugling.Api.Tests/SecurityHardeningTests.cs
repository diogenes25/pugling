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

    /// <summary>
    /// B-282, point 4: unknown id, missing account and disabled login (<c>null</c>/empty stored PIN) cost the same
    /// PBKDF2 work as a wrong PIN, so the response time does not reveal which case it was. Without the decoy the early
    /// paths take microseconds against tens of milliseconds, so the generous factor of 0.3 does not flicker. The paths are
    /// measured interleaved, nine rounds, fastest per path (B-438): measured one after another, a load spike over the
    /// wrong-PIN block alone once made the decoy look three times faster. Red if the decoy derivation in
    /// <c>PinHasher.Verify</c> is removed.
    /// </summary>
    [Fact]
    public void PinHasher_Verify_RechnetAuchOhneGespeichertenHash()
    {
        var hash = PinHasher.Hash("1234");
        (string Name, string? Stored)[] paths =
            [("wrong PIN", hash), ("unknown account", null), ("disabled login", ""), ("legacy plaintext", "1111")];
        var fastest = paths.ToDictionary(p => p.Name, _ => long.MaxValue);
        foreach (var (_, stored) in paths) Assert.False(PinHasher.Verify("0000", stored)); // warm-up
        for (var round = 0; round < 9; round++)
            foreach (var (name, stored) in paths)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Assert.False(PinHasher.Verify("0000", stored));
                fastest[name] = Math.Min(fastest[name], watch.ElapsedTicks);
            }

        var wrongPin = fastest["wrong PIN"];
        foreach (var (name, _) in paths.Skip(1))
            Assert.True(fastest[name] > wrongPin * 0.3, name);
    }

    /// <summary>
    /// B-282, point 4, in the controllers: each login action verifies BEFORE it branches on a missing row, so an
    /// unknown id costs the same PBKDF2 work as a wrong PIN on a known one. Red if one action goes back to
    /// <c>if (x is null || account is null || !PinHasher.Verify(...))</c> – the unknown id then skips the derivation.
    /// Fastest of five requests per path; the gap without the fix is a full PBKDF2 run (tens of milliseconds).
    /// </summary>
    [Theory]
    [InlineData("adult", "adultId")]
    [InlineData("child", "childId")]
    [InlineData("login", "accountId")]
    public async Task Login_mit_unbekannter_Id_kostet_so_viel_wie_eine_falsche_PIN(string route, string idField)
    {
        var client = _factory.CreateClient();
        async Task<long> Fastest(int id)
        {
            var body = new Dictionary<string, object> { [idField] = id, ["pin"] = "9876" };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync($"/api/v1/auth/{route}", body)).StatusCode);
            var best = long.MaxValue;
            for (var i = 0; i < 5; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var res = await client.PostAsJsonAsync($"/api/v1/auth/{route}", body);
                best = Math.Min(best, watch.ElapsedTicks);
                Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
            }
            return best;
        }

        // Id 1 exists for all three (seeded father, seeded child, the father's account); 999999 exists for none.
        var wrongPin = await Fastest(1);
        Assert.True(await Fastest(999_999) > wrongPin * 0.3, $"auth/{route}: an unknown id answered without PBKDF2 work");
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
