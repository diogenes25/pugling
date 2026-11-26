using Godot;

namespace PuglingTower.World;

/// <summary>Plane geometry on the ground (x, z as <see cref="Vector2"/>) shared by the map builder and the level.</summary>
public static class Geo
{
    /// <summary>Lifts a ground point to 3D at height <paramref name="y"/>.</summary>
    public static Vector3 To3(this Vector2 p, float y = 0) => new(p.X, y, p.Y);

    /// <summary>Drops the height of a 3D point.</summary>
    public static Vector2 Flat(this Vector3 p) => new(p.X, p.Z);

    /// <summary>Distance from <paramref name="p"/> to the segment a–b.</summary>
    public static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = ab.LengthSquared() < 1e-6f ? 0 : Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0, 1);
        return p.DistanceTo(a + ab * t);
    }

    /// <summary>Distance from <paramref name="p"/> to a polyline; infinite for fewer than two points.</summary>
    public static float DistToPolyline(Vector2 p, Vector2[] line)
    {
        var best = float.PositiveInfinity;
        for (var i = 1; i < line.Length; i++) best = Mathf.Min(best, DistToSegment(p, line[i - 1], line[i]));
        return best;
    }

    /// <summary>Total length of a polyline.</summary>
    public static float Length(Vector2[] line)
    {
        var sum = 0f;
        for (var i = 1; i < line.Length; i++) sum += line[i - 1].DistanceTo(line[i]);
        return sum;
    }

    /// <summary>The point <paramref name="distance"/> along a polyline and the direction of travel there.</summary>
    public static (Vector2 Pos, Vector2 Dir) PointAt(Vector2[] line, float distance)
    {
        for (var i = 1; i < line.Length; i++)
        {
            var segment = line[i - 1].DistanceTo(line[i]);
            var dir = (line[i] - line[i - 1]).Normalized();
            if (distance <= segment) return (line[i - 1] + dir * distance, dir);
            distance -= segment;
        }
        return (line[^1], (line[^1] - line[^2]).Normalized());
    }
}
