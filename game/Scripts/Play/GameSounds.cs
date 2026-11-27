using Godot;
using PuglingTower.Combat;

namespace PuglingTower.Play;

/// <summary>
/// The level's sound, hung onto its <see cref="GameEvents"/> like <see cref="LevelFeedback"/>. Shots, hits and the
/// castle sound where they happen (3D, heard from the camera, so a tower far off screen is quieter); everything about
/// words and the end of the level plays flat at full volume, because that is what the player is waiting for. The
/// effects are generated, not downloaded (game/assets/sfx, see docs/turm-spiel-plan.md).
/// </summary>
public partial class GameSounds : Node3D
{
    /// <summary>How many sounds may overlap in the scene; the oldest is cut off when all are busy.</summary>
    private const int Voices = 10;

    /// <summary>The same sound again within this many seconds is skipped: six towers firing at once is noise, not news.</summary>
    private const double MinGap = 0.09;

    private readonly Dictionary<string, AudioStream> _streams = [];
    private readonly Dictionary<string, double> _lastPlayed = [];
    private readonly List<AudioStreamPlayer3D> _voices = [];
    private readonly AudioStreamPlayer _flat = new();
    private readonly AudioStreamPlayer _flat2 = new();
    private int _next;

    /// <summary>Subscribes to the level's events.</summary>
    public GameSounds(GameEvents events)
    {
        events.ShotFired += p => At(ShotSound(p), p.Node.Position, p.From == Shooter.Hero ? -4 : -9);
        events.EnemyDefeated += (e, _) => At("bones", e.Node.Position, -6);
        events.CastleHit += at => At("castle_hit", at, 0);
        events.WordPickedUp += _ => Flat("pickup", -4);
        events.WordLaidDown += _ => Flat("drop", -4);
        events.WordAssigned += a => Flat(a.Correct ? "correct" : "wrong", -2);
        events.LevelEnded += (_, goal) => Flat(goal ? "victory" : "defeat", 0, second: true);
    }

    /// <summary>Needed by Godot for nodes made from code; use the other constructor.</summary>
    public GameSounds()
    {
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddChild(_flat);
        AddChild(_flat2);
        for (var i = 0; i < Voices; i++)
        {
            // The camera hangs about 18 units above the hero: sounds around him play at full volume.
            var voice = new AudioStreamPlayer3D { UnitSize = 18, MaxDistance = 60, AttenuationFilterCutoffHz = 20500 };
            AddChild(voice);
            _voices.Add(voice);
        }
    }

    private static string ShotSound(Projectile p) => p.Kind.Name switch
    {
        "hero-bolt" => "punch",
        "boulder" or "fire-boulder" => "cannon",
        "fire-arrow" => "fire_arrow",
        _ => "arrow",
    };

    private void At(string name, Vector3 at, float volumeDb)
    {
        if (!IsInsideTree() || _voices.Count == 0 || !Due(name)) return;
        var voice = _voices[_next];
        _next = (_next + 1) % _voices.Count;
        voice.Stream = Stream(name);
        voice.VolumeDb = volumeDb;
        voice.GlobalPosition = at;
        voice.Play();
    }

    private void Flat(string name, float volumeDb, bool second = false)
    {
        if (!IsInsideTree() || !Due(name)) return;
        // The end fanfare gets its own player so the last "correct" is not cut off by it.
        var player = second ? _flat2 : _flat;
        player.Stream = Stream(name);
        player.VolumeDb = volumeDb;
        player.Play();
    }

    private bool Due(string name)
    {
        var now = Time.GetTicksMsec() / 1000.0;
        if (_lastPlayed.TryGetValue(name, out var last) && now - last < MinGap) return false;
        _lastPlayed[name] = now;
        return true;
    }

    private AudioStream Stream(string name)
    {
        if (!_streams.TryGetValue(name, out var stream)) _streams[name] = stream = GD.Load<AudioStream>($"res://assets/sfx/{name}.wav");
        return stream;
    }
}
