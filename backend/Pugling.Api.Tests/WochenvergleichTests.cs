using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-253: the child compares its earned coins with its own previous week - up to the same moment, plus the whole
/// previous week beside it. "Earned" means positive coin entries except gifts (<see cref="PointKind.Manual"/>); shop
/// purchases, fines and gems stay out. The week runs Monday to Sunday on the UTC calendar. Every case freezes the
/// clock in a different, long-past year so the cases cannot see each other's entries (or the seed's, booked now).
/// </summary>
public class WochenvergleichTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Url = "/api/v1/student/me/points/week";

    private void Book(int amount, PointKind kind, DateTime at)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ChildPointsEntries.Add(new ChildPointsEntry { ChildId = 1, Amount = amount, Kind = kind, Reason = "Wochenvergleich", CreatedAt = at });
        db.SaveChanges();
    }

    private static DateTime Utc(int y, int mo, int d, int h, int mi = 0) => new(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    /// <summary>
    /// AK 1 and AK 2. Wednesday 2025-01-15, 12:00 UTC. Red when shop, fine, gift or gems count into this week, or when
    /// the previous week is not cut at the same moment (Wednesday 13:00 must stay out of "so far").
    /// </summary>
    [Fact]
    public async Task Diese_Woche_Gegen_Die_Vorwoche_Bis_Jetzt()
    {
        factory.Clock.FreezeAt(Utc(2025, 1, 15, 12));
        Book(30, PointKind.Base, Utc(2025, 1, 13, 9));
        Book(20, PointKind.Goal, Utc(2025, 1, 14, 9));
        Book(-50, PointKind.ShopCoins, Utc(2025, 1, 14, 10));
        Book(100, PointKind.Manual, Utc(2025, 1, 14, 11));
        Book(-10, PointKind.GoalPenalty, Utc(2025, 1, 14, 12));
        Book(5, PointKind.Combo, Utc(2025, 1, 14, 13));
        Book(40, PointKind.Base, Utc(2025, 1, 6, 9));
        Book(10, PointKind.Base, Utc(2025, 1, 8, 11));
        Book(7, PointKind.Base, Utc(2025, 1, 8, 13));
        Book(20, PointKind.Goal, Utc(2025, 1, 11, 9));
        var child = await TestApi.ChildAsync(factory);

        var week = await child.GetFromJsonAsync<JsonElement>(Url);

        Assert.Equal(50, week.GetProperty("thisWeek").GetInt32());
        Assert.Equal(50, week.GetProperty("lastWeekSoFar").GetInt32());
        Assert.Equal(77, week.GetProperty("lastWeekTotal").GetInt32());
    }

    /// <summary>
    /// AK 3. Monday 2024-01-15, 00:30 UTC: the week starts on this Monday, and an entry on Sunday 23:59 belongs to the
    /// old week. Red when the week starts on Sunday (the entry would count as this week and the start would be the 14th).
    /// </summary>
    [Fact]
    public async Task Woche_Beginnt_Am_Montag()
    {
        factory.Clock.FreezeAt(Utc(2024, 1, 15, 0, 30));
        Book(5, PointKind.Base, Utc(2024, 1, 14, 23, 59));
        var child = await TestApi.ChildAsync(factory);

        var week = await child.GetFromJsonAsync<JsonElement>(Url);

        Assert.Equal("2024-01-15", week.GetProperty("weekStart").GetString());
        Assert.Equal(0, week.GetProperty("thisWeek").GetInt32());
        Assert.Equal(5, week.GetProperty("lastWeekTotal").GetInt32());
    }

    /// <summary>
    /// AK 4: the comparison is the child's own; a supervisor token gets 403 like <c>points</c>. Statement of intent:
    /// there are two walls (the role gate and the missing <c>cid</c> claim), so removing either one alone keeps this
    /// green - measured in the red probe; it turns red only when both go.
    /// </summary>
    [Fact]
    public async Task Supervisor_Bekommt_403()
    {
        var father = await TestApi.AdultAsync(factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await father.GetAsync(Url)).StatusCode);
    }
}
