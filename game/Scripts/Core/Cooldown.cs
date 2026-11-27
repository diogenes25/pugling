namespace PuglingTower.Core;

/// <summary>A reload timer: ticks down every frame and reports when a new shot may go.</summary>
public sealed class Cooldown
{
    private float _left;

    /// <summary>Counts down by <paramref name="dt"/>; true once the timer has run out.</summary>
    public bool Tick(float dt) => (_left -= dt) <= 0;

    /// <summary>Starts the next wait.</summary>
    public void Start(float seconds) => _left = seconds;
}
