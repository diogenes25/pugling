using Godot;

namespace PuglingTower.Combat;

/// <summary>Who fired a projectile.</summary>
public enum Shooter
{
    /// <summary>A tower.</summary>
    Tower,

    /// <summary>The hero himself.</summary>
    Hero,
}

/// <summary>A projectile on its way to its target.</summary>
public sealed class Projectile
{
    /// <summary>What it is.</summary>
    public required ProjectileKind Kind { get; init; }

    /// <summary>Who fired it.</summary>
    public required Shooter From { get; init; }

    /// <summary>The skeleton it homes in on.</summary>
    public required Enemy Target { get; init; }

    /// <summary>Its scene node.</summary>
    public required MeshInstance3D Node { get; init; }
}

/// <summary>A projectile that reached its living target.</summary>
/// <param name="Projectile">The projectile.</param>
/// <param name="At">Where it hit.</param>
/// <param name="Killed">The hit took the skeleton's last hit point; it is already dying.</param>
public readonly record struct Hit(Projectile Projectile, Vector3 At, bool Killed);

/// <summary>Homing projectiles: fired at a skeleton, they fly until they reach it and then deal their damage.</summary>
public sealed class Projectiles(Node3D parent)
{
    private static readonly SphereMesh Sphere = new() { Radius = 0.16f, Height = 0.32f, RadialSegments = 8, Rings = 4 };
    private readonly List<Projectile> _flying = [];

    /// <summary>Fires one projectile from <paramref name="from"/> at <paramref name="target"/>.</summary>
    public Projectile Fire(ProjectileKind kind, Shooter shooter, Vector3 from, Enemy target)
    {
        var node = new MeshInstance3D
        {
            Mesh = Sphere, MaterialOverride = kind.Material, Position = from, Scale = Vector3.One * kind.Size,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(node);
        var projectile = new Projectile { Kind = kind, From = shooter, Target = target, Node = node };
        _flying.Add(projectile);
        return projectile;
    }

    /// <summary>Moves every projectile one frame and deals the damage of those that arrive; returns their hits.</summary>
    public List<Hit> Move(float dt)
    {
        var hits = new List<Hit>();
        for (var i = _flying.Count - 1; i >= 0; i--)
        {
            var p = _flying[i];
            // The skeleton may be gone already (freed after its death); the projectile then just fizzles where it is.
            var alive = GodotObject.IsInstanceValid(p.Target.Node) && !p.Target.Dying;
            var dest = alive ? p.Target.Node.Position + new Vector3(0, 1f, 0) : p.Node.Position;
            var to = dest - p.Node.Position;
            var step = p.Kind.Speed * dt;
            if (to.Length() > step)
            {
                p.Node.Position += to.Normalized() * step;
                continue;
            }
            p.Node.QueueFree();
            _flying.RemoveAt(i);
            if (!alive) continue;
            var killed = (p.Target.Hp -= p.Kind.Damage) <= 0;
            // Dying right away: a second projectile arriving in the same frame must fizzle, not kill twice.
            if (killed) Enemies.Defeat(p.Target);
            hits.Add(new Hit(p, dest, killed));
        }
        return hits;
    }
}
