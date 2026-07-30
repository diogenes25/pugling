using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pugling.Api.Data;

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
        // Zählt prozessweit mit, welche Actions erfolgreich bedient wurden – Datenquelle des
        // Abdeckungs-Wächters (EndpointCoverageGuard). Rein beobachtend, ändert kein Verhalten.
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IStartupFilter, EndpointCoverageStartupFilter>();
            s.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>
    /// After startup, <b>delete the seeded time slots</b> (wall-clock neutralization).
    /// <para>
    /// The seed sets up multipliers across the day (morning ×1.5, afternoon ×1.0, evening ×0.8). That
    /// makes the score for the same correct answer depend on the time of day the test run happens at –
    /// a run at 9 am booked 15, one at 7 pm booked 8. For assertions on "&gt; 0" that's harmless, but not
    /// for the docs <b>checked in</b> by <see cref="DocsCaptureTests"/>: every run at a different time of
    /// day produced diff noise in <c>docs/api-examples/</c>. Without a time slot, the factor is 1.0.
    /// </para>
    /// Tests that check the multiplier itself set up their own time slots explicitly
    /// (<see cref="ScoringTimeSlotTests"/>) – making them independent of the wall clock regardless.
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
