using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-700 AC 6: the migration <c>AufbauStufe</c> against a database in the state before it. An exercise written in the
/// old shape (raw SQL, without the new columns) survives with both build fields <c>null</c> and its other values intact.
/// SQLite rebuilds the table for the new foreign key (temp table, DROP, rename) and <c>Exercises</c> is the root of most
/// child data, so the test also seeds the children a cascading DROP would take with it - an item with the learning progress
/// on it, a grant and a study plan position (review finding: the row alone would stay green under such a DROP).
/// </summary>
public sealed class AufbauStufeMigrationTests
{
    private const string Before = "20261122081510_DutyUntilCadenceUntil";
    private const string Target = "20261122201417_AufbauStufe";

    /// <summary>
    /// Red when the migration adds a non-nullable column, the table rebuild drops or alters the row, or the DROP cascades to
    /// the exercise's items, learning progress, grants or positions.
    /// </summary>
    [Fact]
    public async Task Bestehende_Uebung_Bleibt_Unveraendert_Lesbar()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-aufbaustufe-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int exerciseId, itemId, progressId, grantId, positionId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.GetService<IMigrator>().MigrateAsync(Before);
                // Subjects, series and units are unchanged by this migration, so the current model may write them.
                var subject = new Subject { Name = "Englisch" };
                db.Subjects.Add(subject);
                await db.SaveChangesAsync();
                var series = new TextbookSeries { Name = "Access", Slug = "access", SubjectId = subject.Id };
                db.TextbookSeries.Add(series);
                await db.SaveChangesAsync();
                var unit = new SeriesUnit { SeriesId = series.Id, Label = "Unit 1", OrderIndex = 1 };
                db.SeriesUnits.Add(unit);
                await db.SaveChangesAsync();

                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO Exercises (SeriesUnitId, Type, Title, OrderIndex, RewardPoints, ConfigJson, SchoolTypes,
                        DefaultUseLeitner, DefaultRequireTypedTest, ExecutePublic, CreatedAt, BookPages)
                    VALUES ({unit.Id}, 'Vocabulary', 'Alt', 3, 15, '{"{}"}', 0, 1, 0, 1, '2026-11-01 08:00:00', '42')
                    """);
                exerciseId = await db.Database.SqlQuery<int>($"SELECT Id AS Value FROM Exercises").SingleAsync();

                // Children of the exercise, written with today's model: their tables are unchanged by this step.
                var adult = new Adult { Name = "Vater" };
                var vocab = new Vocabulary { Key = "en-de:dog", SourceLanguage = "en", TargetLanguage = "de", Word = "dog", Translation = "Hund" };
                db.AddRange(adult, vocab);
                await db.SaveChangesAsync();
                // The child table gained columns after this step, so the child goes in as a raw row.
                var childId = await LegacyChildRow.InsertAsync(db, "Kind");
                var item = new ExerciseItem { ExerciseId = exerciseId, OrderIndex = 0, VocabularyId = vocab.Id };
                var grant = new ExerciseGrant { ExerciseId = exerciseId, CreatorId = adult.Id, Permission = GrantPermission.Owner, GrantedByAdultId = adult.Id };
                var plan = new StudyPlan { ChildId = childId, Title = "Plan" };
                db.AddRange(item, grant, plan);
                await db.SaveChangesAsync();
                var progress = new ItemProgress { ChildId = childId, ItemId = item.Id, ExerciseId = exerciseId, VocabularyId = vocab.Id, Box = 3 };
                var position = new PlanPosition { StudyPlanId = plan.Id, ExerciseId = exerciseId };
                db.AddRange(progress, position);
                await db.SaveChangesAsync();
                (itemId, progressId, grantId, positionId) = (item.Id, progress.Id, grant.Id, position.Id);

                await db.GetService<IMigrator>().MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var exercise = await db.Exercises.AsNoTracking().SingleAsync(e => e.Id == exerciseId);
                Assert.Null(exercise.BuildLevel);
                Assert.Null(exercise.BuildTargetExerciseId);
                Assert.Equal("Alt", exercise.Title);
                Assert.Equal(15, exercise.RewardPoints);
                Assert.Equal("42", exercise.BookPages);
                Assert.True(exercise.DefaultUseLeitner);

                Assert.Equal(exerciseId, (await db.ExerciseItems.AsNoTracking().SingleAsync(i => i.Id == itemId)).ExerciseId);
                Assert.Equal(3, (await db.ItemProgress.AsNoTracking().SingleAsync(p => p.Id == progressId)).Box);
                Assert.Equal(exerciseId, (await db.ExerciseGrants.AsNoTracking().SingleAsync(g => g.Id == grantId)).ExerciseId);
                Assert.Equal(exerciseId, (await db.PlanPositions.AsNoTracking().SingleAsync(p => p.Id == positionId)).ExerciseId);
            }
        }
        finally
        {
            SqliteConnection.ClearPool(new SqliteConnection(connectionString));
            File.Delete(dbPath);
        }
    }
}
