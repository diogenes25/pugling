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
    // Auch die hochgeladenen Bilder in einen Wegwerf-Ordner: sonst schriebe jeder Testlauf in den
    // Medien-Ordner des Entwicklungsbaums und ließe dort Dateien liegen.
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"pugling_media_{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Media:RootPath", _mediaPath);
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
        try { if (Directory.Exists(_mediaPath)) Directory.Delete(_mediaPath, recursive: true); }
        catch { /* dito */ }
    }
}
