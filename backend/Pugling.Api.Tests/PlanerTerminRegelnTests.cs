namespace Pugling.Api.Tests;

/// <summary>
/// B-680: the planner's rules for class test, timetable and weak subject as a pure function, without a database. Each case
/// names the change that turns it red.
/// </summary>
public class PlanerTerminRegelnTests
{
    private static readonly PlannerLimits Wide = new(TasksMin: 1, TasksMax: 10, WordsMin: 2, WordsMax: 20);
    private static readonly DateOnly Exam = new(2026, 12, 3);

    private static PlannerCandidate C(int id, int order, int newAvailable = 0, int due = 0, int? subject = 1,
        DateOnly? exam = null, int examDays = 0, bool lesson = false, double weakness = 0, bool presses = true) =>
        new(id, ExerciseId: 100 + id, $"Übung {id}", subject, order, due, newAvailable, 0, 0, false, 4, 3, exam, examDays,
            lesson, weakness, presses);

    private static int[] Ids(PlannerResult r) => [.. r.Tasks.Select(t => t.Candidate.PositionId)];

    /// <summary>
    /// AK 2: the same word list needs more new words per day the nearer its test - 12 words in 3 learning days are 4 a day, in
    /// 18 learning days the floor of 2 holds. Red if the planner ignores the test (both days give the floor).
    /// </summary>
    [Fact]
    public void Naehere_Klassenarbeit_ergibt_mehr_neue_Woerter_bei_gleicher_Wortliste()
    {
        var near = DailyPlanRules.Compose([C(1, 1, newAvailable: 12, exam: Exam, examDays: 3)], Wide, tempo: null);
        var far = DailyPlanRules.Compose([C(1, 1, newAvailable: 12, exam: Exam, examDays: 18)], Wide, tempo: null);

        Assert.Equal(4, near.NewWordsBudget);
        Assert.Equal(2, far.NewWordsBudget);
        Assert.Equal(4, near.Tasks.Single().NewWords);
    }

