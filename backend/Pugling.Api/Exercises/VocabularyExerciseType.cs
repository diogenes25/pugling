using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Vocabulary training: word ↔ translation across several stages (self-assessment, multiple choice, letter boxes,
/// free text, listening). Carries the lion's share of the type-specific rules – store-backed items, distractors, stages,
/// and cross-plan item learning progress. Canonical projection front → back; the query direction turns it into the
/// cards (<see cref="ExerciseContentProvider.WithDirection"/>).
/// </summary>
public sealed class VocabularyExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Vocabulary;

    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        // "Vokabelkarten", not "Vokabeln" (B-163): the type axis names the method or form, the category axis
        // ("Art") names the subject matter. A type named after subject matter must collide with a category -
        // and the seeded categories are exactly "Vokabeln"/"Grammatik". The label follows the renderer below.
        ExerciseTypeKeys.Vocabulary, "Vokabelkarten", "flashcards", 1, "vocabulary",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Vocabulary,
        ["letterHints", "audio", "selfAssess", "multipleChoice"], ItemNoun: "Karten", ItemNounOne: "Karte");

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

    /// <summary>
    /// The atoms of this type are the cards built from the item rows, for the same reason <see cref="ItemsOf"/> is
    /// empty (B-424): one per row, two with the direction "both" (B-472). An unreadable config counts its rows.
    /// </summary>
    public override int? ItemCount(string configJson, int itemTableRows)
    {
        try
        {
            var direction = string.IsNullOrWhiteSpace(configJson)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<VocabularyConfig>(configJson, ConfigJson)?.Direction;
            return ExerciseContentProvider.CardCount(itemTableRows, direction);
        }
        catch (System.Text.Json.JsonException)
        {
            return itemTableRows;
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions ConfigJson = new(System.Text.Json.JsonSerializerDefaults.Web);

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
    /// The vocabulary ladder (B-476): box 1 turns the card over and asks "did you know it?", box 2 offers a choice,
    /// from box 3 on the card is asked at the target. Recognition before recall - typing a word the child has only
    /// seen once is the hardest form there is. The target stays the ceiling: a position set to self-assessment never
    /// climbs into multiple choice. No letter-box rung on purpose: every rung costs one Leitner interval before the
    /// first typed answer.
    /// </summary>
    public override int CardStage(int targetStage, int box)
    {
        var rung = box <= 1 ? TestStage.SelfAssess : box == 2 ? TestStage.MultipleChoice : (TestStage)targetStage;
        return Difficulty(rung) < Difficulty((TestStage)targetStage) ? (int)rung : targetStage;
    }

    /// <summary>
    /// B-597: the forward card of a word asked both ways stops at the choice - its reversed sibling is the one that is
    /// written, and two cards of one word must not ask the same thing.
    /// </summary>
    public override int CardCeiling(ContentItem item, int stage) =>
        item.RecognitionOnly && IsWritingStage((TestStage)stage) ? (int)TestStage.MultipleChoice : stage;

    /// <summary>
    /// B-597: on a writing stage the child writes the learnt word, whatever the exercise's direction - German shown,
    /// the foreign word expected. The direction keeps deciding recognition (getting acquainted, self-assessment, choice).
    /// </summary>
    public override ContentItem CardForStage(ContentItem item, int stage) =>
        IsWritingStage((TestStage)stage) && item.Written is { } written ? written : item;

    /// <summary>
    /// The stages on which the child writes the word (B-597): letter boxes, letter tiles, free text and the dictation.
    /// The choice is no writing stage - it taps, and "walk - what is that?" trains recognition.
    /// </summary>
    public static bool IsWritingStage(TestStage stage) =>
        stage is TestStage.LetterBoxes or TestStage.LetterScramble or TestStage.FreeText or TestStage.Audio;

    // The enum values are not ordered by difficulty (multiple choice came last), so the ladder ranks them itself.
    // Everything the child produces from memory shares the top rank: none of it is a rung below another target.
    private static int Difficulty(TestStage stage) => stage switch
    {
        TestStage.ShowBoth => 0,
        TestStage.SelfAssess => 1,
        TestStage.MultipleChoice => 2,
        _ => 3,
    };

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
    public override (int? LetterBoxLength, string? AudioUrl, string? ImageUrl, string? AnswerPattern) StageFacets(ContentItem item, int stage) =>
        (AsLetterBoxes(item, stage) ? item.Answer.Length : null,
         // B-597: the dictation reads the learnt word, which is the answer of the card asked there.
         (TestStage)stage == TestStage.Audio ? item.DictationAudioUrl : null,
         IsTypedStage(stage) ? null : item.ImageUrl,
         AsLetterBoxes(item, stage) ? StageMechanics.LetterBoxPattern(item.Answer) : null);

    /// <summary>
    /// Letter boxes on their own stage - and on the scramble stage for an answer that cannot be scrambled (B-05 AC 3):
    /// that card falls back on its own, the rest of the round stays tiles.
    /// </summary>
    private static bool AsLetterBoxes(ContentItem item, int stage) =>
        (TestStage)stage == TestStage.LetterBoxes
        || ((TestStage)stage == TestStage.LetterScramble && !StageMechanics.IsScramblable(item.Answer));

    /// <summary>The answer's letters on the scramble stage, when the answer is one short word (B-05).</summary>
    public override string? LetterTileSource(ContentItem item, int stage) =>
        (TestStage)stage == TestStage.LetterScramble && StageMechanics.IsScramblable(item.Answer) ? item.Answer : null;

    /// <summary>
    /// At the dictation stage the recording <b>is</b> the question: showing the word next to it would make the dictation
    /// a reading task. The card therefore arrives without a prompt.
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
        new((int)TestStage.LetterScramble, "Buchstaben ordnen"),
        new((int)TestStage.FreeText, "Freitext (tippen)"),
        new((int)TestStage.Audio, "Diktat"),
    ];

    /// <inheritdoc/>
    public override bool SupportsItemProgress => true;
    /// <inheritdoc/>
    public override bool SupportsObjectives => true;
    /// <inheritdoc/>
    public override StoreResolution StoreResolution => StoreResolution.ItemTable;
}
