using Godot;
using PuglingTower.Combat;
using PuglingTower.Core;
using PuglingTower.Heroes;
using PuglingTower.Words;
using PuglingTower.World;

namespace PuglingTower.Ui;

/// <summary>What the overlay shows in one frame.</summary>
/// <param name="Clock">Seconds since the level started (pulses).</param>
/// <param name="Chosen">The tower the "give" button points at, if any.</param>
public readonly record struct OverlayFrame(float Clock, Tower? Chosen);

/// <summary>
/// The flat 2D layer over the 3D scene: everything that must stay readable on a phone. Words on the books, health bars,
/// tower signs with meaning and ammo (or an edge badge for towers off screen), the carried word above the hero, the HUD
/// and the joystick. A new overlay element is one more block in <see cref="Paint"/>.
/// </summary>
public sealed class LevelOverlay(Camera3D camera, WordBooks words, Enemies enemies, Towers towers, Hero hero, RunTally tally, TouchStick stick)
{
    /// <summary>The ammo bar is full at this many shots.</summary>
    private const int FullAmmoBar = 20;

    private static readonly Color Ink = new(0.2f, 0.12f, 0f);

    /// <summary>Draws one frame.</summary>
    public void Paint(Painter p, OverlayFrame frame)
    {
        var screen = p.GetViewportRect().Size;

        foreach (var book in words.All.Where(b => b.Node.Visible))
            if (OnScreen(book.Node.Position + new Vector3(0, 1.1f, 0), screen, 0, out var at))
                p.Pill(at, book.Data.Word, 24, new Color(Palette.Gold, 0.95f), Ink);

        foreach (var e in enemies.All.Where(e => !e.Dying))
            if (OnScreen(e.Node.Position + new Vector3(0, 2.1f, 0), screen, 0, out var at))
            {
                var bar = new Rect2(at - new Vector2(26, 5), new Vector2(52, 10));
                p.Box(bar.Grow(2), new Color(0, 0, 0, 0.6f), radius: 5);
                p.Box(new Rect2(bar.Position, new Vector2(bar.Size.X * e.Hp / e.Kind.Hp, bar.Size.Y)),
                    e.Hp * 2 > e.Kind.Hp ? new Color(0.4f, 0.9f, 0.3f) : new Color(0.95f, 0.35f, 0.25f), radius: 4);
            }

        foreach (var tower in towers.All) PaintTower(p, tower, screen, frame);

        if (hero.Carried is not null && OnScreen(hero.Node.Position + new Vector3(0, 3.4f, 0), screen, 0, out var head))
            p.Pill(head, hero.Carried.Word, 26, Palette.Gold, Ink, Colors.White);

        PaintHud(p);

        // Joystick: a hint where the left thumb goes, and the stick while it is held.
        var center = stick.Held ? stick.Center : new Vector2(150, screen.Y - 150);
        p.DrawCircle(center, 95, new Color(1, 1, 1, 0.12f));
        p.DrawArc(center, 95, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.35f), 3, true);
        p.DrawCircle(center + stick.Vector * TouchStick.Reach, 40, new Color(1, 1, 1, 0.45f));
    }

    // Hits towards the goal and the castle's lives.
    private void PaintHud(Painter p)
    {
        var hud = new Rect2(16, 14, 330, 64);
        p.Box(hud, Palette.Night, new Color(1, 1, 1, 0.25f), 2, 18);
        p.Text(new Vector2(hud.Position.X + 82, hud.GetCenter().Y), $"{tally.Correct}/{tally.Goal}", 30, Palette.Gold);
        p.Text(new Vector2(hud.Position.X + 82, hud.GetCenter().Y + 25), "Treffer", 14, Colors.White, 0);
        for (var i = 0; i < Balance.CastleLives; i++)
            p.Box(new Rect2(hud.Position.X + 160 + i * 16, hud.GetCenter().Y - 9, 12, 18),
                i < tally.Lives ? new Color(0.95f, 0.3f, 0.35f) : new Color(1, 1, 1, 0.15f), radius: 4);
        p.Text(new Vector2(hud.Position.X + 238, hud.GetCenter().Y + 25), "Burg", 14, Colors.White, 0);
    }

    private void PaintTower(Painter p, Tower tower, Vector2 screen, OverlayFrame frame)
    {
        var ammo = tower.Ammo;
        var low = tower.Target is not null && ammo.IsLow;
        var chosen = tower == frame.Chosen;
        var border = chosen ? Palette.Gold : low ? new Color(1f, 0.3f, 0.25f) : new Color(1, 1, 1, 0.3f);
        var label = tower.Target is null ? "fertig" : ammo.IsStrong ? "★ " + tower.Target.Prompt : tower.Target.Prompt;
        if (OnScreen(tower.Pos.To3(4.6f), screen, 40, out var at))
        {
            var width = Mathf.Max(150, ThemeDB.FallbackFont.GetStringSize(label, fontSize: 26).X + 34);
            var panel = new Rect2(at - new Vector2(width / 2, 40), new Vector2(width, 66));
            var pulse = chosen ? 1 + 0.06f * Mathf.Sin(frame.Clock * 8) : 1;
            panel = panel.Grow((pulse - 1) * 40);
            var fillColor = tower.Target is null ? new Color(0.2f, 0.2f, 0.2f, 0.7f) : ammo.IsStrong ? new Color(0.45f, 0.18f, 0.05f, 0.88f) : Palette.Night;
            p.Box(panel, fillColor, border, chosen ? 5 : 2, 16);
            p.Text(new Vector2(panel.GetCenter().X, panel.Position.Y + 22), label, 26, tower.Target is null ? new Color(1, 1, 1, 0.6f) : Colors.White);
            if (tower.Target is null) return;
            var bar = new Rect2(panel.Position.X + 14, panel.End.Y - 22, panel.Size.X - 64, 12);
            p.Box(bar, new Color(0, 0, 0, 0.55f), radius: 6);
            var fill = Mathf.Clamp((float)ammo.Count / FullAmmoBar, 0, 1);
            if (fill > 0) p.Box(new Rect2(bar.Position, new Vector2(Mathf.Max(12, bar.Size.X * fill), bar.Size.Y)), AmmoColor(ammo.Count), radius: 6);
            p.Text(new Vector2(panel.End.X - 26, bar.GetCenter().Y), ammo.Count.ToString(), 20, AmmoColor(ammo.Count), 4);
            return;
        }

        if (tower.Target is null) return;
        // Off screen: a badge on the edge points the way, red when the tower is running dry.
        var mid = screen / 2;
        var dir = (at - mid).Normalized();
        var inset = new Vector2(70, 70);
        var scale = Mathf.Min((mid.X - inset.X) / Mathf.Max(0.001f, Mathf.Abs(dir.X)), (mid.Y - inset.Y) / Mathf.Max(0.001f, Mathf.Abs(dir.Y)));
        var edge = mid + dir * scale;
        var color = low ? new Color(0.85f, 0.2f, 0.2f, 0.92f) : Palette.Night;
        var tip = edge + dir * 46;
        var side = dir.Orthogonal() * 16;
        p.DrawColoredPolygon([tip, edge + dir * 24 + side, edge + dir * 24 - side], border);
        p.Box(new Rect2(edge - new Vector2(34, 34), new Vector2(68, 68)), color, border, 3, 34);
        p.Text(edge, ammo.Count.ToString(), 26, AmmoColor(ammo.Count));
        p.Pill(edge + new Vector2(0, dir.Y > 0 ? -54 : 54), label, 18, Palette.Night, Colors.White);
    }

    private static Color AmmoColor(int ammo) =>
        ammo <= Balance.WrongPenalty ? new Color(1f, 0.35f, 0.3f) : ammo <= 6 ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 0.95f, 0.45f);

    private bool OnScreen(Vector3 world, Vector2 screen, float margin, out Vector2 at)
    {
        at = camera.UnprojectPosition(world);
        if (camera.IsPositionBehind(world)) at = screen - at;
        return at.X >= margin && at.Y >= margin && at.X <= screen.X - margin && at.Y <= screen.Y - margin;
    }
}
