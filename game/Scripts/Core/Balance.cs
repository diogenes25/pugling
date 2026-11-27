namespace PuglingTower.Core;

/// <summary>
/// The game's tuning knobs in one place. Numbers that belong to one kind of thing (a tower type, an enemy type, a
/// projectile, a map) live with that kind's definition; everything here is a rule of the whole game.
/// </summary>
public static class Balance
{
    /// <summary>The hero's walking speed in units per second.</summary>
    public const float HeroSpeed = 6.5f;

    /// <summary>The hero's body radius against obstacles.</summary>
    public const float HeroRadius = 0.45f;

    /// <summary>Books closer than this to the hero are offered for pick-up.</summary>
    public const float PickupRadius = 2.6f;

    /// <summary>A tower closer than this to the hero accepts the carried word.</summary>
    public const float TowerReach = 3.6f;

    /// <summary>
    /// The hero's own shot: shorter range and slower than a tower and only ever one damage, so the towers stay the main
    /// defence - but without ammo, so empty towers never end a run alone.
    /// </summary>
    public const float HeroRange = 5.5f, HeroShotInterval = 1.0f;

    /// <summary>Shots a tower starts with.</summary>
    public const int StartAmmo = 10;

    /// <summary>Shots a correct word adds.</summary>
    public const int RefillAmmo = 8;

    /// <summary>Shots a wrong word costs (user decision of 2026-11-26; it was 1 before).</summary>
    public const int WrongPenalty = 3;

    /// <summary>A tower with more shots than this, or refilled <see cref="MasterRefills"/> times, shoots strong arrows.</summary>
    public const int StrongAmmo = 30, MasterRefills = 5;

    /// <summary>Skeletons that may reach the castle before it falls.</summary>
    public const int CastleLives = 10;

    /// <summary>Seconds a dropped book lies on the field before it goes back into the pool; laid-down books stay.</summary>
    public const float TokenLifetime = 30f;

    /// <summary>A book blinks during its last seconds on the field.</summary>
    public const float TokenBlinkWarning = 5f;

    /// <summary>Seconds before the first skeleton.</summary>
    public const float FirstSpawnDelay = 2f;

    /// <summary>The spawn interval shrinks by this many seconds per played second, but never below the floor factor.</summary>
    public const float SpawnIntervalRamp = 0.012f, SpawnIntervalFloor = 0.55f;

    /// <summary>Skeletons get faster by this per played second, by at most <see cref="MaxSpeedRamp"/>.</summary>
    public const float SpeedRamp = 0.004f, MaxSpeedRamp = 0.8f;

    /// <summary>Every n-th skeleton is a warrior.</summary>
    public const int WarriorEvery = 4;

    /// <summary>The app reports the foreground play time about this often (B-851: the server clamps each beat).</summary>
    public const double BeatInterval = 15;
}
