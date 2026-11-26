using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-593, AK 11: the migration <c>SavingsGoal</c> applied to a database that already holds a child with a balance and a
/// shop listing. SQLite adds a foreign key by rebuilding the table, so the probe checks that the child survives with its
/// data and ledger, and that no goal is pre-set. Run like <see cref="DailyBoxOpenedAtMigrationTests"/>: fully migrated
/// and seeded, one step back (drops the two columns), forward again.
/// </summary>
public sealed class SparzielMigrationTests
{
    private const string Before = "20261125100246_AccentLeniency";
    private const string Target = "20261125213333_SavingsGoal";

    /// <summary>
    /// Red when the migration pre-fills the goal (e.g. an <c>UPDATE … SET SavingsGoalListingId = …</c>) or when the table
    /// rebuild loses the child's columns or ledger.
    /// </summary>
    [Fact]
    public async Task Bestandskind_Hat_Nach_Der_Migration_Kein_Ziel_Und_Denselben_Stand()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-sparziel-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int childId;
            Guid stamp;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var adult = new Adult { Name = "Bestandsvater" };
                var child = new Child { Name = "Bestand", Grade = 6, OwnedSkins = ["default", "ninja"] };
                db.Adults.Add(adult);
                db.Children.Add(child);
                await db.SaveChangesAsync();
                childId = child.Id;
                stamp = child.ConcurrencyStamp;
                var article = new ShopArticle { AdultId = adult.Id, ArticleNumber = "K-1", Title = "Kino" };
                db.ShopArticles.Add(article);
                await db.SaveChangesAsync();
                db.ShopListings.Add(new ShopListing { ShopArticleId = article.Id, CoinPrice = 100, MaxStock = 2 });
                db.ChildPointsEntries.Add(new ChildPointsEntry
                {
                    ChildId = childId,
                    Amount = 42,
                    Kind = PointKind.Base,
                    Reason = "Bestand",
                    CreatedAt = new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc),
                });
                await db.SaveChangesAsync();

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            // Read with raw SQL: the schema stands at Target, while the entity model already carries columns of later
            // migrations, so an entity query would select columns that do not exist yet.
            await using (var raw = new SqliteConnection(connectionString))
            {
                await raw.OpenAsync();
                await using var cmd = raw.CreateCommand();
                cmd.CommandText =
                    "SELECT SavingsGoalListingId, SavingsGoalTitle, Grade, ConcurrencyStamp, OwnedSkins, " +
                    "(SELECT SUM(Amount) FROM ChildPointsEntries WHERE ChildId = $id) FROM Children WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", childId);
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.True(reader.IsDBNull(0));
                Assert.True(reader.IsDBNull(1));
                Assert.Equal(6, reader.GetInt32(2));
                Assert.Equal(stamp, reader.GetGuid(3));
                Assert.Equal("[\"default\",\"ninja\"]", reader.GetString(4));
                Assert.Equal(42, reader.GetInt32(5));
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
