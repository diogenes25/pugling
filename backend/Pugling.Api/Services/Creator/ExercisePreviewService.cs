using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>
/// Preview mode for the adult/teacher: plays through a single catalog exercise exactly the way the child
/// experiences it in a study plan position's final test – but <b>entirely free of side effects</b>. No
/// <see cref="TestAttempt"/>, no <see cref="PositionItemProgress"/>, no points (<c>ChildPoints</c>) and no
/// gamification are created. This lets the adult get familiar with the exercise and verify it before
/// assigning it via a study plan – without having to wait for the child's feedback.
/// <para>
/// Grading is deliberately identical to the real test (<see cref="IExerciseType.IsTypedStage(int)"/> +
/// <see cref="AnswerGrader.Matches"/> against <see cref="ContentItem.AcceptedAnswers"/>) – one source of truth,
/// no duplicated grading logic. Only the persisting steps of the test controller are left out.
/// </para>
/// </summary>
public class ExercisePreviewService(ExerciseContentResolver content, AnswerGrader grader, ExerciseTypeRegistry registry)
{
    // Die Vertrags-Records (PreviewItem/PreviewData/PreviewAnswer/PreviewItemOutcome/PreviewResult)
    // leben im Vertrags-Projekt (Pugling.Contracts.Creator).

    /// <summary>
    /// Builds the playable state of an exercise. Returns <c>null</c> if the exercise has no gradable content
    /// (e.g. empty configuration or a type without item-by-item matching).
    /// </summary>
    public async Task<PreviewData?> BuildAsync(Exercise exercise, int? stageOverride = null,
        CancellationToken ct = default)
    {
        var items = await content.ItemsOfAsync(exercise, ct: ct);
        if (items.Count == 0) return null;

        if (registry.ByKey(exercise.Type) is not { } type) return null;
        // Der Vater darf im Testmodus jede Abfrageform durchprobieren (stageOverride); ohne Wahl bevorzugt die
        // vom Ersteller gewählte Standard-Abfrageform der Übung, sonst die repräsentative Stufe.
        var stage = stageOverride ?? exercise.DefaultStage ?? type.PreviewStage;
        var typed = type.IsTypedStage(stage);
        var presented = items.Select(i => Present(i, type, stage, typed, type.Choices(items, i, stage))).ToList();
        return new PreviewData(exercise.Type, stage, typed, type.StageOptions, presented);
    }

    /// <summary>
    /// Grades the answers type-neutrally against the item solutions – identical to the real plan position test,
    /// but without any persistence. Returns <c>null</c> if the exercise has no gradable content.
    /// </summary>
    public async Task<PreviewResult?> CheckAsync(Exercise exercise, IReadOnlyList<PreviewAnswer> answers,
        int? stageOverride = null, CancellationToken ct = default)
    {
        var items = await content.ItemsOfAsync(exercise, ct: ct);
        if (items.Count == 0) return null;

        if (registry.ByKey(exercise.Type) is not { } type) return null;
        // Dieselbe Stufe wie beim Bauen (sonst driften „getippt" hier und im Client auseinander).
        var typed = type.IsTypedStage(stageOverride ?? exercise.DefaultStage ?? type.PreviewStage);

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

    // Projektion einer Aufgabe für die Anzeige: getippte Stufen decken die Lösung NICHT auf, Selbsteinschätzung
    // schon. Buchstabenkästchen (nur Vokabel) verraten die Länge, die Hör-Stufe die Audioquelle. Spiegelt
    // PositionTestsController.ToItem.
    private static PreviewItem Present(ContentItem item, IExerciseType type, int stage, bool typed, IReadOnlyList<string>? choices)
    {
        // Kein Bild im Testmodus: die Auswahl hängt am Profil eines Kindes, der Vater probiert hier aber
        // kind-neutral aus. Ein beliebiges Bild wäre irreführend – es zeigte nicht, was sein Sohn sieht.
        var (letterBoxLength, audioUrl, _) = type.StageFacets(item, stage);
        return new PreviewItem(item.Index, item.Prompt, item.GapIndex,
            typed ? item.Hint : null,
            letterBoxLength,
            typed ? null : item.Answer,
            choices,
            audioUrl);
    }
}
