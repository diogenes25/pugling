using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifies via EXPLAIN QUERY PLAN that the most important hotpath queries use the composite
/// indexes. Runs against a fresh temp DB with all migrations applied.
/// <para>
/// The fixture is built <b>via the DbContext</b>, not via raw <c>INSERT</c>. That is not a style
/// choice: raw inserts have to name every NOT-NULL column themselves, and this only worked here
/// because many columns carried a SQL <c>DEFAULT</c> clause – which did not originate from the model,
/// but was a byproduct of once having been appended via <c>AddColumn(defaultValue:…)</c>. A freshly
/// generated <c>CreateTable</c> no longer writes these clauses, and the test failed with
/// <c>NOT NULL constraint failed</c> at a spot that has nothing to do with indexes.
/// Going through the graph, the compiler keeps the fixture aligned with the schema; a new mandatory
/// column no longer breaks here at runtime, but does not compile at all.
/// </para>
/// The raw <see cref="SqliteConnection"/> stays – <c>EXPLAIN QUERY PLAN</c> is only available there.
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

        Ids ids;
        await using (var db = new PuglingDbContext(options))
        {
            await db.Database.MigrateAsync();
            ids = await SeedGraphAsync(db);
        }

        await using var con = new SqliteConnection($"Data Source={dbPath}");
        await con.OpenAsync();

        await AssertUsesIndexAsync(con,
            $"SELECT Id FROM ChildPointsEntries WHERE ChildId = {ids.Child} ORDER BY CreatedAt DESC, Id DESC LIMIT 20;",
            "IX_ChildPointsEntries_ChildId_CreatedAt_Id");

        // Since the enum convention, `Kind` sits in the DB as TEXT (the contract always spoke strings anyway).
        // The composite index has to apply to that too - which is exactly what this assurance checks.
        await AssertUsesIndexAsync(con,
            $"SELECT SUM(Amount) FROM ChildPointsEntries WHERE ChildId = {ids.Child} AND Kind IN ('Base', 'Combo', 'Manual');",
            "IX_ChildPointsEntries_ChildId_Kind");

        await AssertUsesIndexAsync(con,
            $"SELECT EXISTS(SELECT 1 FROM PracticeSessions WHERE PlanPositionId = {ids.Position} AND Day >= '2026-01-01' AND Day <= '2026-12-31' AND Mode = 1);",
            "IX_PracticeSessions_PlanPositionId_Day_Mode");

        await AssertUsesIndexAsync(con,
            $"SELECT EXISTS(SELECT 1 FROM TestAttempts WHERE PlanPositionId = {ids.Position} AND Day >= '2026-01-01' AND Day <= '2026-12-31' AND CompletedAt IS NOT NULL AND Passed = 1);",
            "IX_TestAttempts_PlanPositionId_Day_CompletedAt_Passed");

        await AssertUsesIndexAsync(con,
            $"SELECT Id FROM PlanPositions WHERE StudyPlanId = {ids.Plan} ORDER BY `Order`, Id LIMIT 20;",
            "IX_PlanPositions_StudyPlanId_Order_Id");

        await AssertUsesIndexAsync(con,
            $"SELECT COUNT(*) FROM PracticeSessions WHERE StudyPlanId = {ids.Plan} AND Day >= '2026-01-01' AND Day <= '2026-12-31';",
            "IX_PracticeSessions_StudyPlanId_Day");

        await AssertUsesIndexAsync(con,
            $"SELECT COUNT(*) FROM TestAttempts WHERE StudyPlanId = {ids.Plan} AND Day >= '2026-01-01' AND Day <= '2026-12-31';",
            "IX_TestAttempts_StudyPlanId_Day");

        await AssertUsesIndexAsync(con,
            $"SELECT COUNT(*) FROM ItemProgress WHERE ChildId = {ids.Child} AND ExerciseId = {ids.Exercise};",
            "IX_ItemProgress_ChildId_ExerciseId");

        // The hottest creator path: the duplicate lookup when creating vocabulary. It ran as a full table scan,
        // because the query compared `LOWER(Word)` - no column index applies over an expression. Only the
        // NOCASE collation + dropping the ToLower() make it usable; this assurance is the proof that both work
        // together and not just that the index exists.
        await AssertUsesIndexAsync(con,
            "SELECT Id FROM Vocabularies WHERE Word = 'w';",
            "IX_Vocabularies_Word");

        // The opposite direction of the media link ("which links does this asset have?"). The three filtered
        // unique indexes start with MediaAssetId but cannot serve this query.
        await AssertUsesIndexAsync(con,
            "SELECT Id FROM MediaLinks WHERE MediaAssetId = 1;",
            "IX_MediaLinks_MediaAssetId");
    }

    /// <summary>The ids of the fixture rows – the queries above filter on real values, not guessed ones.</summary>
    private readonly record struct Ids(int Child, int Plan, int Position, int Exercise);

    /// <summary>
    /// Creates exactly one path child → plan → position → exercise → item → progress. The graph is as
    /// small as possible: <c>EXPLAIN QUERY PLAN</c> picks the index based on the schema, not on the
    /// row count (without <c>ANALYZE</c> there are no statistics it could draw on).
    /// </summary>
    private static async Task<Ids> SeedGraphAsync(PuglingDbContext db)
    {
        var day = new DateOnly(2026, 7, 12);

        var child = new Child { Name = "P", Pin = "1234" };
        var subject = new Subject { Name = "E" };
        var chapter = new Chapter { Subject = subject, Name = "C", OrderIndex = 1 };
        var exercise = new Exercise { Chapter = chapter, Type = "Vocabulary", Title = "X", OrderIndex = 1, RewardPoints = 1 };
        var vocab = new Vocabulary { Key = "k", SourceLanguage = "en", TargetLanguage = "de", Word = "w", Translation = "t" };
        var item = new ExerciseItem { Exercise = exercise, Vocabulary = vocab, OrderIndex = 0 };
        var position = new PlanPosition { Exercise = exercise, Order = 0 };
        var plan = new StudyPlan
        {
            Child = child,
            Title = "S",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Positions = { position },
        };

        db.AddRange(
            child, subject, chapter, exercise, vocab, item, plan,
            new PracticeSession { StudyPlan = plan, PlanPosition = position, Day = day, ActiveSeconds = 30 },
            new TestAttempt
            {
                StudyPlan = plan,
                PlanPosition = position,
                Day = day,
                StageValue = 3,
                Graded = true,
                CompletedAt = DateTime.UtcNow,
                TotalItems = 1,
                CorrectItems = 1,
                ScorePercent = 100,
                Passed = true,
            },
            new ChildPointsEntry { Child = child, Amount = 10, Kind = PointKind.Base, Reason = "r" });
        await db.SaveChangesAsync();

        // Only after saving: ItemProgress carries ExerciseId/VocabularyId denormalized (without an FK), so the
        // values have to come from the assigned ids.
        db.Add(new ItemProgress
        {
            ChildId = child.Id,
            ItemId = item.Id,
            ExerciseId = exercise.Id,
            VocabularyId = vocab.Id,
            Box = 1,
            MasteryPercent = 20,
            SeenCount = 1,
        });
        await db.SaveChangesAsync();

        return new Ids(child.Id, plan.Id, position.Id, exercise.Id);
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
}
