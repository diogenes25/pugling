using Godot;

namespace PuglingTower.Heroes;

/// <summary>A moment the hero acts out; walking and standing are covered by <see cref="IHeroLook.Update"/>.</summary>
public enum HeroAction
{
    /// <summary>Picks a book up.</summary>
    PickUp,

    /// <summary>Lays the carried book down.</summary>
    LayDown,

    /// <summary>Hands the carried word to a tower.</summary>
    Give,

    /// <summary>Fires his own shot.</summary>
    Shoot,

    /// <summary>The tower took the word as correct.</summary>
    Correct,

    /// <summary>The word was wrong.</summary>
    Wrong,

    /// <summary>The level is won.</summary>
    Victory,

    /// <summary>The level is lost or given up.</summary>
    Defeat,
}

/// <summary>
/// How the hero looks - a 3D model, a flat billboard from image sequences, anything. The <see cref="Hero"/> owns
/// position, movement and the carried word; the look only shows them. A look may ignore actions it has no clip for.
/// </summary>
public interface IHeroLook
{
    /// <summary>The visual, added below the hero's node at the hero's feet.</summary>
    Node3D Root { get; }

    /// <summary>Called every frame while the level runs.</summary>
    /// <param name="dt">Seconds since the last frame.</param>
    /// <param name="moving">The hero is walking.</param>
    /// <param name="yaw">The direction he faces, in radians around the up axis (0 = towards the camera).</param>
    void Update(float dt, bool moving, float yaw);

    /// <summary>Acts out a moment; walking and standing resume when the clip is over.</summary>
    void Play(HeroAction action);
}
