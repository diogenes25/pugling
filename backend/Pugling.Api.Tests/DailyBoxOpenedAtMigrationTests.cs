using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-527, AK 6: the migration <c>DailyBoxOpenedAt</c> applied to a database that already holds a reward box claim.
/// Before B-527 a claim was booked to the ledger the moment it was earned, so an existing claim must come out opened -
/// otherwise it would wait as a chest and be credited a second time. The chain is run on a throwaway SQLite file:
/// fully migrated and seeded through the DbContext (raw inserts would have to name every NOT NULL column), then taken
/// back one step - which drops the column - and forward again, so the claim meets the migration as a pre-B-527 row.
/// </summary>
public sealed class DailyBoxOpenedAtMigrationTests
{
    private const string Before = "20261114092721_ExtraRunContentHash";
    private const string Target = "20261114114551_DailyBoxOpenedAt";

    /// <summary>
    /// Red when the <c>UPDATE … SET OpenedAt = AwardedAt</c> is missing from the migration's <c>Up</c>: the claim then
    /// comes back with <c>OpenedAt</c> null, i.e. as a waiting chest.
    /// </summary>
    [Fact]
    public async Task Bestandsclaim_Gilt_Nach_Der_Migration_Als_Geoeffnet()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-boxmigration-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            var awardedAt = new DateTime(2026, 11, 13, 17, 30, 0, DateTimeKind.Utc);
            int childId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                await db.SaveChangesAsync();
                childId = child.Id;
                db.DailyBoxClaims.Add(new DailyBoxClaim
                {
                    ChildId = childId,
                    Day = new DateOnly(2026, 11, 13),
                    CoinsAwarded = 20,
                    GemsAwarded = 1,
                    StreakAtClaim = 2,
                    AwardedAt = awardedAt,
                });
                await db.SaveChangesAsync();

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var claim = await db.DailyBoxClaims.AsNoTracking().SingleAsync(c => c.ChildId == childId);
                Assert.Equal(awardedAt, claim.OpenedAt);
                Assert.False(await db.DailyBoxClaims.AnyAsync(c => c.ChildId == childId && c.OpenedAt == null));
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
