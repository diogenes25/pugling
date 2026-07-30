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
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Vocabulary;

    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Vocabulary, "Vokabeln", "flashcards", 1, "vocabulary",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Vocabulary,
        ["letterHints", "audio", "selfAssess", "multipleChoice"]);

    /// <summary>
    /// <b>Immer leer</b> – und das ist der Punkt: die Inhalte dieses Typs liegen in der
    /// <see cref="ExerciseItem"/>-Tabelle (<see cref="StoreResolution.ItemTable"/>), nicht in der Config.
    /// <c>VocabularyConfig.Items</c>/<c>.Refs</c> sind reine <b>Eingabeform</b>; nach dem Anlegen leert
    /// <c>VocabularyController.AfterSaveAsync</c> sie.
    /// <para>
    /// Hier stand die Projektion aus der Config. Sie war der zweite Inhaltsweg desselben Typs und damit die
    /// zweite Wahrheit – erreichbar nur über einen Datenstand, den es seit dem Materialisieren nicht mehr
    /// gibt. Wer Vokabel-Inhalte braucht, geht über <c>ExerciseContentResolver.ItemsOfAsync</c>; der Weg
    /// über die Config gibt bewusst nichts zurück, statt etwas Plausibles ohne ItemId zu erfinden
    /// (das kostete den plan-übergreifenden Lernstand).
    /// </para>
    /// </summary>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) => [];

    // Fürs Ausprobieren die getippte Freitext-Stufe (schwierigster, aussagekräftigster Test).
    /// <inheritdoc/>
    public override int PreviewStage => (int)TestStage.FreeText;

    /// <inheritdoc/>
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

    /// <summary>
    /// Buchstabenkästchen geben die Länge, die Hör-Stufe die Audioquelle – und das Bild erscheint
    /// <b>nur auf nicht-getippten Stufen</b>.
    /// <para>
    /// Das ist strenger als beim Audio, und zwar aus einem konkreten Grund: die Aussprache liest ein
    /// einzelnes Wort vor (nach dem Richtungstausch entfällt sie deshalb gezielt), ein Motiv dagegen zeigt
    /// die <i>Bedeutung</i>. „Ein Einhorn läuft" verrät bei <c>run → laufen</c> die Lösung genauso wie eine
    /// vorgesprochene Antwort. Deshalb hier die konservative Regel statt einer richtungsabhängigen
    /// Feinunterscheidung: gezeigt wird nur, wo die Lösung ohnehin aufgedeckt ist (Selbsteinschätzung) –
    /// genau die Stufe, auf der das Bild seinen Zweck erfüllt, nämlich das Einprägen.
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
    public override bool SupportsObjectives => true;
    /// <inheritdoc/>
    public override StoreResolution StoreResolution => StoreResolution.ItemTable;
}
