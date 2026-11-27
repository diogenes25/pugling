using Godot;

namespace PuglingTower.Ui;

/// <summary>A full-screen, click-through canvas whose drawing is done by its owner (the level's 2D overlay).</summary>
public partial class Painter : Control
{
    /// <summary>Draws the overlay; called from <see cref="_Draw"/>.</summary>
    public Action<Painter>? Paint { get; set; }

    /// <inheritdoc />
    public override void _Draw() => Paint?.Invoke(this);

    private readonly Dictionary<(Color, Color, int), StyleBoxFlat> _boxes = [];

    /// <summary>Draws a rounded box; styles are cached by colour.</summary>
    public void Box(Rect2 rect, Color fill, Color? border = null, int borderWidth = 0, int radius = 14)
    {
        var key = (fill, border ?? Colors.Transparent, borderWidth * 100 + radius);
        if (!_boxes.TryGetValue(key, out var box))
        {
            box = new StyleBoxFlat { BgColor = fill, BorderColor = border ?? Colors.Transparent, AntiAliasing = true };
            box.SetCornerRadiusAll(radius);
            box.SetBorderWidthAll(borderWidth);
            _boxes[key] = box;
        }
        box.Draw(GetCanvasItem(), rect);
    }

    /// <summary>Text centred on <paramref name="center"/> with a dark outline.</summary>
    public void Text(Vector2 center, string text, int size, Color color, int outline = 6)
    {
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(text, fontSize: size).X;
        var pos = center + new Vector2(-width / 2, (font.GetAscent(size) - font.GetDescent(size)) / 2);
        if (outline > 0) DrawStringOutline(font, pos, text, fontSize: size, size: outline, modulate: new Color(0, 0, 0, 0.85f * color.A));
        DrawString(font, pos, text, fontSize: size, modulate: color);
    }

    /// <summary>A pill with centred text; returns its rectangle.</summary>
    public Rect2 Pill(Vector2 center, string text, int size, Color fill, Color textColor, Color? border = null)
    {
        var width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: size).X + 26;
        var rect = new Rect2(center - new Vector2(width / 2, size * 0.75f + 6), new Vector2(width, size * 1.5f + 12));
        Box(rect, fill, border, border is null ? 0 : 3, (int)(rect.Size.Y / 2));
        Text(center, text, size, textColor, outline: 0);
        return rect;
    }
}
