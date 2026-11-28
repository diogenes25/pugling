using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The migration <c>TowerGameCollect</c> applied to a database that already holds children, a mission and a tower-game
/// run: existing rows come along. The chain is run on a throwaway SQLite file, taken back one step and forward again,
/// so the rows meet the migration as rows from before it. The results are read as raw SQL, not through the model.
/// </summary>
public sealed class TowerGameCollectMigrationTests
{
    private const string Before = "20261127072327_OffenerPlan";
    private const string Target = "20261127231144_TowerGameCollect";

    /// <summary>
    /// Red when the mission <c>INSERT</c>s or the <c>UPDATE … SET Value = 2</c> are missing from the migration's
    /// <c>Up</c>, and red when the insert is not guarded per metric (the child with its own wave task would get two).
    /// </summary>
    [Fact]
    public async Task Bestand_Bekommt_Die_Spielaufgaben_Und_Den_Mittleren_Wortwert()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-towercollect-{Guid.NewGuid():N}.db");
        // Without foreign keys: the run rows stand in for a real run without building its plan and exercise.
        var connectionString = $"Data Source={dbPath};Foreign Keys=False";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int plain, withOwnTask;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                // A fresh chain has no children yet, so the migration's own inserts found nobody.
                Assert.Equal(0L, await ScalarAsync(db, "SELECT COUNT(*) FROM Missions"));
                var first = new Child { Name = "Bestand" };
                var second = new Child { Name = "Mit eigener Aufgabe" };
                db.Children.AddRange(first, second);
                await db.SaveChangesAsync();
                (plain, withOwnTask) = (first.Id, second.Id);

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                // The migrator switches foreign keys back on for its connection.
                await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO Missions (ChildId, Title, Metric, Target, Period, RewardPoints, Active, CreatedAt)
                    VALUES ({withOwnTask}, 'Eigene Welle', 'TowerBestWave', 9, 'Weekly', 50, 0, '2026-11-01 00:00:00');
                    INSERT INTO TowerGameRuns (Id, ChildId, PlanPositionId, Day, StartedAt, LastBeatAt, PlayedSeconds, Won, CoinsAwarded)
                    VALUES (7, {plain}, 1, '2026-11-27', '2026-11-27 16:00:00', '2026-11-27 16:01:00', 60, 0, 0);
                    INSERT INTO TowerGameRunItems (RunId, ExerciseItemId, "Order", SolvedAt)
                    VALUES (7, 1, 0, '2026-11-27 16:00:30'), (7, 2, 1, NULL);
                    """);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                Assert.Equal(3L, await ScalarAsync(db, $"SELECT COUNT(*) FROM Missions WHERE ChildId = {plain}"));
                Assert.Equal(3L, await ScalarAsync(db, $"SELECT COUNT(*) FROM Missions WHERE ChildId = {withOwnTask}"));
                Assert.Equal(3L, await ScalarAsync(db,
                    $"SELECT COUNT(DISTINCT Metric) FROM Missions WHERE ChildId = {plain} AND Period = 'Daily' AND Active = 1 " +
                    "AND RewardPoints = 10 AND Metric IN ('TowerWordsCorrect', 'TowerBestWave', 'TowerNewWordsCorrect')"));
                // The child's own task is untouched and stays its only one on that metric.
                Assert.Equal(1L, await ScalarAsync(db,
                    $"SELECT COUNT(*) FROM Missions WHERE ChildId = {withOwnTask} AND Metric = 'TowerBestWave' AND Target = 9 AND Active = 0"));
                // The model reads what the migration wrote.
                Assert.Equal(20, (await db.Missions.AsNoTracking()
                    .SingleAsync(m => m.ChildId == plain && m.Metric == ProgressMetric.TowerWordsCorrect)).Target);

                Assert.Equal(2L, await ScalarAsync(db, "SELECT COUNT(*) FROM TowerGameRunItems WHERE Value = 2"));
                Assert.Equal(0L, await ScalarAsync(db, "SELECT Trophies + Wave FROM TowerGameRuns WHERE Id = 7"));
                Assert.Equal(1L, await ScalarAsync(db, "SELECT COUNT(*) FROM TowerGameRunItems WHERE RunId = 7 AND SolvedAt IS NOT NULL"));
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }

    private static async Task<long> ScalarAsync(PuglingDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
