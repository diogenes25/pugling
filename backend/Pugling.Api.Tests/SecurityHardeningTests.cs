using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Pugling.Api.Auth;

namespace Pugling.Api.Tests;

/// <summary>
/// Baseline-Härtung: PINs werden gehasht gespeichert (mit Klartext-Fallback für Alt-Konten) und das
/// Login ist gegen Brute-Force ratenbegrenzt.
/// </summary>
public class SecurityHardeningTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public void PinHasher_HashUndVerify_RoundTrip()
    {
        var hash = PinHasher.Hash("1234");
        Assert.NotEqual("1234", hash);                // nicht im Klartext gespeichert
        Assert.True(PinHasher.Verify("1234", hash));  // richtige PIN
        Assert.False(PinHasher.Verify("0000", hash)); // falsche PIN
    }

    [Fact]
    public void PinHasher_Verify_AkzeptiertAltKlartext()
    {
        // Vor der Umstellung gespeicherte Klartext-PIN bleibt nutzbar (kein Aussperren).
        Assert.True(PinHasher.Verify("0000", "0000"));
        Assert.False(PinHasher.Verify("9999", "0000"));
    }

    [Fact]
    public async Task GeseederterLogin_FunktioniertMitGehashterPin()
    {
        // Der Seed hasht die PIN "0000"; das Login muss weiterhin durchgehen.
        var father = await TestApi.FatherAsync(_factory);
        var res = await father.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_UeberschreitetRateLimit_Liefert429()
    {
        // Rate-Limit gezielt aktivieren (die Default-Factory schaltet es für die übrige Suite ab).
        using var limited = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:LoginEnabled", "true"));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "falsch" });
            statuses.Add(res.StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, statuses[0]);     // erste Versuche erlaubt (nur PIN falsch)
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);  // die Brute-Force-Bremse greift
    }
}
