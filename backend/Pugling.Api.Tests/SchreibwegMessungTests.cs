using System.Data.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Counts the database commands the host runs while <see cref="Enabled"/> is set - a measuring tool, not a rule. It counts
/// commands, not round trips: transaction statements go through another interceptor type and are not counted. Every
/// command text is kept, so a measurement can split one kind of query from the rest.
/// </summary>
public sealed class CommandCounter : DbCommandInterceptor
{
    private readonly List<string> _commands = [];

    /// <summary>Whether commands are counted right now; off by default, so setup traffic stays out of a measurement.</summary>
    public bool Enabled { get; set; }

    /// <summary>The texts of the commands counted since the last <see cref="Reset"/>.</summary>
    public IReadOnlyList<string> Commands
    {
        get { lock (_commands) return [.. _commands]; }
    }

    /// <summary>Forgets every counted command.</summary>
    public void Reset()
    {
        lock (_commands) _commands.Clear();
    }

    private void Count(DbCommand command)
    {
        if (!Enabled) return;
        lock (_commands) _commands.Add(command.CommandText);
    }

    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Count(command);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Count(command);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Count(command);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// The test host of <see cref="PuglingWebAppFactory"/> with a <see cref="CommandCounter"/> hooked into the database context
/// (B-609). A factory of its own, so no other test class pays for the interceptor or shares its counter.
/// </summary>
public sealed class CommandCountingWebAppFactory : PuglingWebAppFactoryBase
{
    /// <summary>The counter every database command of this host passes.</summary>
    public CommandCounter Counter { get; } = new();

    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        builder.UseSetting("Scoring:TimeSlotsEnabled", "false");
        builder.ConfigureServices(s => s.ConfigureDbContext<PuglingDbContext>(o => o.AddInterceptors(Counter)));
    }
}

