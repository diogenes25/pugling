using Godot;

namespace PuglingTower.World;

/// <summary>Small 3D building blocks shared by the level's parts: flat rings, flat-coloured materials, spark bursts.</summary>
public static class Fx
{
    /// <summary>A flat colour that ignores the light.</summary>
    public static StandardMaterial3D Unshaded(Color color) =>
        new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = color };

    /// <summary>A thin translucent ring lying on the ground (tower range, pick-up reach, book markers).</summary>
    public static MeshInstance3D Ring(Vector3 at, float radius, Color color) => new()
    {
        Mesh = new TorusMesh { InnerRadius = radius - 0.12f, OuterRadius = radius, Rings = 48, RingSegments = 4 },
        Position = at,
        Scale = new Vector3(1, 0.05f, 1),
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        },
    };

    /// <summary>A one-shot burst of little sparks below <paramref name="parent"/>: hits, rewards, mistakes.</summary>
    public static void Burst(Node3D parent, Vector3 at, Color color, int amount, float size = 1f)
    {
        var particles = new CpuParticles3D
        {
            Emitting = true, OneShot = true, Explosiveness = 0.95f, Amount = amount, Lifetime = 0.9,
            Mesh = new SphereMesh { Radius = 0.09f * size, Height = 0.18f * size, RadialSegments = 6, Rings = 3 },
            MaterialOverride = Unshaded(color),
            Direction = Vector3.Up, Spread = 70, InitialVelocityMin = 3 * size, InitialVelocityMax = 7 * size,
            Gravity = new Vector3(0, -12, 0), ScaleAmountMin = 0.6f, ScaleAmountMax = 1.4f,
            Position = at, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(particles);
        parent.GetTree().CreateTimer(1.3).Timeout += particles.QueueFree;
    }
}
