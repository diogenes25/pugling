using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Positions-basierter Lern-Motor (neues Modell): wählt die heute fälligen Inhalte einer
/// <see cref="PlanPosition"/>, terminiert ihren Karteikasten-Fortschritt (<see cref="PositionItemProgress"/>)
/// und löst die Teststufe auf. Der Inhalt kommt aus der Übungs-Config über den
/// <see cref="ExerciseContentProvider"/>, die Bewertung bleibt beim <see cref="AnswerGrader"/>.
/// Das Gegenstück zu den früheren plan-weiten Schedule-/Fortschritts-Services, aber pro Übung
/// statt pro Plan – Ziele und Punkte hängen jetzt an der Position.
/// </summary>
public class PositionPlayService(PuglingDbContext db, ExerciseContentResolver content)
{
    /// <summary>Standard-Leitner-Intervalle in Tagen (Index = Box; Index 0 ungenutzt).</summary>
    private static readonly int[] DefaultBoxIntervalDays = [0, 1, 2, 4, 7, 14];

    /// <summary>Leitner-Intervalle der Position (eigene, sonst Standard).</summary>
    public IReadOnlyList<int> BoxIntervals(PlanPosition pos) =>
        pos.BoxIntervalDays is { Count: > 1 } custom ? custom : DefaultBoxIntervalDays;

    /// <summary>Die Inhalts-Items der Übung dieser Position (Store-aufgelöst bei referenzierten Vokabeln).</summary>
    public async Task<IReadOnlyList<ContentItem>> ItemsOfAsync(PlanPosition pos) =>
        pos.Exercise is { } ex ? await content.ItemsOfAsync(ex) : [];

    /// <summary>
    /// Darf der Sohn diesen Plan heute spielen (üben/testen)? Nur ein aktiver Plan innerhalb seiner
    /// Laufzeit ist spielbar – so kann sich das Kind keinen leichten oder abgelaufenen Plan zum bequemen
    /// Punktesammeln aussuchen (Anti-Schummel). Der Vater ist davon ausgenommen (Vorschau/Nachtrag).
    /// </summary>
    public static bool PlanPlayableForChild(StudyPlan plan, DateOnly today) =>
        plan.Active && plan.StartDate <= today && today <= plan.EndDate;

    /// <summary>
    /// Für einen Tag geltende Teststufe der Position: Fahrplan (falls gesetzt) → Positions-Override →
    /// Übungs-Default → Verfahrens-Standard. Serverseitig erzwungen (nicht vom Client wählbar).
    /// </summary>
    public static int StageForDay(PlanPosition pos, StudyPlan plan, DateOnly day)
    {
        var dayNumber = day.DayNumber - plan.StartDate.DayNumber + 1;
        var step = pos.StageSchedule?
            .Where(s => s.DayNumber <= dayNumber)
            .OrderByDescending(s => s.DayNumber)
            .FirstOrDefault();
        return step?.Stage ?? pos.Stage ?? pos.Exercise?.DefaultStage ?? DefaultStageFor(pos.Exercise?.Type);
    }

    private static int DefaultStageFor(ExerciseType? type) => type switch
    {
        ExerciseType.Cloze => (int)ClozeStage.TranslationWordBank,
        ExerciseType.Matching => (int)MatchStage.Direct,
        _ => (int)TestStage.SelfAssess,
    };

    /// <summary>
    /// Ist die Stufe „getippt"/objektiv (serverseitig gegen die Lösung prüfbar) – im Gegensatz zu reiner
    /// Anzeige/Selbsteinschätzung? Objektive Verfahren (Zuordnung, Liste, Rechnen …) sind immer getippt;
    /// bei Vokabeln/Lückentexten entscheidet die Stufe.
    /// </summary>
    public static bool IsTypedStage(ExerciseType type, int stage) => type switch
    {
        ExerciseType.Vocabulary => StageMechanics.IsTyped((TestStage)stage),
        ExerciseType.Cloze => StageMechanics.IsTyped((ClozeStage)stage),
        _ => true,
    };

