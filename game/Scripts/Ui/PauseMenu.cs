using Godot;

namespace PuglingTower.Ui;

/// <summary>Dims the level while it is paused: carry on, or stop here and see the result.</summary>
public static class PauseMenu
{
    /// <summary>Builds the hidden menu; the caller shows it while the level is paused.</summary>
    public static Control Build(Action resume, Action giveUp)
    {
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f), Visible = false };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var menu = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        menu.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        menu.AddThemeConstantOverride("separation", 22);
        var title = UiKit.Caption("Pause");
        title.AddThemeFontSizeOverride("font_size", 44);
        menu.AddChild(title);
        foreach (var button in new[]
        {
            UiKit.Button("Weiter", Palette.Go, resume),
            UiKit.Button("Aufhören", new Color(0.7f, 0.25f, 0.2f), giveUp),
        })
        {
            button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            menu.AddChild(button);
        }
        dim.AddChild(menu);
        return dim;
    }
}
