using Godot;
using PuglingTower.Core;
using PuglingTower.World;

namespace PuglingTower.Combat;

/// <summary>One skeleton on its way to the castle.</summary>
public sealed class Enemy
{
    /// <summary>What it is.</summary>
    public required EnemyKind Kind { get; init; }

    /// <summary>Index into the map's paths.</summary>
    public required int Path { get; init; }

    /// <summary>Walking speed in units per second.</summary>
    public required float Speed { get; init; }

    /// <summary>Its scene node.</summary>
    public required Node3D Node { get; init; }

    /// <summary>The model's animator, if it has one.</summary>
    public AnimationPlayer? Anim { get; init; }

    /// <summary>Distance walked along its path.</summary>
    public float Distance { get; set; }

    /// <summary>Distance left to the castle; towers aim at the one closest to it.</summary>
    public float Remaining { get; set; }

    /// <summary>Hit points left.</summary>
    public int Hp { get; set; }

    /// <summary>Defeated and playing its death; no longer a target.</summary>
    public bool Dying { get; set; }

    /// <summary>Seconds of the death clip left before the node goes.</summary>
    public float DyingTime { get; set; }

    /// <summary>Where it is on the ground.</summary>
    public Vector2 Ground => Node.Position.Flat();
}

/// <summary>The skeletons of a level: spawning by the <see cref="SpawnSchedule"/>, walking their paths, dying.</summary>
public sealed class Enemies(Node3D parent, LevelDef def)
{
    private const float DeathSeconds = 1.3f;
    private readonly SpawnSchedule _schedule = new(def.SpawnInterval, def.MonsterSpeed, def.Paths.Length);
    private readonly float[] _pathLengths = def.Paths.Select(Geo.Length).ToArray();
    private readonly List<Enemy> _all = [];

    /// <summary>Every skeleton on the map, dying ones included.</summary>
    public IReadOnlyList<Enemy> All => _all;

    /// <summary>Spawns the next skeleton when its time has come; returns it, or <c>null</c>.</summary>
    public Enemy? Spawn(float dt, float elapsed)
    {
        if (_schedule.Tick(dt, elapsed) is not { } spawn) return null;
        var node = Models.Character(spawn.Kind.Model).Instantiate<Node3D>();
        node.Scale = Vector3.One * spawn.Kind.Scale;
        Models.KeepOnly(node, spawn.Kind.MeshPrefix);
        var enemy = new Enemy
        {
            Kind = spawn.Kind, Path = spawn.Path, Speed = spawn.Speed, Node = node, Anim = Models.Animator(node),
            Hp = spawn.Kind.Hp,
        };
        parent.AddChild(node);
        Models.Play(enemy.Anim, true, "Walking_A", "Walking_B", "Running_A");
        _all.Add(enemy);
        Place(enemy);
        return enemy;
    }

    /// <summary>Walks every skeleton one frame and clears the dead; returns how many reached the castle.</summary>
    public int Move(float dt)
    {
        var arrived = 0;
        for (var i = _all.Count - 1; i >= 0; i--)
        {
            var e = _all[i];
            if (e.Dying)
            {
                if ((e.DyingTime -= dt) > 0) continue;
                e.Node.QueueFree();
                _all.RemoveAt(i);
                continue;
            }
            e.Distance += e.Speed * dt;
            if (e.Distance < _pathLengths[e.Path])
            {
                Place(e);
                continue;
            }
            e.Node.QueueFree();
            _all.RemoveAt(i);
            arrived++;
        }
        return arrived;
    }

    /// <summary>Starts a skeleton's death; it stays a moment for the clip and is no target any more.</summary>
    public static void Defeat(Enemy e)
    {
        e.Dying = true;
        e.DyingTime = DeathSeconds;
        Models.Play(e.Anim, false, "Death_A", "Death_B");
    }

    /// <summary>Where on its path a skeleton is and which way the path runs there.</summary>
    public (Vector2 Pos, Vector2 Dir) PathPoint(Enemy e) => Geo.PointAt(def.Paths[e.Path], e.Distance);

    /// <summary>The living skeleton in reach that is closest to the castle (a tower's choice).</summary>
    public Enemy? Foremost(Vector2 from, float range) =>
        _all.Where(e => !e.Dying && e.Ground.DistanceTo(from) <= range).OrderBy(e => e.Remaining).FirstOrDefault();

    /// <summary>The living skeleton in reach that is closest to <paramref name="from"/> (the hero's choice).</summary>
    public Enemy? Nearest(Vector2 from, float range) =>
        _all.Where(e => !e.Dying && e.Ground.DistanceTo(from) <= range).OrderBy(e => e.Ground.DistanceTo(from)).FirstOrDefault();

    private void Place(Enemy e)
    {
        var (pos, dir) = PathPoint(e);
        e.Node.Position = pos.To3(0.05f);
        e.Node.Rotation = new Vector3(0, Mathf.Atan2(dir.X, dir.Y), 0);
        e.Remaining = _pathLengths[e.Path] - e.Distance;
    }
}
