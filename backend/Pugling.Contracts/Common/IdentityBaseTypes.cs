namespace Pugling.Contracts;

/// <summary>The three domain tiers as a role – independent of login.</summary>
public enum ProfileRole
{
    /// <summary>Creates content/exercises (today: bound to an <c>Adult</c> profile).</summary>
    Creator = 0,
    /// <summary>Controls: study plans, goals/points, shop (today: <c>Adult</c> profile).</summary>
    Supervisor = 1,
    /// <summary>Learns, earns, buys/activates (today: <c>Child</c> profile).</summary>
    Student = 2,
}
