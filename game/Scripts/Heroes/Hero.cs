using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Core;
using PuglingTower.World;

namespace PuglingTower.Heroes;

/// <summary>
/// The player's figure: walks the map around obstacles, carries one word at a time (shown as a book above his head) and
/// reloads his own shot. What he looks like is the <see cref="IHeroLook"/>'s business.
/// </summary>
public sealed class Hero
{
    private readonly MapInfo _map;
    private readonly Node3D _book;
    private float _yaw;

    /// <summary>Builds the hero below <paramref name="parent"/> at <paramref name="start"/>.</summary>
    public Hero(Node3D parent, MapInfo map, IHeroLook look, Vector2 start)
    {
        _map = map;
        Look = look;
        Position = start;
        Node = new Node3D();
        parent.AddChild(Node);
        Node.AddChild(look.Root);
        Node.AddChild(Fx.Ring(new Vector3(0, 0.07f, 0), Balance.PickupRadius, new Color(1f, 0.9f, 0.4f, 0.35f)));
        _book = Models.Character("spellbook_closed.gltf").Instantiate<Node3D>();
        _book.Scale = Vector3.One * 1.4f;
        _book.Visible = false;
        Node.AddChild(_book);
        Place(0);
    }

    /// <summary>The hero's node; the camera follows it.</summary>
    public Node3D Node { get; }

    /// <summary>The hero's appearance.</summary>
    public IHeroLook Look { get; }

    /// <summary>Where he stands on the ground.</summary>
    public Vector2 Position { get; private set; }

    /// <summary>The word in his hands, if any.</summary>
    public TowerToken? Carried { get; private set; }

    /// <summary>Reloads his own shot.</summary>
    public Cooldown Reload { get; } = new();

    /// <summary>Walks one frame along <paramref name="move"/> (screen up = world -Z), sliding along blockers.</summary>
    /// <param name="dt">Seconds since the last frame.</param>
    /// <param name="move">Joystick or keyboard direction, length at most 1.</param>
    /// <param name="clock">Seconds since the level started; bobs the carried book.</param>
    public void Move(float dt, Vector2 move, float clock)
    {
        var moving = move.Length() > 0.05f;
        if (moving)
        {
            var step = move * Balance.HeroSpeed * dt;
            var pos = Position;
            if (_map.Walkable(pos + step, Balance.HeroRadius)) pos += step;
            else if (_map.Walkable(pos + new Vector2(step.X, 0), Balance.HeroRadius)) pos.X += step.X;
            else if (_map.Walkable(pos + new Vector2(0, step.Y), Balance.HeroRadius)) pos.Y += step.Y;
            Position = pos;
            // He turns smoothly towards where the thumb points, so the direction of travel is always visible.
            _yaw = Mathf.LerpAngle(_yaw, Mathf.Atan2(move.X, move.Y), 1 - Mathf.Exp(-14 * dt));
        }
        Look.Update(dt, moving, _yaw);
        Place(clock);
    }

    /// <summary>Takes a word into his hands.</summary>
    public void Carry(TowerToken token)
    {
        Carried = token;
        _book.Visible = true;
    }

    /// <summary>Empties his hands; returns the word he carried.</summary>
    public TowerToken? Release()
    {
        var token = Carried;
        Carried = null;
        _book.Visible = false;
        return token;
    }

    private void Place(float clock)
    {
        Node.Position = Position.To3();
        _book.Position = new Vector3(0, 2.5f + 0.12f * Mathf.Sin(clock * 4), 0);
        _book.Rotation = new Vector3(0, clock * 1.5f, 0);
    }
}
