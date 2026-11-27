using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Net;
using PuglingTower.Play;
using PuglingTower.World;

namespace PuglingTower;

/// <summary>
/// The screen flow of the prototype: login with the child's PIN, pick a vocabulary position, play a level, see the
/// result. Screens are built in code so the scene file stays a single root node.
/// </summary>
public partial class GameRoot : Control
{
    private readonly PuglingApi _api = new();
    private Node? _screen;
    private ulong _lastBack;
    private Label _status = null!;
    private int _lastPosition;
    private LevelDef _lastLevel = LevelDef.All[0];
    /// <summary>What the Android back button does on the current screen.</summary>
    private System.Action _back = () => { };

    /// <inheritdoc />
    public override void _Ready() => ShowLogin();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _api.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void _Notification(int what)
    {
        // project.godot keeps the app open on "back" (quit_on_go_back=false); each screen decides where back leads.
        // One press arrives more than once on Android; without the debounce it would skip a screen.
        if (what != NotificationWMGoBackRequest || Time.GetTicksMsec() - _lastBack < 400) return;
        _lastBack = Time.GetTicksMsec();
        _back();
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
        // The emulator is the only x86_64 target; a real phone (arm64) starts on the home server.
        var server = Field(Servers[OS.HasFeature("x86_64") ? 0 : 1].Url, "Server");
        var child = Field("3", "Kind-Nr.");
        var pin = Field("", "PIN", secret: true);
        _back = () => GetTree().Quit();

        // Digits come only from the on-screen keypad, as in the web login: the phone's keyboard would pop up over the
        // form and hide the keypad. A hardware keyboard still types into the focused field.
        child.VirtualKeyboardEnabled = false;
        pin.VirtualKeyboardEnabled = false;
        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 12);
        foreach (var (label, url) in Servers)
        {
            var preset = Action(label, () => server.Text = url);
            preset.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            presets.AddChild(preset);
        }

        var target = pin;
        void Select(LineEdit field)
        {
            target = field;
            // The field the keypad writes into is the bright one.
            child.Modulate = field == child ? Colors.White : new Color(1, 1, 1, 0.55f);
            pin.Modulate = field == pin ? Colors.White : new Color(1, 1, 1, 0.55f);
        }
        child.FocusEntered += () => Select(child);
        pin.FocusEntered += () => Select(pin);
        Select(pin);

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
        _back = ShowLogin;
        box.AddChild(_status);
        try
        {
            var overview = await _api.OverviewAsync();
            var minutesLeft = Math.Max(0, overview.LimitSeconds - overview.PlayedSecondsToday) / 60;
            box.AddChild(Text($"Spielzeit heute noch {minutesLeft} Min.  ·  Münzen heute {overview.CoinsToday}/{overview.DailyCoinCap} 🪙", 26));
            if (overview.Positions.Count == 0) _status.Text = "Kein aktiver Plan mit genug Wörtern.";
            foreach (var p in overview.Positions)
                box.AddChild(Action($"{p.SubjectName} · {p.ExerciseTitle} ({p.WordCount})", () => ShowLevels(p.PlanPositionId)));
        }
        catch (ApiException ex)
        {
            _status.Text = ex.Message;
        }
        box.AddChild(Action("Abmelden", ShowLogin));
        box.AddChild(Action("Spiel beenden", () => GetTree().Quit()));
    }

    private void ShowLevels(int planPositionId)
    {
        var box = Page("Welche Karte?");
        _back = () => _ = ShowPositionsAsync();
        foreach (var level in LevelDef.All)
        {
            box.AddChild(Action(level.Name, () => _ = StartAsync(planPositionId, level)));
            box.AddChild(Text(level.Description, 22));
        }
        box.AddChild(Action("Andere Wörter", () => _ = ShowPositionsAsync()));
        box.AddChild(_status);
    }

    private async Task StartAsync(int planPositionId, LevelDef map)
    {
        try
        {
            _status.Text = "Level wird gebaut …";
            var run = await _api.StartRunAsync(planPositionId);
            _lastPosition = planPositionId;
            _lastLevel = map;
            var level = new Level();
            level.Setup(_api, run, map);
            level.Finished += (runId, correct, total, reason) => _ = ShowResultAsync(runId, correct, total, reason);
            Replace(level);
            _back = level.TogglePause;
        }
        catch (ApiException ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async Task ShowResultAsync(int runId, int correct, int total, LevelEnd reason)
    {
        _back = () => _ = ShowPositionsAsync();
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
        // Without the server's answer nobody knows whether the level is won: say so and offer the end call again
        // (it is idempotent), instead of showing a won level as lost and never booking its coins.
        var title = end is null ? "Ergebnis noch nicht gespeichert."
            : end.Won ? "Level geschafft!" : reason switch
            {
                LevelEnd.TimeUp => "Spielzeit für heute ist um.",
                LevelEnd.GaveUp => "Spiel beendet.",
                _ => "Die Monster sind durchgebrochen.",
            };
        var box = Page(title);
        box.AddChild(Text($"{correct} von {total} Zuweisungen richtig.", 30));
        if (end is null)
            box.AddChild(Action("Ergebnis nochmal senden", () => _ = ShowResultAsync(runId, correct, total, reason)));
        if (end is not null)
            box.AddChild(Text(end.CoinsAwarded > 0
                ? $"+{end.CoinsAwarded} 🪙  (heute {end.CoinsToday}/{end.DailyCoinCap})"
                : end.Won ? $"Münzdeckel für heute erreicht ({end.CoinsToday}/{end.DailyCoinCap} 🪙)." : "Keine Münzen diesmal.", 30));
        box.AddChild(Action("Noch einmal", () => _ = StartAsync(_lastPosition, _lastLevel)));
        box.AddChild(Action("Andere Karte", () => ShowLevels(_lastPosition)));
        box.AddChild(Action("Andere Wörter", () => _ = ShowPositionsAsync()));
        box.AddChild(_status);
        if (error is not null) _status.Text = error;
    }
}
