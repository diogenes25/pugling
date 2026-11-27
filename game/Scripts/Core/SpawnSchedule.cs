namespace PuglingTower.Core;

/// <summary>One enemy to put on the map: what it is, which path it takes, how fast it walks.</summary>
/// <param name="Kind">The enemy kind.</param>
/// <param name="Path">Index into the map's paths.</param>
/// <param name="Speed">Walking speed in units per second.</param>
public sealed record Spawn(EnemyKind Kind, int Path, float Speed);

/// <summary>
/// When the next enemy comes and what it is. The level gets harder the longer it runs: the interval shrinks and the
/// skeletons speed up (both capped), every <see cref="Balance.WarriorEvery"/>-th one is a warrior, and several paths
/// take turns.
/// </summary>
/// <param name="interval">The map's start seconds between two enemies.</param>
/// <param name="speed">The map's start speed.</param>
/// <param name="paths">Number of paths on the map.</param>
public sealed class SpawnSchedule(float interval, float speed, int paths)
{
    private float _timer = Balance.FirstSpawnDelay;

    /// <summary>Enemies spawned so far.</summary>
    public int Spawned { get; private set; }

    /// <summary>Advances the timer; returns the enemy to spawn now, or <c>null</c>.</summary>
    /// <param name="dt">Seconds since the last frame.</param>
    /// <param name="elapsed">Played seconds of the run.</param>
    public Spawn? Tick(float dt, float elapsed)
    {
        if ((_timer -= dt) > 0) return null;
        _timer = Math.Max(interval * Balance.SpawnIntervalFloor, interval - elapsed * Balance.SpawnIntervalRamp);
        var kind = ++Spawned % Balance.WarriorEvery == 0 ? EnemyKind.Warrior : EnemyKind.Minion;
        return new Spawn(kind, Spawned % paths, (speed + Math.Min(Balance.MaxSpeedRamp, elapsed * Balance.SpeedRamp)) * kind.SpeedFactor);
    }
}
