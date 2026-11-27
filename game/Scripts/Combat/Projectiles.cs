using Godot;
using PuglingTower.World;

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
    public required Node3D Node { get; init; }

    /// <summary>Where it is on the straight line to the target; the node floats above it by the bow.</summary>
    public Vector3 Track { get; set; }

    /// <summary>Distance flown so far.</summary>
    public float Flown { get; set; }
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
        Node3D node = kind.Model is { } model
            ? Models.Scenery(model).Instantiate<Node3D>()
            : new MeshInstance3D { Mesh = Sphere, MaterialOverride = kind.Material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        node.Position = from;
        node.Scale = Vector3.One * kind.Size;
        parent.AddChild(node);
        if (kind.Burning) node.AddChild(Fx.Trail(kind.Color));
        var projectile = new Projectile { Kind = kind, From = shooter, Target = target, Node = node, Track = from };
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
            var dest = alive ? p.Target.Node.Position + new Vector3(0, 1f, 0) : p.Track;
            var to = dest - p.Track;
            var step = p.Kind.Speed * dt;
            if (to.Length() > step)
            {
                p.Track += to.Normalized() * step;
                p.Flown += step;
                // The bow: highest halfway between launch and target, measured on what is still to fly.
                var share = p.Flown / (p.Flown + to.Length());
                p.Node.Position = p.Track + Vector3.Up * (p.Kind.Arc * 4 * share * (1 - share));
                if (p.Kind.Arc > 0) p.Node.RotateX(6 * dt);
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
