using Godot;

namespace PuglingTower.Ui;

/// <summary>
/// The left thumb: a floating joystick wherever it lands on the left part of the screen. The right part belongs to the
/// buttons (two-thumb layout).
/// </summary>
public sealed class TouchStick
{
    private const float LeftShare = 0.45f, DeadZone = 10f;

    /// <summary>How far the thumb may pull the stick, in pixels.</summary>
    public const float Reach = 90f;

    private int _finger = -1;

    /// <summary>A finger holds the stick.</summary>
    public bool Held => _finger >= 0;

    /// <summary>Where the finger came down.</summary>
    public Vector2 Center { get; private set; }

    /// <summary>The pull, at most length 1; screen up is negative Y.</summary>
    public Vector2 Vector { get; private set; }

    /// <summary>Feeds a touch event; <paramref name="screenWidth"/> decides what counts as the left part.</summary>
    public void Handle(InputEvent e, float screenWidth)
    {
        if (e is InputEventScreenTouch touch)
        {
            if (touch.Pressed && _finger < 0 && touch.Position.X < screenWidth * LeftShare)
            {
                _finger = touch.Index;
                Center = touch.Position;
                Vector = Vector2.Zero;
            }
            else if (!touch.Pressed && touch.Index == _finger) Release();
        }
        else if (e is InputEventScreenDrag drag && drag.Index == _finger)
        {
            var offset = drag.Position - Center;
            Vector = offset.Length() < DeadZone ? Vector2.Zero : offset.LimitLength(Reach) / Reach;
        }
    }

    /// <summary>Lets go of the stick (pause, finger lifted).</summary>
    public void Release()
    {
        _finger = -1;
        Vector = Vector2.Zero;
    }
}
