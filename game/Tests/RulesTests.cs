using PuglingTower.Core;

namespace PuglingTower.Tests;

/// <summary>The tower's ammunition economy (decisions E2 and of 2026-11-26).</summary>
public class TowerAmmoTests
{
    [Fact]
    public void A_correct_word_adds_eight_shots_and_a_wrong_one_costs_three()
    {
        var ammo = new TowerAmmo();
        Assert.Equal(10, ammo.Count);
        ammo.Refill();
        Assert.Equal(18, ammo.Count);
        ammo.Penalize();
        Assert.Equal(15, ammo.Count);
    }

    [Fact]
    public void A_penalty_never_drives_the_ammo_below_zero()
    {
        var ammo = new TowerAmmo();
        for (var i = 0; i < 9; i++) ammo.Use();
        ammo.Penalize();
        Assert.Equal(0, ammo.Count);
    }

    [Fact]
    public void More_than_thirty_shots_make_a_tower_strong_and_the_turn_is_reported_once()
    {
        var ammo = new TowerAmmo();
        Assert.False(ammo.Refill()); // 18
        Assert.False(ammo.Refill()); // 26
        Assert.True(ammo.Refill()); // 34
        Assert.True(ammo.IsStrong);
        Assert.False(ammo.Refill()); // already strong
    }

    [Fact]
    public void Five_refills_keep_a_tower_strong_even_when_its_ammo_runs_low()
    {
        var ammo = new TowerAmmo();
        for (var i = 0; i < 5; i++)
        {
            ammo.Refill();
            for (var k = 0; k < 8; k++) ammo.Use();
        }
        Assert.Equal(10, ammo.Count);
        Assert.True(ammo.IsStrong);
    }

    [Fact]
    public void A_shot_reports_the_strength_it_was_fired_with()
    {
        var ammo = new TowerAmmo();
        for (var i = 0; i < 3; i++) ammo.Refill(); // 34: strong
        for (var i = 0; i < 3; i++) Assert.True(ammo.Use()); // 34, 33, 32 shoot strong
        Assert.True(ammo.Use()); // 31 > 30: still strong when fired
        Assert.False(ammo.Use()); // 30 is no longer more than thirty
    }

    [Fact]
    public void Three_shots_or_fewer_count_as_low()
    {
        var ammo = new TowerAmmo();
        for (var i = 0; i < 6; i++) ammo.Use();
        Assert.False(ammo.IsLow); // 4
        ammo.Use();
        Assert.True(ammo.IsLow); // 3
    }
}

/// <summary>The client's count of a run and its log line.</summary>
public class RunTallyTests
{
    [Fact]
    public void Only_correct_words_count_towards_the_goal()
    {
        var tally = new RunTally(2);
        tally.Assigned(correct: false);
        tally.Assigned(correct: true);
        Assert.False(tally.GoalReached);
        tally.Assigned(correct: true);
        Assert.True(tally.GoalReached);
        Assert.Equal(3, tally.Total);
    }

    [Fact]
    public void The_castle_falls_with_the_tenth_skeleton()
    {
        var tally = new RunTally(10);
        for (var i = 0; i < 9; i++) Assert.False(tally.CastleHit());
        Assert.True(tally.CastleHit());
    }

    [Fact]
    public void The_summary_keeps_the_words_the_run_reports_are_read_by()
    {
        var tally = new RunTally(10) { Elapsed = 97.4f };
        tally.Assigned(true);
        tally.Assigned(false);
        tally.CastleHit();
        tally.HeroShot();
        tally.HeroShot();
        tally.HeroKill();
        Assert.Equal("ende GoalReached 1/2 richtig, 97s, Burg 9/10, Heldenschüsse 2, Heldentreffer 1", tally.Summary("GoalReached"));
    }
}

