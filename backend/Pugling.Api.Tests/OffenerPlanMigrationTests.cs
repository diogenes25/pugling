using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-735, AK 6: the migration <c>OffenerPlan</c> makes <c>StudyPlans.EndDate</c> nullable. SQLite rebuilds the table for
/// that, so every existing plan must come through with its date and its dependents (positions, sessions, attempts,
/// remarks) intact. The chain runs on a throwaway file: migrated and seeded, taken back one step and forward again.
/// </summary>
public sealed class OffenerPlanMigrationTests
{
    private const string Before = "20261126220154_Tagesplaner";
    private const string Target = "20261127072327_OffenerPlan";

    /// <summary>
    /// Red when the migration is missing (the later insert of an open plan fails on NOT NULL), when the rebuild drops or changes a
    /// plan's date (read as raw text, not through the model), or when it cascades away a plan's children.
    /// </summary>
    [Fact]
    public async Task Bestandsplaene_Behalten_Datum_Und_Kinder_Und_Ein_Offener_Plan_Ist_Moeglich()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-offenerplan-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int planId, openId, remarkId;
            Dictionary<int, int> positionsBefore, sessionsBefore, attemptsBefore;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                var subject = new Subject { Name = "Offen-Migration" };
                var unit = new SeriesUnit
                {
                    Series = new TextbookSeries { Subject = subject, Name = "O", Slug = "migration-offen" },
                    Label = "O1",
                    OrderIndex = 1,
                };
                var exercise = new Exercise { SeriesUnit = unit, Type = "Vocabulary", Title = "X", OrderIndex = 0, RewardPoints = 1 };
                var plan = new StudyPlan
                {
                    Child = child,
                    Title = "Mit Ende",
                    Subject = subject,
                    StartDate = new DateOnly(2026, 11, 1),
                    EndDate = new DateOnly(2026, 11, 30),
                };
                plan.Positions.Add(new PlanPosition { Exercise = exercise, Order = 0 });
                db.StudyPlans.Add(plan);
                var day = new DateOnly(2026, 11, 5);
                db.PracticeSessions.Add(new PracticeSession { StudyPlan = plan, PlanPosition = plan.Positions[0], Day = day });
                db.TestAttempts.Add(new TestAttempt { StudyPlan = plan, PlanPosition = plan.Positions[0], Day = day });
                var remark = new Remark
                {
                    Text = "Bestandsanmerkung",
                    Account = new Account { DisplayName = "Bestand" },
                    AuthorRole = ProfileRole.Supervisor,
                    StudyPlan = plan,
                };
                db.Remarks.Add(remark);
                await db.SaveChangesAsync();
                planId = plan.Id;
                remarkId = remark.Id;
                positionsBefore = await db.PlanPositions.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.StudyPlanId);
                sessionsBefore = await db.PracticeSessions.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.StudyPlanId);
                attemptsBefore = await db.TestAttempts.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.StudyPlanId);

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
                await migrator.MigrateAsync();

                var open = new StudyPlan
                {
                    ChildId = child.Id,
                    Title = "Ohne Ende",
                    SubjectId = subject.Id,
                    StartDate = new DateOnly(2026, 12, 1),
                    EndDate = null,
                };
                db.StudyPlans.Add(open);
                await db.SaveChangesAsync();
                openId = open.Id;
            }

            await using (var db = new PuglingDbContext(options))
            {
                // Raw text, not the model: an unreadable value would otherwise surface as a default date.
                var raw = await db.Database.SqlQueryRaw<string>("SELECT COALESCE(CAST(\"EndDate\" AS TEXT), 'NULL') AS \"Value\" FROM \"StudyPlans\" WHERE \"Id\" = {0}", planId).SingleAsync();
                Assert.StartsWith("2026-11-30", raw);
                Assert.Null((await db.StudyPlans.AsNoTracking().SingleAsync(p => p.Id == openId)).EndDate);

                Assert.Equal(positionsBefore, await db.PlanPositions.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.StudyPlanId));
                Assert.Equal(sessionsBefore, await db.PracticeSessions.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.StudyPlanId));
                Assert.Equal(attemptsBefore, await db.TestAttempts.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.StudyPlanId));
                Assert.Single(positionsBefore);
                Assert.Single(sessionsBefore);
                Assert.Single(attemptsBefore);
                Assert.Equal(planId, (await db.Remarks.AsNoTracking().SingleAsync(r => r.Id == remarkId)).StudyPlanId);
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
