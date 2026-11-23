using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Core;
using PuglingTower.Net;

namespace PuglingTower;

/// <summary>
/// One level of "Vokabel-Türme" (B-853): monsters walk a path towards the base, five towers shoot them while they have
/// ammo, a defeated monster drops a vocabulary token. The player carries one token at a time to a tower and assigns it;
/// the server grades the assignment. A correct one refills the tower and gives it the next meaning, a wrong one costs
/// the token and one shot (B-822 E2). Won after <see cref="TowerRunResponse.Goal"/> correct assignments, lost when the
/// base falls. Placeholder graphics are drawn in code.
/// </summary>
public partial class Level : Node2D
{
    /// <summary>Raised once when the level ends: won?, correct and total assignments.</summary>
    public event Action<int, int, int, bool>? Finished;

    private static readonly Vector2[] Path =
        [new(1340, 110), new(930, 110), new(930, 610), new(470, 610), new(470, 230), new(70, 230)];
    private static readonly Vector2[] TowerSpots =
        [new(1110, 250), new(780, 300), new(700, 470), new(320, 400), new(250, 125)];

    private const float TowerRange = 250f, ShotInterval = 0.7f, PickupRadius = 80f, TowerReach = 100f;
    private const float PlayerSpeed = 330f, TokenLifetime = 25f;
    private const int StartAmmo = 10, RefillAmmo = 8, BaseLives = 10, MonsterHp = 2;

    private sealed class Tower
    {
        public Vector2 Pos;
        public TowerTarget? Target;
        public int Ammo = StartAmmo;
        public float Cooldown;
        public Vector2? ShotAt;
        public float ShotFlash;
    }

    private sealed class Monster
    {
        public float Distance;
        public int Hp = MonsterHp;
        public float Speed;
        public Vector2 Pos;
    }

    private sealed class Token
    {
        public required TowerToken Data;
        public Vector2 Pos;
        public float Age;
    }

    private PuglingApi _api = null!;
    private TowerRunResponse _run = null!;
    private TokenPool _pool = null!;
    private readonly Queue<TowerTarget> _nextTargets = new();
    private readonly List<Tower> _towers = [];
    private readonly List<Monster> _monsters = [];
    private readonly List<Token> _tokens = [];
    private readonly Dictionary<string, TowerToken> _tokenById = [];

    private Vector2 _player = new(640, 400);
    private TowerToken? _carried;
    private int _correct, _total, _lives = BaseLives;
    private float _spawnTimer = 1f, _elapsed;
    private bool _busy, _over, _beating;
    private double _sinceBeat;
    /// <summary>The app reports the foreground play time about this often (B-851: the server clamps each beat).</summary>
    private const double BeatInterval = 15;

    // Left thumb: virtual joystick.
    private int _stickFinger = -1;
    private Vector2 _stickCenter, _stickVector;

    private Label _hud = null!, _carry = null!, _feedback = null!, _question = null!;
    private VBoxContainer _pickBar = null!;
    private Control _assignPanel = null!;
    private Tower? _assignTower;
    private float _feedbackTime;
    private string _pickSignature = "";

