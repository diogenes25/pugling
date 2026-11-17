using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-539, AK 3: the migration <c>StudyPlanSeriesUnit</c> applied to plans that predate the mandatory subject. A plan
/// without a subject takes the majority subject of its positions; a tie stays empty instead of guessing, and a subject
/// already set is never overwritten. The chain runs on a throwaway SQLite file: fully migrated and seeded through the
/// DbContext, taken back one step and forward again, so the plans meet the migration as pre-B-539 rows.
/// </summary>
public sealed class StudyPlanSubjectMigrationTests
{
    private const string Before = "20261116071258_ExerciseBookPages";
    private const string Target = "20261117061523_StudyPlanSeriesUnit";

    /// <summary>
    /// Red when the backfill is missing from <c>Up</c> (majority plan stays empty), when it ignores the tie rule (tie plan
    /// gets a subject) or when its <c>WHERE SubjectId IS NULL</c> is dropped (the set subject is overwritten by English).
    /// </summary>
    [Fact]
    public async Task Plan_Ohne_Fach_Bekommt_Das_Mehrheitsfach_Seiner_Positionen()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-plansubject-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int majorityId, tieId, keptId, emptyId, englishId, mathId, remarkId;
            Dictionary<int, int> positionsBefore, sessionsBefore, attemptsBefore;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                var english = new Subject { Name = "Englisch-Migration" };
                var math = new Subject { Name = "Mathe-Migration" };
                var englishUnit = new SeriesUnit
                {
                    Series = new TextbookSeries { Subject = english, Name = "E", Slug = "migration-e" },
                    Label = "E1",
                    OrderIndex = 1,
                };
                var mathUnit = new SeriesUnit
                {
                    Series = new TextbookSeries { Subject = math, Name = "M", Slug = "migration-m" },
                    Label = "M1",
                    OrderIndex = 1,
                };
                var order = 0;
                Exercise Ex(SeriesUnit unit) =>
                    new() { SeriesUnit = unit, Type = "Vocabulary", Title = $"X{order}", OrderIndex = order, RewardPoints = 1 };
                PlanPosition Pos(SeriesUnit unit) => new() { Exercise = Ex(unit), Order = order++ };
                StudyPlan Plan(string title, Subject? subject, params PlanPosition[] positions)
                {
                    var plan = new StudyPlan
                    {
                        Child = child,
                        Title = title,
                        Subject = subject,
                        StartDate = new DateOnly(2026, 11, 1),
                        EndDate = new DateOnly(2026, 11, 30),
                    };
                    plan.Positions.AddRange(positions);
                    return plan;
                }

                var majority = Plan("Mehrheit", null, Pos(englishUnit), Pos(englishUnit), Pos(mathUnit));
                var tie = Plan("Gleichstand", null, Pos(englishUnit), Pos(mathUnit));
                var kept = Plan("Gesetzt", math, Pos(englishUnit), Pos(englishUnit));
                var empty = Plan("Leer", null);
                db.StudyPlans.AddRange(majority, tie, kept, empty);
                // Dependents of StudyPlans: SQLite rebuilds the table for the new foreign key (temp table, DROP, rename),
                // so cascading children and the SetNull remark must come through both directions of the step.
                var day = new DateOnly(2026, 11, 5);
                db.PracticeSessions.Add(new PracticeSession { StudyPlan = majority, PlanPosition = majority.Positions[0], Day = day });
                db.PracticeSessions.Add(new PracticeSession { StudyPlan = tie, PlanPosition = tie.Positions[1], Day = day });
                db.TestAttempts.Add(new TestAttempt { StudyPlan = majority, PlanPosition = majority.Positions[1], Day = day });
                db.TestAttempts.Add(new TestAttempt { StudyPlan = kept, PlanPosition = kept.Positions[0], Day = day });
                var remark = new Remark
                {
                    Text = "Bestandsanmerkung",
                    Account = new Account { DisplayName = "Bestand" },
                    AuthorRole = ProfileRole.Supervisor,
                    StudyPlan = majority,
                };
                db.Remarks.Add(remark);
                await db.SaveChangesAsync();
                (majorityId, tieId, keptId, emptyId) = (majority.Id, tie.Id, kept.Id, empty.Id);
                (englishId, mathId) = (english.Id, math.Id);
                remarkId = remark.Id;
                positionsBefore = await db.PlanPositions.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.StudyPlanId);
                sessionsBefore = await db.PracticeSessions.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.StudyPlanId);
                attemptsBefore = await db.TestAttempts.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.StudyPlanId);

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
                // The chain goes on after the step under test; the reads below use today's model, so they need today's schema.
                await migrator.MigrateAsync();
            }

            await using (var db = new PuglingDbContext(options))
            {
                var subjects = await db.StudyPlans.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.SubjectId);
                Assert.Equal(englishId, subjects[majorityId]);
                Assert.Null(subjects[tieId]);
                Assert.Equal(mathId, subjects[keptId]);
                Assert.Null(subjects[emptyId]);
                Assert.False(await db.StudyPlans.AnyAsync(p => p.SeriesUnitId != null));
            }

            await using (var db = new PuglingDbContext(options))
            {
                // The table rebuild must not cascade-delete the plans' children nor null the remark's plan reference.
                Assert.Equal(positionsBefore, await db.PlanPositions.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.StudyPlanId));
                Assert.Equal(sessionsBefore, await db.PracticeSessions.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.StudyPlanId));
                Assert.Equal(attemptsBefore, await db.TestAttempts.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.StudyPlanId));
                Assert.Equal(7, positionsBefore.Count);
                Assert.Equal(2, sessionsBefore.Count);
                Assert.Equal(2, attemptsBefore.Count);
                Assert.Equal(majorityId, (await db.Remarks.AsNoTracking().SingleAsync(r => r.Id == remarkId)).StudyPlanId);
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
