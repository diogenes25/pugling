using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-744: on the test day the child's level in the test's subject is written once onto the class test
/// (<see cref="Klassenarbeit.PredictedLevel"/>), by the same lazy daily run that evaluates the level. The evaluation day
/// is passed explicitly, the mastery is written as <see cref="ItemProgress"/> rows (same setup as
/// <see cref="ChildSubjectLevelTests"/>), and the class tests are planned and moved through the supervisor endpoints.
/// Each test plants a level that differs from the one a wrong rule would write, so a stand-in or overwrite shows.
/// </summary>
public class KlassenarbeitPrognoseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly DateOnly Day0 = new(2031, 5, 12);

    private static async Task<(int SubjectId, int ExerciseId)> VocabSubjectAsync(HttpClient father, string prefix, int count)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName(prefix) }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId, "Unit 1");
        var tag = Guid.NewGuid().ToString("N")[..8];
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
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
        return (subjectId, exerciseId);
    }

    private int SeedPlan(int subjectId, bool active, int exerciseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = new StudyPlan { ChildId = 1, SubjectId = subjectId, Title = "Prognose", StartDate = Day0, EndDate = Day0.AddDays(60), Active = active };
        plan.Positions.Add(new PlanPosition { ExerciseId = exerciseId, Order = 0, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return plan.Id;
    }

    // Puts the first `count` words of the exercise into box 5 (mastered); words mastered before stay as they are.
    private void Master(int exerciseId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var known = db.ItemProgress.Where(p => p.ChildId == 1 && p.ExerciseId == exerciseId).Select(p => p.ItemId).ToHashSet();
        foreach (var item in db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).Take(count).ToList())
            if (!known.Contains(item.Id)) db.ItemProgress.Add(new ItemProgress
            {
                ChildId = 1,
                ItemId = item.Id,
                ExerciseId = exerciseId,
                VocabularyId = item.VocabularyId,
                IntroducedAt = Day0,
                Box = ItemProgress.MaxBox,
                MasteryPercent = 100,
                SeenCount = 5,
                CorrectCount = 5,
            });
        db.SaveChanges();
    }

    private async Task EvaluateAsync(DateOnly day)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SubjectLevelService>().EvaluateAsync(1, day, CancellationToken.None);
    }

    // Moves the stored level without the daily rules (a same-day evaluation would not touch it again).
    private void SetStoredLevel(int subjectId, int level)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var row = db.ChildSubjectLevels.Single(l => l.ChildId == 1 && l.SubjectId == subjectId);
        row.Level = level;
        db.SaveChanges();
    }

    private int? PredictedOf(int testId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Klassenarbeiten.AsNoTracking()
            .Where(k => k.Id == testId).Select(k => k.PredictedLevel).Single();
    }

    private static async Task<int> PlanTestAsync(HttpClient father, int? subjectId, DateOnly date)
    {
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = 1,
            title = "Vokabeltest",
            subjectId,
            scheduledDate = date.ToString("yyyy-MM-dd"),
            prepWindowDays = 0,
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
    }

    // 10 words, 7 mastered = 70 % → level 3: neither the best nor the worst grade, so a stand-in 1 or 6 shows.
    private async Task<(int SubjectId, int ExerciseId)> SubjectOnLevel3Async(HttpClient father, string prefix)
    {
        var (subjectId, exerciseId) = await VocabSubjectAsync(father, prefix, 10);
        SeedPlan(subjectId, active: true, exerciseId);
        Master(exerciseId, 7);
        return (subjectId, exerciseId);
    }

    // AK 1: the run on the test day writes the level; a second run the same day does not replace it.
    // Red if the condition "PredictedLevel is empty" goes (the second run would write the planted 2).
    [Fact]
    public async Task Klausurtag_schreibt_die_Stufe_einmal()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, _) = await SubjectOnLevel3Async(father, "PrognoseEins");
        var testId = await PlanTestAsync(father, subjectId, Day0);

        await EvaluateAsync(Day0);
        Assert.Equal(3, PredictedOf(testId));

        SetStoredLevel(subjectId, 2);
        await EvaluateAsync(Day0);
        Assert.Equal(3, PredictedOf(testId));
    }

    // AK 2: before the test day nothing is written, and neither on a later day when the test day passed without a run.
    // Red if the day condition becomes "on or after" (day 2) or "on or before" (day -1).
    [Fact]
    public async Task Nur_am_Klausurtag_nicht_davor_und_nicht_danach()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, _) = await SubjectOnLevel3Async(father, "PrognoseTag");
        var testId = await PlanTestAsync(father, subjectId, Day0.AddDays(1));

        await EvaluateAsync(Day0);
        Assert.Null(PredictedOf(testId));

        await EvaluateAsync(Day0.AddDays(2));
        Assert.Null(PredictedOf(testId));
    }

    // AK 3: a test without a subject gets no snapshot, although the child has a measured level in another subject on
    // that day. Statement of intent: held structurally, a null subject equals no level row in SQL, so no one-line
    // change of the query makes it red; it stays as the guard for a rewrite that matched the level by child alone.
    [Fact]
    public async Task Ohne_Fach_kein_Schnappschuss()
    {
        var father = await TestApi.AdultAsync(factory);
        await SubjectOnLevel3Async(father, "PrognoseOhneFach");
        var testId = await PlanTestAsync(father, null, Day0.AddDays(3));

        await EvaluateAsync(Day0.AddDays(3));

        Assert.Null(PredictedOf(testId));
    }

    // AK 4, first half: a subject with a stored level but no measure (its plan went inactive) writes no stand-in - not
    // 0, not 6, not the stored level. Red if the snapshot read the stored row without the measure, or wrote a default.
    [Fact]
    public async Task Ohne_Mass_bleibt_der_Schnappschuss_leer()
    {
        var father = await TestApi.AdultAsync(factory);
        // A second subject with a measure, so the evaluation does not return early for a child without any measure -
        // the snapshot must skip the unmeasured subject itself.
        await SubjectOnLevel3Async(father, "PrognoseMitMass");
        var (subjectId, exerciseId) = await VocabSubjectAsync(father, "PrognoseOhneMass", 10);
        var planId = SeedPlan(subjectId, active: true, exerciseId);
        Master(exerciseId, 7);
        await EvaluateAsync(Day0.AddDays(4)); // stored level 3 exists from here on
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.StudyPlans.Single(p => p.Id == planId).Active = false;
            db.SaveChanges();
        }
        var testId = await PlanTestAsync(father, subjectId, Day0.AddDays(5));

        await EvaluateAsync(Day0.AddDays(5));

        Assert.Null(PredictedOf(testId));
    }

    // AK 4, second half: a level that comes into being later on the test day still fills the snapshot; one that comes
    // into being only the day after does not. Red if the snapshot ran only when the evaluation changed a level row (the
    // test planned after the day's evaluation), or on any day after the test day (the last case).
    [Fact]
    public async Task Spaeter_am_Klausurtag_entstandene_Stufe_fuellt_noch_am_Folgetag_nicht()
    {
        var father = await TestApi.AdultAsync(factory);
        var (sameDay, exSame) = await VocabSubjectAsync(father, "PrognoseSpaet", 10);
        var (nextDay, exNext) = await VocabSubjectAsync(father, "PrognoseMorgen", 10);
        var sameTest = await PlanTestAsync(father, sameDay, Day0.AddDays(6));
        var nextTest = await PlanTestAsync(father, nextDay, Day0.AddDays(6));

        await EvaluateAsync(Day0.AddDays(6)); // first access of the test day: neither subject has a measure yet
        Assert.Null(PredictedOf(sameTest));

        SeedPlan(sameDay, active: true, exSame);
        Master(exSame, 7);
        await EvaluateAsync(Day0.AddDays(6));
        Assert.Equal(3, PredictedOf(sameTest));

        // A test the father plans after the day's evaluation already ran: no level row changes on the next access, the
        // snapshot is still due.
        var lateTest = await PlanTestAsync(father, sameDay, Day0.AddDays(6));
        await EvaluateAsync(Day0.AddDays(6));
        Assert.Equal(3, PredictedOf(lateTest));

        SeedPlan(nextDay, active: true, exNext);
        Master(exNext, 7);
        await EvaluateAsync(Day0.AddDays(7));
        Assert.Null(PredictedOf(nextTest));
    }

    // AK 5: moving the test date after the snapshot neither clears nor renews it, even when the run hits the new date.
    // Red if the update endpoint cleared the snapshot on a date change (null right after the PATCH, 1 after the run).
    [Fact]
    public async Task Verschobener_Termin_laesst_den_Schnappschuss_stehen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, exerciseId) = await SubjectOnLevel3Async(father, "PrognoseVerschoben");
        var testId = await PlanTestAsync(father, subjectId, Day0.AddDays(8));
        await EvaluateAsync(Day0.AddDays(8));
        Assert.Equal(3, PredictedOf(testId));

        var patch = await father.PatchAsJsonAsync($"/api/v1/supervisor/class-tests/{testId}",
            new { scheduledDate = Day0.AddDays(9).ToString("yyyy-MM-dd") });
        patch.EnsureSuccessStatusCode();
        Assert.Equal(3, PredictedOf(testId));

        Master(exerciseId, 10); // a rise applies at once: the level on the new date is 1
        await EvaluateAsync(Day0.AddDays(9));
        Assert.Equal(3, PredictedOf(testId));
    }

    // AK 6: the snapshot books nothing to the ledger. Red if the run added a ChildPointsEntry for the child.
    [Fact]
    public async Task Schnappschuss_bucht_nichts()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, _) = await SubjectOnLevel3Async(father, "PrognoseBuchung");
        var testId = await PlanTestAsync(father, subjectId, Day0.AddDays(10));
        await EvaluateAsync(Day0.AddDays(9)); // the level row exists before, so only the snapshot runs on the test day
        int Entries()
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ChildPointsEntries.Count(e => e.ChildId == 1);
        }
        var before = Entries();

        await EvaluateAsync(Day0.AddDays(10));

        Assert.Equal(3, PredictedOf(testId));
        Assert.Equal(before, Entries());
    }

    // B-746 AK 2: the supervisor reads the snapshot on the class test - null (not 0) while none is written, the number
    // afterwards, in the single read and in the list. Red if the mapper left the field out, filled a missing snapshot
    // with a stand-in, or passed something other than the stored column.
    [Fact]
    public async Task Vater_liest_die_Prognose_an_der_Arbeit()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, _) = await SubjectOnLevel3Async(father, "PrognoseLesen");
        var testId = await PlanTestAsync(father, subjectId, Day0.AddDays(12));
        async Task<JsonElement> ReadAsync() =>
            (await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/class-tests/{testId}")).GetProperty("klassenarbeit");

        Assert.Equal(JsonValueKind.Null, (await ReadAsync()).GetProperty("predictedLevel").ValueKind);

        await EvaluateAsync(Day0.AddDays(12));

        Assert.Equal(3, (await ReadAsync()).GetProperty("predictedLevel").GetInt32());
        var list = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/class-tests?childId=1");
        Assert.Equal(3, list.EnumerateArray().Single(k => k.GetProperty("id").GetInt32() == testId).GetProperty("predictedLevel").GetInt32());
    }

    // Review finding: a prediction belongs to its subject. Correcting the subject after the snapshot drops it; on the test
    // day the next access takes it in the new subject. Red if the old subject's level stayed (it is 3, the new one is 1)
    // or if a re-save with the same subject dropped it.
    [Fact]
    public async Task Fachwechsel_verwirft_die_Prognose_des_alten_Fachs()
    {
        var father = await TestApi.AdultAsync(factory);
        var (oldSubject, _) = await SubjectOnLevel3Async(father, "PrognoseAltesFach");
        var (newSubject, exNew) = await VocabSubjectAsync(father, "PrognoseNeuesFach", 10);
        SeedPlan(newSubject, active: true, exNew);
        Master(exNew, 10);
        var testId = await PlanTestAsync(father, oldSubject, Day0.AddDays(14));
        await EvaluateAsync(Day0.AddDays(14));
        Assert.Equal(3, PredictedOf(testId));

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/class-tests/{testId}", new { subjectId = oldSubject })).EnsureSuccessStatusCode();
        Assert.Equal(3, PredictedOf(testId));

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/class-tests/{testId}", new { subjectId = newSubject })).EnsureSuccessStatusCode();
        Assert.Null(PredictedOf(testId));

        await EvaluateAsync(Day0.AddDays(14));
        Assert.Equal(1, PredictedOf(testId));
    }
}
