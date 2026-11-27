using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Core;
using PuglingTower.World;

namespace PuglingTower.Combat;

/// <summary>A tower on the map: it asks for one meaning at a time and shoots while it has ammo.</summary>
public sealed class Tower
{
    /// <summary>What kind of tower it is.</summary>
    public required TowerKind Kind { get; init; }

    /// <summary>Where it stands.</summary>
    public required Vector2 Pos { get; init; }

    /// <summary>The meaning it asks for now; <c>null</c> once the run has no more.</summary>
    public TowerTarget? Target { get; set; }

    /// <summary>Its shots.</summary>
    public TowerAmmo Ammo { get; } = new();

    /// <summary>Its reload timer.</summary>
    public Cooldown Reload { get; } = new();

    /// <summary>Its scene model (for recoil and effects).</summary>
    public required Node3D Model { get; init; }
}

/// <summary>A shot a tower just fired.</summary>
/// <param name="Tower">The tower.</param>
/// <param name="Projectile">What it fired.</param>
public readonly record struct TowerShot(Tower Tower, Projectile Projectile);

/// <summary>The towers of a level: built on the map's spots, each one shooting the foremost skeleton in reach.</summary>
public sealed class Towers
{
    private readonly List<Tower> _all = [];

    /// <summary>Builds a tower on every spot of the map, the kinds taken in turn; each gets the next target.</summary>
    public Towers(Node3D parent, LevelDef def, MapInfo map, TowerKind[] kinds, Func<TowerTarget?> nextTarget)
    {
        for (var i = 0; i < def.TowerSpots.Length; i++)
        {
            var spot = def.TowerSpots[i];
            var kind = kinds[i % kinds.Length];
            var model = Models.Scenery(kind.Models[i % kind.Models.Length]).Instantiate<Node3D>();
            model.Position = spot.To3();
            model.Scale = Vector3.One * kind.ModelScale;
            parent.AddChild(model);
            parent.AddChild(Fx.Ring(spot.To3(0.06f), kind.Range, new Color(1, 1, 1, 0.13f)));
            map.Obstacles.Add((spot, kind.Footprint));
            _all.Add(new Tower { Kind = kind, Pos = spot, Target = nextTarget(), Model = model });
        }
    }

    /// <summary>All towers of the level.</summary>
    public IReadOnlyList<Tower> All => _all;

    /// <summary>Lets every loaded tower shoot once its reload is over; returns the shots fired.</summary>
    public List<TowerShot> Shoot(float dt, Enemies enemies, Projectiles projectiles)
    {
        var shots = new List<TowerShot>();
        foreach (var tower in _all)
        {
            if (!tower.Reload.Tick(dt) || tower.Ammo.Count <= 0) continue;
            if (enemies.Foremost(tower.Pos, tower.Kind.Range) is not { } target) continue;
            tower.Reload.Start(tower.Kind.ShotInterval);
            var kind = tower.Ammo.Use() ? tower.Kind.StrongShot : tower.Kind.Shot;
            shots.Add(new TowerShot(tower, projectiles.Fire(kind, Shooter.Tower, tower.Pos.To3(tower.Kind.MuzzleHeight), target)));
        }
        return shots;
    }

    /// <summary>The nearest tower within <paramref name="reach"/> that still asks for a word.</summary>
    public Tower? NearestAsking(Vector2 from, float reach) =>
        _all.Where(t => t.Target is not null && t.Pos.DistanceTo(from) <= reach).OrderBy(t => t.Pos.DistanceTo(from)).FirstOrDefault();
}
