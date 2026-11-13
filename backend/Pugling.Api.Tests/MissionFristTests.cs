using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-528: every mission names how many days its period has left (<c>daysLeft</c>), computed by the server on the UTC
/// calendar - 0 is the last day, a one-off mission has no deadline. A class of its own because it freezes the shared
/// clock of its factory on chosen weekdays, which must not leak into the other gamification tests.
/// </summary>
public class MissionFristTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // A Wednesday and the Sunday that closes the same ISO week (UTC).
    private static readonly DateTimeOffset Wednesday = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Sunday = new(2026, 11, 15, 23, 0, 0, TimeSpan.Zero);

    private async Task<JsonElement> MissionAsync(string period)
    {
        var father = await TestApi.AdultAsync(factory);
        var title = TestApi.UniqueName($"Frist-{period}");
        (await father.PostAsJsonAsync("/api/v1/supervisor/children/1/missions", new
        {
            title,
            metric = "CorrectReviews",
            target = 50,
            period,
            rewardPoints = 5,
        })).EnsureSuccessStatusCode();
        var child = await TestApi.ChildAsync(factory);
        var missions = await child.GetFromJsonAsync<JsonElement>("/api/v1/student/me/missions");
        return missions.EnumerateArray().Single(m => m.GetProperty("title").GetString() == title);
    }

    /// <summary>AK 1: a daily mission ends today. Red when the field is missing or the week is counted instead of the day.</summary>
    [Fact]
    public async Task Tagesmission_Endet_Heute()
    {
        factory.Clock.FreezeAt(Wednesday);
        Assert.Equal(0, (await MissionAsync("Daily")).GetProperty("daysLeft").GetInt32());
    }

    /// <summary>
    /// AK 2: on Wednesday a weekly mission has four more days after today, on Sunday none. Red when the week does not
    /// end on Sunday (UTC) - e.g. a Sunday-to-Saturday week would leave three on Wednesday and six on Sunday.
    /// </summary>
    [Fact]
    public async Task Wochenmission_Endet_Am_Sonntag()
    {
        factory.Clock.FreezeAt(Wednesday);
        Assert.Equal(4, (await MissionAsync("Weekly")).GetProperty("daysLeft").GetInt32());

        factory.Clock.FreezeAt(Sunday);
        Assert.Equal(0, (await MissionAsync("Weekly")).GetProperty("daysLeft").GetInt32());
    }

    /// <summary>AK 3: a one-off mission has no period and so no deadline. Red when <c>OneOff</c> gets a number.</summary>
    [Fact]
    public async Task Einmalige_Mission_Hat_Keine_Frist()
    {
        factory.Clock.FreezeAt(Wednesday);
        Assert.Equal(JsonValueKind.Null, (await MissionAsync("OneOff")).GetProperty("daysLeft").ValueKind);
    }
}
