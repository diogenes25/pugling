using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-752, AK 8: the migration <c>ClassTestPrepDuty</c> applied to a database that already holds a class test and plan
/// positions. Nothing is backfilled: every new column stays <c>NULL</c> - a value would start a preparation duty nobody
/// asked for (<c>PrepStartedOn</c>), switch it off or bend its window (<c>PrepWindowDays</c>), or tie an existing position
/// to a class test (<c>SourceKlassenarbeitId</c>), which the reconcile would then stretch. Same pattern as
/// <see cref="DutyUntilMigrationTests"/>: fully migrated and seeded, taken back one step and forward again.
/// </summary>
public sealed class VorbereitungsPflichtMigrationTests
{
    private const string Before = "20261124021849_NotenstufeChildSubjectLevel";
    private const string Target = "20261124022042_ClassTestPrepDuty";

    /// <summary>Red when the migration's <c>Up</c> fills any of the three columns for existing rows.</summary>
    [Fact]
    public async Task Bestand_Bleibt_Ohne_Vorbereitung()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-classtestprep-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            var start = new DateOnly(2026, 11, 10);
            List<int> positionIds;
            int testId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                var series = new TextbookSeries { Name = "Reihe", Slug = "reihe-classtestprep" };
                var unit = new SeriesUnit { Series = series, Label = "Unit 1" };
                var exercise = new Exercise { SeriesUnit = unit, Type = ExerciseTypeKeys.Vocabulary, Title = "Wörter" };
                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();
                var plan = new StudyPlan { ChildId = child.Id, Title = "Bestand", StartDate = start, EndDate = start.AddDays(20) };
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, Cadence = GoalCadence.Daily, PenaltyCoins = 5 });
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, Cadence = GoalCadence.Weekly });
                db.StudyPlans.Add(plan);
                var test = new Klassenarbeit { ChildId = child.Id, Title = "Vokabeltest", ScheduledDate = start.AddDays(5) };
                test.Exercises.Add(new KlassenarbeitExercise { ExerciseId = exercise.Id });
                db.Klassenarbeiten.Add(test);
                await db.SaveChangesAsync();
                positionIds = plan.Positions.Select(p => p.Id).ToList();
                testId = test.Id;
                // Rows hanging on the positions (cascade children): the FK added to PlanPositions rebuilds that table in
                // SQLite, and a rebuild with foreign keys on would take the child's penalty history and progress with it.
                var daily = plan.Positions.First();
                db.PositionGoalPenalties.Add(new PositionGoalPenalty
                {
                    PlanPositionId = daily.Id,
                    Cadence = GoalCadence.Daily,
                    PeriodStart = start,
                    Day = start,
                    Points = 5,
                    AppliedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                });
                db.PositionItemProgress.Add(new PositionItemProgress { PlanPositionId = daily.Id, ExerciseId = exercise.Id, ItemIndex = 0, Box = 3 });
                db.TestAttempts.Add(new TestAttempt
                {
                    StudyPlanId = plan.Id,
                    PlanPositionId = daily.Id,
                    Day = start,
                    StartedAt = start.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
                });
                await db.SaveChangesAsync();

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                // Projected: the current model may carry columns a later migration adds, absent at this target.
                var positions = await db.PlanPositions.AsNoTracking().Where(p => positionIds.Contains(p.Id))
                    .Select(p => new { p.Cadence, p.SourceKlassenarbeitId, p.DutyUntil, p.PenaltyCoins }).ToListAsync();
                Assert.Equal(2, positions.Count);
                Assert.All(positions, p => Assert.Null(p.SourceKlassenarbeitId));
                Assert.All(positions, p => Assert.Null(p.DutyUntil));
                Assert.Equal(5, positions.Single(p => p.Cadence == GoalCadence.Daily).PenaltyCoins);

                var test = await db.Klassenarbeiten.AsNoTracking().Where(k => k.Id == testId)
                    .Select(k => new { k.PrepStartedOn, k.PrepWindowDays, k.Status }).SingleAsync();
                Assert.Null(test.PrepStartedOn);
                Assert.Null(test.PrepWindowDays);
                Assert.Equal(KlassenarbeitStatus.Planned, test.Status);

                var daily = positionIds[0];
                Assert.Equal(1, await db.PositionGoalPenalties.CountAsync(p => p.PlanPositionId == daily));
                Assert.Equal(3, await db.PositionItemProgress.Where(p => p.PlanPositionId == daily).Select(p => p.Box).SingleAsync());
                Assert.Equal(1, await db.TestAttempts.CountAsync(t => t.PlanPositionId == daily));
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
