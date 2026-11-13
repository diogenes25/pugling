using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Pugling.Api.Tests;

/// <summary>
/// The SPA fallback must not swallow API paths. With a built frontend in wwwroot, every unknown path loads
/// index.html for the client-side router - but an unknown <c>/api/…</c> path answered that way is a 200 with HTML,
/// and a frontend newer than the running API (a stale published build) then fails on a JSON parse error instead of
/// a 404 that names the problem.
/// </summary>
public class SpaFallbackTests
{
    /// <summary>
    /// Red without the <c>/api/{**rest}</c> fallback in Program.cs: the unknown API path then returns the index.html
    /// placed here with 200. The SPA route stays served, so the fix does not simply break the fallback.
    /// </summary>
    [Fact]
    public async Task Unbekannter_ApiPfad_Ist_404_Problem_Und_Nicht_Die_Spa()
    {
        var webRoot = Directory.CreateTempSubdirectory("pugling-webroot-").FullName;
        await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), "<!doctype html><title>spa</title>");
        await using var factory = new PuglingWebAppFactory();
        try
        {
            var client = factory.WithWebHostBuilder(b => b.UseSetting(WebHostDefaults.WebRootKey, webRoot)).CreateClient();

            var spa = await client.GetAsync("/sohn/bonus");
            Assert.Equal(HttpStatusCode.OK, spa.StatusCode);
            Assert.Contains("<title>spa</title>", await spa.Content.ReadAsStringAsync());

            var api = await client.GetAsync("/api/v1/student/children/1/no-such-endpoint");
            Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
            var body = JsonDocument.Parse(await api.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(404, body.GetProperty("status").GetInt32());
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }
}
