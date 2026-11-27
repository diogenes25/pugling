using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-734: the migration starts every existing child with the planner off and no plans - after the deploy nothing is planned
/// until the supervisor switches it on. Red if the backfill is left out (an empty mode in the database) or sets another
/// mode.
/// </summary>
public class TagesplanerMigrationTests
{
    private const string Before = "20261126162132_ReviewEventDay";
    private const string Target = "20261126220154_Tagesplaner";

    [Fact]
    public async Task Bestandskinder_starten_mit_ausgeschaltetem_Planer()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-planner-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            await using (var db = new PuglingDbContext(options))
            {
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                // A raw child row: every NOT NULL column without a default gets a neutral value, so the row stands for any
                // child of the live database without naming its columns here.
                await db.Database.OpenConnectionAsync();
                var connection = (SqliteConnection)db.Database.GetDbConnection();
                var columns = new List<(string Name, string Type)>();
                await using (var info = connection.CreateCommand())
                {
                    info.CommandText = "PRAGMA table_info(Children);";
                    await using var reader = await info.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        if (reader.GetInt32(3) == 1 && reader.IsDBNull(4) && reader.GetInt32(5) == 0)
                            columns.Add((reader.GetString(1), reader.GetString(2)));
                }
                await using (var insert = connection.CreateCommand())
                {
                    insert.CommandText = $"INSERT INTO Children ({string.Join(", ", columns.Select(c => c.Name))}) VALUES (" +
                        string.Join(", ", columns.Select(c => c.Type == "INTEGER" ? "0" : "''")) + ");";
                    await insert.ExecuteNonQueryAsync();
                }
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                // The raw value: EF reads an empty or unknown enum string as the first member (Off), so a missing backfill
                // would look right through the model and leave '' in the live database.
                var stored = await db.Database.SqlQueryRaw<string>("SELECT PlannerMode AS Value FROM Children").SingleAsync();
                Assert.Equal("Off", stored);
                var child = await db.Children.AsNoTracking().Select(c => new { c.PlannerSince }).SingleAsync();
                Assert.Null(child.PlannerSince);
                Assert.Equal(0, await db.DailyPlans.CountAsync());
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
