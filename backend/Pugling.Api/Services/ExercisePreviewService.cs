using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Testmodus für den Vater/Lehrer: spielt eine einzelne Katalog-Übung genau so durch, wie sie das Kind im
/// Abschlusstest einer Lehrplan-Position erlebt – aber <b>vollständig nebenwirkungsfrei</b>. Es entsteht kein
/// <see cref="TestAttempt"/>, kein <see cref="PositionItemProgress"/>, keine Punkte (<c>ChildPoints</c>) und keine
/// Gamification. So kann sich der Vater mit der Übung vertraut machen und sie verifizieren, bevor er sie über einen
/// Lehrplan zuweist – ohne aufs Feedback des Kindes zu warten.
/// <para>
/// Die Bewertung ist bewusst dieselbe wie im echten Test (<see cref="PositionPlayService.IsTypedStage"/> +
/// <see cref="AnswerGrader.Matches"/> gegen <see cref="ContentItem.AcceptedAnswers"/>) – eine Quelle der Wahrheit,
/// keine gedoppelte Prüf-Logik. Nur die persistierenden Schritte des Test-Controllers fallen weg.
/// </para>
/// </summary>
public class ExercisePreviewService(ExerciseContentResolver content, AnswerGrader grader)
{
    /// <summary>
    /// Eine im Testmodus vorgelegte Aufgabe. <c>Reveal</c> trägt bei Selbsteinschätzung die aufgedeckte Lösung
    /// (bei getippten Stufen <c>null</c>); <c>AnswerLength</c> ist nur bei Vokabel-Buchstabenkästchen gesetzt.
    /// </summary>
    public record PreviewItem(int ItemIndex, string Prompt, int? GapIndex, string? Hint, int? AnswerLength, string? Reveal);

    /// <summary>Der spielbare Zustand einer Übung im Testmodus: gewählte Stufe, ob getippt wird, und die Aufgaben.</summary>
    public record PreviewData(int Stage, bool Typed, IReadOnlyList<PreviewItem> Items);

    /// <summary>Eine Antwort des Vaters: getippt (<paramref name="GivenAnswer"/>) oder Selbsteinschätzung (<paramref name="WasKnown"/>).</summary>
    public record PreviewAnswer(int ItemIndex, string? GivenAnswer, bool? WasKnown);

    /// <summary>Einzelauswertung inklusive erwarteter Lösung (im Testmodus wird die Lösung immer offengelegt).</summary>
    public record PreviewItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);

    /// <summary>Gesamtergebnis eines Testmodus-Durchlaufs.</summary>
    public record PreviewResult(int Total, int Correct, int ScorePercent, IReadOnlyList<PreviewItemOutcome> Items);

    /// <summary>
    /// Baut den spielbaren Zustand einer Übung. Liefert <c>null</c>, wenn die Übung keine prüfbaren Inhalte hat
    /// (z. B. leere Konfiguration oder ein Typ ohne Item-für-Item-Abgleich).
    /// </summary>
    public async Task<PreviewData?> BuildAsync(Exercise exercise)
    {
        var items = await content.ItemsOfAsync(exercise);
        if (items.Count == 0) return null;

        var stage = PreviewStage(exercise.Type);
        var typed = PositionPlayService.IsTypedStage(exercise.Type, stage);
        var presented = items.Select(i => Present(i, exercise.Type, stage, typed)).ToList();
        return new PreviewData(stage, typed, presented);
    }

    /// <summary>
    /// Bewertet die Antworten typ-neutral gegen die Item-Lösungen – identisch zum echten Position-Test, aber ohne
    /// jede Persistenz. Liefert <c>null</c>, wenn die Übung keine prüfbaren Inhalte hat.
    /// </summary>
    public async Task<PreviewResult?> CheckAsync(Exercise exercise, IReadOnlyList<PreviewAnswer> answers)
    {
        var items = await content.ItemsOfAsync(exercise);
        if (items.Count == 0) return null;

        var typed = PositionPlayService.IsTypedStage(exercise.Type, PreviewStage(exercise.Type));

        // Letzte Nennung je Index gewinnt (robust gegen doppelte Indizes), wie im ExerciseAnswerChecker.
        var byIndex = new Dictionary<int, PreviewAnswer>();
        foreach (var a in answers) byIndex[a.ItemIndex] = a;

        var outcomes = items.Select(item =>
        {
            byIndex.TryGetValue(item.Index, out var answer);
            var correct = typed
                ? item.AcceptedAnswers.Any(a => grader.Matches(answer?.GivenAnswer, a))
                : answer?.WasKnown ?? false;
            return new PreviewItemOutcome(item.Index, item.Prompt, item.Answer, answer?.GivenAnswer, correct);
        }).ToList();

        var correctCount = outcomes.Count(o => o.WasCorrect);
        var percent = outcomes.Count == 0 ? 0 : (int)Math.Round(100.0 * correctCount / outcomes.Count);
        return new PreviewResult(outcomes.Count, correctCount, percent, outcomes);
    }

    /// <summary>
    /// Repräsentative Stufe fürs Ausprobieren: bei Vokabel/Lückentext die getippte Endstufe (schwierigster,
    /// aussagekräftigster Test); objektive Verfahren (Zuordnung/Rechnen/Liste …) sind ohnehin immer getippt,
    /// hier ist die Stufe nur nominal.
    /// </summary>
    private static int PreviewStage(ExerciseType type) => type switch
    {
        ExerciseType.Vocabulary => (int)TestStage.FreeText,
        ExerciseType.Cloze => (int)ClozeStage.TranslationFreeText,
        ExerciseType.Matching => (int)MatchStage.Direct,
        _ => (int)TestStage.SelfAssess,
    };

    // Projektion einer Aufgabe für die Anzeige: getippte Stufen decken die Lösung NICHT auf, Selbsteinschätzung
    // schon. Buchstabenkästchen (nur Vokabel) verraten die Länge. Spiegelt PositionTestsController.ToItem.
    private static PreviewItem Present(ContentItem item, ExerciseType type, int stage, bool typed)
    {
        var isLetterBoxes = type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.LetterBoxes;
        return new PreviewItem(item.Index, item.Prompt, item.GapIndex,
            typed ? item.Hint : null,
            isLetterBoxes ? item.Answer.Length : null,
            typed ? null : item.Answer);
    }
}