    /// <summary>
    /// B-1034: a test that names no material orders its position by date but sets no pace - no budget lift, no warning. Red if
    /// `ExamPresses` is ignored.
    /// </summary>
    [Fact]
    public void Klassenarbeit_ohne_Stoff_hebt_das_Budget_nicht_und_meldet_nichts()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 100, exam: Exam, examDays: 3, presses: false)], Wide, tempo: null);

        Assert.Equal(2, r.NewWordsBudget);
        Assert.Empty(r.Warnings);
        Assert.Contains("Klassenarbeit am 03.12.", r.Tasks.Single().Reason);
    }

    /// <summary>AK 3: without a test the plan is not empty and the new words sit at the floor. Red if the floor is not applied.</summary>
    [Fact]
    public void Ohne_Klassenarbeit_liegt_der_Plan_am_Boden()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 30)], Wide, tempo: null);

        Assert.Equal(2, r.NewWordsBudget);
        Assert.Equal(2, r.Tasks.Single().NewWords);
    }

    /// <summary>
    /// AK 9: when the needed pace is above the ceiling, the budget stays at the ceiling and the supervisor is told the missing
    /// amount (100 words in 3 days need 34 a day, 14 over the ceiling, for 3 days: 42). Red if the ceiling is exceeded or the
    /// warning is dropped.
    /// </summary>
    [Fact]
    public void Noetiges_Tempo_ueber_dem_Deckel_wird_gedeckelt_und_gemeldet()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 100, exam: Exam, examDays: 3)], Wide, tempo: null);

        Assert.Equal(20, r.NewWordsBudget);
        Assert.Equal(20, r.Tasks.Single().NewWords);
        Assert.Contains(r.Warnings, w => w.Contains("34 neue Wörter pro Lerntag") && w.Contains("fehlen etwa 42 Wörter")
            && w.Contains("03.12."));
    }

    /// <summary>AK 9, the other side: a pace within the ceiling gives no warning. Red if the warning fires without a shortfall.</summary>
    [Fact]
    public void Ohne_Fehlmenge_gibt_es_keine_Meldung()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 12, exam: Exam, examDays: 3)], Wide, tempo: null);

        Assert.DoesNotContain(r.Warnings, w => w.Contains("Klassenarbeit"));
    }

    /// <summary>
    /// AK 11: a near partial test pulls its exercise before the later one - the position with the earlier date leads and takes the
    /// words. Red if only the latest test counts (the order falls back to the plan's).
    /// </summary>
    [Fact]
    public void Nahe_Teilpruefung_geht_der_spaeteren_Klausur_vor()
    {
        var later = new DateOnly(2026, 12, 20);
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 10, exam: later, examDays: 15), C(2, 2, newAvailable: 10, exam: Exam, examDays: 3)],
            new PlannerLimits(1, 10, 2, 4), tempo: null);

        Assert.Equal(2, Ids(r).First());
        Assert.Equal(4, r.Tasks.First().NewWords);
    }

    /// <summary>
    /// AK 12: a subject with a lesson today or tomorrow leads the new words; a subject without an entry still stays in the plan
    /// above the floor of two tasks. Red if the timetable is ignored or the floor fills only from the lesson subjects.
    /// </summary>
    [Fact]
    public void Fach_mit_Stunde_hat_Vorrang_und_ein_Fach_ohne_Eintrag_bleibt_ueber_den_Boden_dran()
    {
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 10, subject: 1), C(2, 2, newAvailable: 10, subject: 2, lesson: true)],
            new PlannerLimits(2, 10, 3, 3), tempo: null);

        Assert.Equal([2, 1], Ids(r));
        Assert.Equal(3, r.Tasks[0].NewWords);
        Assert.Equal(2, r.Tasks.Count);
        Assert.Contains("Stundenplan", r.Tasks[0].Reason);
    }

    /// <summary>
    /// AK 13: at equal data the weaker subject comes first. Red if the weakness is dropped from the ranking (the plan's order wins).
    /// </summary>
    [Fact]
    public void Das_schwaechere_Fach_bekommt_den_Vorrang()
    {
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 10, subject: 1, weakness: 0.1), C(2, 2, newAvailable: 10, subject: 2, weakness: 0.5)],
            new PlannerLimits(1, 10, 3, 3), tempo: null);

        Assert.Equal(2, Ids(r).First());
        Assert.Contains("50 % falsche Antworten", r.Tasks.First().Reason);
    }

    /// <summary>
    /// AK 8: the reason is built from the inputs - the test date and the days left show in it, and a different date reads
    /// differently. Red if the text is constant.
    /// </summary>
    [Fact]
    public void Begruendung_nennt_Termin_und_Lerntage()
    {
        var a = DailyPlanRules.Compose([C(1, 1, newAvailable: 12, exam: Exam, examDays: 3)], Wide, tempo: null);
        var b = DailyPlanRules.Compose([C(1, 1, newAvailable: 12, exam: Exam.AddDays(7), examDays: 8)], Wide, tempo: null);

        Assert.Contains("Klassenarbeit am 03.12., noch 3 Lerntage", a.Tasks.Single().Reason);
        Assert.Contains("Klassenarbeit am 10.12., noch 8 Lerntage", b.Tasks.Single().Reason);
    }

    /// <summary>
    /// AK 7: equal inputs give an equal plan (no clock, no randomness in the rules). Red if the rules read a hidden source.
    /// </summary>
    [Fact]
    public void Gleiche_Eingaben_ergeben_denselben_Plan()
    {
        PlannerCandidate[] input = [C(1, 1, newAvailable: 12, exam: Exam, examDays: 3, weakness: 0.3), C(2, 2, newAvailable: 4, lesson: true)];
        var first = DailyPlanRules.Compose(input, Wide, tempo: 1.5);
        var second = DailyPlanRules.Compose(input, Wide, tempo: 1.5);

        Assert.Equal(first.NewWordsBudget, second.NewWordsBudget);
        Assert.Equal(first.Tasks.Select(t => (t.Candidate.PositionId, t.NewWords, t.Reason)),
            second.Tasks.Select(t => (t.Candidate.PositionId, t.NewWords, t.Reason)));
    }

    /// <summary>
    /// B-680 review: each pressured position gets its own pace before the rest is shared in rank order - A needs 2 a day, B needs
    /// 3, the budget is their sum 5, and both get theirs (the nearer test must not take all five). Red if the budget goes to the
    /// first position in rank order only (A 5, B 0).
    /// </summary>
    [Fact]
    public void Jede_Klausur_bekommt_ihr_eigenes_Tempo_vor_dem_Rest()
    {
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 6, exam: Exam, examDays: 3), C(2, 2, newAvailable: 9, exam: Exam.AddDays(1), examDays: 3)],
            Wide, tempo: null);

        Assert.Equal(5, r.NewWordsBudget);
        Assert.Equal([2, 3], r.Tasks.Select(t => t.NewWords));
    }

    /// <summary>
    /// B-680 review: due reviews still lead; a position with a near test and nothing due follows them. Red if the test date
    /// ranks above rule 1.
    /// </summary>
    [Fact]
    public void Faellige_Wiederholungen_gehen_einer_nahen_Klassenarbeit_vor()
    {
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 10, exam: Exam, examDays: 2), C(2, 2, due: 3)], Wide, tempo: null);

        Assert.Equal(2, Ids(r).First());
    }

    /// <summary>
    /// B-680 review: the reason names a budget a class test lifted - not "Boden ... noch kein Tempo". Red if the source still reads
    /// the floor.
    /// </summary>
    [Fact]
    public void Begruendung_nennt_die_Klassenarbeit_als_Quelle_des_gehobenen_Budgets()
    {
        var r = DailyPlanRules.Compose([C(1, 1, newAvailable: 12, exam: Exam, examDays: 3)], Wide, tempo: null);

        Assert.Contains("4 pro Lerntag bis zur Klassenarbeit", r.Tasks.Single().Reason);
        Assert.DoesNotContain("noch kein Tempo", r.Tasks.Single().Reason);
    }

    /// <summary>
    /// B-680 review: a position with a near test stays in the plan even when an earlier position (due reviews, plenty of new
    /// words) would use the whole budget - its pace is reserved. Red if the pressured position is only chosen from what is left.
    /// </summary>
    [Fact]
    public void Eine_pressierte_Position_bleibt_im_Plan_auch_wenn_eine_faellige_das_Budget_braeuchte()
    {
        var r = DailyPlanRules.Compose(
            [C(1, 1, newAvailable: 10, due: 2), C(2, 2, newAvailable: 6, exam: Exam, examDays: 3)], Wide, tempo: null);

        Assert.Contains(2, Ids(r));
        Assert.Equal(2, r.Tasks.Single(t => t.Candidate.PositionId == 2).NewWords);
    }
}
