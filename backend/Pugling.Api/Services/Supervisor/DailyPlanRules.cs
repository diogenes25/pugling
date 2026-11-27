using System.Globalization;

namespace Pugling.Api.Services.Supervisor;

/// <summary>One position of an active plan as the planner sees it on the planned day.</summary>
/// <param name="PositionId">The position.</param>
/// <param name="ExerciseId">The exercise the position plays that day.</param>
/// <param name="ExerciseTitle">Its title.</param>
/// <param name="SubjectId">The task's subject (plan subject, else the series subject).</param>
/// <param name="Order">The plan's order of the position, the tie-break after the rules.</param>
/// <param name="DueReviews">Introduced words due for review that day.</param>
/// <param name="NewAvailable">Words of the pool never introduced yet.</param>
/// <param name="WrongYesterday">Wrong answers at the position on the last learning day before.</param>
/// <param name="CorrectYesterday">Right answers there.</param>
/// <param name="FailedTestYesterday">A test of the position on that day ended below its pass mark.</param>
/// <param name="DayStage">The stage the position plays that day.</param>
/// <param name="EasierStage">The next easier stage of the type's ladder; <c>null</c> = none (the easiest, or a stage off the
/// ladder).</param>
/// <param name="ExamDate">The nearest class test that covers this position (B-680): one whose bundle holds the exercise, or a
/// subject-wide one without a bundle; <c>null</c> = no test, the position is free material.</param>
/// <param name="ExamLearningDays">Learning days left from the planned day up to the day before the test, at least 1 with a
/// test; 0 without one.</param>
/// <param name="LessonSoon">The position's subject has a timetable lesson on the planned day or the next day.</param>
/// <param name="Weakness">The subject's share of wrong answers over the last four weeks, 0 to 1; 0 without answers.</param>
/// <param name="ExamPresses">The test names material (exercises or tags) that includes this position, so it sets a pace; a
/// test without any only orders the position by its date (B-1034: without material there is nothing to count back from).</param>
public sealed record PlannerCandidate(int PositionId, int ExerciseId, string ExerciseTitle, int? SubjectId, int Order,
    int DueReviews, int NewAvailable, int WrongYesterday, int CorrectYesterday, bool FailedTestYesterday, int DayStage,
    int? EasierStage, DateOnly? ExamDate = null, int ExamLearningDays = 0, bool LessonSoon = false, double Weakness = 0,
    bool ExamPresses = true)
{
    /// <summary>New words per learning day this position needs to have all its pool introduced before its test.</summary>
    public int NewWordsNeededPerDay => ExamDate is null || !ExamPresses || NewAvailable == 0
        ? 0
        : (int)Math.Ceiling(NewAvailable / (double)Math.Max(1, ExamLearningDays));

    /// <summary>Failed the day before: a test below the pass mark, or more wrong than right answers.</summary>
    public bool Failed => FailedTestYesterday || WrongYesterday > CorrectYesterday;
}

/// <summary>The supervisor's limits for the child (B-731), all four set.</summary>
public sealed record PlannerLimits(int TasksMin, int TasksMax, int WordsMin, int WordsMax);

/// <summary>One task the rules chose.</summary>
public sealed record PlannedTask(PlannerCandidate Candidate, int Order, int NewWords, int? Stage, string Reason);

/// <summary>The composed plan.</summary>
public sealed record PlannerResult(int NewWordsBudget, IReadOnlyList<PlannedTask> Tasks, IReadOnlyList<string> Warnings);

