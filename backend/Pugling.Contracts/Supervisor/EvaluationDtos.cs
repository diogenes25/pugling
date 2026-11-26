namespace Pugling.Contracts.Supervisor;

// The supervisor's evaluation of a child's learning (B-733): what was learnt per day and subject, the profile over 7 and 28
// days, and the problem words with the kind of error. Supervisor-only: the child never reads a brother's numbers or its own
// error kinds through these types.

/// <summary>
/// One day and subject of a child (B-733). A closed day is stored once and never recomputed; <paramref name="Live"/> marks
/// today's row, computed on reading and not stored. A day without a row is a gap (no practice, no duty owed), not a zero.
/// </summary>
/// <param name="Day">The learning day (German time).</param>
/// <param name="SubjectId">The subject; <c>null</c> for exercises and plans without one.</param>
/// <param name="SubjectName">The subject's name, empty without a subject.</param>
/// <param name="NewWords">Words introduced for the first time on this day.</param>
/// <param name="ConsolidatedWords">Right answers that moved a word into the top box.</param>
/// <param name="FellBackWords">Wrong answers that dropped a word from above box 1.</param>
/// <param name="CorrectAnswers">Right answers of the day.</param>
/// <param name="WrongAnswers">Wrong answers of the day.</param>
/// <param name="PracticedSeconds">Active practice seconds.</param>
/// <param name="ErrorKinds">Wrong typed answers per error kind (<c>Accent</c>, <c>Typo</c>, <c>Synonym</c>, <c>WrongWord</c>).</param>
/// <param name="DutiesMet">Daily duties owed this day and met.</param>
/// <param name="DutiesMissed">Daily duties owed this day and left open (still open for a live day).</param>
/// <param name="Live">Today's row, computed on reading and not stored.</param>
public record DailyEvaluationResponse(
    DateOnly Day, int? SubjectId, string SubjectName, int NewWords, int ConsolidatedWords, int FellBackWords,
    int CorrectAnswers, int WrongAnswers, int PracticedSeconds, IReadOnlyDictionary<string, int> ErrorKinds,
    int DutiesMet, int DutiesMissed, bool Live);

/// <summary>A child's learning profile per subject (B-733), computed from the stored days, never from raw answers.</summary>
/// <param name="Until">The last closed day the windows end on (yesterday).</param>
/// <param name="Subjects">One entry per subject with a stored day in the last 28 days.</param>
public record EvaluationProfileResponse(DateOnly Until, IReadOnlyList<SubjectProfile> Subjects);

/// <summary>One subject of the profile, over the last 7 and 28 closed days.</summary>
public record SubjectProfile(int? SubjectId, string SubjectName, ProfileWindow Last7Days, ProfileWindow Last28Days);

/// <summary>
/// The profile figures over one window. A window without stored days has zeros and <paramref name="LearningDays"/> 0.
/// </summary>
/// <param name="Days">Length of the window in days (7 or 28).</param>
/// <param name="LearningDays">Stored days in the window on which the child practised.</param>
/// <param name="CorrectAnswers">Right answers in the window.</param>
/// <param name="WrongAnswers">Wrong answers in the window.</param>
/// <param name="MasteryPercent">Right answers in percent of all answers (0 without answers).</param>
/// <param name="NewWords">Words introduced in the window.</param>
/// <param name="ConsolidatedWords">Words moved into the top box in the window.</param>
/// <param name="TempoPerLearningDay">Consolidated words per learning day, one decimal.</param>
/// <param name="TopErrorKinds">Error kinds by frequency, most frequent first.</param>
/// <param name="DutyDays">Stored days in the window that owed a daily duty.</param>
/// <param name="DutyDaysMet">Of those, the days whose daily duties were all met.</param>
/// <param name="ResiliencePercent">Days whose daily duties were all met, in percent of the days that owed one; <c>null</c>
/// without a day that owed a duty.</param>
public record ProfileWindow(
    int Days, int LearningDays, int CorrectAnswers, int WrongAnswers, int MasteryPercent, int NewWords,
    int ConsolidatedWords, double TempoPerLearningDay, IReadOnlyList<ErrorKindCount> TopErrorKinds,
    int DutyDays, int DutyDaysMet, int? ResiliencePercent);

/// <summary>An error kind with its count.</summary>
public record ErrorKindCount(string Kind, int Count);

/// <summary>
/// A word the child got wrong in the period (B-733), with the kinds of its wrong answers - the "why" behind a weak word.
/// </summary>
/// <param name="VocabularyId">The store word.</param>
/// <param name="SubjectId">The subject of the exercise the word was answered in; <c>null</c> without one.</param>
/// <param name="SubjectName">The subject's name, empty without a subject.</param>
/// <param name="Front">The word as the card shows it (store value).</param>
/// <param name="Back">Its translation (store value).</param>
/// <param name="WrongAnswers">Wrong answers to the word in the period.</param>
/// <param name="CorrectAnswers">Right answers to the word in the period.</param>
/// <param name="ErrorKinds">Wrong typed answers per error kind; answers stored before B-732 carry none.</param>
/// <param name="LastWrongAnswer">The last wrong typed answer in the period, <c>null</c> when none was typed.</param>
/// <param name="LastWrongAt">When the last wrong answer in the period was given.</param>
/// <param name="Box">The word's current cross-plan box.</param>
/// <param name="MasteryPercent">The word's current mastery.</param>
public record ProblemWordResponse(
    int VocabularyId, int? SubjectId, string SubjectName, string Front, string Back, int WrongAnswers, int CorrectAnswers,
    IReadOnlyDictionary<string, int> ErrorKinds, string? LastWrongAnswer, DateTime LastWrongAt, int Box, int MasteryPercent);
