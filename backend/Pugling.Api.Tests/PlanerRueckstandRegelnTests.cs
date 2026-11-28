namespace Pugling.Api.Tests;

/// <summary>
/// B-1037, acceptance 7 of B-736: a task left open on the last learning day comes back the next one, within the ceiling of tasks -
/// the planner's rule as a pure function. Each case names the change that turns it red.
/// </summary>
public class PlanerRueckstandRegelnTests
{
    private static PlannerCandidate C(int id, int order, int newAvailable = 0, bool missed = false) =>
        new(id, ExerciseId: 100 + id, $"Übung {id}", 1, order, 0, newAvailable, 0, 0, false, 4, 3, MissedYesterday: missed);

    private static int[] Ids(PlannerResult r) => [.. r.Tasks.Select(t => t.Candidate.PositionId)];

    /// <summary>
    /// Ceiling of one task: the missed position B wins over A, which comes first in the plan's order, and says why. Red when the
    /// missed flag does not rank (A).
    /// </summary>
    [Fact]
    public void Verpasste_Aufgabe_kommt_vor_der_Planreihenfolge()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 5), C(2, 2, newAvailable: 5, missed: true)],
            new PlannerLimits(TasksMin: 1, TasksMax: 1, WordsMin: 2, WordsMax: 6), tempo: null);

        Assert.Equal([2], Ids(r));
        Assert.Contains("am letzten Lerntag offen geblieben", r.Tasks.Single().Reason);
    }

    /// <summary>
    /// A missed position with nothing new and nothing due is still in the plan; the floor is already met by A. Red when a missed
    /// task is not needed by itself (only A).
    /// </summary>
    [Fact]
    public void Verpasste_Aufgabe_ohne_neuen_Stoff_kommt_trotzdem_wieder()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 5), C(2, 2, missed: true)],
            new PlannerLimits(TasksMin: 1, TasksMax: 5, WordsMin: 2, WordsMax: 6), tempo: null);

        Assert.Equal([2, 1], Ids(r));
    }

    /// <summary>
    /// Three missed tasks against a ceiling of two: two come back, the plan says one fell over the ceiling. Red when the backlog
    /// breaks the ceiling (three tasks).
    /// </summary>
    [Fact]
    public void Rueckstand_ueberschreitet_den_Deckel_nie()
    {
        var r = DailyPlanRules.Compose([C(1, 1, missed: true), C(2, 2, missed: true), C(3, 3, missed: true)],
            new PlannerLimits(TasksMin: 1, TasksMax: 2, WordsMin: 1, WordsMax: 6), tempo: null);

        Assert.Equal([1, 2], Ids(r));
        Assert.Contains("1 weitere Aufgaben fielen über den Deckel von 2.", r.Warnings);
    }
}
