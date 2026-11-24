using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-742: the two read endpoints of the grade level - <c>student/me/subject-levels</c> for the child and
/// <c>supervisor/children/{childId}/subject-levels</c> for its supervisor. Both read one child only, both evaluate the
/// day lazily, and <c>wordsToNextLevel</c> counts against the bound of the next better grade, live.
/// Every test works on a child of its own, so the shared host of the class fixture cannot mix their subjects.
/// </summary>
public class SubjectLevelEndpointTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Me = "/api/v1/student/me/subject-levels";

    private static string Supervised(int childId) => $"/api/v1/supervisor/children/{childId}/subject-levels";

    // A fresh child of the seeded adult 1 with its own login.
    private static async Task<(int Id, HttpClient Client)> NewChildAsync(PuglingWebAppFactory f, HttpClient father)
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Stufenkind"), pin = "4444" }));
        return (id, await TestApi.ChildAsync(f, id, "4444"));
    }

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

    private void SeedPlan(int childId, params int[] exerciseIds)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = childId, Title = "Stufen-Plan", StartDate = today, EndDate = today.AddDays(30), Active = true };
        var order = 0;
        foreach (var id in exerciseIds)
            plan.Positions.Add(new PlanPosition { ExerciseId = id, Order = order++, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
    }

    // Puts the first `count` words of the exercise into `box` for the child (5 = mastered).
    private void SetBoxes(int childId, int exerciseId, int count, int box)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var items = db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).Take(count).ToList();
        var existing = db.ItemProgress.Where(p => p.ChildId == childId && p.ExerciseId == exerciseId).ToDictionary(p => p.ItemId);
        foreach (var item in items)
        {
            if (!existing.TryGetValue(item.Id, out var p))
                db.ItemProgress.Add(p = new ItemProgress { ChildId = childId, ItemId = item.Id, ExerciseId = exerciseId, VocabularyId = item.VocabularyId });
            p.Box = box;
            p.MasteryPercent = box * 20;
        }
        db.SaveChanges();
    }

    // Moves the stored evaluation day of the child's levels back, as if the evaluation had run yesterday.
    private void EvaluatedYesterday(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        foreach (var row in db.ChildSubjectLevels.Where(l => l.ChildId == childId))
            row.EvaluatedOn = row.EvaluatedOn.AddDays(-1);
        db.SaveChanges();
    }

    private static JsonElement Row(List<JsonElement> rows, int subjectId) =>
        rows.Single(r => r.GetProperty("subjectId").GetInt32() == subjectId);

    // AK 1: the child reads its own levels; an arithmetic subject has no measure and is missing. Red if a subject
    // without vocabulary appeared with a level.
    [Fact]
    public async Task Kind_liest_eigene_Stufen_Fach_ohne_Mass_fehlt()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await NewChildAsync(factory, father);
        var (vocabSubject, vocabExercise) = await VocabSubjectAsync(father, "LeseVokabel", 10);
        var (mathSubject, _, mathExercise) = await TestApi.CreateArithmeticExerciseAsync(father);
        SeedPlan(childId, vocabExercise, mathExercise);
        SetBoxes(childId, vocabExercise, 7, ItemProgress.MaxBox); // 70 % -> 3

        var rows = await child.GetFromJsonAsync<List<JsonElement>>(Me);

        var row = Row(rows!, vocabSubject);
        Assert.Equal(3, row.GetProperty("level").GetInt32());
        Assert.Equal(3, row.GetProperty("bestLevel").GetInt32());
        Assert.StartsWith("LeseVokabel", row.GetProperty("subjectName").GetString());
        Assert.DoesNotContain(rows!, r => r.GetProperty("subjectId").GetInt32() == mathSubject);
    }

    // AK 2 and 4: a foreign supervisor gets 404 for the child, and a child reading /me never sees a sibling's subject.
    // Red if the ownership filter were missing (200 for the foreign supervisor) or /me read every child of the father.
    [Fact]
    public async Task Fremde_lesen_nicht_mit_und_Geschwister_bleiben_getrennt()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await NewChildAsync(factory, father);
        var (siblingId, _) = await NewChildAsync(factory, father);
        var (own, ownExercise) = await VocabSubjectAsync(father, "LeseEigen", 10);
        var (sibling, siblingExercise) = await VocabSubjectAsync(father, "LeseGeschwister", 10);
        SeedPlan(childId, ownExercise);
        SeedPlan(siblingId, siblingExercise);

        var rows = await child.GetFromJsonAsync<List<JsonElement>>(Me);
        Assert.Contains(rows!, r => r.GetProperty("subjectId").GetInt32() == own);
        Assert.DoesNotContain(rows!, r => r.GetProperty("subjectId").GetInt32() == sibling);

        var siblingRows = await father.GetFromJsonAsync<List<JsonElement>>(Supervised(siblingId));
        Assert.Contains(siblingRows!, r => r.GetProperty("subjectId").GetInt32() == sibling);
        Assert.DoesNotContain(siblingRows!, r => r.GetProperty("subjectId").GetInt32() == own);

        var strangerId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin = "5555" }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, "5555");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Supervised(childId))).StatusCode);
    }

    // AK 3: wordsToNextLevel counts against the bound of the next better grade and drops with every further mastered
    // word on the same day; on level 1 it is null. Red if the number came from the answer count or ignored the bound.
    [Fact]
    public async Task Woerter_bis_zur_naechsten_Stufe_sinken_mit_jedem_Wort()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await NewChildAsync(factory, father);
        var (subject, exercise) = await VocabSubjectAsync(father, "LeseZiel", 20);
        SeedPlan(childId, exercise);
        SetBoxes(childId, exercise, 10, ItemProgress.MaxBox); // 50 % -> 4; the 3 starts at 67 % = 14 of 20 words

        var first = Row((await child.GetFromJsonAsync<List<JsonElement>>(Me))!, subject);
        Assert.Equal(4, first.GetProperty("level").GetInt32());
        Assert.Equal(4, first.GetProperty("wordsToNextLevel").GetInt32());

        SetBoxes(childId, exercise, 11, ItemProgress.MaxBox);
        var second = Row((await father.GetFromJsonAsync<List<JsonElement>>(Supervised(childId)))!, subject);
        Assert.Equal(4, second.GetProperty("level").GetInt32()); // the level waits for the next day
        Assert.Equal(3, second.GetProperty("wordsToNextLevel").GetInt32());

        SetBoxes(childId, exercise, 20, ItemProgress.MaxBox);
        EvaluatedYesterday(childId);
        var best = Row((await child.GetFromJsonAsync<List<JsonElement>>(Me))!, subject);
        Assert.Equal(1, best.GetProperty("level").GetInt32());
        Assert.Equal(JsonValueKind.Null, best.GetProperty("wordsToNextLevel").ValueKind);
    }

    // AK 5: reading twice on the same day lowers at most once. Held twice over: the day check (one evaluation per day)
    // and the seven-day interval after a lowering - red only if both gave way; a single mutant of either stays green.
    [Fact]
    public async Task Zwei_Lesezugriffe_am_selben_Tag_senken_nur_einmal()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await NewChildAsync(factory, father);
        var (subject, exercise) = await VocabSubjectAsync(father, "LeseSturz", 10);
        SeedPlan(childId, exercise);
        SetBoxes(childId, exercise, 10, ItemProgress.MaxBox);
        Assert.Equal(1, Row((await child.GetFromJsonAsync<List<JsonElement>>(Me))!, subject).GetProperty("level").GetInt32());

        SetBoxes(childId, exercise, 10, box: 1);
        EvaluatedYesterday(childId);
        await child.GetFromJsonAsync<List<JsonElement>>(Me);
        var rows = await father.GetFromJsonAsync<List<JsonElement>>(Supervised(childId));

        Assert.Equal(2, Row(rows!, subject).GetProperty("level").GetInt32());
        using var scope = factory.Services.CreateScope();
        var stored = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ChildSubjectLevels.AsNoTracking()
            .Single(l => l.ChildId == childId && l.SubjectId == subject);
        Assert.Equal(1, stored.BestLevel);
    }
}
