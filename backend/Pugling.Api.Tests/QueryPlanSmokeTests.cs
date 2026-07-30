using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifiziert per EXPLAIN QUERY PLAN, dass die wichtigsten Hotpath-Queries die Komposit-Indizes
/// nutzen. Läuft gegen eine frische temp-DB mit allen Migrationen.
/// <para>
/// Die Fixture wird <b>über den DbContext</b> aufgebaut, nicht per Roh-<c>INSERT</c>. Das ist keine
/// Stilfrage: Roh-INSERTs müssen jede NOT-NULL-Spalte selbst benennen und funktionierten hier nur,
/// weil viele Spalten eine SQL-<c>DEFAULT</c>-Klausel trugen – die aber nicht aus dem Modell stammte,
/// sondern ein Nebenprodukt davon war, dass sie einst per <c>AddColumn(defaultValue:…)</c> angehängt
/// wurden. Eine frisch erzeugte <c>CreateTable</c> schreibt diese Klauseln nicht mehr, und der Test
/// scheiterte mit <c>NOT NULL constraint failed</c> an einer Stelle, die nichts mit Indizes zu tun hat.
/// Über den Graphen hält der Compiler die Fixture am Schema fest; eine neue Pflichtspalte bricht hier
/// nicht mehr zur Laufzeit, sondern gar nicht.
/// </para>
/// Die rohe <see cref="SqliteConnection"/> bleibt – <c>EXPLAIN QUERY PLAN</c> gibt es nur dort.
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

        // `Kind` liegt seit der Enum-Konvention als TEXT in der DB (der Vertrag sprach immer schon Strings).
        // Der Komposit-Index muss auch darauf greifen – genau das prüft diese Zusicherung.
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

        // Der heißeste Creator-Pfad: Dubletten-Lookup beim Anlegen von Vokabeln. Er lief als vollständiger
        // Tabellendurchlauf, weil die Query `LOWER(Word)` verglich – über einen Ausdruck greift kein
        // Spaltenindex. Erst Collation NOCASE + Wegfall des ToLower() machen ihn nutzbar; diese
        // Zusicherung ist der Beweis, dass beides zusammen wirkt und nicht nur der Index existiert.
        await AssertUsesIndexAsync(con,
            "SELECT Id FROM Vocabularies WHERE Word = 'w';",
            "IX_Vocabularies_Word");

        // Die Gegenrichtung der Medien-Verknüpfung („welche Verknüpfungen hat dieses Asset?"). Die drei
        // gefilterten Unique-Indizes beginnen mit MediaAssetId, können diese Query aber nicht bedienen.
        await AssertUsesIndexAsync(con,
            "SELECT Id FROM MediaLinks WHERE MediaAssetId = 1;",
            "IX_MediaLinks_MediaAssetId");
    }

    /// <summary>Die Ids der Fixture-Zeilen – die Queries oben filtern auf echte Werte, nicht auf geratene.</summary>
    private readonly record struct Ids(int Child, int Plan, int Position, int Exercise);

    /// <summary>
    /// Legt genau einen Pfad Kind → Plan → Position → Übung → Item → Fortschritt an. Der Graph ist so
    /// klein wie möglich: <c>EXPLAIN QUERY PLAN</c> wählt den Index anhand des Schemas, nicht anhand der
    /// Zeilenzahl (ohne <c>ANALYZE</c> gibt es keine Statistiken, die er heranziehen könnte).
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

        // Erst nach dem Speichern: ItemProgress trägt ExerciseId/VocabularyId denormalisiert (ohne FK),
        // die Werte müssen also aus den vergebenen Ids kommen.
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
