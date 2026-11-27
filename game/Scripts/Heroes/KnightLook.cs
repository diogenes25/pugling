using Godot;
using PuglingTower.World;

namespace PuglingTower.Heroes;

/// <summary>The KayKit knight with sword and shield, animated by the pack's own clips.</summary>
public sealed class KnightLook : IHeroLook
{
    private readonly Node3D _model;
    private readonly AnimationPlayer? _anim;
    // While a one-shot clip plays, walking and standing must not cut it off.
    private float _actionTime;

    /// <summary>Loads the model.</summary>
    public KnightLook()
    {
        _model = Models.Character("Knight.glb").Instantiate<Node3D>();
        _model.Scale = Vector3.One * 0.8f;
        Models.KeepOnly(_model, "Knight_", "1H_Sword", "Round_Shield");
        _anim = Models.Animator(_model);
    }

    /// <inheritdoc />
    public Node3D Root => _model;

    /// <inheritdoc />
    public void Update(float dt, bool moving, float yaw)
    {
        _actionTime -= dt;
        if (_actionTime <= 0) Models.Play(_anim, true, moving ? "Running_A" : "Idle");
        _model.Rotation = new Vector3(0, yaw, 0);
    }

    /// <inheritdoc />
    public void Play(HeroAction action)
    {
        switch (action)
        {
            case HeroAction.PickUp: Act(0.5f, "PickUp", "Interact"); break;
            case HeroAction.LayDown: Act(0.4f, "PickUp", "Interact"); break;
            case HeroAction.Give: Act(0.6f, "Interact", "PickUp"); break;
            case HeroAction.Victory: Models.Play(_anim, false, "Cheer"); break;
            case HeroAction.Defeat: Models.Play(_anim, false, "Death_A"); break;
            // The knight has no clips for shooting or for a graded word; his sword arm stays as it is.
            case HeroAction.Shoot:
            case HeroAction.Correct:
            case HeroAction.Wrong:
            default: break;
        }
    }

    private void Act(float seconds, params string[] clips)
    {
        _actionTime = seconds;
        Models.Play(_anim, false, clips);
    }
}
