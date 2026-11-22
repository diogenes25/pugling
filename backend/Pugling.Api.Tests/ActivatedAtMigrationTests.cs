using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-613, AK 10: the migration <c>DutySinceActivatedAt</c> applied to a database that already holds a backdated plan and a
/// position created after its start. The backfill must restate the rule before the migration - the plan on since its start,
/// the position a duty since its creation day - so no streak and no open penalty moves. Same pattern as
/// <see cref="DailyBoxOpenedAtMigrationTests"/>: fully migrated and seeded through the DbContext, then taken back one step
/// (which drops both columns) and forward again, so the rows meet the migration as pre-B-613 rows.
/// </summary>
public sealed class ActivatedAtMigrationTests
{
    private const string Before = "20261118071748_UntilDoneCompletionBonus";
    private const string Target = "20261118181817_DutySinceActivatedAt";

    /// <summary>
    /// Red when an <c>UPDATE</c> is missing from the migration's <c>Up</c> (the field reads <c>0001-01-01</c>) or takes
    /// the wrong date (the plan's creation instead of its start, the position's plan start instead of its creation day).
    /// </summary>
    [Fact]
    public async Task Bestand_Gilt_Seit_Start_Bzw_Anlegetag()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-activatedat-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            var start = new DateOnly(2026, 11, 10);
            var positionCreated = new DateTime(2026, 11, 14, 21, 30, 0, DateTimeKind.Utc);
            int planId, positionId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                var series = new TextbookSeries { Name = "Reihe", Slug = "reihe-bestand" };
                var unit = new SeriesUnit { Series = series, Label = "Unit 1" };
                var exercise = new Exercise { SeriesUnit = unit, Type = ExerciseTypeKeys.Vocabulary, Title = "Wörter" };
                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();
                var plan = new StudyPlan
                {
                    ChildId = child.Id,
                    Title = "Rückdatiert",
                    StartDate = start,
                    EndDate = start.AddDays(20),
                    CreatedAt = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc),
                };
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, Cadence = GoalCadence.Daily, CreatedAt = positionCreated });
                db.StudyPlans.Add(plan);
                await db.SaveChangesAsync();
                planId = plan.Id;
                positionId = plan.Positions[0].Id;

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                // Projected, not loaded: the database stands at this migration, and a later column of the entity (B-659
                // DutyUntil) does not exist yet.
                Assert.Equal(start, await db.StudyPlans.Where(p => p.Id == planId).Select(p => p.ActivatedAt).SingleAsync());
                Assert.Equal(new DateOnly(2026, 11, 14),
                    await db.PlanPositions.Where(p => p.Id == positionId).Select(p => p.DutySince).SingleAsync());
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
