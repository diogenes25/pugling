using Godot;

namespace PuglingTower.World;

/// <summary>
/// One playable map. Coordinates are world units on the ground plane: <c>X</c> is the world X axis, <c>Y</c> the world
/// Z axis (south). The playable area runs from (0, 0) to <see cref="Size"/>; the map is drawn beyond it so the camera,
/// which never stops following the player, always shows land. Every path ends at the base.
/// </summary>
/// <param name="Name">Shown in the level picker.</param>
/// <param name="Description">One line in the level picker: what makes this map different.</param>
/// <param name="Size">The walkable area.</param>
/// <param name="Paths">Monster routes, each ending at the base; several paths take turns spawning.</param>
/// <param name="TowerSpots">Fixed tower places. Spread out on purpose: which tower to feed first is the walking decision.</param>
/// <param name="PlayerStart">Where the hero stands at the start.</param>
/// <param name="River">Centre line of a river (empty for none); water cells block the hero.</param>
/// <param name="Bridges">Points where a path crosses the river; the cell stays walkable and gets a bridge.</param>
/// <param name="Mountains">Spots for big scenery (mountains, hills) that block the hero.</param>
/// <param name="Seed">Fixes the scattered trees and rocks, so a level looks the same every time.</param>
/// <param name="MonsterSpeed">Start speed in units per second.</param>
/// <param name="SpawnInterval">Start seconds between two monsters.</param>
public sealed record LevelDef(
    string Name,
    string Description,
    Vector2 Size,
    Vector2[][] Paths,
    Vector2[] TowerSpots,
    Vector2 PlayerStart,
    Vector2[] River,
    Vector2[] Bridges,
    Vector2[] Mountains,
    int Seed,
    float MonsterSpeed,
    float SpawnInterval)
{
    /// <summary>The base sits where the paths end.</summary>
    public Vector2 Base => Paths[0][^1];

    /// <summary>The three maps of the prototype, from easy to hard.</summary>
    public static readonly LevelDef[] All =
    [
        new("Lichtung",
            "Ein langer Weg, drei Türme nah beieinander – zum Eingewöhnen.",
            new Vector2(36, 26),
            [[new(40, 4), new(25, 4), new(25, 12), new(9, 12), new(9, 21), new(29, 21)]],
            [new(17, 7.5f), new(16, 16.5f), new(31, 13)],
            new Vector2(19, 16),
            [], [],
            [new(3, 3), new(33, 24.5f)],
            Seed: 11, MonsterSpeed: 1.6f, SpawnInterval: 5f),

        new("Flussfurt",
            "Ein Fluss teilt die Karte. Nur über die Brücken kommst du auf die andere Seite.",
            new Vector2(50, 34),
            [[new(54, 6), new(14, 6), new(14, 17), new(39, 17), new(39, 28), new(8, 28)]],
            [new(45, 11.5f), new(31, 11.5f), new(18, 11.5f), new(20, 23), new(33, 23)],
            new Vector2(33, 14.5f),
            [new(24.5f, -16), new(23.5f, 6), new(25.5f, 17), new(26.5f, 28), new(25.5f, 50)],
            [new(23.5f, 6), new(25.5f, 17), new(26.5f, 28)],
            [new(4, 3), new(46, 31), new(4, 20)],
            Seed: 23, MonsterSpeed: 1.8f, SpawnInterval: 4.5f),

        new("Burgberg",
            "Zwei Eingänge, ein Weg zur Burg. Sechs Türme wollen versorgt werden.",
            new Vector2(54, 40),
            [
                [new(12, -4), new(12, 10), new(30, 10), new(30, 30), new(13, 30)],
                [new(58, 24), new(42, 24), new(42, 14), new(30, 14), new(30, 30), new(13, 30)],
            ],
            [new(19.5f, 5.5f), new(21, 16), new(36, 7.5f), new(36, 20), new(48, 19), new(22, 25)],
            new Vector2(36, 27),
            [], [],
            [new(5, 36), new(47, 4), new(50, 35), new(4, 20)],
            Seed: 37, MonsterSpeed: 1.9f, SpawnInterval: 4.2f),
    ];
}