/// <summary>
/// B-609: how many database commands a hand-in costs, and how many of them only rebuild a day view nobody reads. The
/// numbers are measured on this host, not assumed; each case first proves the counter counts at all.
/// </summary>
public class SchreibwegMessungTests(CommandCountingWebAppFactory factory, ITestOutputHelper output)
    : IClassFixture<CommandCountingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, int ChildId, int PlanId, IReadOnlyList<int> Positions);

    // A fresh child with one plan of `count` daily vocabulary positions, all running since `days` - 1 days ago.
    private async Task<Setup> SetupAsync(int count, int days = 1)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Mess-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var seeded = Enumerable.Range(0, count)
            .Select(_ => TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId))
            .ToList();
        var planId = seeded[0].planId;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var start = Today.AddDays(1 - days);
        var positionIds = seeded.Select(s => s.positionId).ToList();
        foreach (var pos in db.PlanPositions.Where(p => positionIds.Contains(p.Id)))
        {
            pos.StudyPlanId = planId;
            pos.CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }
        var others = seeded.Skip(1).Select(s => s.planId).ToList();
        db.StudyPlans.RemoveRange(db.StudyPlans.Where(p => others.Contains(p.Id)));
        db.StudyPlans.Single(p => p.Id == planId).StartDate = start;
        db.SaveChanges();
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionIds);
    }

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private void ClaimBox(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.DailyBoxClaims.Add(new DailyBoxClaim { ChildId = childId, Day = Today, AwardedAt = factory.Clock.GetUtcNow().UtcDateTime });
        db.SaveChanges();
    }

    // Counts the commands of one passed test submit (the hand-in), the start of the attempt left out.
    private async Task<int> CountSubmitAsync(Setup s)
    {
        var testsUrl = $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.Positions[0]}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } };
        return await CountAsync(async () =>
            (await s.Child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode());
    }

    // Counts the commands of one day view of the plan, loaded the way the hand-in loads it (tracked, no includes).
    private async Task<int> CountDayViewAsync(Setup s)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = await db.StudyPlans.SingleAsync(p => p.Id == s.PlanId);
        var progress = scope.ServiceProvider.GetRequiredService<PositionProgressService>();
        return await CountAsync(() => progress.ComputeDayAsync(plan, Today, CancellationToken.None));
    }

    private async Task<int> CountAsync(Func<Task> act)
    {
        factory.Counter.Reset();
        factory.Counter.Enabled = true;
        try
        {
            await act();
        }
        finally
        {
            factory.Counter.Enabled = false;
        }
        return factory.Counter.Commands.Count;
    }

    /// <summary>
    /// AK 1 (a): commands of a hand-in at 5 and 15 positions, chest still open and already claimed, next to the cost of a
    /// direct day view of the same plan. A measurement - it asserts only that the counter counts (red when the interceptor
    /// is not hooked in: every figure 0 would "prove" the hand-in is free).
    /// </summary>
    [Theory]
    [Trait("Category", "Measurement")]
    [InlineData(5)]
    [InlineData(15)]
    public async Task Messung_Abgabe(int positions)
    {
        var open = await SetupAsync(positions);
        var dayView = await CountDayViewAsync(open);
        var submitOpen = await CountSubmitAsync(open);

        var claimed = await SetupAsync(positions);
        ClaimBox(claimed.ChildId);
        var submitClaimed = await CountSubmitAsync(claimed);

        output.WriteLine($"positions={positions} dayView={dayView} submitChestOpen={submitOpen} submitChestClaimed={submitClaimed}");
        Assert.True(dayView > 0, "the counter counted nothing - is the interceptor hooked in?");
        Assert.True(submitClaimed > 0);
    }

    /// <summary>
    /// AK 2: once the chest is claimed, the hand-in builds no day view any more. Checked by the one query only a day view
    /// sends - the introduced contents of the whole plan (<c>PositionItemProgress</c> joined to <c>PlanPositions</c> by
    /// <c>StudyPlanId</c>) - instead of a total count, so an unrelated query on the hand-in path does not turn it red.
    /// The day view itself must send it exactly once, which proves the marker; with the chest still open the hand-in
    /// sends it once more, for the chest's own check over the child's day. Red while <c>EvaluateAndAwardAsync</c> still
    /// ends in <c>ComputeDayAsync</c> (then the claimed case sends it once). Measured before B-609 on 2026-11-21: a hand-in
    /// with the chest claimed cost 66 commands at 5 positions and 136 at 15, after it 32 and 42 (<see cref="Messung_Abgabe"/>).
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    public async Task Nach_Vergebener_Truhe_Rechnet_Die_Abgabe_Keine_Tagessicht(int positions)
    {
        var s = await SetupAsync(positions);
        await CountDayViewAsync(s);
        Assert.Equal(1, DayViewQueries());

        var open = await SetupAsync(positions);
        await CountSubmitAsync(open);
        var withOpenChest = DayViewQueries();
        ClaimBox(s.ChildId);
        await CountSubmitAsync(s);
        var withClaimedChest = DayViewQueries();

        output.WriteLine($"positions={positions} dayViewQueriesChestOpen={withOpenChest} dayViewQueriesChestClaimed={withClaimedChest}");
        Assert.Equal(1, withOpenChest);
        Assert.Equal(0, withClaimedChest);
    }

    // The query only ComputeDayAsync sends: the introduced contents of a whole plan, joined through its positions.
    private int DayViewQueries() => factory.Counter.Commands.Count(c =>
        c.Contains("FROM \"PositionItemProgress\"", StringComparison.Ordinal)
        && c.Contains("INNER JOIN \"PlanPositions\"", StringComparison.Ordinal)
        && c.Contains("\"StudyPlanId\"", StringComparison.Ordinal));

    /// <summary>
    /// AK 1 (b): commands of the plan's history over 30 days at 15 positions, and the share of the position query with
    /// its series/subject chain (the one command text joining <c>TextbookSeries</c>). A measurement; it asserts only that
    /// the counter counts.
    /// </summary>
    [Fact]
    [Trait("Category", "Measurement")]
    public async Task Messung_Verlauf()
    {
        var s = await SetupAsync(15, days: 30);
        var total = await CountAsync(async () =>
            (await s.Child.GetAsync($"/api/v1/student/study-plans/{s.PlanId}/overview/progress")).EnsureSuccessStatusCode());
        var chain = factory.Counter.Commands.Count(c => c.Contains("\"TextbookSeries\"", StringComparison.Ordinal)
            && c.Contains("\"PlanPositions\"", StringComparison.Ordinal));

        output.WriteLine($"history30Days total={total} positionSubjectChain={chain} share={100.0 * chain / Math.Max(total, 1):F1}%");
        Assert.True(total > 0, "the counter counted nothing - is the interceptor hooked in?");
    }
}
