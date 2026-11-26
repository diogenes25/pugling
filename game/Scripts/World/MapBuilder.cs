using Godot;

namespace PuglingTower.World;

/// <summary>What the level needs to know about the built map: where the hero may stand.</summary>
public sealed class MapInfo(LevelDef def)
{
    /// <summary>Half the width of the river band that blocks the hero.</summary>
    public const float RiverHalfWidth = 2.1f, BridgeReach = 2.3f;

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

/// <summary>
/// Builds a level's scenery from KayKit hex tiles: grass and river cells, a dirt road along every monster path, the
/// castle, scattered trees and rocks inside, and a dense forest beyond the edge so the following camera never shows
/// the end of the world. Tiles and trees go through <see cref="MultiMesh"/>es (one draw call per mesh kind).
/// </summary>
public static class MapBuilder
{
    // KayKit hexes are pointy-top: 2 units wide, 2.3 deep, so rows sit 0.75 * 2.3 apart.
    private const float HexWidth = 2f, HexRow = 1.725f, Border = 16f;

    private static readonly string[] Trees = ["trees_A_large", "trees_A_medium", "trees_B_large", "trees_B_medium", "tree_single_A", "tree_single_B"];
    private static readonly string[] Rocks = ["rock_single_A", "rock_single_B", "rock_single_C"];
    private static readonly string[] Mountains = ["mountain_A_grass_trees", "mountain_B_grass_trees", "hills_A_trees"];

    /// <summary>Adds the map below <paramref name="parent"/> and returns its walkability.</summary>
    public static MapInfo Build(LevelDef def, Node3D parent)
    {
        var map = new MapInfo(def);
        var placed = new Dictionary<string, List<Transform3D>>();
        void Put(string model, Vector2 at, float scale, float angle, float y = 0) =>
            (placed.TryGetValue(model, out var list) ? list : placed[model] = []).Add(
                new Transform3D(Basis.FromEuler(new Vector3(0, angle, 0)).Scaled(Vector3.One * scale), at.To3(y)));

        var rng = new RandomNumberGenerator { Seed = (ulong)def.Seed };
        var blocked = new List<(Vector2 Pos, float Radius)>(); // scenery keeps clear of these

        // Ground: one hex per cell, water where the river runs.
        for (var row = 0; row * HexRow < def.Size.Y + 2 * Border; row++)
            for (var col = 0; col * HexWidth < def.Size.X + 2 * Border + HexWidth; col++)
            {
                var at = new Vector2(col * HexWidth + (row % 2) * HexWidth / 2 - Border, row * HexRow - Border);
                var water = def.River.Length > 1 && Geo.DistToPolyline(at, def.River) < RiverHalfCells;
                Put(water ? "hex_water" : "hex_grass", at, 1, 0);
                if (water && rng.Randf() < 0.12f && def.Bridges.All(b => b.DistanceTo(at) > 3))
                    Put(rng.Randf() < 0.5f ? "waterlily_A" : "waterplant_A", at, 1, rng.Randf() * Mathf.Tau, -0.2f);
            }

        foreach (var bridge in def.Bridges)
        {
            // The bridge runs along the road that crosses here.
            var dir = def.Paths.Select(p => NearestDir(p, bridge)).OrderBy(d => d.Dist).First().Dir;
            Put("building_bridge_A", bridge, 1.9f, Mathf.Atan2(dir.X, dir.Y));
        }

        AddRoads(def, parent);

        // Castle at the end of the paths, a little camp around it.
        var castle = Models.Scenery("building_castle_blue").Instantiate<Node3D>();
        castle.Position = def.Base.To3();
        castle.Scale = Vector3.One * 1.4f;
        parent.AddChild(castle);
        map.Obstacles.Add((def.Base, 2.0f));
        blocked.Add((def.Base, 4f));
        foreach (var (offset, model) in new[] { (new Vector2(-2.6f, 2.2f), "flag_blue"), (new Vector2(2.7f, 2.0f), "flag_blue"),
                     (new Vector2(3.2f, -1.4f), "barrel"), (new Vector2(-3.1f, -1.2f), "crate_A_big"), (new Vector2(0, 3.4f), "sack") })
            Put(model, def.Base + offset, 1.4f, rng.Randf() * Mathf.Tau);

        foreach (var spot in def.TowerSpots) blocked.Add((spot, 2.8f));
        blocked.Add((def.PlayerStart, 2.5f));

        foreach (var spot in def.Mountains)
        {
            Put(Mountains[rng.RandiRange(0, Mountains.Length - 1)], spot, 2.4f, rng.Randf() * Mathf.Tau);
            map.Obstacles.Add((spot, 2.0f));
            blocked.Add((spot, 3.2f));
        }

        bool Clear(Vector2 at, float roadGap) =>
            def.Paths.All(p => Geo.DistToPolyline(at, p) > roadGap)
            && (def.River.Length < 2 || Geo.DistToPolyline(at, def.River) > RiverHalfWidth + 0.8f)
            && blocked.All(b => b.Pos.DistanceTo(at) > b.Radius);

        // Inside: loose trees and rocks the hero walks around.
        var inside = (int)(def.Size.X * def.Size.Y / 16);
        for (var i = 0; i < inside; i++)
        {
            var at = new Vector2(rng.RandfRange(1, def.Size.X - 1), rng.RandfRange(1, def.Size.Y - 1));
            if (!Clear(at, 2.6f)) continue;
            var rock = rng.Randf() < 0.25f;
            var scale = rng.RandfRange(0.9f, 1.3f);
            Put(rock ? Rocks[rng.RandiRange(0, Rocks.Length - 1)] : Trees[rng.RandiRange(0, Trees.Length - 1)], at, scale, rng.Randf() * Mathf.Tau);
            var radius = (rock ? 0.45f : 0.7f) * scale;
            map.Obstacles.Add((at, radius));
            blocked.Add((at, 1.7f));
        }

        // Beyond the edge: a dense forest with the odd mountain, so the world does not end on screen.
        for (var z = -Border; z < def.Size.Y + Border; z += 1.6f)
            for (var x = -Border; x < def.Size.X + Border; x += 1.6f)
            {
                var at = new Vector2(x + rng.RandfRange(-0.5f, 0.5f), z + rng.RandfRange(-0.5f, 0.5f));
                var outside = at.X < -0.5f || at.Y < -0.5f || at.X > def.Size.X + 0.5f || at.Y > def.Size.Y + 0.5f;
                if (!outside || !Clear(at, 2.4f)) continue;
                var roll = rng.Randf();
                if (roll < 0.04f) Put(Mountains[rng.RandiRange(0, Mountains.Length - 1)], at, rng.RandfRange(2f, 3f), rng.Randf() * Mathf.Tau);
                else if (roll < 0.62f) Put(Trees[rng.RandiRange(0, Trees.Length - 1)], at, rng.RandfRange(1f, 1.5f), rng.Randf() * Mathf.Tau);
                else if (roll < 0.68f) Put(Rocks[rng.RandiRange(0, Rocks.Length - 1)], at, 1.2f, rng.Randf() * Mathf.Tau);
            }

        foreach (var (model, transforms) in placed) AddMulti(parent, model, transforms);
        return map;
    }

