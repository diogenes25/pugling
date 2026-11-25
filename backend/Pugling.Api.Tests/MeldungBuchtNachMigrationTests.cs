using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-767 and B-768: the migrations <c>FeedbackAnswerOutcome</c> and <c>FeedbackCorrection</c> applied to a database that
/// already holds a feedback, a failed test, a booked penalty and the child's ledger. Nothing is backfilled - an old feedback
/// gets no answer reference or outcome (it cannot be judged justified, B-767 risk 4), no attempt a correction trail and no
/// penalty a refund mark - and the rebuilt tables (SQLite adds a foreign key by rebuilding the table) keep every row.
/// Same pattern as <see cref="VorbereitungsPflichtMigrationTests"/>: fully migrated and seeded, taken back and forward.
/// </summary>
public sealed class MeldungBuchtNachMigrationTests
{
    private const string Before = "20261124131013_ClassTestPrepLeftOut";
    private const string Target = "20261124202234_FeedbackCorrection";

    /// <summary>Red when either migration fills a new column for existing rows or loses a row in a rebuild.</summary>
    [Fact]
    public async Task Bestand_Bleibt_Ohne_Bezug_Korrektur_Und_Ruecknahme()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-feedbackcorrection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            var day = new DateOnly(2026, 11, 20);
            int childId, feedbackId, attemptId, penaltyId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                var series = new TextbookSeries { Name = "Reihe", Slug = "reihe-feedbackcorrection" };
                var unit = new SeriesUnit { Series = series, Label = "Unit 1" };
                var exercise = new Exercise { SeriesUnit = unit, Type = ExerciseTypeKeys.Vocabulary, Title = "Wörter" };
                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();
                var plan = new StudyPlan { ChildId = child.Id, Title = "Bestand", StartDate = day, EndDate = day.AddDays(20) };
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, Cadence = GoalCadence.Daily, PenaltyCoins = 5 });
                db.StudyPlans.Add(plan);
                await db.SaveChangesAsync();
                var pos = plan.Positions[0];
                var attempt = new TestAttempt
                {
                    StudyPlanId = plan.Id,
                    PlanPositionId = pos.Id,
                    Day = day,
                    TotalItems = 2,
                    CorrectItems = 1,
                    ScorePercent = 50,
                    StartedAt = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
                    CompletedAt = day.ToDateTime(new TimeOnly(10, 5), DateTimeKind.Utc),
                    Results = [new TestItemResult { ItemIndex = 0, GivenAnswer = "halo" }],
                };
                db.TestAttempts.Add(attempt);
                var penalty = new PositionGoalPenalty
                {
                    PlanPositionId = pos.Id,
                    Cadence = GoalCadence.Daily,
                    PeriodStart = day,
                    Day = day,
                    Points = 5,
                    AppliedAt = day.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                };
                db.PositionGoalPenalties.Add(penalty);
                var feedback = new ExerciseFeedback
                {
                    ChildId = child.Id,
                    PlanPositionId = pos.Id,
                    ExerciseId = exercise.Id,
                    ItemIndex = 0,
                    Reason = FeedbackReason.TooStrict,
                    Status = FeedbackStatus.Done,
                    Text = "nur ein l",
                    CreatedAt = DateTime.UtcNow,
                    LastReportedAt = DateTime.UtcNow,
                    DoneAt = DateTime.UtcNow,
                };
                db.ExerciseFeedbacks.Add(feedback);
                db.ChildPointsEntries.Add(new ChildPointsEntry
                {
                    ChildId = child.Id,
                    Kind = PointKind.GoalPenalty,
                    Amount = -5,
                    Reason = "gerissen",
                    CreatedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
                (childId, feedbackId, attemptId, penaltyId) = (child.Id, feedback.Id, attempt.Id, penalty.Id);

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var feedback = await db.ExerciseFeedbacks.AsNoTracking().SingleAsync(f => f.Id == feedbackId);
                Assert.Equal((FeedbackStatus.Done, "nur ein l", (int?)null, (FeedbackOutcome?)null),
                    (feedback.Status, feedback.Text, feedback.TestItemResultId, feedback.Outcome));
                var attempt = await db.TestAttempts.AsNoTracking().SingleAsync(t => t.Id == attemptId);
                // Counted, not loaded: the schema stands at the target, and a result column added by a later migration
                // (B-732's ErrorKind) does not exist here yet.
                var results = await db.Set<TestItemResult>().CountAsync(r => r.TestAttemptId == attemptId);
                Assert.Equal((1, 50, false, (int?)null, (bool?)null, 1),
                    (attempt.CorrectItems, attempt.ScorePercent, attempt.Passed, attempt.CorrectedItems,
                     attempt.PassedAfterCorrection, results));
                var penalty = await db.PositionGoalPenalties.AsNoTracking().SingleAsync(p => p.Id == penaltyId);
                Assert.Equal((5, (DateTime?)null, (int?)null), (penalty.Points, penalty.RefundedAt, penalty.RefundedByFeedbackId));
                Assert.Equal(-5, await db.ChildPointsEntries.Where(e => e.ChildId == childId).SumAsync(e => e.Amount));
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
