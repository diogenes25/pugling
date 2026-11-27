namespace PuglingTower.Core;

/// <summary>
/// One kind of enemy as data. A new enemy is a new instance here plus a rule in <see cref="SpawnSchedule"/> for when it
/// comes; the scene side only reads these fields.
/// </summary>
/// <param name="Model">Model file in the character packs (see <c>World.Models.Character</c>).</param>
/// <param name="MeshPrefix">Only the model's meshes with this prefix stay visible (the packs carry spare weapons).</param>
/// <param name="Scale">Model scale.</param>
/// <param name="Hp">Hit points.</param>
/// <param name="SpeedFactor">Multiplies the map's skeleton speed.</param>
public sealed record EnemyKind(string Model, string MeshPrefix, float Scale, int Hp, float SpeedFactor)
{
    /// <summary>The common skeleton: quick and weak.</summary>
    public static readonly EnemyKind Minion = new("Skeleton_Minion.glb", "Skeleton_Minion_", 0.72f, 2, 1f);

    /// <summary>The armoured skeleton: slower, takes twice the hits.</summary>
    public static readonly EnemyKind Warrior = new("Skeleton_Warrior.glb", "Skeleton_Warrior_", 0.85f, 4, 0.8f);
}
