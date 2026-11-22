using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-659, AK 6: the migration <c>DutyUntilCadenceUntil</c> applied to a database that already holds a daily and a weekly
/// duty. Existing positions were never freed, so both columns must stay <c>NULL</c> - a value would put a duty span on
/// every position and move the child's history (the day reads the span's cadence instead of the current one). Same
/// pattern as <see cref="ActivatedAtMigrationTests"/>: fully migrated and seeded, then taken back one step (which drops
/// both columns) and forward again, so the rows meet the migration as pre-B-659 rows.
/// </summary>
public sealed class DutyUntilMigrationTests
{
    private const string Before = "20261119065732_ShopStockPerChild";
    private const string Target = "20261122081510_DutyUntilCadenceUntil";

    /// <summary>Red when the migration's <c>Up</c> fills either column for existing rows.</summary>
    [Fact]
    public async Task Bestand_Bleibt_Ohne_Spanne()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-dutyuntil-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            var start = new DateOnly(2026, 11, 10);
            List<int> positionIds;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                var series = new TextbookSeries { Name = "Reihe", Slug = "reihe-dutyuntil" };
                var unit = new SeriesUnit { Series = series, Label = "Unit 1" };
                var exercise = new Exercise { SeriesUnit = unit, Type = ExerciseTypeKeys.Vocabulary, Title = "Wörter" };
                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();
                var plan = new StudyPlan { ChildId = child.Id, Title = "Bestand", StartDate = start, EndDate = start.AddDays(20) };
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, Cadence = GoalCadence.Daily });
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, Cadence = GoalCadence.Weekly });
                db.StudyPlans.Add(plan);
                await db.SaveChangesAsync();
                positionIds = plan.Positions.Select(p => p.Id).ToList();

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var positions = await db.PlanPositions.AsNoTracking().Where(p => positionIds.Contains(p.Id)).ToListAsync();
                Assert.Equal(2, positions.Count);
                Assert.All(positions, p =>
                {
                    Assert.Null(p.DutyUntil);
                    Assert.Null(p.CadenceUntil);
                    // Without a span every day reads the current cadence - the history of existing rows does not move.
                    Assert.Equal(p.Cadence, PositionProgressService.DutyCadenceOn(p, start));
                });
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
