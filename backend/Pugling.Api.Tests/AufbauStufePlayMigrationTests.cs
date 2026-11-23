using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-848 AC 8: the migration <c>BuildLevelPlayed</c> against a database in the state before it. A position keeps playing
/// its exercise (<c>CurrentBuildLevel</c> null), and its progress rows are filled with the position's exercise - box, due
/// date, learn-card and remediation state unchanged. The rows go in raw: both tables gained columns in this step.
/// </summary>
public sealed class AufbauStufePlayMigrationTests
{
    private const string Before = "20261123050226_TowerGameRuns";
    private const string Target = "20261123125626_BuildLevelPlayed";

    /// <summary>Red when the fill is missing (ExerciseId 0 breaks the key), or the rebuild alters or drops a row.</summary>
    [Fact]
    public async Task Bestand_Bekommt_Die_Uebung_Seiner_Position()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-buildlevelplayed-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int firstExercise, secondExercise, firstPosition, secondPosition;
            await using (var db = new PuglingDbContext(options))
            {
                await db.GetService<IMigrator>().MigrateAsync(Before);
                var subject = new Subject { Name = "Englisch" };
                db.Subjects.Add(subject);
                await db.SaveChangesAsync();
                var series = new TextbookSeries { Name = "Access", Slug = "access", SubjectId = subject.Id };
                db.TextbookSeries.Add(series);
                await db.SaveChangesAsync();
                var unit = new SeriesUnit { SeriesId = series.Id, Label = "Unit 1", OrderIndex = 1 };
                db.SeriesUnits.Add(unit);
                await db.SaveChangesAsync();
                var first = new Exercise { SeriesUnitId = unit.Id, Type = "Vocabulary", Title = "Eins", ConfigJson = "{}" };
                var second = new Exercise { SeriesUnitId = unit.Id, Type = "Vocabulary", Title = "Zwei", ConfigJson = "{}" };
                db.Exercises.AddRange(first, second);
                await db.SaveChangesAsync();
                (firstExercise, secondExercise) = (first.Id, second.Id);
                var childId = await LegacyChildRow.InsertAsync(db, "Kind");
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO StudyPlans (ChildId, Title, StartDate, EndDate, Active, CreatedAt)
                    VALUES ({childId}, 'Plan', '2026-11-01', '2026-11-30', 1, '2026-11-01 08:00:00')
                    """);
                var planId = await db.Database.SqlQuery<int>($"SELECT MAX(Id) AS Value FROM StudyPlans").SingleAsync();
                firstPosition = await LegacyPositionRow.InsertAsync(db, planId, firstExercise);
                secondPosition = await LegacyPositionRow.InsertAsync(db, planId, secondExercise);
                // Two positions with the same item index: the fill must follow each row's own position.
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO PositionItemProgress (PlanPositionId, ItemIndex, Box, DueOn, ReviewCount, IntroducedAt,
                        ImprintedAt, LastMissed, RemediationDoneAt)
                    VALUES ({firstPosition}, 0, 4, '2026-11-25', 6, '2026-11-02', '2026-11-03', 1, '2026-11-04'),
                           ({secondPosition}, 0, 2, '2026-11-24', 1, '2026-11-05', NULL, 0, NULL)
                    """);

                await db.GetService<IMigrator>().MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var rows = await db.PositionItemProgress.AsNoTracking().OrderBy(p => p.PlanPositionId).ToListAsync();
                Assert.Equal(2, rows.Count);
                Assert.Equal((firstPosition, firstExercise), (rows[0].PlanPositionId, rows[0].ExerciseId));
                Assert.Equal((secondPosition, secondExercise), (rows[1].PlanPositionId, rows[1].ExerciseId));
                Assert.Equal(4, rows[0].Box);
                Assert.Equal(new DateOnly(2026, 11, 25), rows[0].DueOn);
                Assert.Equal(6, rows[0].ReviewCount);
                Assert.Equal(new DateOnly(2026, 11, 3), rows[0].ImprintedAt);
                Assert.True(rows[0].LastMissed);
                Assert.Equal(new DateOnly(2026, 11, 4), rows[0].RemediationDoneAt);
                Assert.Null(rows[1].ImprintedAt);

                var levels = await db.PlanPositions.AsNoTracking().Select(p => p.CurrentBuildLevel).ToListAsync();
                Assert.Equal(2, levels.Count);
                Assert.All(levels, Assert.Null);
            }
        }
        finally
        {
            SqliteConnection.ClearPool(new SqliteConnection(connectionString));
            File.Delete(dbPath);
        }
    }
}
