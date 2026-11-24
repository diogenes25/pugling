using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The setup the mixed review tests share (B-764, B-765): a child with Leitner vocabulary positions in plans of their own
/// subjects, and the calls of the round.
/// </summary>
public abstract class MischrundeTestBase(PuglingWebAppFactory factory)
{
    /// <summary>The in-process server the tests run against.</summary>
    protected PuglingWebAppFactory Factory { get; } = factory;

    protected static readonly (string Front, string Back)[] Words =
        [("dog", "Hund"), ("cat", "Katze"), ("house", "Haus"), ("tree", "Baum")];

    protected sealed record Kid(HttpClient Father, HttpClient Child, int ChildId);

    protected async Task<Kid> KidAsync()
    {
        var father = await TestApi.AdultAsync(Factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Misch-Kind"), pin }));
        return new Kid(father, await TestApi.ChildAsync(Factory, childId, pin), childId);
    }

    // A vocabulary exercise of four words in a unit of its own subject, assigned to the child as a Leitner position of a
    // plan of that subject (one active plan per subject, B-539). Every word introduced and imprinted yesterday, box 3,
    // due in the future - a test makes the cards it needs due.
    // With an exercise given, a further position on that exercise, in a plan of another subject.
    protected async Task<(int PlanId, int PositionId)> PositionAsync(Kid k, GoalCadence cadence = GoalCadence.None,
        int? exerciseId = null)
    {
        var subjectId = await TestApi.IdAsync(await k.Father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Misch-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(k.Father, subjectId, sourceLanguage: "en");
        var vocabId = exerciseId ?? await TestApi.IdAsync(await k.Father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = TestApi.UniqueName("Woerter"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = Words.Select(w => new { front = w.Front, back = w.Back }),
                },
            }));
        var ids = TestApi.SeedLeitnerPosition(Factory, vocabId, (int)TestStage.FreeText, childId: k.ChildId, cadence: cadence);
        Db(db =>
        {
            db.StudyPlans.Single(p => p.Id == ids.planId).SubjectId = subjectId;
            foreach (var row in db.PositionItemProgress.Where(p => p.PlanPositionId == ids.positionId)) row.DueOn = Today.AddDays(3);
        });
        return ids;
    }

    protected DateOnly Today
    {
        get
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LearningDay>().Today;
        }
    }

    protected void Db(Action<PuglingDbContext> change)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db);
        db.SaveChanges();
    }

    protected T Read<T>(Func<PuglingDbContext, T> query)
    {
        using var scope = Factory.Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    protected void SetDue(int positionId, int itemIndex, DateOnly? dueOn) => Db(db =>
        db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex).DueOn = dueOn);

    // A passed test of today written straight into the database: meets the position's goal of the running period.
    protected void SeedPassedTest(int planId, int positionId) => Db(db =>
    {
        var at = Today.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        db.TestAttempts.Add(new TestAttempt
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = Today,
            StageValue = (int)TestStage.FreeText,
            Graded = true,
            StartedAt = at,
            CompletedAt = at,
            TotalItems = 4,
            CorrectItems = 4,
            ScorePercent = 100,
            Passed = true,
        });
    });

    protected static string Base(Kid k) => $"/api/v1/student/children/{k.ChildId}/mixed-review";

    protected static async Task<JsonElement> StartAsync(Kid k)
    {
        var res = await k.Child.PostAsync($"{Base(k)}/runs", null);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    protected static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    protected static List<(int PositionId, int ItemIndex)> Cards(JsonElement run) =>
    [
        .. run.GetProperty("cards").EnumerateArray()
            .Select(c => (c.GetProperty("positionId").GetInt32(), c.GetProperty("card").GetProperty("itemIndex").GetInt32())),
    ];
}
