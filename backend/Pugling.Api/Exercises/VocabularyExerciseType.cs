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

    /// <summary>
    /// <b>Always empty</b> - and that is the point: the contents of this type live in the
    /// <see cref="ExerciseItem"/> table (<see cref="StoreResolution.ItemTable"/>), not in the config.
    /// <c>VocabularyConfig.Items</c>/<c>.Refs</c> are a pure <b>input shape</b>; after creation
    /// <c>VocabularyController.AfterSaveAsync</c> clears them.
    /// <para>
    /// The projection from the config used to sit here. It was the second content path of the same type and
    /// thus a second truth - reachable only through a data state that has not existed since the items were
    /// materialized. Whoever needs vocabulary contents goes through <c>ExerciseContentResolver.ItemsOfAsync</c>;
    /// the path through the config deliberately returns nothing instead of inventing something plausible
    /// without an ItemId (which cost the cross-plan learning state).
    /// </para>
    /// </summary>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) => [];

    // For trying out, the typed free-text stage (the hardest, most telling test).
    /// <inheritdoc/>
    public override int PreviewStage => (int)TestStage.FreeText;

    /// <inheritdoc/>
    public override bool IsTypedStage(int stage) => StageMechanics.IsTyped((TestStage)stage);

    /// <summary>
    /// <c>ShowBoth</c> ("getting acquainted"): both sides visible at once, no self-assessment, no grading, no
    /// Leitner movement (B-96) – the labeled "Kennenlernen" step of the process, distinct from
    /// <see cref="TestStage.SelfAssess"/> which the child judges itself against.
    /// </summary>
    public override bool IsDisplayOnlyStage(int stage) => (TestStage)stage == TestStage.ShowBoth;

    /// <summary>
    /// Multiple-choice options on the stage named after them: correct answer plus up to three distractors from
    /// the remaining items. The pool itself is <see cref="StageMechanics.DistractorPool"/> – shared with the
    /// matching distractor stage, which asks the same question of the same data.
    /// </summary>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage) =>
        (TestStage)stage == TestStage.MultipleChoice ? StageMechanics.DistractorPool(items, item, maxDistractors: 3) : null;

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

    /// <summary>
    /// At the listening stage the recording <b>is</b> the question: showing the word next to it would make
    /// "listen, then type" a reading task. The card therefore arrives without a prompt.
    /// </summary>
    public override bool AudioReplacesPrompt(int stage) => (TestStage)stage == TestStage.Audio;

    /// <summary>
    /// <b>Every</b> stage of the type, not just the interesting ones: the list is also the set the write path
    /// validates a position's stage against (see <c>PlanPositionsController.StageProblem</c>), so a stage
    /// missing here would be unsettable even though the model, the seed and the process documentation use it.
    /// <c>ShowBoth</c> was exactly that case - the acquaint stage of the vocabulary process, reachable through
    /// the API but named by no picker.
    /// </summary>
    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)TestStage.ShowBoth, "Beide zeigen (Kennenlernen)"),
        new((int)TestStage.SelfAssess, "Selbsteinschätzung"),
        new((int)TestStage.MultipleChoice, "Multiple-Choice"),
        new((int)TestStage.LetterBoxes, "Buchstabenkästchen"),
        new((int)TestStage.FreeText, "Freitext (tippen)"),
        new((int)TestStage.Audio, "Hören → tippen"),
    ];

    /// <inheritdoc/>
    public override bool SupportsItemProgress => true;
    /// <inheritdoc/>
    public override bool SupportsObjectives => true;
    /// <inheritdoc/>
    public override StoreResolution StoreResolution => StoreResolution.ItemTable;
}
