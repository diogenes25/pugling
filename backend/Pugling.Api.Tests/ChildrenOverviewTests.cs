using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-39: the father's children over time - <c>GET supervisor/children/overview</c> sums the daily rollup per day, week
/// (Monday to Sunday) or calendar month, pages over the children and never shows a child he does not supervise.
/// </summary>
public class ChildrenOverviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Url = "/api/v1/supervisor/children/overview";

    // A fresh supervisor with its own children, so counts are not shared with other tests of the fixture.
    private async Task<(HttpClient Father, int AdultId)> FreshAdultAsync()
    {
        var pin = "8" + Random.Shared.Next(100, 999);
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = TestApi.UniqueName("Zeitachse"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (await TestApi.AdultAsync(factory, id, pin), id);
    }

    private static async Task<int> ChildAsync(HttpClient father, string name) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name, pin = "5" + Random.Shared.Next(100, 999) }));

    // A daily vocabulary duty for the child that runs through March and April 2026 - a goal on every day of the range.
    private async Task SeedMarchAprilDutyAsync(HttpClient father, int childId)
    {
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        await db.StudyPlans.Where(p => p.Id == planId).ExecuteUpdateAsync(u => u
            .SetProperty(p => p.StartDate, new DateOnly(2026, 3, 1)).SetProperty(p => p.EndDate, new DateOnly(2026, 4, 30)));
        await db.PlanPositions.Where(p => p.Id == positionId).ExecuteUpdateAsync(u => u
            .SetProperty(p => p.CreatedAt, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    private static List<(string Start, string End, int Goals)> Periods(JsonElement overview) =>
    [
        .. overview.GetProperty("children")[0].GetProperty("periods").EnumerateArray()
            .Select(p => (p.GetProperty("start").GetString()!, p.GetProperty("end").GetString()!, p.GetProperty("goalsTotal").GetInt32())),
    ];

    /// <summary>
    /// AK 1 and 2: Sunday 29 March, Monday 30 March, Tuesday 31 March, Wednesday 1 April 2026. By day: four periods. By
    /// week: Sunday closes the week of 23 March, Monday opens the next. By month: March and April, although all four days
    /// after Sunday lie in one week. Red when Sunday falls into the following week, or when a month is grouped like a week
    /// (or as 4 × 7 days).
    /// </summary>
    [Fact]
    public async Task Gruppiert_Nach_Tag_Woche_Und_Kalendermonat()
    {
        var (father, _) = await FreshAdultAsync();
        var childId = await ChildAsync(father, "Anna");
        await SeedMarchAprilDutyAsync(father, childId);

        async Task<List<(string, string, int)>> GroupedAsync(string granularity) =>
            Periods(await father.GetFromJsonAsync<JsonElement>($"{Url}?from=2026-03-29&to=2026-04-01&granularity={granularity}"));

        Assert.Equal(4, (await GroupedAsync("Day")).Count);
        Assert.Equal([("2026-03-29", "2026-03-29", 1), ("2026-03-30", "2026-04-01", 3)], await GroupedAsync("Week"));
        Assert.Equal([("2026-03-29", "2026-03-31", 3), ("2026-04-01", "2026-04-01", 1)], await GroupedAsync("Month"));
    }

    /// <summary>
    /// AK 1: the page runs over the children (X-Total-Count = number of children), and a child of another supervisor never
    /// appears, not even when asked for by id. Red when the total counts periods, or the supervisor filter is missing.
    /// </summary>
    [Fact]
    public async Task Blaettert_Ueber_Die_Kinder_Und_Zeigt_Kein_Fremdes()
    {
        var (father, _) = await FreshAdultAsync();
        await ChildAsync(father, "Anna");
        await ChildAsync(father, "Bert");
        var (stranger, _) = await FreshAdultAsync();
        var foreignChild = await ChildAsync(stranger, "Fremd");

        var page = await father.GetAsync($"{Url}?from=2026-03-01&to=2026-03-07&take=1");
        Assert.Equal("2", page.Headers.GetValues("X-Total-Count").Single());
        var body = await page.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Anna", body.GetProperty("children")[0].GetProperty("name").GetString());
        Assert.Equal(1, body.GetProperty("children").GetArrayLength());

        var foreign = await father.GetFromJsonAsync<JsonElement>($"{Url}?from=2026-03-01&to=2026-03-07&childId={foreignChild}");
        Assert.Equal(0, foreign.GetProperty("children").GetArrayLength());

        // A child without any plan had no goal on any day: its periods are not "done". Red without the at-least-one-goal rule.
        Assert.All(body.GetProperty("children")[0].GetProperty("periods").EnumerateArray(), p => JsonAssert.False(p, "dutyDone"));
    }

    /// <summary>
    /// A range into the future ends today - a day still to come would read as a missed duty; an unknown granularity is
    /// refused. Red when the clamp or the enum check is missing.
    /// </summary>
    [Fact]
    public async Task Zukunft_Endet_Heute_Und_Raster_Wird_Geprueft()
    {
        var (father, _) = await FreshAdultAsync();
        var today = DateOnly.FromDateTime(factory.Services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);
        var res = await father.GetFromJsonAsync<JsonElement>($"{Url}?from={today.AddDays(-3):yyyy-MM-dd}&to={today.AddDays(10):yyyy-MM-dd}");
        Assert.Equal(today.ToString("yyyy-MM-dd"), res.GetProperty("to").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url}?granularity=7")).StatusCode);
    }

    /// <summary>AK 1 (decision 9): more than 92 days, or from after to, is refused. Red when the bounds are missing.</summary>
    [Fact]
    public async Task Zeitraum_Ist_Begrenzt()
    {
        var (father, _) = await FreshAdultAsync();
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"{Url}?from=2026-01-01&to=2026-04-02")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url}?from=2026-01-01&to=2026-04-03")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url}?from=2026-03-02&to=2026-03-01")).StatusCode);
    }

    /// <summary>
    /// AK 2: a period's duty is done when every day with a daily goal was met. Today, after a passed test, the day reads
    /// done; the open days of March do not. Red when <c>dutyDone</c> ignores the days, or reads an open day as done.
    /// </summary>
    [Fact]
    public async Task Pflicht_Der_Periode_Folgt_Den_Tagen()
    {
        var (father, _) = await FreshAdultAsync();
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Carla", pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } },
        })).EnsureSuccessStatusCode();

        var today = await father.GetFromJsonAsync<JsonElement>($"{Url}?granularity=Day&childId={childId}");
        var last = today.GetProperty("children")[0].GetProperty("periods").EnumerateArray().Last();
        JsonAssert.True(last, "dutyDone");
        Assert.True(last.GetProperty("points").GetInt32() > 0);

        var (other, _) = await FreshAdultAsync();
        var openChild = await ChildAsync(other, "Dora");
        await SeedMarchAprilDutyAsync(other, openChild);
        var open = await other.GetFromJsonAsync<JsonElement>($"{Url}?from=2026-03-29&to=2026-04-01&granularity=Week");
        Assert.All(open.GetProperty("children")[0].GetProperty("periods").EnumerateArray(), p => JsonAssert.False(p, "dutyDone"));
    }
}
