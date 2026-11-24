using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-744: the migration <c>KlassenarbeitPredictedLevel</c> applied to a database with an existing class test. Nothing is
/// backfilled: a prediction written today for a test of the past would compare its grade with a level it never predicted.
/// Same pattern as <see cref="PrepLeftOutMigrationTests"/>: fully migrated and seeded, taken back one step and forward again.
/// </summary>
public sealed class PredictedLevelMigrationTests
{
    private const string Before = "20261124131013_ClassTestPrepLeftOut";
    private const string Target = "20261124201304_KlassenarbeitPredictedLevel";

    /// <summary>Red when the migration's <c>Up</c> fills the prediction of existing tests or touches their other columns.</summary>
    [Fact]
    public async Task Bestehende_Arbeit_Bleibt_Ohne_Prognose()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-predictedlevel-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            var start = new DateOnly(2026, 11, 20);
            int testId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                await db.SaveChangesAsync();
                var test = new Klassenarbeit
                {
                    ChildId = child.Id,
                    Title = "Vokabeltest",
                    ScheduledDate = start.AddDays(5),
                    PrepStartedOn = start,
                    PrepWindowDays = 6,
                };
                db.Klassenarbeiten.Add(test);
                await db.SaveChangesAsync();
                testId = test.Id;

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var test = await db.Klassenarbeiten.AsNoTracking().Where(k => k.Id == testId)
                    .Select(k => new { k.PrepStartedOn, k.PrepWindowDays, k.ScheduledDate, k.PredictedLevel }).SingleAsync();
                Assert.Equal(start, test.PrepStartedOn);
                Assert.Equal(6, test.PrepWindowDays);
                Assert.Equal(start.AddDays(5), test.ScheduledDate);
                Assert.Null(test.PredictedLevel);
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
