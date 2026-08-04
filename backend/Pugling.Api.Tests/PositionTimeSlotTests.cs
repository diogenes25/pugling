using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The points time slot <b>per obligation</b> (<c>PlanPosition.TimeSlots</c>): the supervisor sets a window
/// with its own factor on a single position ("homework counts double between 13:00 and 15:00").
/// <para>
/// What is checked here is the part a pure function cannot show: the round trip through the JSON column
/// (<see cref="TimeOnly"/> is not self-evident there), the rejection of a window that would be silently
/// ineffective, and that the stored window actually <b>reaches</b> the scoring path. The factor arithmetic
/// itself – union with the global windows, narrowest wins, kill switch – sits in
/// <see cref="ScoringTimeSlotTests"/> without a host.
/// </para>
/// </summary>
public class PositionTimeSlotTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Plan with one position on a fresh vocabulary exercise; returns the ids and the supervisor client.</summary>
    private async Task<(HttpClient Father, int PlanId, int PositionId)> SetupAsync(object? timeSlots = null)
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kind"), pin = "1111" }));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, timeSlots }));
        return (father, planId, positionId);
    }

    private static JsonElement Slots(JsonElement position) => position.GetProperty("timeSlots");

    /// <summary>
    /// Set, read back, replace, clear. The read-back is the actual point: the column is JSON, and a
    /// <see cref="TimeOnly"/> in there is a serialization assumption – not something to take on trust.
    /// </summary>
    [Fact]
    public async Task Zeitfenster_Ueberlebt_Die_Rundreise_Durch_Die_Json_Spalte()
    {
        var (father, planId, positionId) = await SetupAsync(
            new[] { new { name = "Hausaufgaben", start = "13:00", end = "15:00", multiplier = 2.0 } });
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}";

        // Fresh from the database, not from the tracked instance of the request that wrote it.
        var gelesen = await father.GetFromJsonAsync<JsonElement>(url);
        var slot = Slots(gelesen)[0];
        Assert.Equal("Hausaufgaben", slot.GetProperty("name").GetString());
        Assert.Equal("13:00:00", slot.GetProperty("start").GetString());
        Assert.Equal("15:00:00", slot.GetProperty("end").GetString());
        Assert.Equal(2.0, slot.GetProperty("multiplier").GetDouble());

        // Sending a list replaces the stored one (not a merge – a window has no identity of its own).
        var ersetzt = await father.PatchAsJsonAsync(url, new
        {
            timeSlots = new[] { new { name = "Nachmittag", start = "16:00", end = "17:30", multiplier = 1.5 } },
        });
        ersetzt.EnsureSuccessStatusCode();
        var danach = Slots(await ersetzt.Content.ReadFromJsonAsync<JsonElement>());
        Assert.Equal(1, danach.GetArrayLength());
        Assert.Equal("16:00:00", danach[0].GetProperty("start").GetString());

        // And the switch takes the window away again – only the global windows apply from here on.
        var geleert = await father.PatchAsJsonAsync(url, new { clearTimeSlots = true });
        geleert.EnsureSuccessStatusCode();
        Assert.Equal(JsonValueKind.Null, Slots(await geleert.Content.ReadFromJsonAsync<JsonElement>()).ValueKind);
    }

    /// <summary>
    /// An explicitly sent empty list is "no windows", not "not specified" – and it is stored as <c>null</c>, so
    /// that "nothing" has one spelling only. Read the other way it would be a silent no-op: the caller clears
    /// the list, the API answers 200, and the old window keeps doubling the points.
    /// </summary>
    [Fact]
    public async Task Leere_Liste_Leert_Das_Fenster()
    {
        var (father, planId, positionId) = await SetupAsync(
            new[] { new { name = "Hausaufgaben", start = "13:00", end = "15:00", multiplier = 2.0 } });

        var res = await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", new { timeSlots = Array.Empty<object>() });
        res.EnsureSuccessStatusCode();

        Assert.Equal(JsonValueKind.Null, Slots(await res.Content.ReadFromJsonAsync<JsonElement>()).ValueKind);
    }

    /// <summary>
    /// Both rejections concern the same failure mode: a setting that looks valid in the form and does
    /// <b>nothing</b> (or the opposite). A window ending before it starts never applies; a factor of 0 takes
    /// the points away from every correct answer inside it.
    /// </summary>
    [Theory]
    [InlineData("15:00", "13:00", 2.0)]   // end before start
    [InlineData("13:00", "13:00", 2.0)]   // empty window
    [InlineData("13:00", "15:00", 0.0)]   // factor 0 – silently costs the points
    [InlineData("13:00", "15:00", -1.0)]  // negative factor
    [InlineData("13:00", "15:00", 25.0)]  // beyond the upper bound
    public async Task Unwirksame_Fenster_Werden_Abgelehnt(string start, string end, double multiplier)
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kind"), pin = "1111" }));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new
        {
            exerciseId,
            timeSlots = new[] { new { name = "Kaputt", start, end, multiplier } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
    }

    /// <summary>The same check guards the PATCH – otherwise the rule would only hold when creating.</summary>
    [Fact]
    public async Task Unwirksames_Fenster_Wird_Auch_Beim_Patch_Abgelehnt()
    {
        var (father, planId, positionId) = await SetupAsync();

        var res = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}",
            new { timeSlots = new[] { new { name = "Kaputt", start = "15:00", end = "13:00", multiplier = 2.0 } } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>Without a window nothing changes: the response says <c>null</c>, not an empty list.</summary>
    [Fact]
    public async Task Ohne_Fenster_Bleibt_Das_Feld_Leer()
    {
        var (father, planId, positionId) = await SetupAsync();

        var gelesen = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}");

        Assert.Equal(JsonValueKind.Null, Slots(gelesen).ValueKind);
    }
}

