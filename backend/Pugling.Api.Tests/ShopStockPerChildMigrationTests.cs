using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-645 AK 7 and B-665: the migration <c>ShopStockPerChild</c> against a database in the state before it, with two
/// linked children and real purchases. Migrated up to the step before, filled with raw SQL in the old shape (the listing
/// still carries one family stock), then migrated across. The expected values are decision 11: each child starts with
/// MaxStock minus its OWN purchases since the last refill.
/// </summary>
public sealed class ShopStockPerChildMigrationTests
{
    private const string Before = "20261118215442_ExerciseFeedback";
    private const string Target = "20261119065732_ShopStockPerChild";

    /// <summary>
    /// Red when the migration copies the family stock (child B would get 2 instead of 3 for the daily listing and 0
    /// instead of 1 for the bought-out Once listing), counts a cancelled or an earlier purchase, drops the refill time,
    /// keeps a refilling listing with MaxStock 0, or leaves RefillQuantity/RequestedBy empty.
    /// </summary>
    [Fact]
    public async Task Jedes_Kind_Erhaelt_Seinen_Startbestand()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-shopstock-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int a, b, daily, onceDone, onceAhead, weeklyZero;
            await using (var db = new PuglingDbContext(options))
            {
                await db.GetService<IMigrator>().MigrateAsync(Before);
                var adult = new Adult { Name = "Vater" };
                db.Add(adult);
                await db.SaveChangesAsync();
                (a, b) = (await LegacyChildRow.InsertAsync(db, "A"), await LegacyChildRow.InsertAsync(db, "B"));
                db.SupervisorLinks.AddRange(
                    new SupervisorLink { SupervisorId = adult.Id, StudentId = a },
                    new SupervisorLink { SupervisorId = adult.Id, StudentId = b });
                var article = new ShopArticle { AdultId = adult.Id, ArticleNumber = "M-1", Title = "Zocken" };
                db.ShopArticles.Add(article);
                await db.SaveChangesAsync();

                // Old shape: one family stock and one refill time on the listing.
                daily = await ListingAsync(db, article.Id, "Daily", current: 2, max: 3, last: "2026-11-18 06:00:00");
                onceDone = await ListingAsync(db, article.Id, "Once", current: 0, max: 1, last: "2026-11-10 00:00:00", at: "2026-11-10 00:00:00");
                onceAhead = await ListingAsync(db, article.Id, "Once", current: 0, max: 2, last: null, at: "2099-01-01 00:00:00");
                weeklyZero = await ListingAsync(db, article.Id, "Weekly", current: 2, max: 0, last: null, weekday: "Monday");

                await PurchaseAsync(db, a, daily, "2026-11-18 08:00:00", "Owned");     // counts for A
                await PurchaseAsync(db, a, daily, "2026-11-17 08:00:00", "Owned");     // before the last refill: no
                await PurchaseAsync(db, b, daily, "2026-11-18 09:00:00", "Cancelled"); // cancelled: no
                await PurchaseAsync(db, a, onceDone, "2026-11-11 08:00:00", "Owned");
                await PurchaseAsync(db, b, weeklyZero, "2026-11-11 08:00:00", "Owned");
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO ActivationRequests (ChildId, ShopArticleId, SupervisorId, RequestedQuantity, Status, ArticleTitle, UnitType, ActionType, RequestedAt)
                    VALUES ({a}, {article.Id}, {adult.Id}, 10, 'Pending', 'Zocken', 'Minute', 'Zocken', '2026-11-18 10:00:00')
                    """);

                await db.GetService<IMigrator>().MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var stocks = await db.ShopListingStocks.AsNoTracking().ToListAsync();
                int StockOf(int listing, int child) => stocks.Single(s => s.ShopListingId == listing && s.ChildId == child).CurrentStock;

                Assert.Equal(2, StockOf(daily, a));
                Assert.Equal(3, StockOf(daily, b));
                Assert.Equal(new DateTime(2026, 11, 18, 6, 0, 0), stocks.Single(s => s.ShopListingId == daily && s.ChildId == a).LastRefilledAtUtc);
                Assert.Equal(0, StockOf(onceDone, a));
                Assert.Equal(1, StockOf(onceDone, b));
                Assert.Equal(0, StockOf(onceAhead, a));
                Assert.Null(stocks.Single(s => s.ShopListingId == onceAhead && s.ChildId == b).LastRefilledAtUtc);

                var zero = await db.ShopListings.AsNoTracking().SingleAsync(l => l.Id == weeklyZero);
                Assert.Equal(ShopRefillKind.None, zero.RefillKind);
                Assert.Equal(3, zero.MaxStock); // 2 left + 1 bought
                Assert.Equal(2, StockOf(weeklyZero, b));

                var listings = await db.ShopListings.AsNoTracking().ToListAsync();
                Assert.All(listings, l => Assert.Equal(l.MaxStock, l.RefillQuantity));
                Assert.Equal(ActivationRequester.Child, (await db.ActivationRequests.AsNoTracking().SingleAsync()).RequestedBy);
                Assert.Empty(await db.ShopListingAudiences.ToListAsync());
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }

    private static async Task<int> ListingAsync(PuglingDbContext db, int articleId, string kind, int current, int max,
        string? last, string? at = null, string? weekday = null)
    {
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO ShopListings (ShopArticleId, Title, Description, CoinPrice, GemPrice, UnitsPerPurchase, Active,
                                      CurrentStock, MaxStock, RefillKind, RefillAtUtc, RefillDayOfWeek, LastRefilledAtUtc,
                                      ConcurrencyStamp, CreatedAt)
            VALUES ({0}, '', '', 10, 0, 1, 1, {1}, {2}, {3}, {4}, {5}, {6}, {7}, '2026-11-01 00:00:00')
            """, articleId, current, max, kind, at!, weekday!, last!, Guid.NewGuid().ToString().ToUpperInvariant());
        return await db.Database.SqlQueryRaw<int>("SELECT MAX(Id) AS Value FROM ShopListings").SingleAsync();
    }

    private static Task PurchaseAsync(PuglingDbContext db, int childId, int listingId, string at, string status) =>
        db.Database.ExecuteSqlRawAsync("""
            INSERT INTO ShopPurchases (ChildId, ShopListingId, SupervisorId, ArticleNumber, Title, Description, CoinPrice,
                                       GemPrice, UnitsPerPurchase, Status, PurchasedAt, ConcurrencyStamp)
            VALUES ({0}, {1}, 0, 'M-1', 'Zocken', '', 10, 0, 1, {2}, {3}, {4})
            """, childId, listingId, status, at, Guid.NewGuid().ToString().ToUpperInvariant());
}
