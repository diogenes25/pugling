using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Ein einzelnes übbares/prüfbares Element einer Übung, verfahrensneutral aus der Übungs-Config projiziert.
/// <paramref name="Index"/> ist der stabile Positionsbezug (→ <see cref="PositionItemProgress.ItemIndex"/>).
/// <paramref name="AcceptedAnswers"/> enthält die erwartete Lösung plus zulässige Alternativen (roh, der
/// Textvergleich normalisiert später über <see cref="AnswerGrader"/>). <paramref name="GapIndex"/> ist nur
/// bei Lückentexten gesetzt (die {{n}}-Nummer der Lücke). <paramref name="ItemId"/> und
/// <paramref name="VocabularyId"/> tragen (nur bei Vokabelübungen) die stabile Item- bzw. Store-Identität –
/// die Grundlage für den je Kind/Item protokollierten Lernfortschritt; bei allen anderen Typen <c>null</c>.
/// </summary>
public record ContentItem(
    int Index,
    string Prompt,
    string Answer,
    IReadOnlyList<string> AcceptedAnswers,
    string? Hint = null,
    int? GapIndex = null,
    string? AudioUrl = null,
    int? ItemId = null,
    int? VocabularyId = null);

/// <summary>
/// Dünne Fassade über die <see cref="ExerciseTypeRegistry"/>: projiziert die Inhalte einer Katalog-<see cref="Exercise"/>
/// verfahrensneutral in <see cref="ContentItem"/>s, indem sie an den passenden <see cref="IExerciseType"/> delegiert
/// (Store-freie Projektion; die DB-gestützte Auflösung macht der <see cref="ExerciseContentResolver"/>). Ersetzt den
/// früheren typ-<c>switch</c>; die Richtungs-Drehung (<see cref="WithDirection"/>) bleibt geteilter Helper.
/// </summary>
public class ExerciseContentProvider(ExerciseTypeRegistry registry)
{
    /// <summary>Die Inhalte einer Übung als verfahrensneutrale Item-Liste.</summary>
    public IReadOnlyList<ContentItem> ItemsOf(Exercise exercise) => ItemsOf(exercise.Type, exercise.ConfigJson);

    /// <summary>Wie <see cref="ItemsOf(Exercise)"/>, aber direkt aus Typ-Schlüssel + Roh-JSON (unbekannter Typ → leer).</summary>
    public IReadOnlyList<ContentItem> ItemsOf(string typeKey, string configJson) =>
        registry.ByKey(typeKey)?.ItemsOf(configJson) ?? [];

    /// <summary>
    /// Wendet die Abfragerichtung einer Vokabel auf ein kanonisch (Wort → Übersetzung) gebautes Item an:
    /// <c>back-to-front</c> vertauscht Prompt/Antwort, <c>both</c> vertauscht deterministisch bei ungeradem
    /// Index (stabil pro Item, ohne Zufall). Der Index bleibt gleich – der Leitner-/Test-Fortschritt kippt nicht.
    /// </summary>
    public static ContentItem WithDirection(ContentItem item, string? direction) => direction switch
    {
        "back-to-front" => Swap(item),
        "both" => item.Index % 2 == 0 ? item : Swap(item),
        _ => item,
    };

    // Prompt/Antwort tauschen; die Alternativen des Rückwärts-Falls entfallen (galten für die alte Antwort),
    // der Artikel-Hinweis ebenso (er gehörte zum nun abgefragten Wort). Die Aussprache-Audioquelle entfällt
    // ebenfalls: sie liest das Wort vor, das nach dem Tausch die Lösung ist – sonst würde die Hör-Stufe die
    // Antwort vorsprechen (Anti-Schummel). Rückwärts-Items werden in der Hör-Stufe damit textlich gezeigt.
    private static ContentItem Swap(ContentItem it) =>
        it with { Prompt = it.Answer, Answer = it.Prompt, AcceptedAnswers = [it.Prompt], Hint = null, AudioUrl = null };
}
