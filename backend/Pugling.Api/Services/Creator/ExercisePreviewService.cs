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
    // The contract records (PreviewItem/PreviewData/PreviewAnswer/PreviewItemOutcome/PreviewResult)
    // live in the contract project (Pugling.Contracts.Creator).

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
        // In preview mode the supervisor may try every question form (stageOverride); without a choice we prefer
        // the exercise's default question form as picked by its author, otherwise the representative stage.
        var stage = stageOverride ?? exercise.DefaultStage ?? type.PreviewStage;
        var typed = type.IsTypedStage(stage);
        var presented = items.Select(i => Present(i, type, stage, typed, type.Choices(exercise.ConfigJson, items, i, stage))).ToList();
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
        // The same stage as when building (otherwise "typed" drifts apart here and in the client).
        var typed = type.IsTypedStage(stageOverride ?? exercise.DefaultStage ?? type.PreviewStage);

        // The last mention per index wins (robust against duplicate indexes), as in ExerciseAnswerChecker.
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

    // Projection of one task for display: typed stages do NOT reveal the answer, self-assessment does.
    // Letter boxes (vocabulary only) give away the length, the listening stage the audio source. Mirrors
    // PositionTestsController.ToItem.
    private static PreviewItem Present(ContentItem item, IExerciseType type, int stage, bool typed, IReadOnlyList<string>? choices)
    {
        // No image in preview mode: the selection hangs on a child's profile, but here the supervisor tries
        // things out child-neutrally. An arbitrary image would mislead - it would not show what their child sees.
        var (letterBoxLength, audioUrl, _) = type.StageFacets(item, stage);
        return new PreviewItem(item.Index, item.Prompt, item.GapIndex,
            typed ? item.Hint : null,
            letterBoxLength,
            typed ? null : item.Answer,
            choices,
            audioUrl);
    }
}
