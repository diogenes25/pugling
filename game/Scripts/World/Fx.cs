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
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        },
    };

    /// <summary>Glowing sparks that trail behind a burning projectile; a child of it, so they die with it.</summary>
    public static CpuParticles3D Trail(Color color) => new()
    {
        Emitting = true,
        Amount = 24,
        Lifetime = 0.45,
        LocalCoords = false,
        Mesh = new SphereMesh { Radius = 0.12f, Height = 0.24f, RadialSegments = 6, Rings = 3 },
        MaterialOverride = Unshaded(color),
        Direction = Vector3.Up,
        Spread = 180,
        InitialVelocityMin = 0.3f,
        InitialVelocityMax = 1.2f,
        Gravity = Vector3.Zero,
        ScaleAmountMin = 0.4f,
        ScaleAmountMax = 1.1f,
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
    };

    /// <summary>A catapult's launch: a short fiery flash and a puff of grey smoke drifting upwards.</summary>
    public static void Blast(Node3D parent, Vector3 at)
    {
        Burst(parent, at, new Color(1f, 0.75f, 0.2f), 18, 0.7f);
        Burst(parent, at, new Color(1f, 0.35f, 0.1f), 10, 0.5f);
        var smoke = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Explosiveness = 0.85f,
            Amount = 14,
            Lifetime = 1.6,
            Mesh = new SphereMesh { Radius = 0.35f, Height = 0.7f, RadialSegments = 8, Rings = 4 },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                VertexColorUseAsAlbedo = true,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
            ColorRamp = new Gradient { Colors = [new Color(0.75f, 0.73f, 0.7f, 0.8f), new Color(0.55f, 0.55f, 0.55f, 0f)] },
            Direction = Vector3.Up,
            Spread = 35,
            InitialVelocityMin = 0.8f,
            InitialVelocityMax = 2.0f,
            Gravity = new Vector3(0, 0.6f, 0),
            ScaleAmountMin = 0.7f,
            ScaleAmountMax = 1.6f,
            Position = at,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(smoke);
        parent.GetTree().CreateTimer(2.0).Timeout += smoke.QueueFree;
    }

    /// <summary>A short squash of a model: the tower jolts when it fires.</summary>
    public static void Recoil(Node3D model)
    {
        var rest = model.Scale;
        var tween = model.CreateTween();
        tween.TweenProperty(model, "scale", new Vector3(rest.X * 1.06f, rest.Y * 0.9f, rest.Z * 1.06f), 0.06);
        tween.TweenProperty(model, "scale", rest, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    /// <summary>A one-shot burst of little sparks below <paramref name="parent"/>: hits, rewards, mistakes.</summary>
    public static void Burst(Node3D parent, Vector3 at, Color color, int amount, float size = 1f)
    {
        var particles = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Explosiveness = 0.95f,
            Amount = amount,
            Lifetime = 0.9,
            Mesh = new SphereMesh { Radius = 0.09f * size, Height = 0.18f * size, RadialSegments = 6, Rings = 3 },
            MaterialOverride = Unshaded(color),
            Direction = Vector3.Up,
            Spread = 70,
            InitialVelocityMin = 3 * size,
            InitialVelocityMax = 7 * size,
            Gravity = new Vector3(0, -12, 0),
            ScaleAmountMin = 0.6f,
            ScaleAmountMax = 1.4f,
            Position = at,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(particles);
        parent.GetTree().CreateTimer(1.3).Timeout += particles.QueueFree;
    }
}
