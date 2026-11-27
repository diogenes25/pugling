using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Core;
using PuglingTower.Net;
using PuglingTower.World;

namespace PuglingTower;

/// <summary>Why a level ended; the server alone decides whether it counts as won.</summary>
public enum LevelEnd
{
    /// <summary>The goal of correct words is reached.</summary>
    GoalReached,

    /// <summary>Too many skeletons reached the castle.</summary>
    CastleFallen,

    /// <summary>Today's play time is used up.</summary>
    TimeUp,

    /// <summary>The player stopped from the pause menu.</summary>
    GaveUp,
}

/// <summary>
/// One level of "Vokabel-Türme" in 3D (B-853): skeletons walk fixed roads towards the castle, towers shoot them while
/// they have ammo, a defeated skeleton drops a vocabulary book. The hero walks the map - larger than the screen, the
/// camera keeps the hero in the centre - picks up a word and hands it to a tower; the server grades it. A correct word
/// refills the tower and gives it the next meaning, a wrong one is gone and costs the tower three shots. Won after
/// <see cref="TowerRunResponse.Goal"/> correct words, lost when the castle falls. The hero shoots too - weaker than a
/// tower but without ammo - so a run with empty towers still has a way back; words he lays down stay where they are.
/// </summary>
public partial class Level : Node3D
{
    /// <summary>Raised once when the level ends: run id, correct and total assignments, and why it ended.</summary>
    public event Action<int, int, int, LevelEnd>? Finished;

    private const float TowerRange = 7.5f, ShotInterval = 0.8f, BoltSpeed = 20f;
    private const float PickupRadius = 2.6f, TowerReach = 3.6f, HeroSpeed = 6.5f, HeroRadius = 0.45f, TokenLifetime = 30f;
    private const int StartAmmo = 10, RefillAmmo = 8, WrongPenalty = 3, BaseLives = 10, FullAmmoBar = 20;
    /// <summary>The hero's own shot: shorter range, slower and only ever one damage - the towers stay the main defence.</summary>
    private const float HeroRange = 5.5f, HeroShotInterval = 1.0f;
    /// <summary>A tower well stocked (more than this many shots) or refilled this often shoots double-damage arrows.</summary>
    private const int StrongAmmo = 30, MasterRefills = 5, StrongDamage = 2;
    /// <summary>The app reports the foreground play time about this often (B-851: the server clamps each beat).</summary>
    private const double BeatInterval = 15;
    private static readonly Vector3 CameraOffset = new(0, 15, 9.5f);
    private static readonly Color Gold = new(1f, 0.82f, 0.25f), Night = new(0.08f, 0.1f, 0.2f, 0.82f);

    private sealed class Tower
    {
        public Vector2 Pos;
        public TowerTarget? Target;
        public int Ammo = StartAmmo, Refills;
        public float Cooldown;
        public bool Strong => Ammo > StrongAmmo || Refills >= MasterRefills;
    }

    private sealed class Monster
    {
        public int Path;
        public float Distance, Speed, Remaining, DyingTime;
        public int Hp, MaxHp;
        public bool Dying;
        public Node3D Node = null!;
        public AnimationPlayer? Anim;
    }

    private sealed class Token
    {
        public required TowerToken Data;
        public Vector2 Pos;
        public float Age;
        /// <summary>Laid down by the hero: it stays until picked up again instead of expiring.</summary>
        public bool Placed;
        public Node3D Node = null!;
    }

    private sealed class Bolt
    {
        public Monster Target = null!;
        public MeshInstance3D Node = null!;
        public int Damage = 1;
    }

    private PuglingApi _api = null!;
    private TowerRunResponse _run = null!;
    private LevelDef _def = null!;
    private MapInfo _map = null!;
    private TokenPool _pool = null!;
    private readonly Queue<TowerTarget> _nextTargets = new();
    private readonly List<Tower> _towers = [];
    private readonly List<Monster> _monsters = [];
    private readonly List<Token> _tokens = [];
    private readonly List<Bolt> _bolts = [];
    private readonly Dictionary<string, TowerToken> _tokenById = [];
    private float[] _pathLengths = [];

    private Camera3D _camera = null!;
    private Node3D _hero = null!, _heroModel = null!, _carriedBook = null!;
    private AnimationPlayer? _heroAnim;
    private Vector2 _heroPos;
    private float _heroYaw, _actionTime;
    private TowerToken? _carried;
    private int _correct, _total, _lives = BaseLives, _spawned;
    private float _spawnTimer = 2f, _elapsed, _clock, _heroCooldown;
    private int _heroShots, _heroKills;
    private bool _busy, _over, _beating, _paused;
    private double _sinceBeat;

