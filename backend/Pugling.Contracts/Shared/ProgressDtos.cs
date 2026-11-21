namespace Pugling.Contracts.Shared;

// Tier-spanning contract of study plan progress: the child reads its daily mission from it,
// the supervisor the same rollup as history/reporting.

/// <summary>
/// Status of a single position for one day – enough for the student client to render the right action.
///
/// <c>Testable</c> is about <b>this day</b>, not merely about the exercise type: it is false on a free display
/// stage (B-96), because the exam refuses that stage. A client therefore offers practice whenever the position
/// uses Leitner or is not testable today – the two together always leave exactly one playable way in.
/// <para>
/// <c>IntroducedCount</c> of <c>PoolSize</c> is how far the child has come through the position's contents - with a
/// daily dose (B-04) the one figure that says how much of the material is still ahead.
/// </para>
/// <para>
/// <c>PenaltyCoins</c> is the position's coin penalty for a missed goal (0 = none), shown to the child on purpose
/// (B-525): a price that only appears as a booking the next morning cannot steer anything. It is the configured
/// amount, not a forecast - it is only booked for a closed daily/weekly period of a plan that was due, and it is
/// always 0 for a free position (<c>Cadence = None</c>), which is never charged.
/// <c>SeriesUnitId</c>/<c>SeriesUnitLabel</c> name the textbook unit of the exercise, so a client can group
/// consecutive positions into the sections the child knows from the book.
/// <c>SeriesName</c>/<c>BookPages</c> (B-588) add the series and the printed page reference (<c>null</c> = none), so
/// the child can look the exercise up in its book. Deliberately only here and never in a test or practice payload:
/// looking it up while practicing is the point, the class test stays without it.
/// </para>
/// <para>
/// <c>PlanId</c>/<c>PlanTitle</c> name the plan the position belongs to and <c>SubjectName</c> the subject of that plan
/// (B-614: the child's day is grouped the way the supervisor filed the plans) - only a plan without a subject falls back to
/// the subject of the exercise's textbook series ("" = none). B-606: the child's day view lists the positions of every
/// active plan side by side, and a position must still lead into the path of its own plan.
/// </para>
/// <para>
/// <c>GoalProgress</c> (0…1) is how far the goal of the current period has come, so the child sees a duty filling up
/// rather than only flipping. It is 1 exactly when <c>GoalMet</c> is true. Only a goal measured by a played round
/// (content exercises, free display stage) has a real partial value - the share of the round's required part already
/// played; a goal measured by a passed test is 0 until the test is passed, because a failed attempt is no part of it.
/// </para>
/// <para>
/// <c>ImprintedCount</c> (B-643) counts the contents written from memory once, right and without help, on their learn card
/// ("eingeprägt") - the golden ring. For contents without a learn card (recognition cards, other types) the server counts
/// them once introduced, so the client needs no rule of its own.
/// </para>
/// </summary>
public record PositionStatus(
    int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType, string Renderer,
    int Order, GoalCadence Cadence, ExerciseCheckMode CheckMode, bool UseLeitner, bool Testable,
    bool GoalMet, double GoalProgress, int DueCount, int PoolSize, int PointsGoalMet, int IntroducedCount,
    int PenaltyCoins, int SeriesUnitId, string SeriesUnitLabel, string SeriesName = "", string? BookPages = null,
    int PlanId = 0, string PlanTitle = "", string SubjectName = "", int ImprintedCount = 0);

/// <summary>
/// Daily rollup of a study plan across its positions. <see cref="DutyDone"/> counts every goal (daily and weekly) and
/// drives streak and evaluation; <see cref="RewardsUnlocked"/> counts only the daily goals - or every goal when the
/// plan has no daily one - and gates the daily box and the bonus training (B-535): a week of hard grammar must not
/// lock seven days of rewards.
/// </summary>
public record DayOverview(
    DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded,
    IReadOnlyList<string> Outstanding, IReadOnlyList<PositionStatus> Positions, bool RewardsUnlocked);

/// <summary>One day in the history (for the supervisor's evaluation).</summary>
public record ProgressDay(DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded);

/// <summary>Aggregated history: run-wide metrics plus a filtered/sorted list of days.</summary>
/// <param name="DaysComplete">Days of this plan's run on which its duty was done.</param>
/// <param name="TotalDays">Length of this plan's run in days.</param>
/// <param name="TotalPoints">Points this plan's positions awarded over its run.</param>
/// <param name="CurrentStreak">Consecutive complete days up to today over all plans (the child's streak, not this plan's).</param>
/// <param name="Days">The filtered and sorted days of this plan's run.</param>
public record ProgressView(int DaysComplete, int TotalDays, int TotalPoints, int CurrentStreak,
    IReadOnlyList<ProgressDay> Days);

/// <summary>
/// Today's daily reward chest (B-527): whether it was earned today, whether it is opened, and how many chests wait
/// to be opened in total (none ever lapses). Read-only - the chest is earned only at the practice/test-completion
/// write seams and booked only when the child opens it, never as a side effect of this view.
/// </summary>
/// <param name="ClaimedToday">Today's chest is earned (the reward goals of today are met).</param>
/// <param name="OpenedToday">Today's chest is opened and booked.</param>
/// <param name="Waiting">Earned chests not opened yet, of any day.</param>
/// <param name="CoinsAwarded">Coins of today's chest - only once it is opened; the content stays a surprise until then.</param>
/// <param name="GemsAwarded">Gems of today's chest - only once it is opened.</param>
/// <param name="StreakAtClaim">
/// Streak snapshot of today's chest (0 without one). Visible before opening on purpose: it tells the size class of the
/// draw (the multiplier tier), not the amount, so the surprise stays intact.
/// </param>
public record DailyBoxStatus(bool ClaimedToday, bool OpenedToday, int Waiting, int? CoinsAwarded, int? GemsAwarded,
    int StreakAtClaim);

/// <summary>
/// A daily reward chest the child just opened (B-527): the draw fixed on the day it was earned, now booked to the
/// ledger. The oldest waiting chest is opened first.
/// </summary>
/// <param name="Coins">Coins booked.</param>
/// <param name="Gems">Gems booked.</param>
/// <param name="StreakAtClaim">Streak on the day the chest was earned - it scaled the draw.</param>
/// <param name="Day">Day the chest was earned.</param>
/// <param name="WaitingAfter">Chests still waiting after this one.</param>
public record DailyBoxOpened(int Coins, int Gems, int StreakAtClaim, DateOnly Day, int WaitingAfter);
