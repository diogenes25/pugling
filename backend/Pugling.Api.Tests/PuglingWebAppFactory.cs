using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Pugling.Api.Tests;

/// <summary>
/// Starts the API in-process against a fresh, isolated SQLite file per test class.
/// The real <c>pugling.db</c> stays untouched; environment = Development (seed runs, dev JWT key applies).
/// </summary>
public sealed class PuglingWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pugling_test_{Guid.NewGuid():N}.db");
    // Auch die hochgeladenen Bilder in einen Wegwerf-Ordner: sonst schriebe jeder Testlauf in den
    // Medien-Ordner des Entwicklungsbaums und ließe dort Dateien liegen.
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"pugling_media_{Guid.NewGuid():N}");

    /// <summary>
    /// This host's clock – the real one by default. Test classes that check a rule in the seconds
    /// range (speed bonus) freeze it and advance it themselves.
    /// </summary>
    public TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Media:RootPath", _mediaPath);
        // Der In-Process-TestServer teilt sich eine IP-Partition; ohne Abschalten würden die vielen
        // Test-Logins am Login-Rate-Limit (429) scheitern. Ein eigener Test aktiviert es gezielt.
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        // Wanduhr-Neutralisierung: die Zeitfenster gewichten die Punkte über den Tag (Vormittag ×1,5,
        // Abend ×0,8). Damit hinge die Punktzahl derselben richtigen Antwort an der Uhrzeit des Testlaufs –
        // ein Lauf um 9 Uhr buchte 15, einer um 19 Uhr 8. Für „> 0" harmlos, für die von
        // DocsCaptureTests **eingecheckte** Doku nicht: das wäre Diff-Rauschen in docs/api-examples/.
        // Bis E12 war das ein `db.TimeSlots.ExecuteDelete()` NACH dem Start – jetzt eine Einstellung
        // VOR ihm, weil die Fenster Konfiguration sind und keine Tabelle mehr.
        builder.UseSetting("Scoring:TimeSlotsEnabled", "false");
        // Zählt prozessweit mit, welche Actions erfolgreich bedient wurden – Datenquelle des
        // Abdeckungs-Wächters (EndpointCoverageGuard). Rein beobachtend, ändert kein Verhalten.
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IStartupFilter, EndpointCoverageStartupFilter>();
            s.AddSingleton<TimeProvider>(Clock);
        });
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
