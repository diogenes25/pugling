using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pugling.Api.Data;

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

    /// <summary>
    /// Die Uhr dieses Hosts – standardmäßig die echte. Testklassen, die eine Regel im Sekunden-Bereich
    /// prüfen (Schnelle-Antwort-Bonus), frieren sie ein und rücken sie selbst vor.
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
        // Zählt prozessweit mit, welche Actions erfolgreich bedient wurden – Datenquelle des
        // Abdeckungs-Wächters (EndpointCoverageGuard). Rein beobachtend, ändert kein Verhalten.
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IStartupFilter, EndpointCoverageStartupFilter>();
            s.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>
    /// Nach dem Start die <b>geseedeten Zeitfenster löschen</b> (Wanduhr-Neutralisierung).
    /// <para>
    /// Der Seed legt Multiplikatoren über den Tag (Vormittag ×1,5, Nachmittag ×1,0, Abend ×0,8). Damit
    /// hängt die Punktzahl derselben richtigen Antwort an der Uhrzeit des Testlaufs – ein Lauf um 9 Uhr
    /// buchte 15, einer um 19 Uhr 8. Für Zusicherungen auf „&gt; 0" ist das harmlos, für die von
    /// <see cref="DocsCaptureTests"/> <b>eingecheckte</b> Doku nicht: Jeder Lauf zu anderer Tageszeit
    /// erzeugte Diff-Rauschen in <c>docs/api-examples/</c>. Ohne Fenster gilt Faktor 1,0.
    /// </para>
    /// Tests, die den Multiplikator selbst prüfen, legen ihre Fenster ausdrücklich an
    /// (<see cref="ScoringTimeSlotTests"/>) – die sind damit erst recht unabhängig von der Wanduhr.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder); // startet den Host – Migrate + Seed sind danach durch
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.TimeSlots.ExecuteDelete();
        return host;
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
