using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-760, AK 8: the migration <c>Homework</c> applied to a database that already holds a child with a ledger. The child's
/// two amounts start empty (the server default holds) and no balance moves. Same pattern as
/// <see cref="ActivatedAtMigrationTests"/>: seeded fully migrated, then taken back one step (which drops the two columns) and
/// forward again, so the child meets the migration as a pre-B-760 row.
/// </summary>
public sealed class HausaufgabenMigrationTests
{
    private const string Before = "20261124131013_ClassTestPrepLeftOut";
    private const string Target = "20261124201850_Homework";

    /// <summary>Red when the migration's <c>Up</c> fills the amounts (a default or an <c>UPDATE</c>) or books anything.</summary>
    [Fact]
    public async Task Bestand_Hat_Leere_Betraege_Und_Unveraenderten_Kontostand()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-homework-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int childId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                await db.SaveChangesAsync();
                childId = child.Id;
                db.ChildPointsEntries.AddRange(
                    new ChildPointsEntry { ChildId = childId, Amount = 120, Kind = PointKind.Goal, Reason = "Ziel" },
                    new ChildPointsEntry { ChildId = childId, Amount = -4, Kind = PointKind.GoalPenalty, Reason = "Malus" });
                await db.SaveChangesAsync();

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var amounts = await db.Children.Where(c => c.Id == childId)
                    .Select(c => new { c.HomeworkCoins, c.HomeworkPenaltyCoins }).SingleAsync();
                Assert.Null(amounts.HomeworkCoins);
                Assert.Null(amounts.HomeworkPenaltyCoins);
                Assert.Equal(116, await db.ChildPointsEntries.Where(e => e.ChildId == childId).SumAsync(e => e.Amount));
                Assert.Equal(2, await db.ChildPointsEntries.CountAsync(e => e.ChildId == childId));
                Assert.Equal(0, await db.Homeworks.CountAsync());
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
