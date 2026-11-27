namespace PuglingTower.Core;

/// <summary>
/// A tower's ammunition economy: it shoots while it has shots, a correct word refills it, a wrong one costs shots. A
/// tower that is well stocked or often refilled turns strong and shoots stronger projectiles.
/// </summary>
public sealed class TowerAmmo
{
    /// <summary>Shots left.</summary>
    public int Count { get; private set; } = Balance.StartAmmo;

    /// <summary>How often a correct word refilled this tower.</summary>
    public int Refills { get; private set; }

    /// <summary>Shoots the strong projectile.</summary>
    public bool IsStrong => Count > Balance.StrongAmmo || Refills >= Balance.MasterRefills;

    /// <summary>One more wrong word would empty the tower.</summary>
    public bool IsLow => Count <= Balance.WrongPenalty;

    /// <summary>Uses one shot; returns whether it was a strong one.</summary>
    public bool Use()
    {
        var strong = IsStrong;
        Count--;
        return strong;
    }

    /// <summary>A correct word: adds <see cref="Balance.RefillAmmo"/>; returns whether the tower just turned strong.</summary>
    public bool Refill()
    {
        var wasStrong = IsStrong;
        Count += Balance.RefillAmmo;
        Refills++;
        return !wasStrong && IsStrong;
    }

    /// <summary>A wrong word: costs <see cref="Balance.WrongPenalty"/> shots, never below zero.</summary>
    public void Penalize() => Count = Math.Max(0, Count - Balance.WrongPenalty);
}
