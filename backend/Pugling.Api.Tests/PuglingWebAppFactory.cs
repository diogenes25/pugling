using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pugling.Api.Tests;

/// <summary>
/// Startet die API in-process gegen eine frische, isolierte SQLite-Datei je Testklasse.
/// Die echte <c>pugling.db</c> bleibt unberührt; Umgebung = Development (Seed läuft, Dev-JWT-Key greift).
/// </summary>
public sealed class PuglingWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pugling_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        // Der In-Process-TestServer teilt sich eine IP-Partition; ohne Abschalten würden die vielen
        // Test-Logins am Login-Rate-Limit (429) scheitern. Ein eigener Test aktiviert es gezielt.
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        foreach (var file in new[] { _dbPath, $"{_dbPath}-shm", $"{_dbPath}-wal" })
        {
            try { if (File.Exists(file)) File.Delete(file); }
            catch { /* Temp-Datei; Aufräumen ist best effort. */ }
        }
    }
}