    // Left thumb: a floating joystick wherever it lands on the left part of the screen.
    private int _stickFinger = -1;
    private Vector2 _stickCenter, _stickVector;

    private Painter _painter = null!;
    private VBoxContainer _pickPanel = null!;
    private Button _giveButton = null!, _dropButton = null!, _pauseButton = null!;
    private Control _pauseMenu = null!;
    private Label _feedback = null!;
    private Tower? _giveTower;
    private string _pickSignature = "";

    private static readonly SphereMesh BoltMesh = new() { Radius = 0.16f, Height = 0.32f, RadialSegments = 8, Rings = 4 };
    private static readonly StandardMaterial3D BoltMaterial = Unshaded(new Color(1f, 0.9f, 0.4f)),
        StrongBoltMaterial = Unshaded(new Color(1f, 0.45f, 0.1f)), HeroBoltMaterial = Unshaded(new Color(0.55f, 0.85f, 1f));

    private static StandardMaterial3D Unshaded(Color color) =>
        new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = color };

    /// <summary>Wires the level to a run and a map; call before adding it to the tree.</summary>
    public void Setup(PuglingApi api, TowerRunResponse run, LevelDef def)
    {
        _api = api;
        _run = run;
        _def = def;
        _pool = new TokenPool(run.Tokens.Select(t => t.TokenId));
        foreach (var t in run.Tokens) _tokenById[t.TokenId] = t;
        foreach (var target in run.Towers) _nextTargets.Enqueue(target);
        _pathLengths = def.Paths.Select(Geo.Length).ToArray();
        _heroPos = def.PlayerStart;
    }

    /// <summary>
    /// One line in the game log with the local time. project.godot turns on file logging, so the lines survive in
    /// user://logs on the phone - its logcat buffer is overwritten within minutes.
    /// </summary>
    private static void Log(string line) => GD.Print($"TURM {Time.GetTimeStringFromSystem()} {line}");

    /// <inheritdoc />
    public override void _Ready()
    {
        Log($"start Lauf {_run.RunId} Karte {_def.Name}, Ziel {_run.Goal}");
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.42f, 0.62f, 0.4f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.8f, 0.85f, 1f),
                AmbientLightEnergy = 0.5f,
            },
        });
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-58, -35, 0), LightEnergy = 1.0f, ShadowEnabled = true,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Orthogonal, DirectionalShadowMaxDistance = 40,
        });

        _map = MapBuilder.Build(_def, this);

        for (var i = 0; i < _def.TowerSpots.Length; i++)
        {
            var spot = _def.TowerSpots[i];
            var model = Models.Scenery(i % 2 == 0 ? "building_tower_A_blue" : "building_tower_B_blue").Instantiate<Node3D>();
            model.Position = spot.To3();
            model.Scale = Vector3.One * 1.6f;
            AddChild(model);
            AddChild(Ring(spot.To3(0.06f), TowerRange, new Color(1, 1, 1, 0.13f)));
            _map.Obstacles.Add((spot, 1.0f));
            _towers.Add(new Tower { Pos = spot, Target = _nextTargets.Count > 0 ? _nextTargets.Dequeue() : null });
        }

        _hero = new Node3D();
        AddChild(_hero);
        _heroModel = Models.Character("Knight.glb").Instantiate<Node3D>();
        _heroModel.Scale = Vector3.One * 0.8f;
        Models.KeepOnly(_heroModel, "Knight_", "1H_Sword", "Round_Shield");
        _hero.AddChild(_heroModel);
        _heroAnim = Models.Animator(_heroModel);
        _hero.AddChild(Ring(new Vector3(0, 0.07f, 0), PickupRadius, new Color(1f, 0.9f, 0.4f, 0.35f)));
        _carriedBook = Models.Character("spellbook_closed.gltf").Instantiate<Node3D>();
        _carriedBook.Position = new Vector3(0, 2.5f, 0);
        _carriedBook.Scale = Vector3.One * 1.4f;
        _carriedBook.Visible = false;
        _hero.AddChild(_carriedBook);

        _camera = new Camera3D { Fov = 42 };
        AddChild(_camera);
        _camera.LookAtFromPosition(CameraOffset, Vector3.Zero);
        _camera.MakeCurrent();
        PlaceHero();

        BuildUi();
    }

    private static MeshInstance3D Ring(Vector3 at, float radius, Color color) => new()
    {
        Mesh = new TorusMesh { InnerRadius = radius - 0.12f, OuterRadius = radius, Rings = 48, RingSegments = 4 },
        Position = at,
        Scale = new Vector3(1, 0.05f, 1),
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        },
    };

    private void BuildUi()
    {
        var ui = new CanvasLayer();
        AddChild(ui);
        _painter = new Painter { MouseFilter = Control.MouseFilterEnum.Ignore, Paint = PaintOverlay };
        _painter.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ui.AddChild(_painter);

        _pickPanel = new VBoxContainer();
        _pickPanel.AddThemeConstantOverride("separation", 10);
        ui.AddChild(_pickPanel);

        _giveButton = UiButton("", new Color(0.18f, 0.62f, 0.32f), () => _ = AssignAsync(), 34);
        _giveButton.CustomMinimumSize = new Vector2(300, 110);
        _giveButton.Visible = false;
        ui.AddChild(_giveButton);

        _dropButton = UiButton("", new Color(0.3f, 0.4f, 0.6f), DropCarried, 26);
        _dropButton.CustomMinimumSize = new Vector2(240, 70);
        _dropButton.Visible = false;
        ui.AddChild(_dropButton);

        _pauseButton = UiButton("II", Night, TogglePause, 30);
        _pauseButton.CustomMinimumSize = new Vector2(84, 72);
        ui.AddChild(_pauseButton);

        _feedback = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = Colors.Transparent };
        _feedback.AddThemeFontSizeOverride("font_size", 36);
        _feedback.AddThemeColorOverride("font_outline_color", Colors.Black);
        _feedback.AddThemeConstantOverride("outline_size", 10);
        ui.AddChild(_feedback);

        // The pause menu dims the level and stops it: carry on, or stop here and see the result.
        _pauseMenu = new ColorRect { Color = new Color(0, 0, 0, 0.6f), Visible = false };
        _pauseMenu.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var menu = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        menu.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        menu.AddThemeConstantOverride("separation", 22);
        var title = Caption("Pause");
        title.AddThemeFontSizeOverride("font_size", 44);
        menu.AddChild(title);
        foreach (var button in new[]
        {
            UiButton("Weiter", new Color(0.18f, 0.62f, 0.32f), TogglePause),
            UiButton("Aufhören", new Color(0.7f, 0.25f, 0.2f), () => End(LevelEnd.GaveUp)),
        })
        {
            button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            menu.AddChild(button);
        }
        _pauseMenu.AddChild(menu);
        ui.AddChild(_pauseMenu);
    }

    /// <summary>Opens or closes the pause menu (pause button, Android back button).</summary>
    public void TogglePause()
    {
        if (_over) return;
        _paused = !_paused;
        _pauseMenu.Visible = _paused;
        Log(_paused ? "pause" : "weiter");
        _stickFinger = -1;
        _stickVector = Vector2.Zero;
    }

    /// <summary>A big rounded touch button in the game's style.</summary>
    private static Button UiButton(string text, Color color, Action onPressed, int fontSize = 30)
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

    /// <inheritdoc />
    public override void _Input(InputEvent e)
    {
        // The joystick lives on the left part of the screen; the right belongs to the buttons (two-thumb layout).
        if (_paused) return;
        if (e is InputEventScreenTouch touch)
        {
            if (touch.Pressed && _stickFinger < 0 && touch.Position.X < GetViewport().GetVisibleRect().Size.X * 0.45f)
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
            _stickVector = offset.Length() < 10 ? Vector2.Zero : offset.LimitLength(90) / 90f;
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _clock += dt;
        if (_paused)
        {
            _painter.QueueRedraw();
            return;
        }
        if (!_over)
        {
            _elapsed += dt;
            _sinceBeat += delta;
            if (_sinceBeat >= BeatInterval && !_beating) _ = BeatAsync();
            MoveHero(dt);
            SpawnMonsters(dt);
            MoveMonsters(dt);
            ShootTowers(dt);
            ShootHero(dt);
            UpdateContext();
        }
        MoveBolts(dt);
        AnimateTokens(dt);
        LayoutUi();
        _painter.QueueRedraw();
    }

    private void MoveHero(float dt)
    {
        _actionTime -= dt;
        // Keyboard arrows as a desktop fallback for the left thumb. Screen up is world -Z (the camera looks north).
        var keys = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        var move = _stickVector != Vector2.Zero ? _stickVector : keys;
        if (move.Length() > 0.05f)
        {
            var step = move * HeroSpeed * dt;
            if (_map.Walkable(_heroPos + step, HeroRadius)) _heroPos += step;
            else if (_map.Walkable(_heroPos + new Vector2(step.X, 0), HeroRadius)) _heroPos.X += step.X;
            else if (_map.Walkable(_heroPos + new Vector2(0, step.Y), HeroRadius)) _heroPos.Y += step.Y;
            // The model turns smoothly towards where the thumb points, so the direction of travel is always visible.
            _heroYaw = Mathf.LerpAngle(_heroYaw, Mathf.Atan2(move.X, move.Y), 1 - Mathf.Exp(-14 * dt));
            if (_actionTime <= 0) Models.Play(_heroAnim, true, "Running_A");
        }
        else if (_actionTime <= 0) Models.Play(_heroAnim, true, "Idle");
        PlaceHero();
    }

    private void PlaceHero()
    {
        _hero.Position = _heroPos.To3();
        _heroModel.Rotation = new Vector3(0, _heroYaw, 0);
        _carriedBook.Position = new Vector3(0, 2.5f + 0.12f * Mathf.Sin(_clock * 4), 0);
        _carriedBook.Rotation = new Vector3(0, _clock * 1.5f, 0);
        // The camera keeps the hero in the centre: the map moves, the hero stays.
        _camera.Position = _hero.Position + new Vector3(0, 0.8f, 0) + CameraOffset;
    }

    private void SpawnMonsters(float dt)
    {
        if ((_spawnTimer -= dt) > 0) return;
        _spawnTimer = Mathf.Max(_def.SpawnInterval * 0.55f, _def.SpawnInterval - _elapsed * 0.012f);
        var warrior = ++_spawned % 4 == 0;
        var node = Models.Character(warrior ? "Skeleton_Warrior.glb" : "Skeleton_Minion.glb").Instantiate<Node3D>();
        node.Scale = Vector3.One * (warrior ? 0.85f : 0.72f);
        Models.KeepOnly(node, warrior ? "Skeleton_Warrior_" : "Skeleton_Minion_");
        var monster = new Monster
        {
            Path = _spawned % _def.Paths.Length, Node = node, Anim = Models.Animator(node),
            Hp = warrior ? 4 : 2, MaxHp = warrior ? 4 : 2,
            Speed = (_def.MonsterSpeed + Mathf.Min(0.8f, _elapsed * 0.004f)) * (warrior ? 0.8f : 1f),
        };
        AddChild(node);
        Models.Play(monster.Anim, true, "Walking_A", "Walking_B", "Running_A");
        _monsters.Add(monster);
        PlaceMonster(monster);
    }

    private void PlaceMonster(Monster m)
    {
        var (pos, dir) = Geo.PointAt(_def.Paths[m.Path], m.Distance);
        m.Node.Position = pos.To3(0.05f);
        m.Node.Rotation = new Vector3(0, Mathf.Atan2(dir.X, dir.Y), 0);
        m.Remaining = _pathLengths[m.Path] - m.Distance;
    }

    private void MoveMonsters(float dt)
    {
        for (var i = _monsters.Count - 1; i >= 0; i--)
        {
            var m = _monsters[i];
            if (m.Dying)
            {
                if ((m.DyingTime -= dt) > 0) continue;
                m.Node.QueueFree();
                _monsters.RemoveAt(i);
                continue;
            }
            m.Distance += m.Speed * dt;
            if (m.Distance < _pathLengths[m.Path])
            {
                PlaceMonster(m);
                continue;
            }
            m.Node.QueueFree();
            _monsters.RemoveAt(i);
            Burst(_def.Base.To3(2.5f), new Color(1, 0.3f, 0.2f), 30);
            Flash("Ein Skelett hat die Burg erreicht!", Colors.Salmon);
            if (--_lives <= 0) End(LevelEnd.CastleFallen);
        }
    }

    private void ShootTowers(float dt)
    {
        foreach (var tower in _towers)
        {
            if ((tower.Cooldown -= dt) > 0 || tower.Ammo <= 0) continue;
            var target = _monsters.Where(m => !m.Dying && m.Node.Position.Flat().DistanceTo(tower.Pos) <= TowerRange)
                .OrderBy(m => m.Remaining).FirstOrDefault();
            if (target is null) continue;
            tower.Cooldown = ShotInterval;
            var strong = tower.Strong;
            tower.Ammo--;
            Fire(tower.Pos.To3(3.6f), target, strong ? StrongDamage : 1, strong ? StrongBoltMaterial : BoltMaterial, strong ? 1.6f : 1f);
        }
    }

    // Plan B: the hero shoots the nearest skeleton in reach by himself; no ammo, so empty towers never end a run alone.
    private void ShootHero(float dt)
    {
        if ((_heroCooldown -= dt) > 0) return;
        var target = _monsters.Where(m => !m.Dying && m.Node.Position.Flat().DistanceTo(_heroPos) <= HeroRange)
            .OrderBy(m => m.Node.Position.Flat().DistanceTo(_heroPos)).FirstOrDefault();
        if (target is null) return;
        _heroCooldown = HeroShotInterval;
        _heroShots++;
        Fire(_heroPos.To3(1.4f), target, 1, HeroBoltMaterial, 0.8f);
    }

    private void Fire(Vector3 from, Monster target, int damage, Material material, float size)
    {
        var bolt = new MeshInstance3D
        {
            Mesh = BoltMesh, MaterialOverride = material, Position = from, Scale = Vector3.One * size,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(bolt);
        _bolts.Add(new Bolt { Target = target, Node = bolt, Damage = damage });
    }

    private void MoveBolts(float dt)
    {
        for (var i = _bolts.Count - 1; i >= 0; i--)
        {
            var bolt = _bolts[i];
            // The skeleton may be gone already (freed after its death); the bolt then just fizzles where it is.
            var alive = IsInstanceValid(bolt.Target.Node) && !bolt.Target.Dying;
            var dest = alive ? bolt.Target.Node.Position + new Vector3(0, 1f, 0) : bolt.Node.Position;
            var to = dest - bolt.Node.Position;
            var step = BoltSpeed * dt;
            if (to.Length() > step)
            {
                bolt.Node.Position += to.Normalized() * step;
                continue;
            }
            bolt.Node.QueueFree();
            _bolts.RemoveAt(i);
            if (!alive) continue;
            Burst(dest, new Color(1f, 0.95f, 0.8f), 10, 0.4f);
            if ((bolt.Target.Hp -= bolt.Damage) > 0) continue;
            if (bolt.Node.MaterialOverride == HeroBoltMaterial) _heroKills++;
            Defeat(bolt.Target);
        }
    }

    private void Defeat(Monster m)
    {
        m.Dying = true;
        m.DyingTime = 1.3f;
        Models.Play(m.Anim, false, "Death_A", "Death_B");
        if (_pool.Draw() is not { } id) return;
        // The book lands beside the road, inside the map, where the hero can reach it.
        var (pos, dir) = Geo.PointAt(_def.Paths[m.Path], m.Distance);
        var side = dir.Orthogonal() * (GD.Randf() < 0.5f ? 1.6f : -1.6f);
        DropToken(_tokenById[id], FreeSpot(pos + side), placed: false);
    }

    /// <summary>
    /// The nearest walkable spot to <paramref name="near"/> that keeps a little distance to every other book, so words
    /// never pile up into one unreadable heap. Searches outwards in rings.
    /// </summary>
    private Vector2 FreeSpot(Vector2 near)
    {
        const float Gap = 2.4f;
        var lo = new Vector2(1, 1);
        var hi = _def.Size - new Vector2(1, 1);
        for (var ring = 0; ring <= 6; ring++)
        {
            var steps = ring == 0 ? 1 : ring * 8;
            for (var k = 0; k < steps; k++)
            {
                var angle = Mathf.Tau * k / steps;
                var spot = (near + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 1.1f).Clamp(lo, hi);
                if (_map.Walkable(spot, 0.3f) && _tokens.All(t => t.Pos.DistanceTo(spot) >= Gap)) return spot;
            }
        }
        return near.Clamp(lo, hi);
    }

    private void DropToken(TowerToken data, Vector2 at, bool placed)
    {
        var node = Models.Character("spellbook_closed.gltf").Instantiate<Node3D>();
        node.Scale = Vector3.One * 1.6f;
        AddChild(node);
        // A white ring marks a word the hero laid down himself: it stays.
        node.AddChild(Ring(new Vector3(0, -0.3f, 0), 0.55f, placed ? new Color(1, 1, 1, 0.85f) : new Color(Gold, 0.7f)));
        _tokens.Add(new Token { Data = data, Pos = at, Node = node, Placed = placed });
        Burst(at.To3(0.8f), Gold, 14, 0.5f);
    }

    private void AnimateTokens(float dt)
    {
        for (var i = _tokens.Count - 1; i >= 0; i--)
        {
            var token = _tokens[i];
            // Expired books go back to the pool, so the field never fills up with words nobody picks.
            if (!_over && !token.Placed && (token.Age += dt) >= TokenLifetime)
            {
                _pool.Return(token.Data.TokenId);
                token.Node.QueueFree();
                _tokens.RemoveAt(i);
                continue;
            }
            var blink = !token.Placed && token.Age > TokenLifetime - 5 && Mathf.PosMod(_clock, 0.5f) < 0.2f;
            token.Node.Visible = !blink;
            token.Node.Position = token.Pos.To3(0.6f + 0.15f * Mathf.Sin(_clock * 3 + i));
            token.Node.Rotation = new Vector3(0, _clock * 1.2f + i, 0);
        }
    }

    private void UpdateContext()
    {
        // Every book within reach is offered on the right, nearest first.
        var near = _tokens.Where(t => t.Pos.DistanceTo(_heroPos) <= PickupRadius)
            .OrderBy(t => t.Pos.DistanceTo(_heroPos)).Take(5).ToList();
        var signature = string.Join("|", near.Select(t => t.Data.TokenId));
        if (signature != _pickSignature)
        {
            _pickSignature = signature;
            foreach (var child in _pickPanel.GetChildren()) child.QueueFree();
            if (near.Count > 0) _pickPanel.AddChild(Caption("Aufheben:"));
            foreach (var token in near)
                _pickPanel.AddChild(UiButton(token.Data.Word, new Color(0.85f, 0.6f, 0.12f), () => PickUp(token)));
        }

        // The give button stays up as long as the hero carries a word next to a tower that still asks for one.
        _giveTower = _carried is null || _busy ? null
            : _towers.Where(t => t.Target is not null && t.Pos.DistanceTo(_heroPos) <= TowerReach)
                .OrderBy(t => t.Pos.DistanceTo(_heroPos)).FirstOrDefault();
        _giveButton.Visible = _giveTower is not null;
        if (_giveTower is not null) _giveButton.Text = $"„{_carried!.Word}“\ngeben";
        _dropButton.Visible = _carried is not null && !_busy;
        if (_carried is not null) _dropButton.Text = $"„{_carried.Word}“ ablegen";
    }

    // Sorting words: the carried word goes down next to the hero and stays there for later.
    private void DropCarried()
    {
        if (_over || _busy || _carried is null) return;
        DropToken(_carried, FreeSpot(_heroPos), placed: true);
        Log($"abgelegt '{_carried.Word}'");
        _carried = null;
        _carriedBook.Visible = false;
        _actionTime = 0.4f;
        Models.Play(_heroAnim, false, "PickUp", "Interact");
        _pickSignature = "";
    }

    private static Label Caption(string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 8);
        return label;
    }

    private void LayoutUi()
    {
        var screen = GetViewport().GetVisibleRect().Size;
        _pickPanel.Position = new Vector2(screen.X - _pickPanel.Size.X - 28, 110);
        _giveButton.Position = screen - _giveButton.Size - new Vector2(28, 28);
        _dropButton.Position = new Vector2(screen.X - _dropButton.Size.X - 28,
            (_giveButton.Visible ? _giveButton.Position.Y : screen.Y) - _dropButton.Size.Y - 18);
        _pauseButton.Position = new Vector2(screen.X - _pauseButton.Size.X - 20, 14);
        _feedback.Size = new Vector2(screen.X, 60);
        _feedback.Position = new Vector2(0, screen.Y * 0.16f);
    }

    private void PickUp(Token token)
    {
        if (_over || !_tokens.Remove(token)) return;
        token.Node.QueueFree();
        // One word at a time (B-822): picking another one puts the carried one down here.
        if (_carried is not null) DropToken(_carried, FreeSpot(_heroPos), placed: true);
        _carried = token.Data;
        _carriedBook.Visible = true;
        _actionTime = 0.5f;
        Models.Play(_heroAnim, false, "PickUp", "Interact");
        _pickSignature = "";
    }

    private async Task AssignAsync()
    {
        if (_busy || _over || _giveTower is not { Target: { } target } tower || _carried is not { } token) return;
        _busy = true;
        _giveButton.Visible = false;
        _actionTime = 0.6f;
        Models.Play(_heroAnim, false, "Interact", "PickUp");
        try
        {
            var result = await _api.AssignAsync(_run.RunId, token.TokenId, target.TowerId);
            _total++;
            _carried = null;
            _carriedBook.Visible = false;
            Log($"assign '{token.Word}' -> '{target.Prompt}': {(result.Correct ? "richtig" : "falsch, erwartet " + result.Expected)}");
            if (result.Correct)
            {
                _correct++;
                _pool.Consume(token.TokenId);
                var wasStrong = tower.Strong;
                tower.Ammo += RefillAmmo;
                tower.Refills++;
                tower.Target = _nextTargets.Count > 0 ? _nextTargets.Dequeue() : null;
                Burst(tower.Pos.To3(3.2f), new Color(0.4f, 1f, 0.5f), 40);
                Flash(!wasStrong && tower.Strong
                    ? "Richtig! Der Turm schießt jetzt Feuerpfeile ★"
                    : $"Richtig! „{token.Word}“ = „{target.Prompt}“  +{RefillAmmo} Schuss", Colors.LightGreen);
                if (_correct >= _run.Goal) End(LevelEnd.GoalReached);
            }
            else
            {
                // The word is gone from the hand; it goes back into the pool and drops again later.
                _pool.Return(token.TokenId);
                tower.Ammo = Math.Max(0, tower.Ammo - WrongPenalty);
                Burst(tower.Pos.To3(3.2f), new Color(1f, 0.25f, 0.2f), 40);
                Flash($"Falsch – „{target.Prompt}“ heißt „{result.Expected}“  −{WrongPenalty} Schuss", Colors.Salmon);
            }
        }
        catch (ApiException ex)
        {
            Flash(ex.Message, Colors.Orange);
            if (ex.RunClosed) End(LevelEnd.TimeUp);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>A one-shot burst of little sparks: hits, rewards, mistakes.</summary>
    private void Burst(Vector3 at, Color color, int amount, float size = 1f)
    {
        var particles = new CpuParticles3D
        {
            Emitting = true, OneShot = true, Explosiveness = 0.95f, Amount = amount, Lifetime = 0.9,
            Mesh = new SphereMesh { Radius = 0.09f * size, Height = 0.18f * size, RadialSegments = 6, Rings = 3 },
            MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = color },
            Direction = Vector3.Up, Spread = 70, InitialVelocityMin = 3 * size, InitialVelocityMax = 7 * size,
            Gravity = new Vector3(0, -12, 0), ScaleAmountMin = 0.6f, ScaleAmountMax = 1.4f,
            Position = at, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(particles);
        GetTree().CreateTimer(1.3).Timeout += particles.QueueFree;
    }

    private void Flash(string text, Color color)
    {
        _feedback.Text = text;
        _feedback.AddThemeColorOverride("font_color", color);
        var tween = CreateTween();
        _feedback.Modulate = Colors.White;
        _feedback.PivotOffset = _feedback.Size / 2;
        _feedback.Scale = Vector2.One * 1.25f;
        tween.TweenProperty(_feedback, "scale", Vector2.One, 0.25);
        tween.TweenInterval(2.6);
        tween.TweenProperty(_feedback, "modulate", Colors.Transparent, 0.5);
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
                End(LevelEnd.TimeUp);
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
    private void End(LevelEnd reason)
    {
        if (_over) return;
        _over = true;
        _paused = false;
        _pauseMenu.Visible = false;
        _pauseButton.Visible = false;
        _giveButton.Visible = false;
        _dropButton.Visible = false;
        foreach (var child in _pickPanel.GetChildren()) child.QueueFree();
        Models.Play(_heroAnim, false, _correct >= _run.Goal ? "Cheer" : "Death_A");
        Log($"ende {reason} {_correct}/{_total} richtig, {_elapsed:0}s, Burg {_lives}/{BaseLives}, Heldenschüsse {_heroShots}, Heldentreffer {_heroKills}");
        // A short moment to see the hero cheer (or fall) before the result screen.
        GetTree().CreateTimer(reason == LevelEnd.GaveUp ? 0.3 : 1.6).Timeout += () => Finished?.Invoke(_run.RunId, _correct, _total, reason);
    }

    // ---- 2D overlay: everything that must stay readable on a phone is drawn flat on top of the 3D scene. ----

    private void PaintOverlay(Painter p)
    {
        var screen = p.GetViewportRect().Size;

        foreach (var token in _tokens.Where(t => t.Node.Visible))
            if (OnScreen(token.Node.Position + new Vector3(0, 1.1f, 0), screen, 0, out var at))
                p.Pill(at, token.Data.Word, 24, new Color(Gold, 0.95f), new Color(0.2f, 0.12f, 0f));

        foreach (var m in _monsters.Where(m => !m.Dying))
            if (OnScreen(m.Node.Position + new Vector3(0, 2.1f, 0), screen, 0, out var at))
            {
                var bar = new Rect2(at - new Vector2(26, 5), new Vector2(52, 10));
                p.Box(bar.Grow(2), new Color(0, 0, 0, 0.6f), radius: 5);
                p.Box(new Rect2(bar.Position, new Vector2(bar.Size.X * m.Hp / m.MaxHp, bar.Size.Y)),
                    m.Hp * 2 > m.MaxHp ? new Color(0.4f, 0.9f, 0.3f) : new Color(0.95f, 0.35f, 0.25f), radius: 4);
            }

        foreach (var tower in _towers) PaintTower(p, tower, screen);

        if (_carried is not null && OnScreen(_hero.Position + new Vector3(0, 3.4f, 0), screen, 0, out var head))
            p.Pill(head, _carried.Word, 26, Gold, new Color(0.2f, 0.12f, 0f), Colors.White);

        // HUD: hits towards the goal and the castle's lives.
        var hud = new Rect2(16, 14, 330, 64);
        p.Box(hud, Night, new Color(1, 1, 1, 0.25f), 2, 18);
        p.Text(new Vector2(hud.Position.X + 82, hud.GetCenter().Y), $"{_correct}/{_run.Goal}", 30, Gold);
        p.Text(new Vector2(hud.Position.X + 82, hud.GetCenter().Y + 25), "Treffer", 14, Colors.White, 0);
        for (var i = 0; i < BaseLives; i++)
            p.Box(new Rect2(hud.Position.X + 160 + i * 16, hud.GetCenter().Y - 9, 12, 18),
                i < _lives ? new Color(0.95f, 0.3f, 0.35f) : new Color(1, 1, 1, 0.15f), radius: 4);
        p.Text(new Vector2(hud.Position.X + 238, hud.GetCenter().Y + 25), "Burg", 14, Colors.White, 0);

        // Joystick: a hint where the left thumb goes, and the stick while it is held.
        var center = _stickFinger >= 0 ? _stickCenter : new Vector2(150, screen.Y - 150);
        p.DrawCircle(center, 95, new Color(1, 1, 1, 0.12f));
        p.DrawArc(center, 95, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.35f), 3, true);
        p.DrawCircle(center + _stickVector * 90, 40, new Color(1, 1, 1, 0.45f));
    }

    private void PaintTower(Painter p, Tower tower, Vector2 screen)
    {
        var low = tower.Target is not null && tower.Ammo <= WrongPenalty;
        var chosen = tower == _giveTower;
        var border = chosen ? Gold : low ? new Color(1f, 0.3f, 0.25f) : new Color(1, 1, 1, 0.3f);
        var label = tower.Target is null ? "fertig" : tower.Strong ? "★ " + tower.Target.Prompt : tower.Target.Prompt;
        if (OnScreen(tower.Pos.To3(4.6f), screen, 40, out var at))
        {
            var width = Mathf.Max(150, ThemeDB.FallbackFont.GetStringSize(label, fontSize: 26).X + 34);
            var panel = new Rect2(at - new Vector2(width / 2, 40), new Vector2(width, 66));
            var pulse = chosen ? 1 + 0.06f * Mathf.Sin(_clock * 8) : 1;
            panel = panel.Grow((pulse - 1) * 40);
            var fillColor = tower.Target is null ? new Color(0.2f, 0.2f, 0.2f, 0.7f) : tower.Strong ? new Color(0.45f, 0.18f, 0.05f, 0.88f) : Night;
            p.Box(panel, fillColor, border, chosen ? 5 : 2, 16);
            p.Text(new Vector2(panel.GetCenter().X, panel.Position.Y + 22), label, 26, tower.Target is null ? new Color(1, 1, 1, 0.6f) : Colors.White);
            if (tower.Target is null) return;
            var bar = new Rect2(panel.Position.X + 14, panel.End.Y - 22, panel.Size.X - 64, 12);
            p.Box(bar, new Color(0, 0, 0, 0.55f), radius: 6);
            var fill = Mathf.Clamp((float)tower.Ammo / FullAmmoBar, 0, 1);
            if (fill > 0) p.Box(new Rect2(bar.Position, new Vector2(Mathf.Max(12, bar.Size.X * fill), bar.Size.Y)), AmmoColor(tower.Ammo), radius: 6);
            p.Text(new Vector2(panel.End.X - 26, bar.GetCenter().Y), tower.Ammo.ToString(), 20, AmmoColor(tower.Ammo), 4);
            return;
        }

        if (tower.Target is null) return;
        // Off screen: a badge on the edge points the way, red when the tower is running dry.
        var mid = screen / 2;
        var dir = (at - mid).Normalized();
        var inset = new Vector2(70, 70);
        var scale = Mathf.Min((mid.X - inset.X) / Mathf.Max(0.001f, Mathf.Abs(dir.X)), (mid.Y - inset.Y) / Mathf.Max(0.001f, Mathf.Abs(dir.Y)));
        var edge = mid + dir * scale;
        var color = low ? new Color(0.85f, 0.2f, 0.2f, 0.92f) : Night;
        var tip = edge + dir * 46;
        var side = dir.Orthogonal() * 16;
        p.DrawColoredPolygon([tip, edge + dir * 24 + side, edge + dir * 24 - side], border);
        p.Box(new Rect2(edge - new Vector2(34, 34), new Vector2(68, 68)), color, border, 3, 34);
        p.Text(edge, tower.Ammo.ToString(), 26, AmmoColor(tower.Ammo));
        p.Pill(edge + new Vector2(0, dir.Y > 0 ? -54 : 54), label, 18, Night, Colors.White);
    }

    private static Color AmmoColor(int ammo) =>
        ammo <= WrongPenalty ? new Color(1f, 0.35f, 0.3f) : ammo <= 6 ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 0.95f, 0.45f);

    private bool OnScreen(Vector3 world, Vector2 screen, float margin, out Vector2 at)
    {
        at = _camera.UnprojectPosition(world);
        if (_camera.IsPositionBehind(world)) at = screen - at;
        return at.X >= margin && at.Y >= margin && at.X <= screen.X - margin && at.Y <= screen.Y - margin;
    }
}
