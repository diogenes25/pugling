using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Combat;
using PuglingTower.Core;
using PuglingTower.Heroes;
using PuglingTower.Net;
using PuglingTower.Ui;
using PuglingTower.Words;
using PuglingTower.World;

namespace PuglingTower.Play;

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
/// <para>
/// The level only coordinates: each part lives in its own class (hero, enemies, towers, projectiles, word books, run
/// session, controls, overlay), the rules run here in one frame order, and everything worth a sound or an effect is
/// raised on <see cref="Events"/>.
/// </para>
/// </summary>
public partial class Level : Node3D
{
    private static readonly Vector3 CameraOffset = new(0, 15, 9.5f);

    /// <summary>Raised once when the level ends: run id, correct and total assignments, and why it ended.</summary>
    public event Action<int, int, int, LevelEnd>? Finished;

    /// <summary>The level's events for sound and effects; subscribe before the level enters the tree.</summary>
    public GameEvents Events { get; } = new();

    private RunSession _session = null!;
    private LevelDef _def = null!;
    private RunTally _tally = null!;
    private MapInfo _map = null!;
    private Hero _hero = null!;
    private Enemies _enemies = null!;
    private Towers _towers = null!;
    private Projectiles _projectiles = null!;
    private WordBooks _words = null!;
    private Camera3D _camera = null!;
    private Painter _painter = null!;
    private LevelControls _controls = null!;
    private LevelOverlay _overlay = null!;
    private readonly TouchStick _stick = new();

    private Tower? _giveTower;
    private float _clock;
    private bool _busy, _over, _paused;

    /// <summary>Wires the level to a run and a map; call before adding it to the tree.</summary>
    public void Setup(ITowerRunServer server, TowerRunResponse run, LevelDef def)
    {
        _session = new RunSession(server, run);
        _def = def;
        _tally = new RunTally(run.Goal);
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        RunSession.Log($"start Lauf {_session.Run.RunId} Karte {_def.Name}, Ziel {_session.Run.Goal}");
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
        _towers = new Towers(this, _def, _map, TowerKind.Bolt, _session.NextTarget);
        // The hero's look is the one line to change for another figure (see IHeroLook).
        _hero = new Hero(this, _map, new KnightLook(), _def.PlayerStart);
        _enemies = new Enemies(this, _def);
        _projectiles = new Projectiles(this);
        _words = new WordBooks(this, _def, _map, _session.Run.Tokens);

        _camera = new Camera3D { Fov = 42 };
        AddChild(_camera);
        _camera.LookAtFromPosition(CameraOffset, Vector3.Zero);
        _camera.MakeCurrent();
        FollowHero();

        var ui = new CanvasLayer();
        AddChild(ui);
        _overlay = new LevelOverlay(_camera, _words, _enemies, _towers, _hero, _tally, _stick);
        _painter = new Painter { MouseFilter = Control.MouseFilterEnum.Ignore, Paint = p => _overlay.Paint(p, new OverlayFrame(_clock, _giveTower)) };
        _painter.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ui.AddChild(_painter);
        _controls = new LevelControls(ui, () => _ = AssignAsync(), LayDown, TogglePause, () => End(LevelEnd.GaveUp));

        LevelFeedback.Attach(Events, this, _controls.Say);
    }

    /// <summary>Opens or closes the pause menu (pause button, Android back button).</summary>
    public void TogglePause()
    {
        if (_over) return;
        _paused = !_paused;
        _controls.ShowPause(_paused);
        RunSession.Log(_paused ? "pause" : "weiter");
        _stick.Release();
        Events.RaisePauseChanged(_paused);
    }

