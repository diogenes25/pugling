using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Net;

namespace PuglingTower;

/// <summary>
/// The screen flow of the prototype: login with the child's PIN, pick a vocabulary position, play a level, see the
/// result. Screens are built in code so the scene file stays a single root node.
/// </summary>
public partial class GameRoot : Control
{
    private readonly PuglingApi _api = new();
    private Node? _screen;
    private Label _status = null!;
    private int _lastPosition;

    /// <inheritdoc />
    public override void _Ready() => ShowLogin();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _api.Dispose();
        base.Dispose(disposing);
    }

    private void Replace(Node screen)
    {
        _screen?.QueueFree();
        _screen = screen;
        AddChild(screen);
    }

    private VBoxContainer Page(string title)
    {
        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 14);
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride($"margin_{side}", 40);
        margin.AddChild(box);
        scroll.AddChild(margin);
        Replace(scroll);
        box.AddChild(Text(title, 40));
        _status = Text("", 24);
        return box;
    }

    private static Label Text(string text, int size)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static LineEdit Field(string text, string placeholder, bool secret = false)
    {
        var field = new LineEdit { Text = text, PlaceholderText = placeholder, Secret = secret };
        field.AddThemeFontSizeOverride("font_size", 30);
        return field;
    }

    private static Button Action(string text, System.Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 72) };
        button.AddThemeFontSizeOverride("font_size", 30);
        button.Pressed += onPressed;
        return button;
    }

    /// <summary>Servers to pick with one tap: the emulator reaches the throwaway API via `adb reverse`, the phone the home server in the WLAN.</summary>
    private static readonly (string Label, string Url)[] Servers =
        [("Emulator", "http://127.0.0.1:5280"), ("Heimnetz", "http://192.168.178.55:5200")];

    private void ShowLogin()
    {
        var box = Page("Vokabel-Türme");
        var server = Field(Servers[0].Url, "Server");
        var child = Field("3", "Kind-Nr.");
        var pin = Field("", "PIN", secret: true);

        // The Android soft keyboard does not open for a Godot LineEdit in this setup (focus yes, IME no), so the login
        // never depends on it: servers are presets and digits come from an on-screen keypad. The fields stay editable
        // for a hardware keyboard.
        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 12);
        foreach (var (label, url) in Servers)
        {
            var preset = Action(label, () => server.Text = url);
            preset.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            presets.AddChild(preset);
        }

        var target = pin;
        child.FocusEntered += () => target = child;
        pin.FocusEntered += () => target = pin;

        var form = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        form.AddThemeConstantOverride("separation", 14);
        form.AddChild(server);
        form.AddChild(presets);
        form.AddChild(Text("Kind-Nr. und PIN antippen, dann mit den Ziffern rechts eingeben.", 22));
        form.AddChild(child);
        form.AddChild(pin);
        form.AddChild(Action("Anmelden", () => _ = LoginAsync(server.Text, child.Text, pin.Text)));
        form.AddChild(_status);

        var keypad = new GridContainer { Columns = 3 };
        keypad.AddThemeConstantOverride("h_separation", 10);
        keypad.AddThemeConstantOverride("v_separation", 10);
        foreach (var key in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "C" })
        {
            var button = Action(key, () => target.Text = key switch
            {
                "⌫" => target.Text.Length > 0 ? target.Text[..^1] : "",
                "C" => "",
                _ => target.Text + key,
            });
            button.CustomMinimumSize = new Vector2(110, 80);
            keypad.AddChild(button);
        }

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 30);
        row.AddChild(form);
        row.AddChild(keypad);
        box.AddChild(row);
    }

    private async Task LoginAsync(string server, string child, string pin)
    {
        if (!int.TryParse(child, out var childId))
        {
            _status.Text = "Kind-Nr. muss eine Zahl sein.";
            return;
        }
        try
        {
            _status.Text = "Anmelden …";
            await _api.LoginAsync(server, childId, pin);
            await ShowPositionsAsync();
        }
        catch (ApiException ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async Task ShowPositionsAsync()
    {
        var box = Page($"Hallo {_api.Name}! Welche Wörter?");
        box.AddChild(_status);
        try
        {
            var overview = await _api.OverviewAsync();
            var minutesLeft = Math.Max(0, overview.LimitSeconds - overview.PlayedSecondsToday) / 60;
            box.AddChild(Text($"Spielzeit heute noch {minutesLeft} Min.  ·  Münzen heute {overview.CoinsToday}/{overview.DailyCoinCap} 🪙", 26));
            if (overview.Positions.Count == 0) _status.Text = "Kein aktiver Plan mit genug Wörtern.";
            foreach (var p in overview.Positions)
                box.AddChild(Action($"{p.SubjectName} · {p.ExerciseTitle} ({p.WordCount})", () => _ = StartAsync(p.PlanPositionId)));
        }
        catch (ApiException ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async Task StartAsync(int planPositionId)
    {
        try
        {
            _status.Text = "Level wird gebaut …";
            var run = await _api.StartRunAsync(planPositionId);
            _lastPosition = planPositionId;
            var level = new Level();
            level.Setup(_api, run);
            level.Finished += (runId, correct, total, timeUp) => _ = ShowResultAsync(runId, correct, total, timeUp);
            Replace(level);
        }
        catch (ApiException ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async Task ShowResultAsync(int runId, int correct, int total, bool timeUp)
    {
        TowerRunEndResult? end = null;
        string? error = null;
        try
        {
            end = await _api.EndAsync(runId);
        }
        catch (ApiException ex)
        {
            error = ex.Message;
        }
        var title = end?.Won == true ? "Level geschafft!" : timeUp ? "Spielzeit für heute ist um." : "Die Monster sind durchgebrochen.";
        var box = Page(title);
        box.AddChild(Text($"{correct} von {total} Zuweisungen richtig.", 30));
        if (end is not null)
            box.AddChild(Text(end.CoinsAwarded > 0
                ? $"+{end.CoinsAwarded} 🪙  (heute {end.CoinsToday}/{end.DailyCoinCap})"
                : end.Won ? $"Münzdeckel für heute erreicht ({end.CoinsToday}/{end.DailyCoinCap} 🪙)." : "Keine Münzen diesmal.", 30));
        box.AddChild(Action("Noch ein Level", () => _ = StartAsync(_lastPosition)));
        box.AddChild(Action("Andere Wörter", () => _ = ShowPositionsAsync()));
        box.AddChild(_status);
        if (error is not null) _status.Text = error;
    }
}