    /// <summary>Wires the level to a run; call before adding it to the tree.</summary>
    public void Setup(PuglingApi api, TowerRunResponse run)
    {
        _api = api;
        _run = run;
        _pool = new TokenPool(run.Tokens.Select(t => t.TokenId));
        foreach (var t in run.Tokens) _tokenById[t.TokenId] = t;
        foreach (var target in run.Towers) _nextTargets.Enqueue(target);
        foreach (var spot in TowerSpots)
            _towers.Add(new Tower { Pos = spot, Target = _nextTargets.Count > 0 ? _nextTargets.Dequeue() : null });
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        var ui = new CanvasLayer();
        AddChild(ui);
        _hud = MakeLabel(ui, 26, new Vector2(16, 8));
        _carry = MakeLabel(ui, 30, new Vector2(420, 660));
        _feedback = MakeLabel(ui, 34, new Vector2(380, 40));

        _pickBar = new VBoxContainer { Position = new Vector2(1010, 330), CustomMinimumSize = new Vector2(250, 0) };
        _pickBar.AddThemeConstantOverride("separation", 10);
        ui.AddChild(_pickBar);

        var panel = new PanelContainer { Position = new Vector2(880, 520), CustomMinimumSize = new Vector2(390, 0), Visible = false };
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        _question = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(370, 0) };
        _question.AddThemeFontSizeOverride("font_size", 26);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        var yes = MakeButton("Ja", () => _ = AssignAsync());
        var no = MakeButton("Nein", () => { _assignTower = null; _assignPanel.Visible = false; });
        yes.SizeFlagsHorizontal = no.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(yes);
        row.AddChild(no);
        box.AddChild(_question);
        box.AddChild(row);
        panel.AddChild(box);
        ui.AddChild(panel);
        _assignPanel = panel;
        UpdateHud();
    }

    private static Label MakeLabel(Node parent, int size, Vector2 pos)
    {
        var label = new Label { Position = pos };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        parent.AddChild(label);
        return label;
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 70), FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 30);
        button.Pressed += onPressed;
        return button;
    }

    /// <inheritdoc />
    public override void _Input(InputEvent e)
    {
        // The joystick lives on the left half; the right half belongs to the buttons (two-thumb layout).
        if (e is InputEventScreenTouch touch)
        {
            if (touch.Pressed && _stickFinger < 0 && touch.Position.X < GetViewportRect().Size.X * 0.45f)
            {
                _stickFinger = touch.Index;
                _stickCenter = touch.Position;
                _stickVector = Vector2.Zero;
            }
            else if (!touch.Pressed && touch.Index == _stickFinger)
            {
                _stickFinger = -1;
                _stickVector = Vector2.Zero;
            }
        }
        else if (e is InputEventScreenDrag drag && drag.Index == _stickFinger)
        {
            var offset = drag.Position - _stickCenter;
            _stickVector = offset.Length() < 12 ? Vector2.Zero : offset.LimitLength(90) / 90f;
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_over) return;
        var dt = (float)delta;
        _elapsed += dt;
        _sinceBeat += delta;
        if (_sinceBeat >= BeatInterval && !_beating) _ = BeatAsync();

        // Keyboard as a desktop fallback for the left thumb.
        var keys = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        var move = _stickVector != Vector2.Zero ? _stickVector : keys;
        _player = (_player + move * PlayerSpeed * dt).Clamp(new Vector2(20, 60), new Vector2(1260, 700));

        SpawnMonsters(dt);
        MoveMonsters(dt);
        ShootTowers(dt);
        AgeTokens(dt);
        UpdateContext();

        if (_feedbackTime > 0 && (_feedbackTime -= dt) <= 0) _feedback.Text = "";
        QueueRedraw();
    }

    private void SpawnMonsters(float dt)
    {
        if ((_spawnTimer -= dt) > 0) return;
        _spawnTimer = Mathf.Max(2.4f, 4.5f - _elapsed * 0.01f);
        _monsters.Add(new Monster { Speed = 50f + Mathf.Min(30f, _elapsed * 0.15f), Pos = Path[0] });
    }

    private void MoveMonsters(float dt)
    {
        var length = PathLength();
        for (var i = _monsters.Count - 1; i >= 0; i--)
        {
            var m = _monsters[i];
            m.Distance += m.Speed * dt;
            if (m.Distance >= length)
            {
                _monsters.RemoveAt(i);
                if (--_lives <= 0) End(timeUp: false);
                UpdateHud();
                continue;
            }
            m.Pos = PointAt(m.Distance);
        }
    }

    private void ShootTowers(float dt)
    {
        foreach (var tower in _towers)
        {
            tower.ShotFlash = Mathf.Max(0, tower.ShotFlash - dt);
            if ((tower.Cooldown -= dt) > 0 || tower.Ammo <= 0) continue;
            var target = _monsters.Where(m => m.Pos.DistanceTo(tower.Pos) <= TowerRange)
                .OrderByDescending(m => m.Distance).FirstOrDefault();
            if (target is null) continue;
            tower.Cooldown = ShotInterval;
            tower.Ammo--;
            tower.ShotAt = target.Pos;
            tower.ShotFlash = 0.12f;
            if (--target.Hp > 0) continue;
            _monsters.Remove(target);
            if (_pool.Draw() is { } id)
                _tokens.Add(new Token
                {
                    Data = _tokenById[id],
                    Pos = (target.Pos + new Vector2(0, 28)).Clamp(new Vector2(80, 80), new Vector2(1200, 690)),
                });
        }
    }

    private void AgeTokens(float dt)
    {
        // Expired tokens go back to the pool, so the field never fills up with words nobody picks.
        for (var i = _tokens.Count - 1; i >= 0; i--)
        {
            if ((_tokens[i].Age += dt) < TokenLifetime) continue;
            _pool.Return(_tokens[i].Data.TokenId);
            _tokens.RemoveAt(i);
        }
    }

    private void UpdateContext()
    {
        // Selection bar: the tokens within reach, nearest first, at most three.
        var near = _tokens.Where(t => t.Pos.DistanceTo(_player) <= PickupRadius)
            .OrderBy(t => t.Pos.DistanceTo(_player)).Take(3).ToList();
        var signature = string.Join("|", near.Select(t => t.Data.TokenId));
        if (signature != _pickSignature)
        {
            _pickSignature = signature;
            foreach (var child in _pickBar.GetChildren()) child.QueueFree();
            foreach (var token in near)
                _pickBar.AddChild(MakeButton($"⬆ {token.Data.Word}", () => PickUp(token)));
        }

        // Assign dialog: carrying a token next to a tower that still asks for something.
        var tower = _carried is null || _busy ? null
            : _towers.Where(t => t.Target is not null && t.Pos.DistanceTo(_player) <= TowerReach)
                .OrderBy(t => t.Pos.DistanceTo(_player)).FirstOrDefault();
        if (tower != _assignTower && !_busy)
        {
            _assignTower = tower;
            _assignPanel.Visible = tower is not null;
            if (tower is not null) _question.Text = $"„{_carried!.Word}“ zu „{tower.Target!.Prompt}“ hinzufügen?";
        }
    }

    private void PickUp(Token token)
    {
        if (!_tokens.Remove(token)) return;
        // One token at a time (B-822): picking another one puts the carried one down here.
        if (_carried is not null) _tokens.Add(new Token { Data = _carried, Pos = _player + new Vector2(30, 30) });
        _carried = token.Data;
        _assignTower = null;
        UpdateHud();
    }

    private async Task AssignAsync()
    {
        if (_busy || _assignTower is not { Target: { } target } tower || _carried is not { } token) return;
        _busy = true;
        _assignPanel.Visible = false;
        try
        {
            var result = await _api.AssignAsync(_run.RunId, token.TokenId, target.TowerId);
            _total++;
            _carried = null;
            GD.Print($"TURM assign '{token.Word}' -> '{target.Prompt}': {(result.Correct ? "richtig" : "falsch, erwartet " + result.Expected)}");
            if (result.Correct)
            {
                _correct++;
                _pool.Consume(token.TokenId);
                tower.Ammo += RefillAmmo;
                tower.Target = _nextTargets.Count > 0 ? _nextTargets.Dequeue() : null;
                Flash($"Richtig! „{token.Word}“ = „{target.Prompt}“", Colors.LightGreen);
                if (_correct >= _run.Goal) End(timeUp: false);
            }
            else
            {
                _pool.Return(token.TokenId);
                tower.Ammo = Math.Max(0, tower.Ammo - 1);
                Flash($"Falsch – „{target.Prompt}“ heißt „{result.Expected}“", Colors.Salmon);
            }
        }
        catch (ApiException ex)
        {
            Flash(ex.Message, Colors.Orange);
            if (ex.RunClosed) End(timeUp: true);
        }
        finally
        {
            _busy = false;
            _assignTower = null;
            UpdateHud();
        }
    }

    private void Flash(string text, Color color)
    {
        _feedback.Text = text;
        _feedback.AddThemeColorOverride("font_color", color);
        _feedbackTime = 3f;
    }

    private void UpdateHud()
    {
        _hud.Text = $"Treffer {_correct}/{_run.Goal}   ·   Basis {new string('♥', Math.Max(0, _lives))}";
        _carry.Text = _carried is null ? "" : $"Du trägst: {_carried.Word}";
    }

    private async Task BeatAsync()
    {
        _beating = true;
        _sinceBeat = 0;
        try
        {
            var beat = await _api.HeartbeatAsync(_run.RunId);
            if (beat.TimeUp)
            {
                Flash("Die Spielzeit für heute ist um.", Colors.Orange);
                End(timeUp: true);
            }
        }
        catch (ApiException)
        {
            // A lost beat costs only its seconds; the next one goes out in 15 s.
        }
        finally
        {
            _beating = false;
        }
    }

    // The server decides whether the level is won (its own count of solved towers); the client only says it is over.
    private void End(bool timeUp)
    {
        if (_over) return;
        _over = true;
        _assignPanel.Visible = false;
        GD.Print($"TURM ende {(timeUp ? "zeit um" : "")} {_correct}/{_total}");
        Finished?.Invoke(_run.RunId, _correct, _total, timeUp);
    }

    private static float PathLength()
    {
        var sum = 0f;
        for (var i = 1; i < Path.Length; i++) sum += Path[i - 1].DistanceTo(Path[i]);
        return sum;
    }

    private static Vector2 PointAt(float distance)
    {
        for (var i = 1; i < Path.Length; i++)
        {
            var segment = Path[i - 1].DistanceTo(Path[i]);
            if (distance <= segment) return Path[i - 1].Lerp(Path[i], distance / segment);
            distance -= segment;
        }
        return Path[^1];
    }

    /// <inheritdoc />
    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        DrawRect(new Rect2(0, 0, 1280, 720), new Color(0.16f, 0.32f, 0.18f));
        DrawPolyline(Path, new Color(0.55f, 0.45f, 0.3f), 56);
        DrawCircle(Path[^1], 44, new Color(0.3f, 0.4f, 0.8f));
        DrawString(font, Path[^1] + new Vector2(-30, 8), "Basis", fontSize: 22);

        foreach (var tower in _towers)
        {
            var active = tower.Target is not null;
            DrawCircle(tower.Pos, TowerRange, new Color(1, 1, 1, 0.04f));
            DrawRect(new Rect2(tower.Pos - new Vector2(28, 28), new Vector2(56, 56)),
                active ? (tower.Ammo > 0 ? new Color(0.75f, 0.7f, 0.6f) : new Color(0.5f, 0.3f, 0.3f)) : new Color(0.35f, 0.35f, 0.35f));
            DrawString(font, tower.Pos + new Vector2(-12, 10), tower.Ammo.ToString(), fontSize: 24, modulate: Colors.Black);
            if (active) DrawTag(font, tower.Target!.Prompt, tower.Pos + new Vector2(0, -44), new Color(0.1f, 0.1f, 0.25f, 0.85f));
            if (tower.ShotFlash > 0 && tower.ShotAt is { } at) DrawLine(tower.Pos, at, Colors.Yellow, 4);
        }

        foreach (var m in _monsters)
        {
            DrawCircle(m.Pos, 20, new Color(0.8f, 0.15f, 0.2f));
            DrawRect(new Rect2(m.Pos + new Vector2(-20, -32), new Vector2(40f * m.Hp / MonsterHp, 6)), Colors.LimeGreen);
        }

        foreach (var t in _tokens)
        {
            var fade = t.Age > TokenLifetime - 5 ? 0.5f : 1f;
            DrawTag(font, t.Data.Word, t.Pos, new Color(0.95f, 0.85f, 0.2f, fade), Colors.Black);
        }

        DrawCircle(_player, 22, new Color(0.2f, 0.6f, 1f));
        DrawArc(_player, PickupRadius, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.25f), 2);
        if (_carried is not null) DrawTag(font, _carried.Word, _player + new Vector2(0, -40), new Color(0.95f, 0.85f, 0.2f), Colors.Black);

        // Joystick: a hint where the left thumb goes, and the stick while it is held.
        var center = _stickFinger >= 0 ? _stickCenter : new Vector2(140, 580);
        DrawCircle(center, 90, new Color(1, 1, 1, 0.12f));
        DrawCircle(center + _stickVector * 90, 36, new Color(1, 1, 1, 0.3f));
    }

    private void DrawTag(Font font, string text, Vector2 center, Color background, Color? textColor = null)
    {
        const int size = 22;
        var width = font.GetStringSize(text, fontSize: size).X + 16;
        DrawRect(new Rect2(center - new Vector2(width / 2, 16), new Vector2(width, 32)), background);
        DrawString(font, center + new Vector2(-width / 2 + 8, 8), text, fontSize: size, modulate: textColor ?? Colors.White);
    }
}
