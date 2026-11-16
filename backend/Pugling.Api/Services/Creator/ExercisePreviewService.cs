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
/// <see cref="AnswerGrader.Matches(string?, ContentItem)"/> against <see cref="ContentItem.GradedAnswers"/>) – one source of truth,
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
        if (registry.ByKey(exercise.Type) is not { } type) return null;
        var items = await content.ItemsOfAsync(exercise, ct: ct);
        if (items.Count == 0)
        {
            // A type without atoms may still have something to look at (B-15: the essay's writing task). It is a
            // view, not a task: not typed, not gradable - CheckAsync keeps answering "nothing to check".
            return type.StaticView(exercise.ConfigJson) is { } view
                ? new PreviewData(exercise.Type, type.PreviewStage, false, [],
                    [new PreviewItem(view.Index, view.Prompt, null, view.Hint, null, null, null, null)], Gradable: false)
                : null;
        }

        // In preview mode the supervisor may try every question form (stageOverride); without a choice we prefer
        // the exercise's default question form as picked by its author, otherwise the representative stage.
        var stage = stageOverride ?? exercise.DefaultStage ?? type.PreviewStage;
        var typed = type.IsTypedStage(stage);
        var presented = items.Select(i => Present(exercise.ConfigJson, items, i, type, stage, exercise.Id)).ToList();
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
        var stage = stageOverride ?? exercise.DefaultStage ?? type.PreviewStage;
        var typed = type.IsTypedStage(stage);

        // A set-graded exercise (an unordered list) is graded the way the child's exam grades it: each answer
        // credits the first entry still open, in the order the answers were given. Without this the supervisor's
        // trial run would contradict the exam - and the trial run is what they assign the exercise on.
        var outcomes = typed && type.GradesAsSet(exercise.ConfigJson)
            ? GradeAsSet(items, answers)
            : GradeByIndex(type, items, answers, stage);

        var correctCount = outcomes.Count(o => o.WasCorrect);
        var percent = outcomes.Count == 0 ? 0 : (int)Math.Round(100.0 * correctCount / outcomes.Count);
        return new PreviewResult(outcomes.Count, correctCount, percent, outcomes);
    }

    // One outcome per atom, addressed by index. The last mention per index wins (robust against duplicate
    // indexes), as in ExerciseAnswerChecker.
    // Each card at its own stage (B-597): the run's stage, capped by the card's ceiling, and the card turned the way that
    // stage asks it - the same rule the child's practice and test grade by, and the form Present showed it in.
    private List<PreviewItemOutcome> GradeByIndex(IExerciseType type, IReadOnlyList<ContentItem> items,
        IReadOnlyList<PreviewAnswer> answers, int stage)
    {
        var byIndex = new Dictionary<int, PreviewAnswer>();
        foreach (var a in answers) byIndex[a.ItemIndex] = a;

        return items.Select(raw =>
        {
            var cardStage = type.CardCeiling(raw, stage);
            var staged = PositionPlayService.AtStage(type, items, cardStage);
            var item = type.CardForStage(raw, cardStage);
            var typed = type.IsTypedStage(cardStage);
            byIndex.TryGetValue(item.Index, out var answer);
            var correct = typed
                ? grader.Matches(answer?.GivenAnswer, item)
                : answer?.WasKnown ?? false;
            var misspelled = typed && !correct && grader.IsMisspelled(answer?.GivenAnswer, item, staged);
            return new PreviewItemOutcome(item.Index, item.Prompt, item.Answer, answer?.GivenAnswer, correct, misspelled, item.GapIndex);
        }).ToList();
    }

    // One outcome per ENTRY: an answer credits the entry it hits, a repeat hits nothing. Unlike the exam this
    // keeps no wrong mentions - the preview has no result row to hang them on, and the supervisor is checking
    // the exercise, not their own spelling.
    private List<PreviewItemOutcome> GradeAsSet(IReadOnlyList<ContentItem> items,
        IReadOnlyList<PreviewAnswer> answers)
    {
        var credited = new Dictionary<int, string?>();
        foreach (var answer in answers)
        {
            var open = items.Select(i => i.Index).Where(i => !credited.ContainsKey(i));
            if (PositionPlayService.MatchOpenEntry(items, open, answer.GivenAnswer, grader) is { } hit)
                credited[hit] = answer.GivenAnswer;
        }

        return [.. items.Select(item => new PreviewItemOutcome(item.Index, item.Prompt, item.Answer,
            credited.GetValueOrDefault(item.Index), credited.ContainsKey(item.Index)))];
    }

    // Projection of one task for display - through the SAME projection the child's card uses
    // (PositionPlayService.CardFacets), not a parallel one. It used to be a hand-rolled copy with the comment
    // "mirrors PositionTestsController.ToItem", and the copy fell behind the moment the original learned to
    // withhold the prompt: the supervisor then saw the word AND heard the recording on the listening stage,
    // so the one stage where the audio carries the whole task was the one they could not check.
    private static PreviewItem Present(string configJson, IReadOnlyList<ContentItem> items, ContentItem raw,
        IExerciseType type, int stage, int seed)
    {
        // B-597: the card at its own ceiling and in the form that stage asks it - what the child gets.
        var cardStage = type.CardCeiling(raw, stage);
        var staged = PositionPlayService.AtStage(type, items, cardStage);
        var item = type.CardForStage(raw, cardStage);
        // The exercise id as shuffle seed (B-05): there is no session, and the same preview should not reshuffle.
        var f = PositionPlayService.CardFacets(configJson, staged, item, type, cardStage, type.IsTypedStage(cardStage), seed);
        // No image in preview mode: the selection hangs on a child's profile, but here the supervisor tries
        // things out child-neutrally. An arbitrary image would mislead - it would not show what their child sees.
        return new PreviewItem(item.Index, f.Prompt, f.GapIndex, f.Hint, f.AnswerLength, f.Reveal,
            f.Choices, f.AudioUrl, f.Passage, f.AnyOrder, f.RevealAlternatives, f.Decoding, f.AnswerPattern, f.LetterTiles,
            f.CueBox, f.DictationSpokenAnswer, f.DictationLang);
    }
}
