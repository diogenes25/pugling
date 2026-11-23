using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-849 AC 8: the migration <c>BuildLevelPromotion</c> against a database in the state before it. Positions keep playing
/// at once (<c>BuildLevelEffectiveOn</c> null); an attempt on a staged position is stamped with the level it stands on, an
/// attempt on a free position or without a position stays without a level. The rows go in raw: both tables gain a column.
/// </summary>
public sealed class AufbauStufeAufstiegMigrationTests
{
    private const string Before = "20261123125626_BuildLevelPlayed";
    private const string Target = "20261123151427_BuildLevelPromotion";

    /// <summary>
    /// Red when the fill is missing (the staged attempt stays without a level and would meet a goal as a target test), when
    /// it ignores the row's own position, or when it stamps free positions.
    /// </summary>
    [Fact]
    public async Task Bestand_Bekommt_Die_Stufe_Seiner_Position()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-buildlevelpromotion-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int staged, free, planId;
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
                var exercise = new Exercise { SeriesUnitId = unit.Id, Type = "Vocabulary", Title = "Ziel", ConfigJson = "{}" };
                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();
                var childId = await LegacyChildRow.InsertAsync(db, "Kind");
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO StudyPlans (ChildId, Title, StartDate, EndDate, Active, CreatedAt)
                    VALUES ({childId}, 'Plan', '2026-11-01', '2026-11-30', 1, '2026-11-01 08:00:00')
                    """);
                planId = await db.Database.SqlQuery<int>($"SELECT MAX(Id) AS Value FROM StudyPlans").SingleAsync();
                staged = await LegacyPositionRow.InsertAsync(db, planId, exercise.Id);
                free = await LegacyPositionRow.InsertAsync(db, planId, exercise.Id);
                // Level 2 on purpose: a fill that wrote a constant 1 would pass a level-1 row.
                await db.Database.ExecuteSqlAsync($"UPDATE PlanPositions SET CurrentBuildLevel = 2 WHERE Id = {staged}");
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO TestAttempts (StudyPlanId, PlanPositionId, Day, StageValue, Graded, BySupervisor, StartedAt,
                        CompletedAt, TotalItems, CorrectItems, ScorePercent, Passed, "Order", Cursor)
                    VALUES ({planId}, {staged}, '2026-11-20', 3, 1, 0, '2026-11-20 10:00:00', '2026-11-20 10:05:00', 2, 2, 100, 1, '[0,1]', 2),
                           ({planId}, {free}, '2026-11-20', 3, 1, 0, '2026-11-20 11:00:00', '2026-11-20 11:05:00', 2, 1, 50, 0, '[0,1]', 2),
                           ({planId}, NULL, '2026-11-19', 3, 1, 0, '2026-11-19 11:00:00', NULL, 1, 0, 0, 0, '[0]', 0)
                    """);

                await db.GetService<IMigrator>().MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var attempts = await db.TestAttempts.AsNoTracking().OrderBy(t => t.Id)
                    .Select(t => new { t.PlanPositionId, t.BuildLevel, t.ScorePercent, t.Passed }).ToListAsync();
                Assert.Equal(3, attempts.Count);
                Assert.Equal((staged, 2), (attempts[0].PlanPositionId!.Value, attempts[0].BuildLevel!.Value));
                Assert.Null(attempts[1].BuildLevel);
                Assert.Null(attempts[2].BuildLevel);
                Assert.Equal((100, true), (attempts[0].ScorePercent, attempts[0].Passed));

                var positions = await db.PlanPositions.AsNoTracking().OrderBy(p => p.Id)
                    .Select(p => new { p.CurrentBuildLevel, p.BuildLevelEffectiveOn }).ToListAsync();
                Assert.Equal(2, positions[0].CurrentBuildLevel);
                Assert.All(positions, p => Assert.Null(p.BuildLevelEffectiveOn));
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }
}
