using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// A host on the production zone (B-730). The suite's own factory counts on the UTC calendar; this one sets the
/// family's zone, so the tests below see the day the children see.
/// </summary>
public sealed class BerlinWebAppFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        builder.UseSetting("Scoring:TimeSlotsEnabled", "false");
        builder.UseSetting("LearningDay:TimeZone", "Europe/Berlin");
    }
}

/// <summary>
/// B-730: the learning day is the calendar day in German time and ends at local midnight - "by tonight" must not run until
/// 1 or 2 a.m. because the server counts in UTC. Winter (UTC+1) and summer (UTC+2) both, and the two days a year whose
/// length is not 24 hours.
/// </summary>
public class TagesbegriffTests(BerlinWebAppFactory factory) : IClassFixture<BerlinWebAppFactory>
{
    private static readonly (string, string)[] TwoWords = [("la ville", "die Stadt"), ("la rue", "die Straße")];

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    private sealed record Setup(HttpClient Child, int ChildId, int PlanId, int PositionId);

    // A fresh child per test: the coin assertions read its ledger, and the fixture's default child is shared.
    private async Task<Setup> SetupAsync(bool useLeitner = true)
    {
        var father = await TestApi.AdultAsync(factory);
        const string pin = "7301";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Tag-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, "fr", "de", TwoWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, useLeitner: useLeitner);
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    private static async Task<JsonElement> StartAsync(Setup s) =>
        await (await s.Child.PostAsJsonAsync(TestApi.PracticeBase(s.PlanId, s.PositionId), new { mode = "Lern" }))
            .Content.ReadFromJsonAsync<JsonElement>();

    private static DateOnly DayOf(JsonElement session) => DateOnly.Parse(session.GetProperty("day").GetString()!);

    // AK 3. Red when the session day is taken from the UTC date: at 23:30 local in winter it is still the same UTC day
    // (22:30Z), at 00:30 local in summer it is still the previous UTC day (22:30Z) - in both seasons one of the two
    // sessions lands on the wrong day.
    [Theory]
    [InlineData(1, 15, 1)]  // winter, UTC+1
    [InlineData(7, 15, 2)]  // summer, UTC+2
    public async Task Antwort_Um_2330_Zaehlt_Fuer_Den_Alten_Um_0030_Fuer_Den_Neuen_Tag(int month, int day, int offset)
    {
        factory.Clock.FreezeAt(Utc(2027, month, day, 10));
        var s = await SetupAsync();
        var today = new DateOnly(2027, month, day);

        factory.Clock.FreezeAt(Utc(2027, month, day, 23 - offset, 30));
        Assert.Equal(today, DayOf(await StartAsync(s)));

        factory.Clock.FreezeAt(Utc(2027, month, day, 23 - offset, 30).AddHours(1));
        Assert.Equal(today.AddDays(1), DayOf(await StartAsync(s)));
    }