    // Cells whose centre is this close to the river line become water (a little wider than the blocking band).
    private const float RiverHalfCells = 2.2f;
    private const float RiverHalfWidth = MapInfo.RiverHalfWidth;

    private static (float Dist, Vector2 Dir) NearestDir(Vector2[] path, Vector2 p)
    {
        var best = (Dist: float.PositiveInfinity, Dir: Vector2.Right);
        for (var i = 1; i < path.Length; i++)
        {
            var d = Geo.DistToSegment(p, path[i - 1], path[i]);
            if (d < best.Dist) best = (d, (path[i] - path[i - 1]).Normalized());
        }
        return best;
    }

    private static void AddMulti(Node3D parent, string model, List<Transform3D> transforms)
    {
        foreach (var (mesh, local) in Models.Parts(model))
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, InstanceCount = transforms.Count };
            for (var i = 0; i < transforms.Count; i++) multi.SetInstanceTransform(i, transforms[i] * local);
            var instance = new MultiMeshInstance3D { Multimesh = multi };
            // KayKit's grass is a light lime that reads as yellow under a bright sun; tint it towards green.
            if (model == "hex_grass" && mesh.SurfaceGetMaterial(0) is StandardMaterial3D grass)
            {
                var tinted = (StandardMaterial3D)grass.Duplicate();
                tinted.AlbedoColor = new Color(0.6f, 0.85f, 0.55f);
                instance.MaterialOverride = tinted;
            }
            parent.AddChild(instance);
        }
    }

    /// <summary>Dirt roads along the paths: a dark rim below a lighter track, round patches at the bends.</summary>
    private static void AddRoads(LevelDef def, Node3D parent)
    {
        var rim = new StandardMaterial3D { AlbedoColor = new Color(0.42f, 0.31f, 0.2f), Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        var track = new StandardMaterial3D { AlbedoColor = new Color(0.78f, 0.62f, 0.42f), Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        foreach (var path in def.Paths)
        {
            Ribbon(parent, path, 2.7f, 0.02f, rim);
            Ribbon(parent, path, 2.1f, 0.04f, track);
        }
    }

    private static void Ribbon(Node3D parent, Vector2[] path, float width, float y, Material material)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetNormal(Vector3.Up);
        for (var i = 1; i < path.Length; i++)
        {
            var a = path[i - 1];
            var b = path[i];
            var side = (b - a).Normalized().Orthogonal() * width / 2;
            Vector3[] quad = [(a - side).To3(y), (a + side).To3(y), (b + side).To3(y), (b - side).To3(y)];
            // Godot treats clockwise triangles as front faces; seen from above this order is clockwise.
            foreach (var k in new[] { 0, 1, 2, 0, 2, 3 }) st.AddVertex(quad[k]);
        }
        var mesh = st.Commit();
        parent.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });

        var disc = new CylinderMesh { TopRadius = width / 2, BottomRadius = width / 2, Height = 0.02f, RadialSegments = 24 };
        foreach (var corner in path)
            parent.AddChild(new MeshInstance3D
            {
                Mesh = disc, MaterialOverride = material, Position = corner.To3(y - 0.005f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
    }
}
