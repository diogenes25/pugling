using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-770: the daily overview counts, per child, the answers given that day - practice cards plus test answers,
/// across all of the child's plans, never derived from points.
/// </summary>
public class ChildrenDashboardAnswersTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    private void AddPractice(int planId, DateOnly day, int answers)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var session = new PracticeSession { StudyPlanId = planId, Day = day };
        db.PracticeSessions.Add(session);
        db.SaveChanges();
        for (var i = 0; i < answers; i++)
            db.ReviewEvents.Add(new ReviewEvent { PracticeSessionId = session.Id, WasCorrect = i % 2 == 0 });
        db.SaveChanges();
    }

    /// <summary>
    /// A test attempt as the start creates it (one row per question) with the first <paramref name="answers"/> rows
    /// answered: the first one right without a typed answer (self-assessed "known"), the others typed and wrong, the last
    /// one a wrong mention (ItemIndex null) - the child typed it, so it counts. <paramref name="open"/> rows stay
    /// untouched as after an abandoned test.
    /// </summary>
    private void AddTest(int planId, DateOnly day, int answers, int open = 0, bool bySupervisor = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var attempt = new TestAttempt { StudyPlanId = planId, Day = day, BySupervisor = bySupervisor };
        for (var i = 0; i < answers; i++)
            attempt.Results.Add(i == 0
                ? new TestItemResult { ItemIndex = i, WasCorrect = true }
                : new TestItemResult { ItemIndex = i == answers - 1 ? null : i, GivenAnswer = "falsch", WasCorrect = false });
        for (var i = 0; i < open; i++)
            attempt.Results.Add(new TestItemResult { ItemIndex = answers + i });
        db.TestAttempts.Add(attempt);
        db.SaveChanges();
    }

    [Fact]
    public async Task Antwortzahl_ZaehltUebenUndTestDesTages_NurDiesesKindes()
    {
        var father = await TestApi.AdultAsync(factory);
        var childA = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Antwort-A", pin = "7701" }));
        var childB = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Antwort-B", pin = "7702" }));
        var planA = await TestApi.CreateEmptyPlanAsync(father, childA);
        var planB = await TestApi.CreateEmptyPlanAsync(father, childB);

        // Every source has its own count, so dropping any one filter lands on a different total than 2 + 3 = 5.
        AddPractice(planA, Day, 2);                 // counts (AK 3, practice)
        AddTest(planA, Day, 3, open: 17);           // counts 3 (AK 3, test, incl. a wrong mention); 17 never answered
        AddTest(planA, Day, 19, bySupervisor: true); // the father's preview is not the child's work
        AddPractice(planA, Day.AddDays(-1), 7);     // yesterday (AK 2)
        AddTest(planA, Day.AddDays(-1), 11);        // yesterday (AK 2)
        AddPractice(planB, Day, 13);                // another child (AK 4)

        var dashboard = await (await father.GetAsync($"/api/v1/supervisor/children/daily-overview?date={Day:yyyy-MM-dd}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rows = dashboard.GetProperty("children").EnumerateArray().ToList();
        var rowA = rows.First(r => r.GetProperty("childId").GetInt32() == childA);
        var rowB = rows.First(r => r.GetProperty("childId").GetInt32() == childB);

        Assert.Equal(5, rowA.GetProperty("answersToday").GetInt32());
        Assert.Equal(13, rowB.GetProperty("answersToday").GetInt32());
        // AK 1: answers are not coins - no position, no coins, yet answers.
        Assert.Equal(0, rowA.GetProperty("coinsToday").GetInt32());
    }

    /// <summary>
    /// B-777: a child that answered today but met no goal and earned no points has practised (AK 1) - the father's start
    /// page showed "open" under "5 answers". A child without any activity has not (AK 2). Red for AK 1 when the answers do
    /// not feed <c>practiced</c>, red for AK 2 when it is always true.
    /// </summary>
    [Fact]
    public async Task Geuebt_Heisst_Geantwortet_Auch_Ohne_Ziel_Und_Punkte()
    {
        var father = await TestApi.AdultAsync(factory);
        var busy = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Geuebt-A", pin = "7771" }));
        var idle = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Geuebt-B", pin = "7772" }));
        AddPractice(await TestApi.CreateEmptyPlanAsync(father, busy), Day, 5);
        await TestApi.CreateEmptyPlanAsync(father, idle);

        var dashboard = await (await father.GetAsync($"/api/v1/supervisor/children/daily-overview?date={Day:yyyy-MM-dd}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rows = dashboard.GetProperty("children").EnumerateArray().ToList();
        var rowBusy = rows.First(r => r.GetProperty("childId").GetInt32() == busy);
        var rowIdle = rows.First(r => r.GetProperty("childId").GetInt32() == idle);

        Assert.Equal(0, rowBusy.GetProperty("goalsMet").GetInt32());
        Assert.Equal(0, rowBusy.GetProperty("coinsToday").GetInt32());
        Assert.True(rowBusy.GetProperty("practiced").GetBoolean());
        Assert.False(rowIdle.GetProperty("practiced").GetBoolean());
    }
}
