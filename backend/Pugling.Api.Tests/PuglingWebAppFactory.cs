using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Pugling.Api.Tests;

/// <summary>
/// Shared base of every in-process host in this test project: throwaway SQLite file, throwaway media
/// folder, the coverage filter, and the test clock.
/// <para>
/// <b>Why a base class and not a second factory next to it.</b> The two settings below are what keeps a
/// test run away from the developer's own data. Miss <c>ConnectionStrings:Default</c> in a copy and
/// <c>appsettings.json</c> takes over with <c>Data Source=pugling.db</c> relative to the content root –
/// the test then migrates the <b>real</b> <c>backend/Pugling.Api/pugling.db</c>. That is not a visible
/// test failure; it is a broken development database. The same holds for <c>Media:RootPath</c>, which
/// otherwise fills the development tree with uploads. Both therefore exist exactly once, and a derived
/// factory cannot forget them: <see cref="ConfigureWebHost"/> is <c>sealed</c>, so
/// <see cref="ConfigureFactory"/> is the only hook. Without the seal a new factory could override
/// <c>ConfigureWebHost</c>, forget the <c>base</c> call, and the sentence above would be a wish.
/// </para>
/// </summary>
public abstract class PuglingWebAppFactoryBase : WebApplicationFactory<Program>
{
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"pugling_media_{Guid.NewGuid():N}");

    /// <summary>
    /// The SQLite file this host runs against – a fresh throwaway file per factory instance unless a
    /// derived factory hands one in (the migration-chain probe prepares its own).
    /// </summary>
    protected string DbPath { get; }

    /// <summary>Creates a host against a fresh throwaway database file.</summary>
    protected PuglingWebAppFactoryBase()
        : this(Path.Combine(Path.GetTempPath(), $"pugling_test_{Guid.NewGuid():N}.db"))
    {
    }

    /// <summary>Creates a host against a prepared database file, which is deleted again on dispose.</summary>
    /// <param name="dbPath">Path of the SQLite file to run against.</param>
    protected PuglingWebAppFactoryBase(string dbPath) => DbPath = dbPath;

    /// <summary>
    /// This host's clock – the real one by default. Test classes that check a rule in the seconds
    /// range (speed bonus) freeze it and advance it themselves.
    /// </summary>
    public TestClock Clock { get; } = new();

    /// <summary>The ASP.NET environment name; it decides every <c>IsDevelopment()</c> branch in Program.cs.</summary>
    protected abstract string EnvironmentName { get; }

    /// <summary>
    /// Hook for the settings that make a derived factory what it is – everything except the two protective
    /// ones, which are applied <b>afterwards</b> and therefore win.
    /// </summary>
    /// <param name="builder">The host builder being configured.</param>
    protected virtual void ConfigureFactory(IWebHostBuilder builder) { }

    /// <inheritdoc />
    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        // Counts process-wide which actions were served successfully - the data source of the coverage guard
        // (EndpointCoverageGuard). Purely observing, it changes no behavior.
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IStartupFilter, EndpointCoverageStartupFilter>();
            s.AddSingleton<TimeProvider>(Clock);
        });
        ConfigureFactory(builder);

        // Deliberately **last**: the later `UseSetting` wins, so a derived factory cannot displace these two
        // even by accident. A different database file is set through the constructor, not here - nothing
        // legitimate needs to override them.
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DbPath}");
        builder.UseSetting("Media:RootPath", _mediaPath);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) CleanUp();
    }

    /// <summary>
    /// The async counterpart – <b>and the one that actually runs.</b> xUnit disposes a fixture through
    /// <see cref="IAsyncDisposable"/> when the type offers it, and the base
    /// <see cref="WebApplicationFactory{TEntryPoint}.DisposeAsync()"/> does <b>not</b> route through the
    /// protected <c>Dispose(bool)</c>. Without this override the cleanup below was dead code for every
    /// class fixture.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        CleanUp();
    }

    /// <summary>
    /// Deletes the throwaway database and media folder of this host.
    /// <para>
    /// <b>Why the pool has to be cleared first.</b> The connection pool keeps the file handle open past the
    /// host's last <c>Close()</c>; <c>File.Delete</c> then fails silently in the catch below and the temp
    /// folder fills up with one database per test class. Measured on 2026-08-01 before this fix:
    /// <b>20 880 orphans, 14 GB</b>, accumulated since 4 July.
    /// </para>
    /// <para>
    /// <b><see cref="SqliteConnection.ClearPool"/>, not <c>ClearAllPools</c>.</b> The latter is
    /// process-wide, and xUnit runs test classes in parallel – one factory disposing would reach into the
    /// pools of every host still running. That was measured too: three to four unrelated tests fell per
    /// run. The two other candidates are worse: <c>Pooling=False</c> on the connection string pushed the
    /// suite from ~1 to <b>3 minutes</b> (EF reopens the file for every command), and leaving it be is the
    /// 14 GB above.
    /// </para>
    /// </summary>
    private void CleanUp()
    {
        // Same connection string as the host, so this hits exactly this host's pool and no other.
        using (var own = new SqliteConnection($"Data Source={DbPath}")) SqliteConnection.ClearPool(own);
        foreach (var file in new[] { DbPath, $"{DbPath}-shm", $"{DbPath}-wal" })
        {
            try { if (File.Exists(file)) File.Delete(file); }
            catch { /* Temp file; cleaning up is best effort. */ }
        }
        try { if (Directory.Exists(_mediaPath)) Directory.Delete(_mediaPath, recursive: true); }
        catch { /* ditto */ }
    }
}

/// <summary>
/// Starts the API in-process against a fresh, isolated SQLite file per test class.
/// The real <c>pugling.db</c> stays untouched; environment = Development (seed runs, dev JWT key applies).
/// </summary>
public sealed class PuglingWebAppFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
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
        // Same reason, second source: the daily box draws coins/gems from [Min,Max] via `Random.Shared`, so the
        // checked-in example under docs/api-examples/ moved with every run (measured: coins 10 vs. 27, gems 2 vs.
        // 0) and D4 failed in CI on an unrelated change. Collapsing each range to a single value keeps the draw
        // itself in play but makes it constant; the streak tier still multiplies it, which is the part the tests
        // are about.
        builder.UseSetting("Gamification:DailyBox:MinCoins", "20");
        builder.UseSetting("Gamification:DailyBox:MaxCoins", "20");
        builder.UseSetting("Gamification:DailyBox:MinGems", "2");
        builder.UseSetting("Gamification:DailyBox:MaxGems", "2");
    }
}

/// <summary>
/// Same as <see cref="PuglingWebAppFactory"/> (login rate limit off, time slots neutralized), but additionally
/// switches off <c>OpenApi:ExamplesEnabled</c> (B-57): for test classes that only read <c>paths</c> or
/// <c>components.schemas</c> from the live document (<see cref="ClientRouteGuardTests"/>,
/// <see cref="ErrorCodeTests"/>) - never the examples themselves - this removes their otherwise unnecessary
/// exposure to <c>OpenApiExampleCatalog.Load</c> racing <c>DocsCaptureTests</c>' write of the very file it
/// reads, in the same parallel test run. <c>PuglingWebAppFactory</c> itself cannot be reused here: it is
/// <c>sealed</c>, so a distinct factory type is the only way to opt out for just these two classes.
/// </summary>
public sealed class SchemaOnlyWebAppFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        builder.UseSetting("Scoring:TimeSlotsEnabled", "false");
        builder.UseSetting("OpenApi:ExamplesEnabled", "false");
    }
}
