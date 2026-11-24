using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-954: the supervisor's "coins today" is what the child earned that day in its ledger - the number the child reads
/// as its week - not the goal bonus of its plans. Every case books into a long-past day of a fresh child, so the cases
/// cannot see each other's entries or the seed's.
/// </summary>
public class MuenzenHeuteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static DateTime Utc(int y, int mo, int d, int h, int mi = 0) => new(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    private async Task<(HttpClient Father, int ChildId)> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Muenzen"), pin = "9541" }));
        return (father, childId);
    }

    private static async Task<JsonElement> RowAsync(HttpClient father, int childId, string date)
    {
        var dashboard = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/daily-overview?date={date}");
        return dashboard.GetProperty("children").EnumerateArray().First(c => c.GetProperty("childId").GetInt32() == childId);
    }

    private static async Task<int> CoinsTodayAsync(HttpClient father, int childId, string date) =>
        (await RowAsync(father, childId, date)).GetProperty("coinsToday").GetInt32();

    /// <summary>
    /// Review finding: coins alone are not practice. A chest opened in the morning books earned coins without an answer
    /// or a goal - the child shows its coins but stays "open", not "at it". Red when <c>practiced</c> reads the coins.
    /// </summary>
    [Fact]
    public async Task Muenzen_Aus_Der_Truhe_Sind_Kein_Ueben()
    {
        var (father, childId) = await SetupAsync();
        TestApi.Book(factory, childId, 9, PointKind.DailyBoxCoins, Utc(2024, 5, 15, 7));

        var row = await RowAsync(father, childId, "2024-05-15");

        Assert.Equal(9, row.GetProperty("coinsToday").GetInt32());
        JsonAssert.False(row, "practiced");
    }

    private static async Task<List<int>> PeriodCoinsAsync(HttpClient father, int childId, string from, string to, string granularity) =>
    [
        .. (await father.GetFromJsonAsync<JsonElement>(
                $"/api/v1/supervisor/children/overview?from={from}&to={to}&granularity={granularity}&childId={childId}"))
            .GetProperty("children")[0].GetProperty("periods").EnumerateArray().Select(p => p.GetProperty("coins").GetInt32()),
    ];

    /// <summary>
    /// AK 1 and AK 2 on Wednesday 2024-03-13: coins from answers count without any plan or met goal, the goal bonus counts
    /// too; gems, a fine, a purchase, a gift and a shop refund do not. The amounts are powers of two, so the sum names
    /// the kind that slipped in; a negative entry of an earned kind (a correction) is no earning either. AK 3 at the UTC
    /// calendar: the days before and after stay out, and midnight itself opens the new day.
    /// </summary>
    [Fact]
    public async Task Muenzen_Heute_Sind_Die_Verdienten_Muenzen_Des_Tages()
    {
        var (father, childId) = await SetupAsync();
        TestApi.Book(factory, childId, 1, PointKind.Base, Utc(2024, 3, 13, 9));
        TestApi.Book(factory, childId, 2, PointKind.Goal, Utc(2024, 3, 13, 10));
        TestApi.Book(factory, childId, 4, PointKind.Combo, Utc(2024, 3, 13, 10));
        TestApi.Book(factory, childId, -8, PointKind.GoalPenalty, Utc(2024, 3, 13, 11));
        TestApi.Book(factory, childId, -16, PointKind.ShopCoins, Utc(2024, 3, 13, 12));
        TestApi.Book(factory, childId, 32, PointKind.Manual, Utc(2024, 3, 13, 13));
        TestApi.Book(factory, childId, 64, PointKind.ShopCoins, Utc(2024, 3, 13, 14));
        TestApi.Book(factory, childId, 128, PointKind.Base, Utc(2024, 3, 12, 23, 59));
        TestApi.Book(factory, childId, 256, PointKind.Base, Utc(2024, 3, 14, 0));
        TestApi.Book(factory, childId, -512, PointKind.Base, Utc(2024, 3, 13, 15));

        Assert.Equal(3, await CoinsTodayAsync(father, childId, "2024-03-13"));
        Assert.Equal(128, await CoinsTodayAsync(father, childId, "2024-03-12"));
        Assert.Equal(256, await CoinsTodayAsync(father, childId, "2024-03-14"));
        Assert.Equal(0, await CoinsTodayAsync(father, childId, "2024-03-11"));
    }

    /// <summary>
    /// AK 4: the timeline carries the same daily figures and sums them per period - for a child without any plan, so the
    /// coins cannot come from a plan's goal bonus. Monday 2024-03-11 to Monday 2024-03-18: the week sums three days, the
    /// following Monday starts its own period.
    /// </summary>
    [Fact]
    public async Task Zeitleiste_Summiert_Die_Verdienten_Muenzen_Je_Periode()
    {
        var (father, childId) = await SetupAsync();
        TestApi.Book(factory, childId, 1, PointKind.Base, Utc(2024, 3, 11, 9));
        TestApi.Book(factory, childId, 2, PointKind.Base, Utc(2024, 3, 13, 9));
        TestApi.Book(factory, childId, 4, PointKind.Goal, Utc(2024, 3, 13, 10));
        TestApi.Book(factory, childId, -8, PointKind.GoalPenalty, Utc(2024, 3, 13, 11));
        TestApi.Book(factory, childId, 16, PointKind.Base, Utc(2024, 3, 17, 22));
        TestApi.Book(factory, childId, 32, PointKind.Base, Utc(2024, 3, 18, 9));
        TestApi.Book(factory, childId, 64, PointKind.Base, Utc(2024, 3, 19, 9));

        Assert.Equal([1, 0, 6, 0, 0, 0, 16, 32], await PeriodCoinsAsync(father, childId, "2024-03-11", "2024-03-18", "Day"));
        Assert.Equal([23, 32], await PeriodCoinsAsync(father, childId, "2024-03-11", "2024-03-18", "Week"));
    }
}

