using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-741: the migration <c>NotenstufeChildSubjectLevel</c> applied to a database with existing data. The table is new
/// and starts empty; what matters is that the real database takes it without losing its rows, and that the unique
/// index holds one level per (child, subject) from the first evaluation on.
/// </summary>
public sealed class NotenstufeMigrationTests
{
    private const string Before = "20261124021739_MixMixedReviewRuns";
    private const string Target = "20261124021849_NotenstufeChildSubjectLevel";

    /// <summary>Red if the migration touched existing rows or lacked the unique index on (child, subject).</summary>
    [Fact]
    public async Task Bestand_bleibt_und_die_Stufe_ist_je_Kind_und_Fach_eindeutig()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-notenstufe-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int childId, subjectId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                var subject = new Subject { Name = "Bestandsfach" };
                db.AddRange(child, subject);
                await db.SaveChangesAsync();
                (childId, subjectId) = (child.Id, subject.Id);

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Before);
                await migrator.MigrateAsync(Target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                Assert.True(await db.Children.AnyAsync(c => c.Id == childId));
                Assert.True(await db.Subjects.AnyAsync(s => s.Id == subjectId));
                Assert.False(await db.ChildSubjectLevels.AnyAsync());

                var day = new DateOnly(2026, 11, 23);
                db.ChildSubjectLevels.Add(new ChildSubjectLevel { ChildId = childId, SubjectId = subjectId, Level = 3, BestLevel = 3, EvaluatedOn = day });
                await db.SaveChangesAsync();
                db.ChildSubjectLevels.Add(new ChildSubjectLevel { ChildId = childId, SubjectId = subjectId, Level = 2, BestLevel = 2, EvaluatedOn = day });
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