    // AK 5, a stored instant: a coin earned at 00:10 local on a Monday belongs to the new week, and the week itself starts
    // on that Monday. Red when the week is cut at UTC midnight (the coin falls into last week) or when its Monday comes
    // from the UTC date of "now" (still Sunday).
    [Fact]
    public async Task Muenze_Nach_Mitternacht_Am_Montag_Gehoert_Zur_Neuen_Woche()
    {
        // Monday 2027-07-19, 00:10 local = Sunday 22:10Z.
        factory.Clock.FreezeAt(Utc(2027, 7, 18, 22, 10));
        var father = await TestApi.AdultAsync(factory);
        const string pin = "7302";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Wochen-Kind"), pin }));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.Base,
                Amount = 7,
                Reason = "B-730",
                CreatedAt = new DateTime(2027, 7, 18, 22, 10, 0, DateTimeKind.Utc),
            });
            db.SaveChanges();
        }
        factory.Clock.FreezeAt(Utc(2027, 7, 18, 22, 30));
        var child = await TestApi.ChildAsync(factory, childId, pin);

        var week = await child.GetFromJsonAsync<JsonElement>("/api/v1/student/me/points/week");

        Assert.Equal("2027-07-19", week.GetProperty("weekStart").GetString());
        Assert.Equal(7, week.GetProperty("thisWeek").GetInt32());
        Assert.Equal(0, week.GetProperty("lastWeekTotal").GetInt32());
    }

    // AK 5, `LastReviewedAt`: a word answered right at 00:10 local and again at 01:00 local - one learning day - pays once.
    // Red when the stamp is mapped to its UTC date (22:10Z is still yesterday there, so the second answer pays again).
    [Fact]
    public async Task Zweite_Richtige_Antwort_Nach_Mitternacht_Zahlt_Nicht_Noch_Einmal()
    {
        factory.Clock.FreezeAt(Utc(2027, 7, 15, 10));
        var s = await SetupAsync(useLeitner: false);

        factory.Clock.FreezeAt(Utc(2027, 7, 15, 22, 10));
        var first = await ReviewAsync(s, await IdAsync(StartAsync(s)), "la ville");
        JsonAssert.True(first, "wasCorrect");
        Assert.True(first.GetProperty("awarded").GetInt32() > 0, "the first right answer of the day pays");

        factory.Clock.FreezeAt(Utc(2027, 7, 15, 23));
        var second = await ReviewAsync(s, await IdAsync(StartAsync(s)), "la ville");
        JsonAssert.True(second, "wasCorrect");
        Assert.Equal(0, second.GetProperty("awarded").GetInt32());
    }

    // AK 5, `CreatedAt`: a position created at 00:30 local owes its first duty on that local day, not on the UTC day
    // before. Red when DutyBegins maps CreatedAt to its UTC date.
    [Fact]
    public void Position_Kurz_Nach_Mitternacht_Beginnt_Am_Neuen_Tag()
    {
        using var scope = factory.Services.CreateScope();
        var progress = scope.ServiceProvider.GetRequiredService<PositionProgressService>();
        var position = new PlanPosition { CreatedAt = new DateTime(2027, 7, 15, 22, 30, 0, DateTimeKind.Utc) };

        Assert.Equal(new DateOnly(2027, 7, 16), progress.DutyBegins(position));
    }

    // The shop's daily refill comes at local midnight, like the day (B-730): a stock bought empty at 21:00 local is full
    // again at 00:30 local. Red when the refill calendar ignores the zone (00:30 local is still the same UTC day).
    [Fact]
    public async Task Taeglicher_Shop_Bestand_Fuellt_Sich_Um_Mitternacht_Ortszeit()
    {
        factory.Clock.FreezeAt(Utc(2027, 7, 15, 19));
        var father = await TestApi.AdultAsync(factory);
        const string pin = "7303";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Shop-Kind"), pin }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 500, reason = "Test" }))
            .EnsureSuccessStatusCode();
        var number = TestApi.UniqueName("B730");
        var articleId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
            new { articleNumber = number, title = number, unitType = "Minute", actionType = "Sonstiges" }));
        var listingId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { title = "Spielzeit", coinPrice = 10, gemPrice = 0, unitsPerPurchase = 30, maxStock = 1, refillKind = "Daily" }));
        var child = await TestApi.ChildAsync(factory, childId, pin);
        (await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(child, listingId));

        factory.Clock.FreezeAt(Utc(2027, 7, 15, 22, 30)); // 00:30 local, still 15 July in UTC
        Assert.Equal(1, await StockAsync(child, listingId));
    }

    private static async Task<int> StockAsync(HttpClient child, int listingId)
    {
        var view = await child.GetFromJsonAsync<JsonElement>("/api/v1/student/me/shop");
        return view.GetProperty("available").EnumerateArray()
            .Single(o => o.GetProperty("id").GetInt32() == listingId).GetProperty("currentStock").GetInt32();
    }

    private static async Task<int> IdAsync(Task<JsonElement> session) => (await session).GetProperty("id").GetInt32();

    private static async Task<JsonElement> ReviewAsync(Setup s, int sessionId, string answer) =>
        await (await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, 0, answer, null))
            .Content.ReadFromJsonAsync<JsonElement>();
}

/// <summary>
/// B-730 AK 4: the two days a year that are not 24 hours long. Each calendar day exists exactly once, also across
/// midnight - walked hour by hour, so a day that appears twice or not at all shows as a jump.
/// </summary>
public class LerntagUmstellungTests
{
    private static readonly LearningDay Berlin = TestLearningDay.Berlin;

    [Theory]
    [InlineData(2027, 3, 28, 23)] // spring: 02:00 -> 03:00
    [InlineData(2027, 10, 31, 25)] // autumn: 03:00 -> 02:00
    public void Umstellungstag_Hat_Seine_Stunden_Und_Jeder_Tag_Kommt_Einmal(int year, int month, int day, int hours)
    {
        var date = new DateOnly(year, month, day);

        Assert.Equal(hours, (Berlin.StartUtc(date.AddDays(1)) - Berlin.StartUtc(date)).TotalHours);

        var seen = new List<DateOnly>();
        for (var t = Berlin.StartUtc(date.AddDays(-1)); t < Berlin.StartUtc(date.AddDays(2)); t = t.AddHours(1))
        {
            var d = Berlin.DayOf(t);
            if (seen.Count == 0 || seen[^1] != d) seen.Add(d);
        }
        Assert.Equal([date.AddDays(-1), date, date.AddDays(1)], seen);
    }

    // The start of a day is its local midnight: 23:00Z in winter, 22:00Z in summer. Red when StartUtc is UTC midnight.
    [Fact]
    public void Tag_Beginnt_Um_Mitternacht_Ortszeit()
    {
        Assert.Equal(new DateTime(2027, 1, 14, 23, 0, 0, DateTimeKind.Utc), Berlin.StartUtc(new DateOnly(2027, 1, 15)));
        Assert.Equal(new DateTime(2027, 7, 14, 22, 0, 0, DateTimeKind.Utc), Berlin.StartUtc(new DateOnly(2027, 7, 15)));
    }

    // A zone the system does not know fails with the setting's name, not at the first request.
    [Fact]
    public void Unbekannte_Zone_Scheitert_Mit_Namen()
    {
        var e = Assert.Throws<InvalidOperationException>(() => TestLearningDay.For("Mars/Olympus"));
        Assert.Contains("LearningDay:TimeZone", e.Message);
    }
}