/// <summary>
/// B-954 AK 3 in the family's zone: the day of an entry is the learning day (German time), not the UTC date. The suite's
/// default factory counts in UTC and cannot tell the two apart.
/// </summary>
public class MuenzenHeuteBerlinTests(BerlinWebAppFactory factory) : IClassFixture<BerlinWebAppFactory>
{
    private static DateTime Utc(int y, int mo, int d, int h, int mi = 0) => new(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    /// <summary>
    /// Winter, UTC+1: 23:30 UTC on the 9th is 00:30 on the 10th in Berlin and belongs to the 10th; 23:30 UTC on the 10th
    /// belongs to the 11th. Red with UTC bucketing (the 10th would read 1 + 4 instead of 2 + 1) - in the daily overview
    /// and in the timeline alike.
    /// </summary>
    [Fact]
    public async Task Der_Tag_Einer_Buchung_Ist_Der_Lerntag_In_Deutscher_Zeit()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Muenzen-Berlin"), pin = "9542" }));
        TestApi.Book(factory, childId, 2, PointKind.Base, Utc(2024, 1, 9, 23, 30));
        TestApi.Book(factory, childId, 1, PointKind.Base, Utc(2024, 1, 10, 12));
        TestApi.Book(factory, childId, 4, PointKind.Base, Utc(2024, 1, 10, 23, 30));

        var dashboard = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/children/daily-overview?date=2024-01-10");
        var row = dashboard.GetProperty("children").EnumerateArray().First(c => c.GetProperty("childId").GetInt32() == childId);
        Assert.Equal(3, row.GetProperty("coinsToday").GetInt32());

        var overview = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/children/overview?from=2024-01-09&to=2024-01-11&granularity=Day&childId={childId}");
        int[] coins = [.. overview.GetProperty("children")[0].GetProperty("periods").EnumerateArray().Select(p => p.GetProperty("coins").GetInt32())];
        Assert.Equal([0, 3, 4], coins);
    }
}
