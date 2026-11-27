using Godot;
using PuglingTower.Words;

namespace PuglingTower.Ui;

/// <summary>
/// The level's touch controls on the right side: the books in reach to pick, "give" next to a tower, "lay down", the
/// pause button and menu, and the feedback line in the upper third.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The nodes belong to the scene tree, which frees them with the level.")]
public sealed class LevelControls
{
    private readonly VBoxContainer _pickPanel = new();
    private readonly Button _give, _drop, _pause;
    private readonly Control _pauseMenu;
    private readonly Label _feedback;
    // The pick list is rebuilt only when the books in reach change, so a finger on a button is not cut off.
    private string _pickSignature = "";

    /// <summary>Builds the controls into <paramref name="ui"/>.</summary>
    public LevelControls(CanvasLayer ui, Action give, Action drop, Action togglePause, Action giveUp)
    {
        _pickPanel.AddThemeConstantOverride("separation", 10);
        ui.AddChild(_pickPanel);

        _give = UiKit.Button("", Palette.Go, give, 34);
        _give.CustomMinimumSize = new Vector2(300, 110);
        _give.Visible = false;
        ui.AddChild(_give);

        _drop = UiKit.Button("", new Color(0.3f, 0.4f, 0.6f), drop, 26);
        _drop.CustomMinimumSize = new Vector2(240, 70);
        _drop.Visible = false;
        ui.AddChild(_drop);

        _pause = UiKit.Button("II", Palette.Night, togglePause, 30);
        _pause.CustomMinimumSize = new Vector2(84, 72);
        ui.AddChild(_pause);

        _feedback = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = Colors.Transparent };
        _feedback.AddThemeFontSizeOverride("font_size", 36);
        _feedback.AddThemeColorOverride("font_outline_color", Colors.Black);
        _feedback.AddThemeConstantOverride("outline_size", 10);
        ui.AddChild(_feedback);

        _pauseMenu = PauseMenu.Build(togglePause, giveUp);
        ui.AddChild(_pauseMenu);
    }

    /// <summary>Offers every book in reach, nearest first; rebuilt only when the set changes.</summary>
    public void ShowNearby(IReadOnlyList<WordBook> near, Action<WordBook> pick)
    {
        var signature = string.Join("|", near.Select(b => b.Data.TokenId));
        if (signature == _pickSignature) return;
        _pickSignature = signature;
        ClearPicks();
        if (near.Count > 0) _pickPanel.AddChild(UiKit.Caption("Aufheben:"));
        foreach (var book in near)
            _pickPanel.AddChild(UiKit.Button(book.Data.Word, new Color(0.85f, 0.6f, 0.12f), () => pick(book)));
    }

    /// <summary>Forces the pick list to rebuild on the next frame (the books in reach changed under the same ids).</summary>
    public void RefreshNearby() => _pickSignature = "";

    /// <summary>Shows "give" when <paramref name="giveWord"/> is set and "lay down" when <paramref name="dropWord"/> is.</summary>
    public void ShowCarry(string? giveWord, string? dropWord)
    {
        _give.Visible = giveWord is not null;
        if (giveWord is not null) _give.Text = $"„{giveWord}“\ngeben";
        _drop.Visible = dropWord is not null;
        if (dropWord is not null) _drop.Text = $"„{dropWord}“ ablegen";
    }

    /// <summary>Hides "give" while the server grades.</summary>
    public void HideGive() => _give.Visible = false;

    /// <summary>Shows or hides the pause menu.</summary>
    public void ShowPause(bool paused) => _pauseMenu.Visible = paused;

    /// <summary>The level is over: nothing to press any more.</summary>
    public void Close()
    {
        _pauseMenu.Visible = false;
        _pause.Visible = false;
        _give.Visible = false;
        _drop.Visible = false;
        ClearPicks();
    }

    /// <summary>Shows a line of feedback that pops in and fades after a few seconds.</summary>
    public void Say(string text, Color color)
    {
        _feedback.Text = text;
        _feedback.AddThemeColorOverride("font_color", color);
        var tween = _feedback.CreateTween();
        _feedback.Modulate = Colors.White;
        _feedback.PivotOffset = _feedback.Size / 2;
        _feedback.Scale = Vector2.One * 1.25f;
        tween.TweenProperty(_feedback, "scale", Vector2.One, 0.25);
        tween.TweenInterval(2.6);
        tween.TweenProperty(_feedback, "modulate", Colors.Transparent, 0.5);
    }

    /// <summary>Places the controls for the current screen size.</summary>
    public void Layout(Vector2 screen)
    {
        _pickPanel.Position = new Vector2(screen.X - _pickPanel.Size.X - 28, 110);
        _give.Position = screen - _give.Size - new Vector2(28, 28);
        _drop.Position = new Vector2(screen.X - _drop.Size.X - 28, (_give.Visible ? _give.Position.Y : screen.Y) - _drop.Size.Y - 18);
        _pause.Position = new Vector2(screen.X - _pause.Size.X - 20, 14);
        _feedback.Size = new Vector2(screen.X, 60);
        _feedback.Position = new Vector2(0, screen.Y * 0.16f);
    }

    private void ClearPicks()
    {
        foreach (var child in _pickPanel.GetChildren()) child.QueueFree();
    }
}