/// <summary>
/// The rules of the day planner (B-734) as a pure function: the same inputs give the same plan - no clock, no randomness,
/// no language model (B-644, decisions 10 and 21). Rules, in this order: due reviews first (rule 1), what failed on the last
/// learning day comes back one stage easier (rule 2), then - for the rest - the nearer class test, a lesson today or tomorrow
/// and the weaker subject lead (B-680), and floor and ceiling of tasks and new words (rule 6). The new-word budget is the
/// child's tempo or, when a class test needs more, the tempo that test needs; both between floor and ceiling. Reviews never
/// count against the new-word ceiling. The grade level (B-682) is deliberately no input here.
/// </summary>
public static class DailyPlanRules
{
    /// <summary>Composes the day's tasks from the candidates, the limits and the child's tempo.</summary>
    /// <param name="candidates">The positions of the child's active plans.</param>
    /// <param name="limits">The supervisor's floor and ceiling.</param>
    /// <param name="tempo">Consolidated words per learning day over the last week; <c>null</c> without a learning day.</param>
    public static PlannerResult Compose(IReadOnlyList<PlannerCandidate> candidates, PlannerLimits limits, double? tempo)
    {
        var examNeed = candidates.Sum(c => c.NewWordsNeededPerDay);
        var budget = Math.Clamp(Math.Max(tempo is { } t ? (int)Math.Round(t, MidpointRounding.AwayFromZero) : 0, examNeed),
            limits.WordsMin, limits.WordsMax);
        var warnings = new List<string>();
        if (examNeed > limits.WordsMax)
        {
            // Pressured positions only: the shortfall per learning day lasts until the nearest of their tests.
            var days = candidates.Where(c => c.NewWordsNeededPerDay > 0).Min(c => c.ExamLearningDays);
            var nearest = candidates.Where(c => c.NewWordsNeededPerDay > 0).Min(c => c.ExamDate!.Value);
            warnings.Add($"Bis zur Klassenarbeit am {nearest.Day:00}.{nearest.Month:00}. braucht der Stoff "
                + $"{examNeed} neue Wörter pro Lerntag, der Deckel erlaubt {limits.WordsMax} – bis dahin fehlen etwa "
                + $"{(examNeed - limits.WordsMax) * Math.Max(1, days)} Wörter.");
        }
        if (candidates.Count == 0)
            return new PlannerResult(budget, [], ["Kein aktiver Lehrplan mit Positionen – der Plan bleibt leer."]);

        // Rule 1 before rule 2: due reviews lead (most first), then yesterday's failures, then the rest in the plan's order.
        // Among the rest the nearer test leads, then a lesson today or tomorrow, then the weaker subject (B-680), then the plan's order.
        var byPlan = candidates.OrderBy(c => c.ExamDate ?? DateOnly.MaxValue).ThenByDescending(c => c.LessonSoon)
            .ThenByDescending(c => c.Weakness).ThenBy(c => c.Order).ThenBy(c => c.PositionId).ToList();
        var ranked = byPlan.Where(c => c.DueReviews > 0).OrderByDescending(c => c.DueReviews).ThenBy(c => c.Order)
            .ThenBy(c => c.PositionId)
            .Concat(byPlan.Where(c => c.DueReviews == 0 && c.Failed))
            .Concat(byPlan.Where(c => c.DueReviews == 0 && !c.Failed && c.NewAvailable > 0))
            .Concat(byPlan.Where(c => c.DueReviews == 0 && !c.Failed && c.NewAvailable == 0))
            .ToList();

        // What the day needs: every due review and failure, and positions until the new-word budget is placed; then the floor.
        var chosen = new List<PlannerCandidate>();
        var placed = 0;
        foreach (var c in ranked)
        {
            // A position a class test presses is always in: its own pace is part of the budget, not what is left over.
            var needed = c.DueReviews > 0 || c.Failed || c.NewWordsNeededPerDay > 0 || (placed < budget && c.NewAvailable > 0);
            if (!needed) continue;
            chosen.Add(c);
            placed += c.NewWordsNeededPerDay > 0
                ? Math.Min(c.NewAvailable, c.NewWordsNeededPerDay)
                : Math.Min(c.NewAvailable, budget - Math.Min(placed, budget));
        }
        var filled = new HashSet<int>();
        foreach (var c in ranked)
        {
            if (chosen.Count >= limits.TasksMin) break;
            if (chosen.Contains(c)) continue;
            chosen.Add(c);
            filled.Add(c.PositionId);
        }
        if (chosen.Count < limits.TasksMin)
            warnings.Add($"Nur {chosen.Count} Positionen in den aktiven Plänen – weniger als der Boden von {limits.TasksMin} Aufgaben.");
        if (chosen.Count > limits.TasksMax)
        {
            warnings.Add($"{chosen.Count - limits.TasksMax} weitere Aufgaben fielen über den Deckel von {limits.TasksMax}.");
            chosen = [.. ranked.Where(chosen.Contains).Take(limits.TasksMax)];
        }
        chosen = [.. ranked.Where(chosen.Contains)];

        // Words go first to what a test needs per day (each pressured position gets its own pace, nearest test first), then the
        // rest of the budget in rank order - otherwise the first position would take a budget that is the sum of several paces.
        var remaining = budget;
        var granted = new Dictionary<int, int>();
        foreach (var c in chosen.Where(c => c.NewWordsNeededPerDay > 0))
        {
            var own = Math.Min(Math.Min(remaining, c.NewAvailable), c.NewWordsNeededPerDay);
            granted[c.PositionId] = own;
            remaining -= own;
        }
        var tasks = new List<PlannedTask>(chosen.Count);
        foreach (var c in chosen)
        {
            var newWords = granted.GetValueOrDefault(c.PositionId);
            var extra = Math.Min(remaining, c.NewAvailable - newWords);
            remaining -= extra;
            newWords += extra;
            var stage = c.Failed ? c.EasierStage : null;
            tasks.Add(new PlannedTask(c, tasks.Count + 1, newWords, stage,
                Reason(c, newWords, stage, budget, tempo, limits, examNeed, filled.Contains(c.PositionId))));
        }
        var introduced = budget - remaining;
        if (introduced < limits.WordsMin)
            warnings.Add($"Nur {introduced} neue Wörter verfügbar – weniger als der Boden von {limits.WordsMin}.");
        return new PlannerResult(budget, tasks, warnings);
    }

