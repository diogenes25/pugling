using Godot;
using PuglingTower.Core;
using PuglingTower.World;

namespace PuglingTower.Combat;

/// <summary>
/// One kind of projectile as data: a homing sphere, or a scenery model (<see cref="Model"/>) that may fly in a bow
/// (<see cref="Arc"/>). A new look is a new instance here, not a new loop in <see cref="Projectiles"/>.
/// </summary>
/// <param name="Name">For the log and for sound/effect lookups.</param>
/// <param name="Color">The sphere's colour.</param>
/// <param name="Size">Scale of the sphere (1 = radius 0.16).</param>
/// <param name="Speed">Units per second.</param>
/// <param name="Damage">Hit points it takes off.</param>
public sealed record ProjectileKind(string Name, Color Color, float Size, float Speed, int Damage)
{
    /// <summary>The flat material, made once per kind.</summary>
    public StandardMaterial3D Material { get; } = Fx.Unshaded(Color);

    /// <summary>A scenery model to fly instead of the sphere (scaled by <see cref="Size"/>); <c>null</c> = sphere.</summary>
    public string? Model { get; init; }

    /// <summary>How high the bow rises above the straight line at its middle, in units; 0 = straight.</summary>
    public float Arc { get; init; }

    /// <summary>Burns: the projectile trails sparks of its colour.</summary>
    public bool Burning { get; init; }

    /// <summary>A plain tower's shot.</summary>
    public static readonly ProjectileKind Bolt = new("bolt", new Color(1f, 0.9f, 0.4f), 1f, 20f, 1);

    /// <summary>A strong tower's shot ("Feuerpfeil"): bigger, orange, double damage.</summary>
    public static readonly ProjectileKind FireArrow = new("fire-arrow", new Color(1f, 0.45f, 0.1f), 1.6f, 20f, 2);

    /// <summary>A catapult's boulder: slower, flies in a high bow, two damage.</summary>
    public static readonly ProjectileKind Boulder = new("boulder", new Color(0.35f, 0.33f, 0.3f), 1.5f, 13f, 2)
    {
        Model = "projectile_catapult",
        Arc = 3.5f,
    };

    /// <summary>A strong catapult's burning boulder ("Feuerkugel"): three damage.</summary>
    public static readonly ProjectileKind FireBoulder = new("fire-boulder", new Color(1f, 0.5f, 0.15f), 1.7f, 13f, 3)
    {
        Model = "projectile_catapult",
        Arc = 3.5f,
        Burning = true,
    };

    /// <summary>The hero's own shot: small, pale blue, one damage.</summary>
    public static readonly ProjectileKind HeroBolt = new("hero-bolt", new Color(0.55f, 0.85f, 1f), 0.8f, 20f, 1);
}

/// <summary>
/// One kind of tower as data: looks, reach, rate of fire and what it shoots - plain and once it is strong (see
/// <see cref="TowerAmmo.IsStrong"/>). A new tower type (a cannon, say) is a new instance here; maps pick it per spot.
/// </summary>
/// <param name="Name">For the log and for sound/effect lookups.</param>
/// <param name="Models">Scenery models; the towers of a map take them in turn.</param>
/// <param name="ModelScale">Model scale.</param>
/// <param name="Range">Shooting range in units.</param>
/// <param name="ShotInterval">Seconds between two shots.</param>
/// <param name="MuzzleHeight">Where the shots leave the tower.</param>
/// <param name="Shot">The plain projectile.</param>
/// <param name="StrongShot">The projectile of a strong tower.</param>
public sealed record TowerKind(
    string Name,
    string[] Models,
    float ModelScale,
    float Range,
    float ShotInterval,
    float MuzzleHeight,
    ProjectileKind Shot,
    ProjectileKind StrongShot)
{
    /// <summary>How much ground the tower blocks for the hero.</summary>
    public float Footprint { get; init; } = 1.0f;

    /// <summary>What a strong tower of this kind shoots, for the feedback line.</summary>
    public string StrongShotName { get; init; } = "Feuerpfeile";

    /// <summary>A shot leaves with flash, smoke and a recoil of the model (catapult); otherwise it just leaves.</summary>
    public bool Blast { get; init; }

    /// <summary>The prototype's only tower: blue bolts, fire arrows once strong.</summary>
    public static readonly TowerKind Bolt = new("bolt-tower", ["building_tower_A_blue", "building_tower_B_blue"], 1.6f,
        Range: 7.5f, ShotInterval: 0.8f, MuzzleHeight: 3.6f, ProjectileKind.Bolt, ProjectileKind.FireArrow);

    /// <summary>A catapult: slower, longer reach, boulders in a bow with flash and smoke; burning boulders once strong.</summary>
    public static readonly TowerKind Catapult = new("catapult", ["building_tower_catapult_blue"], 1.6f,
        Range: 8.5f, ShotInterval: 1.5f, MuzzleHeight: 3.4f, ProjectileKind.Boulder, ProjectileKind.FireBoulder)
    {
        StrongShotName = "Feuerkugeln",
        Blast = true,
    };

    /// <summary>The towers a map gets, taken in turn over its spots.</summary>
    public static readonly TowerKind[] Mix = [Bolt, Catapult];
}