/// <summary>When the skeletons come and what they are.</summary>
public class SpawnScheduleTests
{
    [Fact]
    public void The_first_skeleton_comes_after_two_seconds()
    {
        var schedule = new SpawnSchedule(5f, 1.6f, 1);
        Assert.Null(schedule.Tick(1.9f, 1.9f));
        Assert.NotNull(schedule.Tick(0.2f, 2.1f));
    }

    [Fact]
    public void Every_fourth_skeleton_is_a_slower_warrior()
    {
        var schedule = new SpawnSchedule(5f, 2f, 1);
        var kinds = Enumerable.Range(0, 8).Select(_ => schedule.Tick(10f, 0f)!).ToList();
        Assert.Equal([EnemyKind.Warrior, EnemyKind.Warrior], kinds.Where((_, i) => (i + 1) % 4 == 0).Select(s => s.Kind));
        Assert.All(kinds.Where((_, i) => (i + 1) % 4 != 0), s => Assert.Same(EnemyKind.Minion, s.Kind));
        Assert.Equal(1.6f, kinds[3].Speed, 3);
        Assert.Equal(2f, kinds[0].Speed, 3);
    }

    [Fact]
    public void Several_paths_take_turns()
    {
        var schedule = new SpawnSchedule(5f, 2f, 2);
        var paths = Enumerable.Range(0, 4).Select(_ => schedule.Tick(10f, 0f)!.Path);
        Assert.Equal([1, 0, 1, 0], paths);
    }

    [Fact]
    public void The_interval_shrinks_with_play_time_down_to_its_floor()
    {
        var schedule = new SpawnSchedule(5f, 2f, 1);
        schedule.Tick(2f, 100f); // next interval: 5 - 1.2 = 3.8
        Assert.Null(schedule.Tick(3.7f, 100f));
        Assert.NotNull(schedule.Tick(0.2f, 100f));
        schedule.Tick(10f, 1000f); // 5 - 12 is below the floor of 2.75
        Assert.Null(schedule.Tick(2.7f, 1000f));
        Assert.NotNull(schedule.Tick(0.1f, 1000f));
    }

    [Fact]
    public void Skeletons_speed_up_by_at_most_point_eight()
    {
        var schedule = new SpawnSchedule(5f, 2f, 1);
        Assert.Equal(2.4f, schedule.Tick(10f, 100f)!.Speed, 3);
        Assert.Equal(2.8f, schedule.Tick(10f, 10_000f)!.Speed, 3);
    }
}

/// <summary>The run's words between pool, field and hand (B-822 E2).</summary>
public class TokenPoolTests
{
    [Fact]
    public void Every_token_drops_once_until_it_comes_back()
    {
        var pool = new TokenPool(["a", "b"], new Random(1));
        var first = pool.Draw();
        var second = pool.Draw();
        Assert.Equal(["a", "b"], new[] { first!, second! }.Order());
        Assert.Null(pool.Draw());
        pool.Return(first!);
        Assert.Equal(first, pool.Draw());
    }

    [Fact]
    public void A_consumed_token_never_drops_again()
    {
        var pool = new TokenPool(["a"]);
        var id = pool.Draw()!;
        pool.Consume(id);
        pool.Return(id);
        Assert.Null(pool.Draw());
    }

    [Fact]
    public void Returning_a_token_twice_does_not_double_it()
    {
        var pool = new TokenPool(["a"]);
        var id = pool.Draw()!;
        pool.Return(id);
        pool.Return(id);
        Assert.Equal(id, pool.Draw());
        Assert.Null(pool.Draw());
    }
}

/// <summary>The reload timer.</summary>
public class CooldownTests
{
    [Fact]
    public void A_started_cooldown_holds_until_its_time_has_passed()
    {
        var cooldown = new Cooldown();
        Assert.True(cooldown.Tick(0.1f));
        cooldown.Start(1f);
        Assert.False(cooldown.Tick(0.6f));
        Assert.True(cooldown.Tick(0.5f));
    }
}
