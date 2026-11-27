using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Core;
using PuglingTower.Ui;
using PuglingTower.World;

namespace PuglingTower.Words;

/// <summary>A vocabulary book lying on the field.</summary>
public sealed class WordBook
{
    /// <summary>The word it carries.</summary>
    public required TowerToken Data { get; init; }

    /// <summary>Where it lies.</summary>
    public required Vector2 Pos { get; init; }

    /// <summary>Its scene node.</summary>
    public required Node3D Node { get; init; }

    /// <summary>Laid down by the hero: it stays until picked up again instead of expiring.</summary>
    public required bool Placed { get; init; }

    /// <summary>Seconds it has been lying there.</summary>
    public float Age { get; set; }
}

/// <summary>
/// The run's words on the field: a defeated skeleton drops one from the <see cref="TokenPool"/>, an untouched book
/// expires back into the pool, a book the hero lays down stays. Books keep a little distance from each other so words
/// never pile up into one unreadable heap.
/// </summary>
public sealed class WordBooks
{
    private const float Gap = 2.4f;
    private readonly Node3D _parent;
    private readonly LevelDef _def;
    private readonly MapInfo _map;
    private readonly TokenPool _pool;
    private readonly Dictionary<string, TowerToken> _byId = [];
    private readonly List<WordBook> _all = [];

    /// <summary>Takes the run's tokens into the pool.</summary>
    public WordBooks(Node3D parent, LevelDef def, MapInfo map, IReadOnlyList<TowerToken> tokens)
    {
        _parent = parent;
        _def = def;
        _map = map;
        foreach (var t in tokens) _byId[t.TokenId] = t;
        _pool = new TokenPool(tokens.Select(t => t.TokenId));
    }

    /// <summary>Every book on the field.</summary>
    public IReadOnlyList<WordBook> All => _all;

    /// <summary>The run's token bookkeeping (consume on a correct word, return on a wrong one).</summary>
    public TokenPool Pool => _pool;

    /// <summary>Drops a word from the pool beside the road where a skeleton fell; <c>null</c> when the pool is empty.</summary>
    /// <param name="at">The point on the path.</param>
    /// <param name="dir">The path's direction there.</param>
    public WordBook? DropBeside(Vector2 at, Vector2 dir)
    {
        if (_pool.Draw() is not { } id) return null;
        // The book lands beside the road, inside the map, where the hero can reach it.
        var side = dir.Orthogonal() * (GD.Randf() < 0.5f ? 1.6f : -1.6f);
        return Drop(_byId[id], FreeSpot(at + side), placed: false);
    }

    /// <summary>Lays a word down near <paramref name="near"/>; it stays there.</summary>
    public WordBook LayDown(TowerToken token, Vector2 near) => Drop(token, FreeSpot(near), placed: true);

    /// <summary>Takes a book off the field; false when it is already gone.</summary>
    public bool Take(WordBook book)
    {
        if (!_all.Remove(book)) return false;
        book.Node.QueueFree();
        return true;
    }

    /// <summary>Up to <paramref name="max"/> books within <paramref name="radius"/>, nearest first.</summary>
    public List<WordBook> Near(Vector2 from, float radius, int max) =>
        _all.Where(b => b.Pos.DistanceTo(from) <= radius).OrderBy(b => b.Pos.DistanceTo(from)).Take(max).ToList();

    /// <summary>Ages, blinks and bobs the books; expired ones go back to the pool unless the level is over.</summary>
    public void Animate(float dt, float clock, bool frozen)
    {
        for (var i = _all.Count - 1; i >= 0; i--)
        {
            var book = _all[i];
            // Expired books go back to the pool, so the field never fills up with words nobody picks.
            if (!frozen && !book.Placed && (book.Age += dt) >= Balance.TokenLifetime)
            {
                _pool.Return(book.Data.TokenId);
                book.Node.QueueFree();
                _all.RemoveAt(i);
                continue;
            }
            var blink = !book.Placed && book.Age > Balance.TokenLifetime - Balance.TokenBlinkWarning && Mathf.PosMod(clock, 0.5f) < 0.2f;
            book.Node.Visible = !blink;
            book.Node.Position = book.Pos.To3(0.6f + 0.15f * Mathf.Sin(clock * 3 + i));
            book.Node.Rotation = new Vector3(0, clock * 1.2f + i, 0);
        }
    }

    private WordBook Drop(TowerToken data, Vector2 at, bool placed)
    {
        var node = Models.Character("spellbook_closed.gltf").Instantiate<Node3D>();
        node.Scale = Vector3.One * 1.6f;
        _parent.AddChild(node);
        // A white ring marks a word the hero laid down himself: it stays.
        node.AddChild(Fx.Ring(new Vector3(0, -0.3f, 0), 0.55f, placed ? new Color(1, 1, 1, 0.85f) : new Color(Palette.Gold, 0.7f)));
        var book = new WordBook { Data = data, Pos = at, Node = node, Placed = placed };
        _all.Add(book);
        return book;
    }

    /// <summary>
    /// The nearest walkable spot to <paramref name="near"/> that keeps <see cref="Gap"/> to every other book. Searches
    /// outwards in rings.
    /// </summary>
    private Vector2 FreeSpot(Vector2 near)
    {
        var lo = new Vector2(1, 1);
        var hi = _def.Size - new Vector2(1, 1);
        for (var ring = 0; ring <= 6; ring++)
        {
            var steps = ring == 0 ? 1 : ring * 8;
            for (var k = 0; k < steps; k++)
            {
                var angle = Mathf.Tau * k / steps;
                var spot = (near + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 1.1f).Clamp(lo, hi);
                if (_map.Walkable(spot, 0.3f) && _all.All(b => b.Pos.DistanceTo(spot) >= Gap)) return spot;
            }
        }
        return near.Clamp(lo, hi);
    }
}
