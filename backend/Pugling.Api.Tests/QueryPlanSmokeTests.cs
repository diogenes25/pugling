using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifiziert per EXPLAIN QUERY PLAN, dass die wichtigsten Hotpath-Queries die neuen
/// Komposit-Indizes nutzen. Läuft gegen eine frische temp-DB mit allen Migrationen.
/// </summary>
public sealed class QueryPlanSmokeTests
{
    [Fact]
    public async Task Hotpath_Queries_Use_Expected_Indexes()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-queryplan-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<PuglingDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        await using (var setup = new PuglingDbContext(options))
        {
            await setup.Database.MigrateAsync();
        }

        await using var con = new SqliteConnection($"Data Source={dbPath}");
        await con.OpenAsync();

        // Seed minimal rows, damit der Planner realistische Statistiken bekommt.
        await ExecAsync(con, "INSERT INTO Children (Name, Pin, CreatedAt, SelectedSkin, OwnedSkins, ConcurrencyStamp, SchoolType) VALUES ('P', '1234', CURRENT_TIMESTAMP, 'pug', '[\"pug\"]', '00000000-0000-0000-0000-000000000001', 0);");
        await ExecAsync(con, "INSERT INTO StudyPlans (ChildId, Title, StartDate, EndDate, Active, CreatedAt) VALUES (1, 'S', '2026-01-01', '2026-12-31', 1, CURRENT_TIMESTAMP);");
        await ExecAsync(con, "INSERT INTO Subjects (Name, CreatedAt) VALUES ('E', CURRENT_TIMESTAMP);");
        await ExecAsync(con, "INSERT INTO Chapters (SubjectId, Name, OrderIndex) VALUES (1, 'C', 1);");
        await ExecAsync(con, "INSERT INTO Exercises (ChapterId, Type, Title, OrderIndex, RewardPoints, ConfigJson, SchoolTypes, DefaultUseLeitner, DefaultRequireTypedTest, CreatedAt) VALUES (1, 0, 'X', 1, 1, '{}', 0, 1, 0, CURRENT_TIMESTAMP);");
        await ExecAsync(con, "INSERT INTO PlanPositions (StudyPlanId, ExerciseId, `Order`, Scope, Cadence, RequireTypedTest, PointsGoalMet, PenaltyCoins, NewContentPoints, ComboThreshold, ComboBonusPoints, SpeedThresholdSeconds, SpeedBonusPoints, UseLeitner, MaxBox, OrderStrategy, CreatedAt) VALUES (1, 1, 1, 0, 1, 0, 10, 0, 1, 0, 0, 0, 0, 1, 5, 0, CURRENT_TIMESTAMP);");
        await ExecAsync(con, "INSERT INTO PracticeSessions (StudyPlanId, PlanPositionId, Day, StartedAt, ActiveSeconds, Mode, `Order`, Cursor) VALUES (1, 1, '2026-07-12', CURRENT_TIMESTAMP, 30, 1, '[]', 0);");
        await ExecAsync(con, "INSERT INTO TestAttempts (StudyPlanId, PlanPositionId, Day, StageValue, Graded, StartedAt, TotalItems, CorrectItems, ScorePercent, Passed, `Order`, Cursor) VALUES (1, 1, '2026-07-12', 3, 1, CURRENT_TIMESTAMP, 1, 1, 100, 1, '[]', 0);");
        await ExecAsync(con, "INSERT INTO Vocabulary (`Key`, SourceLanguage, TargetLanguage, Word, Translation, Version, PartOfSpeech, CreatedAt) VALUES ('k', 'en', 'de', 'w', 't', 'v1', 0, CURRENT_TIMESTAMP);");
        await ExecAsync(con, "INSERT INTO ExerciseItems (ExerciseId, VocabularyId, OrderIndex, CreatedAt) VALUES (1, 1, 1, CURRENT_TIMESTAMP);");
        await ExecAsync(con, "INSERT INTO ItemProgress (ChildId, ItemId, ExerciseId, VocabularyId, Box, MasteryPercent, SeenCount, CorrectCount) VALUES (1, 1, 1, 1, 1, 20, 1, 0);");
        await ExecAsync(con, "INSERT INTO ChildPoints (ChildId, Amount, Kind, Reason, CreatedAt) VALUES (1, 10, 0, 'r', CURRENT_TIMESTAMP);");

        await AssertUsesIndexAsync(con,
            "SELECT Id FROM ChildPoints WHERE ChildId = 1 ORDER BY CreatedAt DESC, Id DESC LIMIT 20;",
            "IX_ChildPoints_ChildId_CreatedAt_Id");

        await AssertUsesIndexAsync(con,
            "SELECT SUM(Amount) FROM ChildPoints WHERE ChildId = 1 AND Kind IN (0, 1, 2);",
            "IX_ChildPoints_ChildId_Kind");

        await AssertUsesIndexAsync(con,
            "SELECT EXISTS(SELECT 1 FROM PracticeSessions WHERE PlanPositionId = 1 AND Day >= '2026-01-01' AND Day <= '2026-12-31' AND Mode = 1);",
            "IX_PracticeSessions_PlanPositionId_Day_Mode");

        await AssertUsesIndexAsync(con,
            "SELECT EXISTS(SELECT 1 FROM TestAttempts WHERE PlanPositionId = 1 AND Day >= '2026-01-01' AND Day <= '2026-12-31' AND CompletedAt IS NOT NULL AND Passed = 1);",
            "IX_TestAttempts_PlanPositionId_Day_CompletedAt_Passed");

        await AssertUsesIndexAsync(con,
            "SELECT Id FROM PlanPositions WHERE StudyPlanId = 1 ORDER BY `Order`, Id LIMIT 20;",
            "IX_PlanPositions_StudyPlanId_Order_Id");

        await AssertUsesIndexAsync(con,
            "SELECT COUNT(*) FROM PracticeSessions WHERE StudyPlanId = 1 AND Day >= '2026-01-01' AND Day <= '2026-12-31';",
            "IX_PracticeSessions_StudyPlanId_Day");

        await AssertUsesIndexAsync(con,
            "SELECT COUNT(*) FROM TestAttempts WHERE StudyPlanId = 1 AND Day >= '2026-01-01' AND Day <= '2026-12-31';",
            "IX_TestAttempts_StudyPlanId_Day");

        await AssertUsesIndexAsync(con,
            "SELECT COUNT(*) FROM ItemProgress WHERE ChildId = 1 AND ExerciseId = 1;",
            "IX_ItemProgress_ChildId_ExerciseId");
    }

    private static async Task AssertUsesIndexAsync(SqliteConnection con, string sql, string expectedIndex)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"EXPLAIN QUERY PLAN {sql}";
        await using var reader = await cmd.ExecuteReaderAsync();
        var details = new List<string>();
        while (await reader.ReadAsync())
            details.Add(reader.GetString(3));

        Assert.Contains(details, d => d.Contains(expectedIndex, StringComparison.Ordinal));
    }

    private static async Task ExecAsync(SqliteConnection con, string sql)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
