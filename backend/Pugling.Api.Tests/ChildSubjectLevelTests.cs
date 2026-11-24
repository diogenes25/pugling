using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-741: the grade level per child and vocabulary subject. The measure is the share of mastered words in the material
/// of the active plans; the level is evaluated once per learning day, rises at once, sinks at most one step per seven
/// days, keeps its best value and books nothing to the ledger.
/// <para>
/// The mastery of the words is written as <see cref="ItemProgress"/> rows directly: reaching box 5 through the play
/// endpoints takes weeks of Leitner days per word, and the level only reads what the grading points already wrote
/// (the play path into <see cref="ItemProgress"/> has its own tests). The evaluation day is passed explicitly, so the
/// seven-day rule needs no clock.
/// </para>
/// </summary>
public class ChildSubjectLevelTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly DateOnly Day0 = new(2030, 3, 4);

    // Subject → series → unit → one vocabulary exercise with `count` unique words.
    private static async Task<(int SubjectId, int ExerciseId)> VocabSubjectAsync(HttpClient father, string prefix, int count)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName(prefix) }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId, "Unit 1");
        var exerciseId = await VocabInAsync(father, seriesId, unitId, prefix, count);
        return (subjectId, exerciseId);
    }

    private static async Task<int> VocabInAsync(HttpClient father, int seriesId, int unitId, string prefix, int count)
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = prefix,
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = Enumerable.Range(0, count).Select(i => new { front = $"{prefix}{tag}w{i}", back = $"{prefix}{tag}b{i}" }),
                },
            }));
    }

    private int SeedPlan(bool active, params int[] exerciseIds) => SeedPlanIn(null, active, exerciseIds);

    private int SeedPlanIn(int? subjectId, bool active, params int[] exerciseIds)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = new StudyPlan { ChildId = 1, SubjectId = subjectId, Title = "Notenstufe", StartDate = Day0, EndDate = Day0.AddDays(60), Active = active };
        var order = 0;
        foreach (var id in exerciseIds)
            plan.Positions.Add(new PlanPosition { ExerciseId = id, Order = order++, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return plan.Id;
    }

    // Puts the first `count` words of the exercise into `box` (5 = mastered), with many answers each, so a measure that
    // counted answers instead of mastery would show it.
    private void SetBoxes(int exerciseId, int count, int box)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var items = db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).Take(count).ToList();
        var existing = db.ItemProgress.Where(p => p.ChildId == 1 && p.ExerciseId == exerciseId).ToDictionary(p => p.ItemId);
        foreach (var item in items)
        {
            if (!existing.TryGetValue(item.Id, out var p))
                db.ItemProgress.Add(p = new ItemProgress { ChildId = 1, ItemId = item.Id, ExerciseId = exerciseId, VocabularyId = item.VocabularyId, IntroducedAt = Day0 });
            p.Box = box;
            p.MasteryPercent = box * 20;
            p.SeenCount = 40;
            p.CorrectCount = 40;
        }
        db.SaveChanges();
    }

    private async Task EvaluateAsync(DateOnly day)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SubjectLevelService>().EvaluateAsync(1, day, CancellationToken.None);
    }

    private ChildSubjectLevel? LevelOf(int subjectId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ChildSubjectLevels.AsNoTracking()
            .SingleOrDefault(l => l.ChildId == 1 && l.SubjectId == subjectId);
    }

    // AK 1: nothing mastered ranks below everything mastered; many answers on unmastered words do not lift it.
    // Red if the denominator were the introduced words or the answer count counted.
    [Fact]
    public async Task Null_beherrscht_steht_unter_alles_beherrscht()
    {
        var father = await TestApi.AdultAsync(factory);
        var (none, exNone) = await VocabSubjectAsync(father, "StufeNull", 10);
        var (all, exAll) = await VocabSubjectAsync(father, "StufeAlle", 10);
        SeedPlan(true, exNone);
        SeedPlan(true, exAll);
        SetBoxes(exNone, 10, box: 4);
        SetBoxes(exAll, 10, box: ItemProgress.MaxBox);

        await EvaluateAsync(Day0);

        Assert.Equal(6, LevelOf(none)!.Level);
        Assert.Equal(1, LevelOf(all)!.Level);
    }

    // AK 2: three perfect words out of 200 are a worse level than 150 mastered. Red if AvgMasteryPercent (mean over the
    // introduced words) were the measure - then three perfect words would be 100 percent.
    [Fact]
    public async Task Drei_von_200_ist_schlechter_als_150_von_200()
    {
        var father = await TestApi.AdultAsync(factory);
        var (few, exFew) = await VocabSubjectAsync(father, "StufeWenig", 200);
        var (many, exMany) = await VocabSubjectAsync(father, "StufeViel", 200);
        SeedPlan(true, exFew);
        SeedPlan(true, exMany);
        SetBoxes(exFew, 3, ItemProgress.MaxBox);
        SetBoxes(exMany, 150, ItemProgress.MaxBox);

        await EvaluateAsync(Day0);

        Assert.Equal(6, LevelOf(few)!.Level);   // 1.5 %
        Assert.Equal(3, LevelOf(many)!.Level);  // 75 %: from 67 on a 3
    }

    // AK 3 and 5: a second evaluation on the same day changes nothing, even when the data moved in between; the next
    // day takes the new data. Red if the evaluation ran per access instead of per day.
    [Fact]
    public async Task Zweiter_Lauf_am_selben_Tag_aendert_nichts()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subject, ex) = await VocabSubjectAsync(father, "StufeTag", 10);
        SeedPlan(true, ex);
        SetBoxes(ex, 5, ItemProgress.MaxBox); // 50 % -> 4

        await EvaluateAsync(Day0);
        Assert.Equal(4, LevelOf(subject)!.Level);

        SetBoxes(ex, 10, ItemProgress.MaxBox); // 100 % -> 1, but the day is evaluated already
        await EvaluateAsync(Day0);
        Assert.Equal(4, LevelOf(subject)!.Level);
        Assert.Equal(Day0, LevelOf(subject)!.EvaluatedOn);

        await EvaluateAsync(Day0.AddDays(1));
        Assert.Equal(1, LevelOf(subject)!.Level); // rising is not limited
    }

    // AK 4 and 6: a collapse of the learning state lowers by one step, then not again for seven days; the best level
    // stays. Red if one run lowered by two, lowered twice within seven days, or the lowering overwrote BestLevel.
    [Fact]
    public async Task Absturz_senkt_eine_Stufe_je_sieben_Tage_Bestwert_bleibt()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subject, ex) = await VocabSubjectAsync(father, "StufeSturz", 10);
        SeedPlan(true, ex);
        SetBoxes(ex, 10, ItemProgress.MaxBox);
        await EvaluateAsync(Day0);
        Assert.Equal(1, LevelOf(subject)!.Level);

        SetBoxes(ex, 10, box: 1); // measured 6 from now on
        await EvaluateAsync(Day0.AddDays(1));
        var afterFirst = LevelOf(subject)!;
        Assert.Equal(2, afterFirst.Level);
        Assert.Equal(Day0.AddDays(1), afterFirst.LastLoweredOn);

        await EvaluateAsync(Day0.AddDays(7));     // six days after the lowering
        Assert.Equal(2, LevelOf(subject)!.Level);

        await EvaluateAsync(Day0.AddDays(8));     // seven days after it
        var afterSecond = LevelOf(subject)!;
        Assert.Equal(3, afterSecond.Level);
        Assert.Equal(1, afterSecond.BestLevel);

        // Rising is not bound to the lowering interval, and a single step up counts as well (red if a rise needed
        // more than one grade of distance): 9 of 10 words = 90 percent = a 2, the day after the second lowering.
        SetBoxes(ex, 9, ItemProgress.MaxBox);
        await EvaluateAsync(Day0.AddDays(9));
        Assert.Equal(2, LevelOf(subject)!.Level);
    }

    // AK 7: a subject without vocabulary (arithmetic) gets no row. Red if a subject without measure got a level.
    [Fact]
    public async Task Fach_ohne_Wortschatz_bekommt_keine_Zeile()
    {
        var father = await TestApi.AdultAsync(factory);
        var (mathSubject, _, mathExercise) = await TestApi.CreateArithmeticExerciseAsync(father);
        SeedPlan(true, mathExercise);

        await EvaluateAsync(Day0);

        Assert.Null(LevelOf(mathSubject));
    }

    // AK 8: mastered words of an inactive plan do not lift the level. Red if the frame were every plan.
    [Fact]
    public async Task Nur_der_aktive_Plan_zaehlt()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("StufeAktiv") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId, "Unit 1");
        var oldExercise = await VocabInAsync(father, seriesId, unitId, "StufeAlt", 10);
        var newExercise = await VocabInAsync(father, seriesId, unitId, "StufeNeu", 10);
        SeedPlan(false, oldExercise);
        SeedPlan(true, newExercise);
        SetBoxes(oldExercise, 10, ItemProgress.MaxBox);

        await EvaluateAsync(Day0);

        Assert.Equal(6, LevelOf(subjectId)!.Level);
    }

    // The level hangs on the subject of the active plan - the subject the child's day view files the card under - and
    // on the series' subject only for a plan without one. Red if the measure grouped by the series' subject: then the
    // level sat on a subject without a card and the card stayed empty (review finding, Sprint 127).
    [Fact]
    public async Task Stufe_haengt_am_Fach_des_Plans()
    {
        var father = await TestApi.AdultAsync(factory);
        var planSubject = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("StufePlanfach") }));
        var (seriesSubject, ex) = await VocabSubjectAsync(father, "StufeReihenfach", 10);
        SeedPlanIn(planSubject, true, ex);
        SetBoxes(ex, 10, ItemProgress.MaxBox);

        await EvaluateAsync(Day0);

        Assert.Equal(1, LevelOf(planSubject)!.Level);
        Assert.Null(LevelOf(seriesSubject));
    }

    // AK 9: a level change books nothing. Red if a ChildPointsEntry appeared with the change.
    [Fact]
    public async Task Stufenwechsel_bucht_nichts()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subject, ex) = await VocabSubjectAsync(father, "StufeKasse", 10);
        SeedPlan(true, ex);
        await EvaluateAsync(Day0);
        Assert.Equal(6, LevelOf(subject)!.Level);
        var entriesBefore = LedgerCount();

        SetBoxes(ex, 10, ItemProgress.MaxBox);
        await EvaluateAsync(Day0.AddDays(1));

        Assert.Equal(1, LevelOf(subject)!.Level);
        Assert.Equal(entriesBefore, LedgerCount());
    }

    // The daily run hangs on the child login (no scheduler). Red if the login no longer evaluated.
    [Fact]
    public async Task Kind_Anmeldung_bewertet_den_Tag()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subject, ex) = await VocabSubjectAsync(father, "StufeLogin", 10);
        SeedPlan(true, ex);
        Assert.Null(LevelOf(subject));

        await TestApi.ChildAsync(factory);

        Assert.NotNull(LevelOf(subject));
    }

    private int LedgerCount()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ChildPointsEntries.Count(e => e.ChildId == 1);
    }
}
