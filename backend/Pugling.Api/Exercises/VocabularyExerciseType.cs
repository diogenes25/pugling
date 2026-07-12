using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Vokabeltraining: Wort ↔ Übersetzung über mehrere Stufen (Selbsteinschätzung, Multiple-Choice, Buchstaben­kästchen,
/// Freitext, Hören). Trägt den Löwenanteil der typ-spezifischen Regeln – Store-gestützte Items, Ablenker, Stufen und
/// den plan-übergreifenden Item-Lernstand. Kanonische Projektion Vorderseite → Rückseite; die Abfragerichtung dreht
/// das Item (<see cref="ExerciseContentProvider.WithDirection"/>).
/// </summary>
public sealed class VocabularyExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Vocabulary;

    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Vocabulary, "Vokabeln", "flashcards", 1, "vocabulary",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Vocabulary,
        ["letterHints", "audio", "selfAssess", "multipleChoice"]);

    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<VocabularyConfig>(configJson);
        return [.. c.Items.Select((v, i) => ExerciseContentProvider.WithDirection(
            new ContentItem(i, v.Front ?? "", v.Back ?? "", [v.Back ?? ""], v.Hint), c.Direction))];
    }

    // Fürs Ausprobieren die getippte Freitext-Stufe (schwierigster, aussagekräftigster Test).
    public override int PreviewStage => (int)TestStage.FreeText;

    public override bool IsTypedStage(int stage) => StageMechanics.IsTyped((TestStage)stage);

    /// <summary>
    /// Multiple-Choice-Auswahl: richtige Antwort plus bis zu drei Ablenker aus den übrigen Items (dedupliziert,
    /// normalisiert). Deterministische Rotation je Index, damit die Lösung nicht immer vorn steht (kein Zufall).
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

    // Buchstabenkästchen geben die Länge, die Hör-Stufe die Audioquelle.
    public override (int? LetterBoxLength, string? AudioUrl) StageFacets(ContentItem item, int stage) =>
        ((TestStage)stage == TestStage.LetterBoxes ? item.Answer.Length : null,
         (TestStage)stage == TestStage.Audio ? item.AudioUrl : null);

    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)TestStage.SelfAssess, "Selbsteinschätzung"),
        new((int)TestStage.MultipleChoice, "Multiple-Choice"),
        new((int)TestStage.LetterBoxes, "Buchstabenkästchen"),
        new((int)TestStage.FreeText, "Freitext (tippen)"),
        new((int)TestStage.Audio, "Hören → tippen"),
    ];

    public override bool SupportsItemProgress => true;
    public override bool SupportsLearnGoals => true;
    public override bool SupportsObjectives => true;
    public override StoreResolution StoreResolution => StoreResolution.ItemTable;
}
