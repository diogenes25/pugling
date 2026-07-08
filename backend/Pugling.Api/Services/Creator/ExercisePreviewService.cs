using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

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
    public record PreviewItem(int ItemIndex, string Prompt, int? GapIndex, string? Hint, int? AnswerLength, string? Reveal,
        IReadOnlyList<string>? Choices, string? AudioUrl);

    /// <summary>Eine im Testmodus umschaltbare Abfrageform (Stufenwert + Anzeigename).</summary>
    public record StageOption(int Value, string Label);

    /// <summary>
    /// Der spielbare Zustand einer Übung im Testmodus: Typ, gewählte Stufe, ob getippt wird, die Aufgaben und
    /// – zum Durchprobieren – die für diesen Übungstyp umschaltbaren Abfrageformen (<see cref="Stages"/>).
    /// </summary>
    public record PreviewData(string Type, int Stage, bool Typed, IReadOnlyList<StageOption> Stages, IReadOnlyList<PreviewItem> Items);

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
    public async Task<PreviewData?> BuildAsync(Exercise exercise, int? stageOverride = null)
    {
        var items = await content.ItemsOfAsync(exercise);
        if (items.Count == 0) return null;

        // Der Vater darf im Testmodus jede Abfrageform durchprobieren (stageOverride); ohne Wahl bevorzugt die
        // vom Ersteller gewählte Standard-Abfrageform der Übung, sonst die repräsentative Stufe.
        var stage = stageOverride ?? exercise.DefaultStage ?? PreviewStage(exercise.Type);
        var typed = PositionPlayService.IsTypedStage(exercise.Type, stage);
        var presented = items.Select(i =>
            Present(i, exercise.Type, stage, typed, PositionPlayService.ChoicesFor(items, i, exercise.Type, stage))).ToList();
        return new PreviewData(exercise.Type.ToString(), stage, typed, StagesFor(exercise.Type), presented);
    }

    /// <summary>
    /// Bewertet die Antworten typ-neutral gegen die Item-Lösungen – identisch zum echten Position-Test, aber ohne
    /// jede Persistenz. Liefert <c>null</c>, wenn die Übung keine prüfbaren Inhalte hat.
    /// </summary>
    public async Task<PreviewResult?> CheckAsync(Exercise exercise, IReadOnlyList<PreviewAnswer> answers, int? stageOverride = null)
    {
        var items = await content.ItemsOfAsync(exercise);
        if (items.Count == 0) return null;

        // Dieselbe Stufe wie beim Bauen (sonst driften „getippt" hier und im Client auseinander).
        var typed = PositionPlayService.IsTypedStage(exercise.Type, stageOverride ?? exercise.DefaultStage ?? PreviewStage(exercise.Type));

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

    /// <summary>
    /// Die im Testmodus umschaltbaren Abfrageformen eines Übungstyps (damit der Vater jede Form durchprobieren
    /// kann). Nur Vokabeln/Lückentexte haben mehrere Stufen; objektive Verfahren sind ohnehin immer getippt und
    /// liefern eine leere Liste (kein Umschalter nötig).
    /// </summary>
    private static IReadOnlyList<StageOption> StagesFor(ExerciseType type) => type switch
    {
        ExerciseType.Vocabulary =>
        [
            new((int)TestStage.SelfAssess, "Selbsteinschätzung"),
            new((int)TestStage.MultipleChoice, "Multiple-Choice"),
            new((int)TestStage.LetterBoxes, "Buchstabenkästchen"),
            new((int)TestStage.FreeText, "Freitext (tippen)"),
            new((int)TestStage.Audio, "Hören → tippen"),
        ],
        ExerciseType.Cloze =>
        [
            new((int)ClozeStage.TranslationWordBank, "Wortbank"),
            new((int)ClozeStage.TranslationFreeText, "Übersetzung + Freitext"),
            new((int)ClozeStage.FreeText, "Freitext"),
        ],
        _ => [],
    };

    // Projektion einer Aufgabe für die Anzeige: getippte Stufen decken die Lösung NICHT auf, Selbsteinschätzung
    // schon. Buchstabenkästchen (nur Vokabel) verraten die Länge, die Hör-Stufe die Audioquelle. Spiegelt
    // PositionTestsController.ToItem.
    private static PreviewItem Present(ContentItem item, ExerciseType type, int stage, bool typed, IReadOnlyList<string>? choices)
    {
        var isLetterBoxes = type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.LetterBoxes;
        var isAudio = type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.Audio;
        return new PreviewItem(item.Index, item.Prompt, item.GapIndex,
            typed ? item.Hint : null,
            isLetterBoxes ? item.Answer.Length : null,
            typed ? null : item.Answer,
            choices,
            isAudio ? item.AudioUrl : null);
    }
}