    // The reason is assembled from the inputs, one part per rule that put the task in the plan (B-644, acceptance 4).
    private static string Reason(PlannerCandidate c, int newWords, int? stage, int budget, double? tempo, PlannerLimits limits,
        int examNeed, bool filledForFloor)
    {
        var parts = new List<string>();
        if (c.DueReviews > 0)
            parts.Add(c.DueReviews == 1 ? "1 fällige Wiederholung" : $"{c.DueReviews} fällige Wiederholungen");
        if (c.Failed)
        {
            var what = c.FailedTestYesterday && c.WrongYesterday <= c.CorrectYesterday
                ? "Test am letzten Lerntag nicht bestanden"
                : $"am letzten Lerntag {c.WrongYesterday} von {c.WrongYesterday + c.CorrectYesterday} falsch";
            parts.Add(stage is { } s
                ? $"{what} – eine Stufe leichter (Stufe {s} statt {c.DayStage})"
                : $"{what} – keine leichtere Stufe verfügbar");
        }
        if (newWords > 0)
            parts.Add($"{newWords} neue {(newWords == 1 ? "Wort" : "Wörter")} ({BudgetSource(budget, tempo, limits, examNeed)})");
        if (c.ExamDate is { } exam)
            parts.Add($"Klassenarbeit am {exam.Day:00}.{exam.Month:00}., noch {c.ExamLearningDays} {(c.ExamLearningDays == 1 ? "Lerntag" : "Lerntage")}");
        if (c.LessonSoon)
            parts.Add("Fach laut Stundenplan heute oder morgen");
        if (c.Weakness > 0)
            parts.Add($"{(int)Math.Round(c.Weakness * 100)} % falsche Antworten im Fach");
        if (filledForFloor)
            parts.Add($"aufgefüllt bis zum Boden von {limits.TasksMin} Aufgaben");
        if (parts.Count == 0)
            parts.Add("weiter in der Reihenfolge des Plans");
        return string.Join("; ", parts);
    }

    private static string BudgetSource(int budget, double? tempo, PlannerLimits limits, int examNeed)
    {
        // A budget a class test lifted above the child's tempo (and the floor) is named for the test, not for a tempo that did not make it.
        var own = tempo is { } tt ? (int)Math.Round(tt, MidpointRounding.AwayFromZero) : 0;
        if (examNeed > Math.Max(own, limits.WordsMin) && budget == Math.Min(examNeed, limits.WordsMax))
            return examNeed > limits.WordsMax ? $"Deckel {limits.WordsMax}, die Klassenarbeit braucht {examNeed} pro Lerntag"
                : $"{examNeed} pro Lerntag bis zur Klassenarbeit";
        if (tempo is not { } t) return $"Boden {limits.WordsMin}, noch kein Tempo";
        var shown = t.ToString("0.#", CultureInfo.GetCultureInfo("de-DE"));
        if (budget == limits.WordsMin && t < limits.WordsMin) return $"Boden {limits.WordsMin}, Tempo {shown} pro Lerntag";
        if (budget == limits.WordsMax && t > limits.WordsMax) return $"Deckel {limits.WordsMax}, Tempo {shown} pro Lerntag";
        return $"Tempo {shown} pro Lerntag";
    }
}
