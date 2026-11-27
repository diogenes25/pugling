using Godot;

namespace PuglingTower.Ui;

/// <summary>The level's colours, shared by the 3D markers and the flat overlay.</summary>
public static class Palette
{
    /// <summary>Words, the goal counter, the chosen tower.</summary>
    public static readonly Color Gold = new(1f, 0.82f, 0.25f);

    /// <summary>The dark translucent background of panels and badges.</summary>
    public static readonly Color Night = new(0.08f, 0.1f, 0.2f, 0.82f);

    /// <summary>Confirming buttons (give, carry on).</summary>
    public static readonly Color Go = new(0.18f, 0.62f, 0.32f);
}

/// <summary>The level's touch controls in the game's style.</summary>
public static class UiKit
{
    /// <summary>A big rounded touch button.</summary>
    public static Button Button(string text, Color color, Action onPressed, int fontSize = 30)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(260, 76) };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        foreach (var (state, shade) in new[] { ("normal", 0f), ("hover", 0.1f), ("pressed", -0.2f), ("focus", 0f) })
        {
            var box = new StyleBoxFlat
            {
                BgColor = shade >= 0 ? color.Lightened(shade) : color.Darkened(-shade), BorderColor = color.Darkened(0.45f),
                ShadowColor = new Color(0, 0, 0, 0.35f), ShadowSize = 6, ShadowOffset = new Vector2(0, 4), AntiAliasing = true,
            };
            box.SetCornerRadiusAll(24);
            box.SetBorderWidthAll(4);
            box.SetContentMarginAll(12);
            button.AddThemeStyleboxOverride(state, box);
        }
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.6f));
        button.AddThemeConstantOverride("outline_size", 6);
        button.Pressed += onPressed;
        return button;
    }

    /// <summary>A centred caption with a dark outline, readable over the scene.</summary>
    public static Label Caption(string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 8);
        return label;
    }
}
