using Godot;

namespace PuglingTower.World;

/// <summary>What the level needs to know about the built map: where the hero may stand.</summary>
public sealed class MapInfo(LevelDef def)
{
    /// <summary>Half the width of the river band that blocks the hero.</summary>
    public const float RiverHalfWidth = 2.1f;

    /// <summary>Around a bridge point the river is walkable.</summary>
    public const float BridgeReach = 2.3f;

    /// <summary>The level the map was built from.</summary>
    public LevelDef Def { get; } = def;

    /// <summary>Round blockers on the ground: towers, the castle, trees, rocks, mountains.</summary>
    public List<(Vector2 Pos, float Radius)> Obstacles { get; } = [];

    /// <summary>River water without a bridge.</summary>
    public bool IsWater(Vector2 p) =>
        Def.River.Length > 1 && Geo.DistToPolyline(p, Def.River) < RiverHalfWidth
        && !Def.Bridges.Any(b => b.DistanceTo(p) < BridgeReach);

    /// <summary>Whether a body of <paramref name="radius"/> may stand at <paramref name="p"/>.</summary>
    public bool Walkable(Vector2 p, float radius) =>
        p.X >= radius && p.Y >= radius && p.X <= Def.Size.X - radius && p.Y <= Def.Size.Y - radius
        && !IsWater(p) && !Obstacles.Any(o => o.Pos.DistanceTo(p) < o.Radius + radius);
}
