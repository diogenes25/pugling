namespace PuglingTower.Core;

/// <summary>
/// The client's count of one run: words given, the castle's lives, the hero's shots. Only for the screen and the log -
/// whether the level is won is the server's own count (B-851).
/// </summary>
public sealed class RunTally(int goal)
{
    /// <summary>Correct words needed for the level.</summary>
    public int Goal { get; } = goal;

    /// <summary>Correct assignments so far.</summary>
    public int Correct { get; private set; }

    /// <summary>All graded assignments so far.</summary>
    public int Total { get; private set; }

    /// <summary>Skeletons the castle can still take.</summary>
    public int Lives { get; private set; } = Balance.CastleLives;

    /// <summary>Played seconds, without pauses.</summary>
    public float Elapsed { get; set; }

    /// <summary>Shots the hero fired himself.</summary>
    public int HeroShots { get; private set; }

    /// <summary>Skeletons the hero's own shots finished off.</summary>
    public int HeroKills { get; private set; }

    /// <summary>The goal of correct words is reached.</summary>
    public bool GoalReached => Correct >= Goal;

    /// <summary>Counts a graded assignment.</summary>
    public void Assigned(bool correct)
    {
        Total++;
        if (correct) Correct++;
    }

    /// <summary>A skeleton reached the castle; returns whether the castle has fallen.</summary>
    public bool CastleHit() => --Lives <= 0;

    /// <summary>Counts one shot of the hero.</summary>
    public void HeroShot() => HeroShots++;

    /// <summary>Counts one skeleton the hero finished off.</summary>
    public void HeroKill() => HeroKills++;

    /// <summary>The run's last log line; the reports on the phone are read from it, so keep its words stable.</summary>
    public string Summary(string reason) =>
        $"ende {reason} {Correct}/{Total} richtig, {Elapsed:0}s, Burg {Lives}/{Balance.CastleLives}, Heldenschüsse {HeroShots}, Heldentreffer {HeroKills}";
}
