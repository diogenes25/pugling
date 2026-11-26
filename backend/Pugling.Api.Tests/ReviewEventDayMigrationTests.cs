using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-1028 AK 4: the migration gives every existing answer the German day of its timestamp - until then every answer was
/// written on its own day. Red if the backfill is left out (the default day) or takes the UTC date.
/// </summary>
public class ReviewEventDayMigrationTests
{
    private const string Before = "20261126113424_Tagesauswertung";
    private const string Target = "20261126162132_ReviewEventDay";

    [Fact]
    public async Task Bestandsantworten_bekommen_den_deutschen_Tag_ihres_Zeitpunkts()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-reviewday-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            await using (var db = new PuglingDbContext(options))
            {
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                // Raw rows: the model already knows the new column, the database does not yet. Only the backfill is under
                // test, so the rows need no child (one open connection keeps the pragma).
                await db.Database.OpenConnectionAsync();
                await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                foreach (var at in new[] { "2026-11-25 23:30:00.1234567", "2026-11-26 07:01:35.2220719", "2026-07-01 22:30:00" })
                {
                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO ItemReviewEvents (ChildId, ExerciseId, VocabularyId, Source, StageValue, WasCorrect, AccentSlip, At) " +
                        "VALUES (1, 1, 1, 0, 0, 0, 0, {0});", at);
                }
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var days = await db.ItemReviewEvents.AsNoTracking().OrderBy(e => e.Id).Select(e => e.Day).ToListAsync();
                // 23:30 UTC in winter is 00:30 in Germany, 22:30 UTC in summer is 00:30 too.
                Assert.Equal([new DateOnly(2026, 11, 26), new DateOnly(2026, 11, 26), new DateOnly(2026, 7, 2)], days);
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
