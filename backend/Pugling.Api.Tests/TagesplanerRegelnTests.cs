using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-734: the rules of the day planner as a pure function, without a database. Each case names the change that turns it red.
/// </summary>
public class TagesplanerRegelnTests
{
    private static readonly PlannerLimits Wide = new(TasksMin: 1, TasksMax: 10, WordsMin: 1, WordsMax: 20);

    private static PlannerCandidate C(int id, int order, int due = 0, int newAvailable = 0, int wrong = 0, int correct = 0,
        bool failedTest = false, int stage = 4, int? easier = 3) =>
        new(id, ExerciseId: 100 + id, $"Übung {id}", SubjectId: 1, order, due, newAvailable, wrong, correct, failedTest, stage,
            easier);

    private static int[] Ids(PlannerResult r) => [.. r.Tasks.Select(t => t.Candidate.PositionId)];

    /// <summary>AK 3, rule 1: due reviews lead, most first. Red if the due ranking is dropped or reversed.</summary>
    [Fact]
    public void Faellige_Wiederholungen_kommen_zuerst_die_meisten_vorn()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 5), C(2, 2, due: 6), C(3, 3, due: 2)], Wide, tempo: 3);

        Assert.Equal([2, 3, 1], Ids(r));
    }

    /// <summary>
    /// AK 3, order of the rules: a due review outranks yesterday's failure, a failure outranks a position that only brings new
    /// words. Red if rule 2 is ranked before rule 1, or failures fall back to the plan's order.
    /// </summary>
    [Fact]
    public void Gescheitertes_folgt_den_faelligen_und_steht_vor_neuen_Woertern()
    {
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 5), C(2, 2, wrong: 3, correct: 1), C(3, 3, due: 1)], Wide, tempo: 3);

        Assert.Equal([3, 2, 1], Ids(r));
    }

    /// <summary>
    /// AK 3, rule 2: what failed comes back one stage easier, named in the reason. Red if the stage stays, or a position
    /// without failure gets the easier stage too.
    /// </summary>
    [Fact]
    public void Gescheitertes_kommt_eine_Stufe_leichter_wieder()
    {
        var r = DailyPlanRules.Compose([C(1, 1, wrong: 3, correct: 1, stage: 4, easier: 3), C(2, 2, due: 2, easier: 3)],
            Wide, tempo: null);

        var failed = r.Tasks.Single(t => t.Candidate.PositionId == 1);
        Assert.Equal(3, failed.Stage);
        Assert.Contains("am letzten Lerntag 3 von 4 falsch – eine Stufe leichter (Stufe 3 statt 4)", failed.Reason);
        Assert.Null(r.Tasks.Single(t => t.Candidate.PositionId == 2).Stage);
    }

    /// <summary>
    /// AK 3, rule 2: a test below the pass mark is a failure even without wrong answers; at the easiest stage the task comes
    /// back as it is and says so. Red if a failed test is ignored or the missing easier stage is passed as a stage.
    /// </summary>
    [Fact]
    public void Nicht_bestandener_Test_zaehlt_als_gescheitert()
    {
        var r = DailyPlanRules.Compose([C(1, 1, failedTest: true, easier: 3), C(2, 2, failedTest: true, easier: null)],
            Wide, tempo: null);

        Assert.Equal(3, r.Tasks[0].Stage);
        Assert.Contains("Test am letzten Lerntag nicht bestanden – eine Stufe leichter", r.Tasks[0].Reason);
        Assert.Null(r.Tasks[1].Stage);
        Assert.Contains("keine leichtere Stufe verfügbar", r.Tasks[1].Reason);
    }

    /// <summary>
    /// AK 2, floor of tasks: a day that needs one task is filled up to the floor from the rest of the plan, and the reason says
    /// so. Red if the floor is not applied.
    /// </summary>
    [Fact]
    public void Boden_der_Aufgaben_fuellt_auf()
    {
        var r = DailyPlanRules.Compose([C(1, 1), C(2, 2), C(3, 3, due: 4), C(4, 4)],
            Wide with { TasksMin = 3 }, tempo: null);

        Assert.Equal([3, 1, 2], Ids(r));
        Assert.Contains("aufgefüllt bis zum Boden von 3 Aufgaben", r.Tasks[1].Reason);
        Assert.DoesNotContain("aufgefüllt", r.Tasks[0].Reason);
    }

    /// <summary>
    /// AK 2, never empty: a learning day without due reviews, failures or new words still gets the floor's task. Red if the
    /// floor only fills a plan that already has a task.
    /// </summary>
    [Fact]
    public void Lerntag_ohne_Anlass_ist_nie_leer()
    {
        var r = DailyPlanRules.Compose([C(1, 1), C(2, 2)], Wide, tempo: null);

        Assert.Equal([1], Ids(r));
    }

    /// <summary>
    /// AK 2, ceiling of tasks: more needed tasks than the ceiling keep the highest ranked and warn. Red if the ceiling is not
    /// applied or cuts the wrong end.
    /// </summary>
    [Fact]
    public void Deckel_der_Aufgaben_schneidet_die_niedrigsten_ab()
    {
        var r = DailyPlanRules.Compose([C(1, 1, due: 1), C(2, 2, due: 9), C(3, 3, due: 5), C(4, 4, due: 3)],
            Wide with { TasksMax = 2 }, tempo: null);

        Assert.Equal([2, 3], Ids(r));
        Assert.Contains("2 weitere Aufgaben fielen über den Deckel von 2.", r.Warnings);
    }

    /// <summary>
    /// AK 2, ceiling of new words: a fast tempo is held at the ceiling, and due reviews next to the new words do not use up
    /// the ceiling. Red if the budget is not clamped, or reviews are subtracted from it.
    /// </summary>
    [Fact]
    public void Deckel_der_neuen_Woerter_ohne_die_Wiederholungen()
    {
        var r = DailyPlanRules.Compose([C(1, 1, due: 7, newAvailable: 10)], Wide with { WordsMax = 4 }, tempo: 50);

        Assert.Equal(4, r.NewWordsBudget);
        Assert.Equal(4, r.Tasks[0].NewWords);
        Assert.Equal(7, r.Tasks[0].Candidate.DueReviews);
        Assert.Contains("4 neue Wörter (Deckel 4, Tempo 50 pro Lerntag)", r.Tasks[0].Reason);
    }

    /// <summary>
    /// AK 2, floor of new words: without a tempo, or with one below the floor, the floor is the budget. Red if the floor is
    /// not applied to the tempo.
    /// </summary>
    [Fact]
    public void Boden_der_neuen_Woerter_ohne_oder_unter_dem_Tempo()
    {
        var limits = Wide with { WordsMin = 3 };

        var noTempo = DailyPlanRules.Compose([C(1, 1, newAvailable: 10)], limits, tempo: null);
        var slow = DailyPlanRules.Compose([C(1, 1, newAvailable: 10)], limits, tempo: 0.4);

        Assert.Equal(3, noTempo.Tasks[0].NewWords);
        Assert.Contains("3 neue Wörter (Boden 3, noch kein Tempo)", noTempo.Tasks[0].Reason);
        Assert.Equal(3, slow.Tasks[0].NewWords);
        Assert.Contains("(Boden 3, Tempo 0,4 pro Lerntag)", slow.Tasks[0].Reason);
    }

    /// <summary>
    /// AK 2: the budget is spread over positions in rank order until it is placed, taking a further position for the rest;
    /// too few new words left warns. Red if the budget is given to one position beyond its pool, or the warning is missing.
    /// </summary>
    [Fact]
    public void Budget_verteilt_sich_ueber_die_Positionen()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 2), C(2, 2, newAvailable: 3), C(3, 3, newAvailable: 9)],
            Wide, tempo: 4);
        var short_ = DailyPlanRules.Compose([C(1, 1, newAvailable: 1)], Wide with { WordsMin = 3 }, tempo: null);

        Assert.Equal([2, 2], r.Tasks.Select(t => t.NewWords));
        Assert.Equal([1, 2], Ids(r));
        Assert.Contains("Nur 1 neue Wörter verfügbar – weniger als der Boden von 3.", short_.Warnings);
    }

    /// <summary>
    /// AK 4: the reason comes from the inputs - two positions with different data read differently, and the counts appear.
    /// Red if the reason is a constant.
    /// </summary>
    [Fact]
    public void Begruendung_folgt_den_Daten()
    {
        var r = DailyPlanRules.Compose([C(1, 1, due: 6), C(2, 2, due: 1)], Wide, tempo: null);

        Assert.Equal("6 fällige Wiederholungen", r.Tasks[0].Reason);
        Assert.Equal("1 fällige Wiederholung", r.Tasks[1].Reason);
    }

    /// <summary>
    /// AK 5: the same inputs give the same plan, whatever order the candidates arrive in. Red if the ranking falls back to the
    /// input order (no tie-break) or anything random enters.
    /// </summary>
    [Fact]
    public void Gleiche_Daten_ergeben_denselben_Plan()
    {
        PlannerCandidate[] input = [C(1, 3, newAvailable: 4), C(2, 1, newAvailable: 4), C(3, 2), C(4, 1, due: 2), C(5, 2, due: 2)];
        var limits = Wide with { TasksMin = 4 };

        var first = JsonSerializer.Serialize(DailyPlanRules.Compose(input, limits, 2.5));
        var again = JsonSerializer.Serialize(DailyPlanRules.Compose(input, limits, 2.5));
        var reversed = JsonSerializer.Serialize(DailyPlanRules.Compose([.. input.Reverse()], limits, 2.5));

        Assert.Equal(first, again);
        Assert.Equal(first, reversed);
    }

    /// <summary>
    /// AK 5: the planner depends on no chat client and no randomness - its constructor takes none. A statement of intent held
    /// mechanically: red as soon as such a dependency is added.
    /// </summary>
    [Fact]
    public void Planer_haengt_an_keinem_Sprachmodell_und_keinem_Zufall()
    {
        var parameters = typeof(DailyPlanner).GetConstructors().Single().GetParameters();

        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(Random)
            || (p.ParameterType.Namespace ?? "").StartsWith("Microsoft.Extensions.AI", StringComparison.Ordinal)
            || p.ParameterType.Name.Contains("Chat", StringComparison.Ordinal));
    }

    /// <summary>Without positions the plan is empty and says why. Red if an empty plan carries no warning.</summary>
    [Fact]
    public void Ohne_Positionen_bleibt_der_Plan_leer_mit_Warnung()
    {
        var r = DailyPlanRules.Compose([], Wide, tempo: null);

        Assert.Empty(r.Tasks);
        Assert.Single(r.Warnings);
    }
}
