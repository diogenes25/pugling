using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-624: in practice an accent slip ("ecole" for "école") counts half on a lenient position - the box holds and the card
/// comes back at its next due date, no coins, the combo carries on - while the final test, a strict position and the
/// cross-plan learning state per word stay binary. The folding itself is unit-tested in <see cref="AkzentMildeTests"/>.
/// Own fixture, because the cases freeze the shared test clock.
/// </summary>
public class AkzentMildePfadTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, HttpClient Father, int PlanId, int PositionId, int ExerciseId,
        DateTimeOffset Start);

    private static readonly (string, string)[] Words = [("école", "Schule"), ("forêt", "Wald"), ("élève", "Schüler")];

    /// <summary>
    /// AK 1: box 3, "ecole" → half: not correct, marked as accent slip, box 3, due after box 3's interval (4 days, not
    /// today), no coins. Red when the slip runs the miss branch (box 1, due today), the hit branch (box 4, coins) or is
    /// not recognized (accentSlip false).
    /// </summary>
    [Fact]
    public async Task Akzentfehler_Haelt_Den_Kasten_Und_Zahlt_Nichts()
    {
        var s = await SetupAsync(lenient: true, startBox: 3);
        var coinsBefore = Coins();

        var outcome = await ReviewAsync(s, await SessionAsync(s), 0, "ecole");

        Assert.False(outcome.GetProperty("wasCorrect").GetBoolean());
        Assert.True(outcome.GetProperty("accentSlip").GetBoolean());
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal("école", outcome.GetProperty("expected").GetString());
        var prog = Progress(s.PositionId, 0);
        Assert.Equal(3, prog.Box);
        Assert.Equal(DateOnly.FromDateTime(s.Start.UtcDateTime).AddDays(4), prog.DueOn);
        Assert.Equal(coinsBefore, Coins());
    }

    /// <summary>
    /// AK 1, combo (risk 1): hit, slip, hit in one round → combo 1, 1, 2. Red when the slip breaks the streak (third 1)
    /// or extends it (second 2).
    /// </summary>
    [Fact]
    public async Task Akzentfehler_Reisst_Die_Combo_Nicht()
    {
        var s = await SetupAsync(lenient: true, startBox: 3);
        var session = await SessionAsync(s);

        var combos = new List<int>();
        foreach (var (index, answer) in new[] { (1, "forêt"), (0, "ecole"), (2, "élève") })
            combos.Add((await ReviewAsync(s, session, index, answer)).GetProperty("combo").GetInt32());

        Assert.Equal([1, 1, 2], combos);
    }

    /// <summary>AK 2: a wrong letter stays a full miss, accent leniency or not. Red when the slip test folds letters too.</summary>
    [Fact]
    public async Task Buchstabenfehler_Bleibt_Ein_Ganzer_Fehler()
    {
        var s = await SetupAsync(lenient: true, startBox: 3);

        var outcome = await ReviewAsync(s, await SessionAsync(s), 0, "ecolle");

        // "Misspelled" is not asserted: "ecolle" is two edits from the five-letter "école", past B-422's threshold of one.
        Assert.False(outcome.GetProperty("wasCorrect").GetBoolean());
        Assert.False(outcome.GetProperty("accentSlip").GetBoolean());
        Assert.Equal(1, Progress(s.PositionId, 0).Box);
    }

    /// <summary>
    /// AK 3: the final test stays binary - "ecole" is a wrong item there. A statement of intent: green before B-624 as
    /// well, red only if the test path starts counting a slip (it never calls <see cref="AnswerGrader.IsAccentSlip"/>).
    /// </summary>
    [Fact]
    public async Task Abschlusstest_Wertet_Den_Akzentfehler_Als_Falsch()
    {
        var s = await SetupAsync(lenient: true, startBox: 3);
        var baseUrl = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await s.Child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "ecole" },
                new { itemIndex = 1, givenAnswer = "forêt" },
                new { itemIndex = 2, givenAnswer = "élève" },
            },
        });

        submit.EnsureSuccessStatusCode();
        Assert.Equal(2, (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correctItems").GetInt32());
    }

    /// <summary>AK 4: with the switch off practice is strict as before - box 1. Red when the review ignores the switch.</summary>
    [Fact]
    public async Task Ohne_Haken_Ist_Der_Akzentfehler_Ein_Ganzer_Fehler()
    {
        var s = await SetupAsync(lenient: false, startBox: 3);

        var outcome = await ReviewAsync(s, await SessionAsync(s), 0, "ecole");

        Assert.False(outcome.GetProperty("accentSlip").GetBoolean());
        Assert.Equal(1, Progress(s.PositionId, 0).Box);
    }

    /// <summary>
    /// AK 5, new positions: created without the field the position is lenient, and the father can switch it off and on.
    /// Red when the create default is false or the PATCH does not apply the field.
    /// </summary>
    [Fact]
    public async Task Neue_Position_Ist_Mild_Und_Abschaltbar()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("plage", "Strand"));
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var created = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var position = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(position.GetProperty("accentLenient").GetBoolean());

        var url = $"/api/v1/supervisor/study-plans/{planId}/positions/{position.GetProperty("id").GetInt32()}";
        var off = await father.PatchAsJsonAsync(url, new { accentLenient = false });
        Assert.False((await off.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accentLenient").GetBoolean());
        var untouched = await father.PatchAsJsonAsync(url, new { penaltyCoins = 1 });
        Assert.False((await untouched.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accentLenient").GetBoolean());
        var on = await father.PatchAsJsonAsync(url, new { accentLenient = true });
        Assert.True((await on.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accentLenient").GetBoolean());
    }

    /// <summary>
    /// AK 7: the position report counts the slip per item. Red when the review does not flag the history row, the
    /// report does not read it, counts every answer (item 1 is answered right and must show 0), or counts an ungraded slip.
    /// </summary>
    [Fact]
    public async Task Report_Zaehlt_Akzentfehler_Je_Wort()
    {
        var s = await SetupAsync(lenient: true, startBox: 3);
        var session = await SessionAsync(s);
        await ReviewAsync(s, session, 0, "ecole");
        await ReviewAsync(s, session, 1, "forêt");
        // The same slip again today in a new round is not graded (already reviewed today) and counted no half: no second count.
        await ReviewAsync(s, await SessionAsync(s), 0, "ecole");

        var report = await s.Father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}/report");
        var rows = report.GetProperty("items").EnumerateArray().ToDictionary(r => r.GetProperty("itemIndex").GetInt32());
        Assert.Equal(1, rows[0].GetProperty("accentSlips").GetInt32());
        Assert.Equal(0, rows[1].GetProperty("accentSlips").GetInt32());
    }

    /// <summary>
    /// AK 8: the cross-plan learning state per word stays hard - seeded box 4 drops to 1 on the slip while the position
    /// holds box 3. Red when the slip is recorded as a hit or not at all in <see cref="ItemProgressService"/>.
    /// </summary>
    [Fact]
    public async Task Planuebergreifender_Lernstand_Wertet_Den_Akzentfehler_Als_Falsch()
    {
        var s = await SetupAsync(lenient: true, startBox: 3);
        int itemId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var item = db.ExerciseItems.Where(i => i.ExerciseId == s.ExerciseId).OrderBy(i => i.OrderIndex).First();
            itemId = item.Id;
            db.ItemProgress.Add(new ItemProgress
            {
                ChildId = 1,
                ItemId = item.Id,
                ExerciseId = s.ExerciseId,
                VocabularyId = item.VocabularyId,
                Box = 4,
            });
            db.SaveChanges();
        }

        await ReviewAsync(s, await SessionAsync(s), 0, "ecole");

        var crossPlan = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/1/vocabulary-progress/{itemId}");
        Assert.Equal(1, crossPlan.GetProperty("box").GetInt32());
        Assert.Equal(3, Progress(s.PositionId, 0).Box);
    }

    /// <summary>
    /// AK 5, existing positions: a position stored strict before the migration is lenient after it (decision 4). The
    /// row is taken back one migration (the column is dropped) and forward again. Red when the migration's backfill
    /// <c>UPDATE</c> is missing - the column would then come back as false.
    /// </summary>
    [Fact]
    public async Task Migration_Schaltet_Bestehende_Positionen_Mild()
    {
        const string before = "20261124202234_FeedbackCorrection";
        const string target = "20261125100246_AccentLeniency";
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-akzent-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int positionId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                var unit = new SeriesUnit { Series = new TextbookSeries { Name = "Reihe", Slug = "reihe-akzent" }, Label = "Unit 1" };
                var exercise = new Exercise { SeriesUnit = unit, Type = ExerciseTypeKeys.Vocabulary, Title = "Wörter" };
                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();
                var start = new DateOnly(2026, 11, 20);
                var plan = new StudyPlan { ChildId = child.Id, Title = "Bestand", StartDate = start, EndDate = start.AddDays(20) };
                plan.Positions.Add(new PlanPosition { ExerciseId = exercise.Id, AccentLenient = false });
                db.StudyPlans.Add(plan);
                await db.SaveChangesAsync();
                positionId = plan.Positions.Single().Id;

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(before);
                await migrator.MigrateAsync(target);
            }

            await using (var db = new PuglingDbContext(options))
                Assert.True(await db.PlanPositions.Where(p => p.Id == positionId).Select(p => p.AccentLenient).SingleAsync());
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }

    private async Task<Setup> SetupAsync(bool lenient, int startBox)
    {
        // Noon UTC of the real day, so no day boundary sits inside a case.
        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero);
        factory.Clock.FreezeAt(start);
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, Words);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        var today = DateOnly.FromDateTime(start.UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var plan = db.StudyPlans.Single(p => p.Id == planId);
            plan.StartDate = today.AddDays(-30);
            plan.EndDate = today.AddDays(30);
            db.PlanPositions.Single(p => p.Id == positionId).AccentLenient = lenient;
            // Introduced a month ago: past the ladder deadline, so every card is asked typed whatever its box (B-476).
            foreach (var prog in db.PositionItemProgress.Where(p => p.PlanPositionId == positionId))
            {
                prog.Box = startBox;
                prog.DueOn = today;
                prog.IntroducedAt = today.AddDays(-30);
                prog.NewBonusPaidAt = start.UtcDateTime.AddDays(-30);
            }
            db.SaveChanges();
        }
        return new Setup(await TestApi.ChildAsync(factory), father, planId, positionId, exerciseId, start);
    }

    private Task<int> SessionAsync(Setup s) => TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);

    private static async Task<JsonElement> ReviewAsync(Setup s, int sessionId, int itemIndex, string answer)
    {
        var res = await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, itemIndex, givenAnswer: answer);
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private int Coins()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ChildPointsEntries.Where(e => e.ChildId == 1).Sum(e => e.Amount);
    }

    private PositionItemProgress Progress(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
    }
}