    /// <inheritdoc />
    public override void _Input(InputEvent e)
    {
        if (!_paused) _stick.Handle(e, GetViewport().GetVisibleRect().Size.X);
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
            _tally.Elapsed += dt;
            _session.Tick(delta, OnTimeUp);
            MoveHero(dt);
            SpawnEnemies(dt);
            MoveEnemies(dt);
            ShootTowers(dt);
            ShootHero(dt);
            UpdateContext();
        }
        MoveProjectiles(dt);
        _words.Animate(dt, _clock, frozen: _over);
        _controls.Layout(GetViewport().GetVisibleRect().Size);
        _painter.QueueRedraw();
    }

    private void MoveHero(float dt)
    {
        // Keyboard arrows as a desktop fallback for the left thumb. Screen up is world -Z (the camera looks north).
        var keys = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        _hero.Move(dt, _stick.Vector != Vector2.Zero ? _stick.Vector : keys, _clock);
        FollowHero();
    }

    // The camera keeps the hero in the centre: the map moves, the hero stays.
    private void FollowHero() => _camera.Position = _hero.Node.Position + new Vector3(0, 0.8f, 0) + CameraOffset;

    private void SpawnEnemies(float dt)
    {
        if (_enemies.Spawn(dt, _tally.Elapsed) is { } enemy) Events.RaiseEnemySpawned(enemy);
    }

    private void MoveEnemies(float dt)
    {
        for (var arrived = _enemies.Move(dt); arrived > 0; arrived--)
        {
            Events.RaiseCastleHit(_def.Base.To3(2.5f));
            if (_tally.CastleHit()) End(LevelEnd.CastleFallen);
        }
    }

    private void ShootTowers(float dt)
    {
        foreach (var shot in _towers.Shoot(dt, _enemies, _projectiles)) Events.RaiseShotFired(shot.Projectile);
    }

    // Plan B: the hero shoots the nearest skeleton in reach by himself; no ammo, so empty towers never end a run alone.
    private void ShootHero(float dt)
    {
        if (!_hero.Reload.Tick(dt)) return;
        if (_enemies.Nearest(_hero.Position, Balance.HeroRange) is not { } target) return;
        _hero.Reload.Start(Balance.HeroShotInterval);
        _tally.HeroShot();
        _hero.Look.Play(HeroAction.Shoot);
        Events.RaiseShotFired(_projectiles.Fire(ProjectileKind.HeroBolt, Shooter.Hero, _hero.Position.To3(1.4f), target));
    }

    private void MoveProjectiles(float dt)
    {
        foreach (var hit in _projectiles.Move(dt))
        {
            Events.RaiseProjectileHit(hit.Projectile, hit.At);
            if (!hit.Killed) continue;
            var enemy = hit.Projectile.Target;
            if (hit.Projectile.From == Shooter.Hero) _tally.HeroKill();
            Events.RaiseEnemyDefeated(enemy, hit.Projectile.From);
            var (pos, dir) = _enemies.PathPoint(enemy);
            if (_words.DropBeside(pos, dir) is { } book) Events.RaiseWordAppeared(book);
        }
    }

    private void UpdateContext()
    {
        // Every book within reach is offered on the right, nearest first.
        _controls.ShowNearby(_words.Near(_hero.Position, Balance.PickupRadius, 5), PickUp);
        // The give button stays up as long as the hero carries a word next to a tower that still asks for one.
        var carried = _busy ? null : _hero.Carried;
        _giveTower = carried is null ? null : _towers.NearestAsking(_hero.Position, Balance.TowerReach);
        _controls.ShowCarry(_giveTower is null ? null : carried!.Word, carried?.Word);
    }

    private void PickUp(WordBook book)
    {
        if (_over || !_words.Take(book)) return;
        // One word at a time (B-822): picking another one puts the carried one down here.
        if (_hero.Release() is { } previous) Events.RaiseWordAppeared(_words.LayDown(previous, _hero.Position));
        _hero.Carry(book.Data);
        _hero.Look.Play(HeroAction.PickUp);
        Events.RaiseWordPickedUp(book.Data);
        _controls.RefreshNearby();
    }

    // Sorting words: the carried word goes down next to the hero and stays there for later.
    private void LayDown()
    {
        if (_over || _busy || _hero.Carried is not { } token) return;
        Events.RaiseWordAppeared(_words.LayDown(token, _hero.Position));
        RunSession.Log($"abgelegt '{token.Word}'");
        _hero.Release();
        _hero.Look.Play(HeroAction.LayDown);
        Events.RaiseWordLaidDown(token);
        _controls.RefreshNearby();
    }

    private async Task AssignAsync()
    {
        if (_busy || _over || _giveTower is not { Target: { } target } tower || _hero.Carried is not { } token) return;
        _busy = true;
        _controls.HideGive();
        _hero.Look.Play(HeroAction.Give);
        Events.RaiseWordGiven(tower, token);
        try
        {
            var result = await _session.AssignAsync(token, target);
            _tally.Assigned(result.Correct);
            _hero.Release();
            RunSession.Log($"assign '{token.Word}' -> '{target.Prompt}': {(result.Correct ? "richtig" : "falsch, erwartet " + result.Expected)}");
            var becameStrong = false;
            if (result.Correct)
            {
                _words.Pool.Consume(token.TokenId);
                becameStrong = tower.Ammo.Refill();
                tower.Target = _session.NextTarget();
            }
            else
            {
                // The word is gone from the hand; it goes back into the pool and drops again later.
                _words.Pool.Return(token.TokenId);
                tower.Ammo.Penalize();
            }
            _hero.Look.Play(result.Correct ? HeroAction.Correct : HeroAction.Wrong);
            Events.RaiseWordAssigned(new Assignment(tower, token, target, result.Correct, result.Expected, becameStrong));
            if (result.Correct && _tally.GoalReached) End(LevelEnd.GoalReached);
        }
        catch (ApiException ex)
        {
            Events.RaiseServerMessage(ex.Message);
            if (ex.RunClosed) End(LevelEnd.TimeUp);
        }
        finally
        {
            _busy = false;
        }
    }

    private void OnTimeUp()
    {
        Events.RaiseTimeUp();
        End(LevelEnd.TimeUp);
    }

    // The server decides whether the level is won (its own count of solved towers); the client only says it is over.
    private void End(LevelEnd reason)
    {
        if (_over) return;
        _over = true;
        _paused = false;
        _controls.Close();
        _hero.Look.Play(_tally.GoalReached ? HeroAction.Victory : HeroAction.Defeat);
        RunSession.Log(_tally.Summary(reason.ToString()));
        Events.RaiseLevelEnded(reason, _tally.GoalReached);
        // A short moment to see the hero cheer (or fall) before the result screen.
        GetTree().CreateTimer(reason == LevelEnd.GaveUp ? 0.3 : 1.6).Timeout +=
            () => Finished?.Invoke(_session.Run.RunId, _tally.Correct, _tally.Total, reason);
    }
}
