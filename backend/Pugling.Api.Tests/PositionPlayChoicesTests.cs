using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Unit tests of the multiple-choice distractor selection (<see cref="IExerciseType.Choices"/>, here
/// <see cref="VocabularyExerciseType"/>): stateless, without DB/HTTP. Secures the anti-guessing guarantees:
/// exactly one correct answer plus up to three <b>distinct</b> distractors, deterministic rotation (the
/// solution isn't always in front), and no selection for other techniques/stages.
/// </summary>
public class PositionPlayChoicesTests
{
    private static readonly VocabularyExerciseType VocabType = new();

    private static ContentItem Item(int index, string answer) => new(index, $"Frage {index}", answer, []);

    private static readonly IReadOnlyList<ContentItem> Vocab =
    [
        Item(0, "Haus"), Item(1, "gehen"), Item(2, "Katze"), Item(3, "Hund"), Item(4, "Baum"),
    ];

    private static IReadOnlyList<string>? Choices(IReadOnlyList<ContentItem> items, int index, TestStage stage) =>
        VocabType.Choices("", items, items[index], (int)stage);

    [Fact]
    public void MultipleChoice_LiefertLoesungPlusDreiDistinkteAblenker()
    {
        var choices = Choices(Vocab, 0, TestStage.MultipleChoice)!;

        Assert.Equal(4, choices.Count);                             // 1 correct + 3 distractors
        Assert.Contains("Haus", choices);                           // the correct answer is among them
        Assert.Equal(choices.Count, choices.Distinct().Count());    // no duplicates
        Assert.All(choices, c => Assert.Contains(c, Vocab.Select(v => v.Answer))); // real answers only
    }

    [Fact]
    public void MultipleChoice_RotiertDeterministisch_LoesungNichtImmerVorne()
    {
        // Rotation = index % count: item 0 → shift 0 (the solution up front), item 1 → shift 1 (the solution
        // moves away from position 0). No randomness → reproducible.
        Assert.Equal("Haus", Choices(Vocab, 0, TestStage.MultipleChoice)![0]);
        var forItem1 = Choices(Vocab, 1, TestStage.MultipleChoice)!;
        Assert.NotEqual("gehen", forItem1[0]);
        Assert.Contains("gehen", forItem1);
    }

    [Fact]
    public void MultipleChoice_DoppelteAntwort_ZaehltNichtDoppelt()
    {
        IReadOnlyList<ContentItem> items = [Item(0, "Haus"), Item(1, "Haus"), Item(2, "gehen")];

        // For "gehen" the only distinct distractor is "Haus" - the second "Haus" card is deduplicated.
        var choices = Choices(items, 2, TestStage.MultipleChoice)!;
        Assert.Equal(new[] { "gehen", "Haus" }, choices);
    }

    /// <summary>
    /// B-65: an answer declared equally valid must never appear as a <b>wrong</b> option of the same question.
    /// Otherwise multiple choice would contradict the free-text stage – there "sehr groß" counts, here it
    /// would be the trap.
    /// </summary>
    [Fact]
    public void MultipleChoice_GleichwertigeAntwort_IstNieAblenker()
    {
        IReadOnlyList<ContentItem> items =
        [
            new(0, "huge", "riesig", ["riesig", "sehr groß"]),
            Item(1, "sehr groß"),
            Item(2, "klein"),
        ];

        var choices = Choices(items, 0, TestStage.MultipleChoice)!;
        Assert.Contains("riesig", choices);
        Assert.DoesNotContain("sehr groß", choices);
    }

    /// <summary>
    /// The same, but declared from the other side only: the <i>other</i> card knows the equivalence, the
    /// asked one does not. Deduplication therefore looks at the accepted answers of the candidate too –
    /// otherwise "riesig" would be offered as the wrong option for a card whose solution "sehr groß" that very
    /// entry declares as valid.
    /// </summary>
    [Fact]
    public void MultipleChoice_GleichwertigkeitNurAufDerGegenkarte_ZaehltAuch()
    {
        IReadOnlyList<ContentItem> items =
        [
            Item(0, "sehr groß"),
            new(1, "huge", "riesig", ["riesig", "sehr groß"]),
            Item(2, "klein"),
        ];

        var choices = Choices(items, 0, TestStage.MultipleChoice)!;
        Assert.Equal(new[] { "sehr groß", "klein" }, choices);
    }

    [Fact]
    public void NichtMultipleChoice_LiefertKeineAuswahl()
    {
        Assert.Null(Choices(Vocab, 0, TestStage.SelfAssess)); // a different stage
        // A different method: arithmetic knows no multiple choice (the base default is null).
        Assert.Null(new ArithmeticExerciseType().Choices("", Vocab, Vocab[0], (int)TestStage.MultipleChoice));
    }
}
