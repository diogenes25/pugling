namespace Pugling.Api.Services.Shared;

/// <summary>
/// What a study-plan position inherits as its target reward when the supervisor states nothing (B-45).
/// <para>
/// It lives here, and not privately in the controller that applies it, because a <b>second</b> caller reads
/// the same rule: the catalog search publishes the effective figure so the supervisor's form can pre-fill
/// with the number that will actually apply (B-192). While the rule sat in <b>one</b> controller, the form
/// pre-filled from the raw recommendation instead - and an exercise carrying 100000 showed 100000 while
/// the server would have given 20. Two places deriving one rule is exactly the silent drift this repo has
/// paid for before.
/// </para>
/// </summary>
public static class GoalPointsPolicy
{
    /// <summary>
    /// Upper bound for a recommendation a position inherits <b>silently</b>.
    /// <para>
    /// It guards a hole that inheriting opened: the target reward is paid out as coins, the catalog is
    /// shared, and <c>Exercise.RewardPoints</c> has no bound of its own - so a foreign exercise carrying
    /// 100000 would push that into the family economy for every caller that omits the field. Before
    /// inheriting existed, the ceiling was the hard <see cref="Fallback"/>.
    /// </para>
    /// <para>
    /// The bound applies <b>only</b> to inheritance, never to a value the supervisor states explicitly -
    /// that is their own decision and stays unbounded, as it was. That asymmetry is what makes a tight
    /// bound safe: the highest figure anything in the product intends is 30 (the wizard's most intense
    /// setting), so 200 cannot be reached by a plausible recommendation.
    /// </para>
    /// </summary>
    public const int MaxInherited = 200;

    /// <summary>
    /// The model default, used when a recommendation is unusable. Falling back rather than clamping to
    /// <see cref="MaxInherited"/> is deliberate: 200 would be a number nobody chose, while 20 is the
    /// documented default the position had before recommendations existed.
    /// </summary>
    public const int Fallback = 20;

    /// <summary>
    /// The recommendation, or <see cref="Fallback"/> when it is unusable.
    /// </summary>
    /// <remarks>
    /// Safe to call inside a <b>final</b> EF projection: it is evaluated on the client while the DTO is
    /// built, which costs nothing extra. The one consequence is that a value derived this way lives only
    /// in memory - it cannot be sorted or filtered in SQL. Should a caller ever need that, spell the
    /// ternary out against the two constants above rather than changing the numbers here.
    /// </remarks>
    public static int Effective(int rewardPoints) =>
        rewardPoints is > 0 and <= MaxInherited ? rewardPoints : Fallback;
}
