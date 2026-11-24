using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-947, AK 7: the migration <c>ClassTestPrepLeftOut</c> applied to a database with an already started class test. Nothing
/// is backfilled: a snapshot written for an old start would pull in exercises the father never saw left out. Same pattern as
/// <see cref="VorbereitungsPflichtMigrationTests"/>: fully migrated and seeded, taken back one step and forward again.
/// </summary>
public sealed class PrepLeftOutMigrationTests
{
    private const string Before = "20261124022042_ClassTestPrepDuty";
    private const string Target = "20261124131013_ClassTestPrepLeftOut";

    /// <summary>Red when the migration's <c>Up</c> writes a snapshot for existing tests or touches their start.</summary>
    [Fact]
    public async Task Gestartete_Arbeit_Bleibt_Ohne_Momentaufnahme()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-prepleftout-{Guid.NewGuid():N}.db");
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
                    .Select(k => new { k.PrepStartedOn, k.PrepWindowDays, k.PrepLeftOutExerciseIds }).SingleAsync();
                Assert.Equal(start, test.PrepStartedOn);
                Assert.Equal(6, test.PrepWindowDays);
                Assert.Null(test.PrepLeftOutExerciseIds);
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
