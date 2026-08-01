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
    // The uploaded images go into a throwaway folder as well: otherwise every test run would write into the
    // media folder of the development tree and leave files behind there.
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
        // The in-process test server shares one IP partition; without switching it off the many test logins
        // would fail at the login rate limit (429). A dedicated test enables it deliberately.
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        // Neutralizing the wall clock: the time slots weight the points over the day (morning ×1.5, evening
        // ×0.8). The score of the same correct answer would then hang on the time of the test run - a run at
        // 9 a.m. booked 15, one at 7 p.m. booked 8. Harmless for "> 0", not for the documentation **checked in**
        // by DocsCaptureTests: that would be diff noise in docs/api-examples/. Until E12 this was a
        // `db.TimeSlots.ExecuteDelete()` AFTER startup - now it is a setting BEFORE it, because the slots are
        // configuration and no longer a table.
        builder.UseSetting("Scoring:TimeSlotsEnabled", "false");
        // Counts process-wide which actions were served successfully - the data source of the coverage guard
        // (EndpointCoverageGuard). Purely observing, it changes no behavior.
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
