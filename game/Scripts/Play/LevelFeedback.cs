using Godot;
using PuglingTower.Ui;
using PuglingTower.World;

namespace PuglingTower.Play;

/// <summary>
/// The level's visible reactions to its <see cref="GameEvents"/>: spark bursts in the scene and the feedback line on
/// screen. A sound player hangs onto the same events the same way.
/// </summary>
public static class LevelFeedback
{
    private static readonly Color Spark = new(1f, 0.95f, 0.8f), Good = new(0.4f, 1f, 0.5f), Bad = new(1f, 0.25f, 0.2f);

    /// <summary>Subscribes the reactions; <paramref name="scene"/> holds the bursts, <paramref name="say"/> shows a line.</summary>
    public static void Attach(GameEvents events, Node3D scene, Action<string, Color> say)
    {
        events.ProjectileHit += (p, at) => Fx.Burst(scene, at, p.Kind.Arc > 0 ? p.Kind.Color.Lightened(0.3f) : Spark,
            p.Kind.Arc > 0 ? 22 : 10, p.Kind.Arc > 0 ? 0.7f : 0.4f);
        events.TowerFired += shot =>
        {
            if (!shot.Tower.Kind.Blast) return;
            Fx.Blast(scene, shot.Tower.Pos.To3(shot.Tower.Kind.MuzzleHeight));
            Fx.Recoil(shot.Tower.Model);
        };
        events.WordAppeared += book => Fx.Burst(scene, book.Pos.To3(0.8f), Palette.Gold, 14, 0.5f);
        events.CastleHit += at =>
        {
            Fx.Burst(scene, at, new Color(1, 0.3f, 0.2f), 30);
            say("Ein Skelett hat die Burg erreicht!", Colors.Salmon);
        };
        events.WordAssigned += a =>
        {
            Fx.Burst(scene, a.Tower.Pos.To3(3.2f), a.Correct ? Good : Bad, 40);
            if (!a.Correct)
                say($"Falsch – „{a.Target.Prompt}“ heißt „{a.Expected}“  −{Core.Balance.WrongPenalty} Schuss", Colors.Salmon);
            else if (a.BecameStrong) say($"Richtig! Der Turm schießt jetzt {a.Tower.Kind.StrongShotName} ★", Colors.LightGreen);
            else say($"Richtig! „{a.Word.Word}“ = „{a.Target.Prompt}“  +{Core.Balance.RefillAmmo} Schuss", Colors.LightGreen);
        };
        events.ServerMessage += text => say(text, Colors.Orange);
        events.TimeUp += () => say("Die Spielzeit für heute ist um.", Colors.Orange);
    }
}
