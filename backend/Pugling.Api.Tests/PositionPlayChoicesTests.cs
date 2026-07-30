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
        VocabType.Choices(items, items[index], (int)stage);

    [Fact]
    public void MultipleChoice_LiefertLoesungPlusDreiDistinkteAblenker()
    {
        var choices = Choices(Vocab, 0, TestStage.MultipleChoice)!;

        Assert.Equal(4, choices.Count);                             // 1 richtige + 3 Ablenker
        Assert.Contains("Haus", choices);                           // die richtige Antwort ist dabei
        Assert.Equal(choices.Count, choices.Distinct().Count());    // keine Dubletten
        Assert.All(choices, c => Assert.Contains(c, Vocab.Select(v => v.Answer))); // nur echte Antworten
    }

    [Fact]
    public void MultipleChoice_RotiertDeterministisch_LoesungNichtImmerVorne()
    {
        // Rotation = Index % Anzahl: Item 0 → Verschiebung 0 (Lösung vorne),
        // Item 1 → Verschiebung 1 (Lösung rutscht weg von Position 0). Kein Zufall → reproduzierbar.
        Assert.Equal("Haus", Choices(Vocab, 0, TestStage.MultipleChoice)![0]);
        var forItem1 = Choices(Vocab, 1, TestStage.MultipleChoice)!;
        Assert.NotEqual("gehen", forItem1[0]);
        Assert.Contains("gehen", forItem1);
    }

    [Fact]
    public void MultipleChoice_DoppelteAntwort_ZaehltNichtDoppelt()
    {
        IReadOnlyList<ContentItem> items = [Item(0, "Haus"), Item(1, "Haus"), Item(2, "gehen")];

        // Für "gehen" ist "Haus" der einzige distinkte Ablenker – die zweite "Haus"-Karte wird dedupliziert.
        var choices = Choices(items, 2, TestStage.MultipleChoice)!;
        Assert.Equal(new[] { "gehen", "Haus" }, choices);
    }

    [Fact]
    public void NichtMultipleChoice_LiefertKeineAuswahl()
    {
        Assert.Null(Choices(Vocab, 0, TestStage.SelfAssess)); // andere Stufe
        // Anderes Verfahren: Rechnen kennt keine MC-Auswahl (Basis-Default null).
        Assert.Null(new ArithmeticExerciseType().Choices(Vocab, Vocab[0], (int)TestStage.MultipleChoice));
    }
}
