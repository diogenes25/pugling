using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-702 AC 10: the migration <c>BuildLevelDemotion</c> against a database in the state before it. Every position keeps
/// its level and its waiting promotion; the count of failed tests and the day of the level start out unknown (null), so no
/// existing child is one failure away from a descent.
/// </summary>
public sealed class AufbauStufeAbstiegMigrationTests
{
    private const string Before = "20261123165222_PracticeSessionBuildLevel";
    private const string Target = "20261123211614_BuildLevelDemotion";

    /// <summary>
    /// Red when a new column is not nullable or gets a fill (a constant count or day), or when the migration rewrites the
    /// level or the waiting promotion of an existing row.
    /// </summary>
    [Fact]
    public async Task Bestand_Behaelt_Stufe_Und_Beginnt_Ohne_Zaehlung()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-buildleveldemotion-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int staged, free;
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
                var planId = await db.Database.SqlQuery<int>($"SELECT MAX(Id) AS Value FROM StudyPlans").SingleAsync();
                staged = await LegacyPositionRow.InsertAsync(db, planId, exercise.Id);
                free = await LegacyPositionRow.InsertAsync(db, planId, exercise.Id);
                await db.Database.ExecuteSqlAsync(
                    $"UPDATE PlanPositions SET CurrentBuildLevel = 2, BuildLevelEffectiveOn = '2026-11-24' WHERE Id = {staged}");

                await db.GetService<IMigrator>().MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var positions = await db.PlanPositions.AsNoTracking().OrderBy(p => p.Id)
                    .Select(p => new { p.Id, p.CurrentBuildLevel, p.BuildLevelEffectiveOn, p.FailedBuildTestsInRow, p.CurrentBuildLevelSince })
                    .ToListAsync();
                Assert.Equal([staged, free], positions.Select(p => p.Id));
                Assert.Equal((2, (DateOnly?)new DateOnly(2026, 11, 24)), (positions[0].CurrentBuildLevel, positions[0].BuildLevelEffectiveOn));
                Assert.Null(positions[1].CurrentBuildLevel);
                Assert.All(positions, p => Assert.Null(p.FailedBuildTestsInRow));
                Assert.All(positions, p => Assert.Null(p.CurrentBuildLevelSince));
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }
}
