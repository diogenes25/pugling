using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-118: <see cref="PuglingWebAppFactory"/> pins the daily box's coin/gem draw to a single value (Min ==
/// Max) so documentation captures stay byte-stable - which means no test in the suite ever exercises a real
/// draw over <c>[Min,Max]</c>. This class uses its own factory with a narrow range, distinct from both the
/// production defaults and the pinned test value, so a swapped bound or an off-by-one on the inclusive
/// upper bound (<c>DailyBoxService.cs</c>: <c>Random.Shared.Next(opts.MinCoins, opts.MaxCoins + 1)</c>)
/// would show up as a value outside the range or a missing boundary.
/// </summary>
public class DailyBoxRangeTests(DailyBoxRangeFactory factory) : IClassFixture<DailyBoxRangeFactory>
{
    private const int Trials = 60;

    /// <summary>
    /// Draws the box <see cref="Trials"/> times (one throwaway plan per day, so the idempotency-per-day
    /// check never blocks a repeat) and checks both bounds are honored AND both boundary values actually
    /// occur - the latter is what an exclusive-instead-of-inclusive upper bound would fail, and what a
    /// single draw could never prove.
    /// </summary>
    [Fact]
    public async Task Ziehungsspanne_Haelt_Beide_Grenzen_Ein_Und_Erreicht_Sie()
    {
        var coins = new List<int>();
        var gems = new List<int>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var box = scope.ServiceProvider.GetRequiredService<DailyBoxService>();

        for (var i = 0; i < Trials; i++)
        {
            var day = today.AddDays(-i);
            // Zero positions -> DutyDone can never come from a real duty; EvaluateAndAwardAsync only reads
            // the DayOverview passed in for the award gate, so DutyDone is asserted true directly. The
            // internal streak recomputation then sees zero positions too and settles at streak 0 either way
            // - well below the first escalation tier (7), so the multiplier stays 1.0 for every trial and
            // the raw [Min,Max] draw is what ends up on the claim.
            var plan = new StudyPlan { ChildId = 1, Title = $"DailyBoxRange {i}", StartDate = day, EndDate = day };
            db.StudyPlans.Add(plan);
            await db.SaveChangesAsync();

            var overview = new DayOverview(day, true, 0, 0, 0, [], []);
            await box.EvaluateAndAwardAsync(plan, day, overview);

            var claim = await db.DailyBoxClaims.AsNoTracking().SingleAsync(c => c.ChildId == 1 && c.Day == day);
            coins.Add(claim.CoinsAwarded);
            gems.Add(claim.GemsAwarded);
        }

        Assert.All(coins, c => Assert.InRange(c, DailyBoxRangeFactory.MinCoins, DailyBoxRangeFactory.MaxCoins));
        Assert.All(gems, g => Assert.InRange(g, DailyBoxRangeFactory.MinGems, DailyBoxRangeFactory.MaxGems));
        Assert.Contains(DailyBoxRangeFactory.MinCoins, coins);
        Assert.Contains(DailyBoxRangeFactory.MaxCoins, coins);
        Assert.Contains(DailyBoxRangeFactory.MinGems, gems);
        Assert.Contains(DailyBoxRangeFactory.MaxGems, gems);
    }
}

/// <summary>
/// A narrow coin/gem range, deliberately different from both the production defaults (10-30/0-2) and the
/// single-value pin in <see cref="PuglingWebAppFactory"/> (20/2) - three possible values per currency keep
/// <see cref="DailyBoxRangeTests"/>'s 60 trials statistically safe (missing one specific value across 60
/// draws: (2/3)^60 ≈ 3×10⁻¹¹) while running fast.
/// </summary>
public sealed class DailyBoxRangeFactory : PuglingWebAppFactoryBase
{
    public const int MinCoins = 7;
    public const int MaxCoins = 9;
    public const int MinGems = 2;
    public const int MaxGems = 4;

    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("Gamification:DailyBox:MinCoins", MinCoins.ToString());
        builder.UseSetting("Gamification:DailyBox:MaxCoins", MaxCoins.ToString());
        builder.UseSetting("Gamification:DailyBox:MinGems", MinGems.ToString());
        builder.UseSetting("Gamification:DailyBox:MaxGems", MaxGems.ToString());
    }
}