/// <summary>
/// The end-to-end half: does the stored window reach the score? Needs its <b>own</b> host, because the
/// standard test host switches the time slots off on purpose (otherwise the score of the same correct answer
/// would hang on the time of the run – see <see cref="PuglingWebAppFactory"/>).
/// </summary>
public class PositionTimeSlotScoringTests(TimeSlotsOnFactory factory) : IClassFixture<TimeSlotsOnFactory>
{
    /// <summary>
    /// Self-protection for the two tests below: they only hold while <b>no global window</b> can apply, and
    /// that is what the factory neutralizes. Checked instead of assumed – growing the list in
    /// <c>appsettings.json</c> beyond the factory's bound would otherwise make them silently clock-dependent
    /// (red only between 08:00 and 21:00).
    /// </summary>
    [Fact]
    public void Kein_Globales_Fenster_Ist_In_Diesem_Host_Aktiv()
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ScoringOptions>>().Value;

        Assert.True(options.TimeSlotsEnabled, "The host has to have the slots ON - otherwise the tests below prove nothing.");
        Assert.All(options.TimeSlots, s =>
            Assert.True(s.Start >= s.End, $"Global window '{s.Name}' ({s.Start}-{s.End}) applies and would distort the score."));
    }

    /// <summary>
    /// New content yields <c>NewContentPoints</c> (10) as its base; the position's window doubles it. The
    /// window covers the whole day, so the expectation holds no matter when the suite runs – the global
    /// windows are neutralized in the factory for the same reason.
    /// </summary>
    [Fact]
    public async Task Positions_Fenster_Verdoppelt_Die_Punkte_Der_Antwort()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess,
            comboThreshold: 0,
            // MaxValue, not 23:59:59: the end is EXCLUSIVE, so the last second of the day would otherwise fall
            // outside the window - a flake that only shows up around midnight.
            timeSlots: [new ScoringTimeSlot { Name = "Ganztags", Start = TimeOnly.MinValue, End = TimeOnly.MaxValue, Multiplier = 2.0 }]);

        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex: 0, wasKnown: true);
        res.EnsureSuccessStatusCode();

        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, outcome.GetProperty("awarded").GetInt32());
    }

    /// <summary>The counter-sample on the same host: without a window the base points stay untouched.</summary>
    [Fact]
    public async Task Ohne_Positions_Fenster_Bleiben_Die_Basispunkte_Stehen()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess,
            comboThreshold: 0);

        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex: 0, wasKnown: true);
        res.EnsureSuccessStatusCode();

        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, outcome.GetProperty("awarded").GetInt32());
    }
}

/// <summary>
/// A host with the time slots switched <b>on</b> – for the one test that has to see the factor take effect.
/// </summary>
public sealed class TimeSlotsOnFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <summary>Number of global windows that get neutralized – generously above the three in appsettings.json.</summary>
    private const int NeutralizedSlots = 10;

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        // As in the standard factory: the in-process server shares one IP partition.
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        builder.UseSetting("Scoring:TimeSlotsEnabled", "true");
        /*
         * appsettings.json ships three global windows (morning ×1.5 … evening ×0.8). With the slots on, a run
         * at 09:00 would weight differently than one at 19:00 - the very flake the kill switch exists for.
         * They are therefore NEUTRALIZED rather than removed: configuration can override a list entry but not
         * delete it, and a window with start == end never applies (start <= t < end is false for every t).
         */
        for (var i = 0; i < NeutralizedSlots; i++)
        {
            builder.UseSetting($"Scoring:TimeSlots:{i}:Name", $"neutral-{i}");
            builder.UseSetting($"Scoring:TimeSlots:{i}:Start", "00:00");
            builder.UseSetting($"Scoring:TimeSlots:{i}:End", "00:00");
            builder.UseSetting($"Scoring:TimeSlots:{i}:Multiplier", "1.0");
        }
    }
}
