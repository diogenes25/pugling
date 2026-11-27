using Pugling.Contracts.Shared;

namespace Pugling.Contracts.Student;

// Contract of the child's views on its own progress: daily mission and runtime history of a study plan,
// plus the cross-plan vocabulary progress (per item, per word, with answer history).

/// <summary>Daily mission: the plan overview plus today's rollup over its positions.</summary>
public record OverviewResponse(int PlanId, string Title, DateOnly StartDate, DateOnly? EndDate, bool Active,
    int CurrentStreak, DayOverview Today, DailyBoxStatus DailyBox);

/// <summary>
/// The child's day across all of its active, running plans (B-606): the one place that says whether today's duty is
/// done, whether the day's rewards are unlocked, how long the streak is and whether the daily chest is earned. With a
/// single active plan it reads the same as that plan's <see cref="OverviewResponse"/>.
/// </summary>
/// <param name="ChildId">The child the day belongs to.</param>
/// <param name="Day">The server's calendar day (UTC).</param>
/// <param name="DutyDone">Every goal of every active plan is met - and there is at least one.</param>
/// <param name="GoalsTotal">Goals (daily and weekly) over all active plans.</param>
/// <param name="GoalsMet">Of those, the goals met.</param>
/// <param name="Outstanding">The child-facing lines of the open goals, plan by plan.</param>
/// <param name="RewardsUnlocked">The day's reward goals are met: the daily chest and the bonus training open. Reward goals are
/// the daily goals of all active plans, or every goal when no plan has a daily one.</param>
/// <param name="CurrentStreak">Consecutive complete days up to today over all plans (the child's streak).</param>
/// <param name="Positions">The positions of all active plans, each carrying its plan and subject.</param>
/// <param name="DailyBox">Today's reward chest.</param>
/// <param name="RewardGoalsTotal">How many reward goals the day has - the list behind <paramref name="RewardsUnlocked"/>.</param>
/// <param name="RewardGoalsMet">Of those, the goals met; the chest's progress bar.</param>
/// <param name="RewardCadence"><c>Daily</c> when the reward goals are the daily goals, <c>Weekly</c> when there are only
/// weekly ones, <c>None</c> without any goal - what the child is told still stands before the chest.</param>
public record TodayResponse(int ChildId, DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet,
    IReadOnlyList<string> Outstanding, bool RewardsUnlocked, int CurrentStreak, IReadOnlyList<PositionStatus> Positions,
    DailyBoxStatus DailyBox, int RewardGoalsTotal, int RewardGoalsMet, GoalCadence RewardCadence);

/// <summary>Day-by-day progress over the entire run (completed days, goals reached, points). An open plan (<c>EndDate</c> <c>null</c>, B-735) is read up to today, so <c>TotalDays</c> counts the days run so far and grows each day.</summary>
public record ProgressResponse(int PlanId, DateOnly StartDate, DateOnly? EndDate, int DaysComplete,
    int TotalDays, int TotalPoints, int CurrentStreak, IReadOnlyList<ProgressDay> Days);

/// <summary>Learning progress of a child on an item (front/back from the store, canonically word → translation).</summary>
public record ItemProgressResponse(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
    int Box, int MaxBox, int MasteryPercent, int SeenCount, int CorrectCount,
    DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect,
    [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>
/// Aggregated word mastery status across all exercises that use this store word. <c>SourceLanguage</c>/<c>TargetLanguage</c>
/// are the store entry's language pair (B-584): the father's review groups a page of weak words by it, because one review
/// takes one pair.
/// </summary>
public record WordMasteryResponse(int VocabularyId, string Word, string Translation, int ItemCount,
    int AvgMasteryPercent, int MinBox, int SeenCount, int CorrectCount, int CorrectPercent,
    [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary,
    string SourceLanguage = "", string TargetLanguage = "");

/// <summary>A logged answer event from the item history.</summary>
public record HistoryResponse(DateTime At, string Source, int StageValue, string? GivenAnswer,
    bool WasCorrect, int? PlanPositionId);