    /// <summary>
    /// Auswahlmöglichkeiten für eine Multiple-Choice-Vokabelaufgabe: die richtige Antwort plus bis zu drei
    /// Ablenker aus den übrigen Items derselben Übung (dedupliziert, normalisiert). <c>null</c> für alle
    /// anderen Verfahren/Stufen. Deterministische Rotation, damit die Lösung nicht immer vorne steht (kein Zufall).
    /// </summary>
    public static IReadOnlyList<string>? ChoicesFor(IReadOnlyList<ContentItem> items, ContentItem item, ExerciseType type, int stage)
    {
        if (type != ExerciseType.Vocabulary || (TestStage)stage != TestStage.MultipleChoice) return null;
        if (string.IsNullOrWhiteSpace(item.Answer)) return null;

        var seen = new HashSet<string>(StringComparer.Ordinal) { StageMechanics.Normalize(item.Answer) };
        var distractors = new List<string>();
        foreach (var other in items)
        {
            if (other.Index == item.Index || string.IsNullOrWhiteSpace(other.Answer)) continue;
            if (seen.Add(StageMechanics.Normalize(other.Answer))) distractors.Add(other.Answer);
            if (distractors.Count >= 3) break;
        }

        var choices = new List<string>(distractors.Count + 1) { item.Answer };
        choices.AddRange(distractors);
        var shift = item.Index % choices.Count;
        return [.. choices.Skip(shift), .. choices.Take(shift)];
    }

    /// <summary>Anzahl genutzter Inhalte der Position (Override, sonst alle vorhandenen).</summary>
    public int PoolSize(PlanPosition pos, int available) =>
        pos.ItemCount is > 0 ? Math.Min(pos.ItemCount.Value, available) : available;

    /// <summary>
    /// Wählt die Item-Indizes, die heute dran sind: begrenzt auf den Pool (<see cref="PlanPosition.ItemCount"/>),
    /// gefiltert nach <see cref="ItemScope"/> (neu/alt/alle) und – bei Leitner – nur die fälligen
    /// (nie gesehen zählt als fällig), sortiert nach Box und Index. Der Fortschritt wird dazu geladen,
    /// aber NICHT neu angelegt (das passiert erst beim Bewerten in <see cref="ApplyReview"/>).
    /// </summary>
    public async Task<IReadOnlyList<int>> DueItemIndicesAsync(PlanPosition pos, DateOnly day)
    {
        var poolSize = PoolSize(pos, (await ItemsOfAsync(pos)).Count);
        if (poolSize == 0) return [];

        var progress = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == pos.Id && p.ItemIndex < poolSize)
            .ToDictionaryAsync(p => p.ItemIndex);

        return Enumerable.Range(0, poolSize)
            .Select(i => (Index: i, Prog: progress.GetValueOrDefault(i)))
            .Where(x => ScopeMatch(pos.Scope, x.Prog) && (!pos.UseLeitner || IsDue(x.Prog, day)))
            .OrderBy(x => x.Prog?.Box ?? 1).ThenBy(x => x.Index)
            .Select(x => x.Index)
            .ToList();
    }

    private static bool ScopeMatch(ItemScope scope, PositionItemProgress? prog) => scope switch
    {
        ItemScope.New => prog?.IntroducedAt is null,
        ItemScope.Old => prog?.IntroducedAt is not null,
        _ => true,
    };

    // Fällig, wenn nie gesehen (kein Fortschritt) oder Fälligkeit erreicht.
    private static bool IsDue(PositionItemProgress? prog, DateOnly day) =>
        prog is null || prog.DueOn is null || prog.DueOn <= day;

    /// <summary>Holt den Fortschritts-Satz eines Inhaltsatoms der Position oder legt ihn (nachverfolgt) an.</summary>
    public async Task<PositionItemProgress> ProgressForAsync(int positionId, int itemIndex)
    {
        var prog = await db.PositionItemProgress
            .FirstOrDefaultAsync(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
        if (prog is null)
        {
            prog = new PositionItemProgress { PlanPositionId = positionId, ItemIndex = itemIndex };
            db.PositionItemProgress.Add(prog);
        }
        return prog;
    }

    /// <summary>
    /// Verbucht eine Leitner-Wiederholung auf dem Fortschritt: richtig → eine Box höher (längeres
    /// Intervall), falsch → zurück in Box 1 und sofort wieder fällig. Der Aufrufer speichert.
    /// </summary>
    public void ApplyReview(PlanPosition pos, PositionItemProgress prog, bool correct, DateOnly today, DateTime nowUtc)
    {
        var intervals = BoxIntervals(pos);
        prog.ReviewCount++;
        prog.LastReviewedAt = nowUtc;

        if (correct)
        {
            prog.Box = Math.Min(pos.MaxBox, Math.Max(1, prog.Box) + 1);
            prog.DueOn = today.AddDays(intervals[Math.Min(prog.Box, intervals.Count - 1)]);
        }
        else
        {
            prog.Box = 1;
            prog.DueOn = today; // gleicher Tag: sofort erneut üben
        }
    }
}
