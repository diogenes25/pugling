using Godot;

namespace PuglingTower.World;

/// <summary>
/// Access to the KayKit models (CC0, game/assets/kaykit). Static scenery is flattened into its meshes so the map can
/// draw hundreds of tiles and trees through a few <see cref="MultiMesh"/>es; animated characters are instanced whole.
/// </summary>
public static class Models
{
    private const string Hex = "res://assets/kaykit/hex/", Chars = "res://assets/kaykit/chars/";
    private static readonly Dictionary<string, PackedScene> Scenes = [];
    private static readonly Dictionary<string, List<(Mesh Mesh, Transform3D Local)>> PartCache = [];

    /// <summary>A scenery model by file name without extension (e.g. <c>hex_grass</c>).</summary>
    public static PackedScene Scenery(string name) => Load(Hex + name + ".gltf");

    /// <summary>A character or prop model from the character packs (e.g. <c>Knight.glb</c>, <c>arrow.gltf</c>).</summary>
    public static PackedScene Character(string file) => Load(Chars + file);

    private static PackedScene Load(string path)
    {
        if (!Scenes.TryGetValue(path, out var scene)) Scenes[path] = scene = GD.Load<PackedScene>(path);
        return scene;
    }

    /// <summary>The meshes of a scenery model with their transforms relative to the model's root.</summary>
    public static List<(Mesh Mesh, Transform3D Local)> Parts(string name)
    {
        if (PartCache.TryGetValue(name, out var cached)) return cached;
        var parts = new List<(Mesh, Transform3D)>();
        var root = Scenery(name).Instantiate<Node3D>();
        Collect(root, Transform3D.Identity, parts);
        root.Free();
        return PartCache[name] = parts;
    }

    private static void Collect(Node node, Transform3D parent, List<(Mesh, Transform3D)> parts)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is not Node3D n3) continue;
            var xf = parent * n3.Transform;
            if (n3 is MeshInstance3D { Mesh: { } mesh }) parts.Add((mesh, xf));
            Collect(n3, xf, parts);
        }
    }

    /// <summary>The first <see cref="AnimationPlayer"/> below a character instance.</summary>
    public static AnimationPlayer? Animator(Node root) =>
        root.FindChildren("*", nameof(AnimationPlayer), recursive: true, owned: false).OfType<AnimationPlayer>().FirstOrDefault();

    /// <summary>Plays the first animation of <paramref name="names"/> the player has; loops it when asked.</summary>
    public static void Play(AnimationPlayer? animator, bool loop, params string[] names)
    {
        if (animator is null) return;
        foreach (var name in names)
        {
            if (!animator.HasAnimation(name)) continue;
            if (loop) animator.GetAnimation(name).LoopMode = Animation.LoopModeEnum.Linear;
            if (animator.CurrentAnimation != name) animator.Play(name, customBlend: 0.15);
            return;
        }
    }

    /// <summary>
    /// Hides a character's extra meshes (weapons, spare shields). An entry ending in <c>_</c> keeps every mesh with that
    /// prefix, any other entry exactly one mesh.
    /// </summary>
    public static void KeepOnly(Node root, params string[] keep)
    {
        foreach (var mesh in root.FindChildren("*", nameof(MeshInstance3D), recursive: true, owned: false).OfType<MeshInstance3D>())
        {
            var name = mesh.Name.ToString();
            mesh.Visible = keep.Any(k => k.EndsWith('_') ? name.StartsWith(k, StringComparison.Ordinal) : name == k);
        }
    }
}
