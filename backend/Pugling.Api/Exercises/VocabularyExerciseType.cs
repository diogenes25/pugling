using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Vocabulary training: word ↔ translation across several stages (self-assessment, multiple choice, letter boxes,
/// free text, listening). Carries the lion's share of the type-specific rules – store-backed items, distractors, stages,
/// and cross-plan item learning progress. Canonical projection front → back; the query direction flips
/// the item (<see cref="ExerciseContentProvider.WithDirection"/>).
/// </summary>
public sealed class VocabularyExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Vocabulary;

    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Vocabulary, "Vokabeln", "flashcards", 1, "vocabulary",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Vocabulary,
        ["letterHints", "audio", "selfAssess", "multipleChoice"]);

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<VocabularyConfig>(configJson);
        return [.. c.Items.Select((v, i) => ExerciseContentProvider.WithDirection(
            new ContentItem(i, v.Front ?? "", v.Back ?? "", [v.Back ?? ""], v.Hint), c.Direction))];
    }

    // Fürs Ausprobieren die getippte Freitext-Stufe (schwierigster, aussagekräftigster Test).
    /// <inheritdoc/>
    public override int PreviewStage => (int)TestStage.FreeText;

    /// <inheritdoc/>
    public override bool IsTypedStage(int stage) => StageMechanics.IsTyped((TestStage)stage);

    /// <summary>
    /// Multiple-choice options: correct answer plus up to three distractors from the remaining items (deduplicated,
    /// normalized). Deterministic rotation per index, so the solution isn't always at the front (no randomness).
    /// </summary>
    public override IReadOnlyList<string>? Choices(IReadOnlyList<ContentItem> items, ContentItem item, int stage)
    {
        if ((TestStage)stage != TestStage.MultipleChoice || string.IsNullOrWhiteSpace(item.Answer)) return null;

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

    /// <summary>
    /// Letter boxes give the length, the listening stage the audio source – and the image appears
    /// <b>only on non-typed stages</b>.
    /// <para>
    /// This is stricter than for audio, for a concrete reason: the pronunciation reads out a
    /// single word (after the direction swap it is therefore deliberately dropped), whereas a motif shows
    /// the <i>meaning</i>. "A unicorn is running" gives away the solution for <c>run → laufen</c> just as much as a
    /// spoken-out answer would. Hence the conservative rule here instead of a direction-dependent
    /// fine distinction: it is shown only where the solution is revealed anyway (self-assessment) –
    /// exactly the stage where the image serves its purpose, namely memorization.
    /// </para>
    /// </summary>
    public override (int? LetterBoxLength, string? AudioUrl, string? ImageUrl) StageFacets(ContentItem item, int stage) =>
        ((TestStage)stage == TestStage.LetterBoxes ? item.Answer.Length : null,
         (TestStage)stage == TestStage.Audio ? item.AudioUrl : null,
         IsTypedStage(stage) ? null : item.ImageUrl);

    /// <inheritdoc/>
    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)TestStage.SelfAssess, "Selbsteinschätzung"),
        new((int)TestStage.MultipleChoice, "Multiple-Choice"),
        new((int)TestStage.LetterBoxes, "Buchstabenkästchen"),
        new((int)TestStage.FreeText, "Freitext (tippen)"),
        new((int)TestStage.Audio, "Hören → tippen"),
    ];

    /// <inheritdoc/>
    public override bool SupportsItemProgress => true;
    /// <inheritdoc/>
    public override bool SupportsLearnGoals => true;
    /// <inheritdoc/>
    public override bool SupportsObjectives => true;
    /// <inheritdoc/>
    public override StoreResolution StoreResolution => StoreResolution.ItemTable;
}
