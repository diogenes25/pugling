namespace Pugling.Api.Models;

/// <summary>
/// Ein einzelnes, stabil identifizierbares Item einer Vokabelübung: eine positionierte Referenz auf
/// einen Eintrag des Vokabel-Stores (<see cref="Vocabulary"/>). Löst die frühere index-basierte Adressierung
/// der Inline-<c>VocabularyConfig.Items</c>/<c>Refs</c> ab – jedes Item trägt nun eine eigene <see cref="Id"/>
/// (die „ItemId"), sodass Umsortieren/Löschen den Lernfortschritt nicht mehr auf ein anderes Atom kippen lässt
/// und der Fortschritt pro Kind stabil je Item (und – über <see cref="VocabularyId"/> – je Wort) auswertbar wird.
/// <para>
/// Front/Rückseite sind bewusst <b>nicht</b> dupliziert: sie sind Eigenschaften der referenzierten Vokabel
/// (zentral pflegbar, live) und werden erst bei der Inhalts-Auflösung aus dem Store gelesen. <see cref="Hint"/>
/// ist ein optionaler übungslokaler Hinweis, der den abgeleiteten Store-Hinweis (z. B. Artikel) übersteuert.
/// </para>
/// </summary>
public class ExerciseItem
{
    public int Id { get; set; }

    /// <summary>Übung, zu der das Item gehört (nur Vokabelübungen); verschwindet mit ihr (Cascade).</summary>
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>
    /// Reihenfolge innerhalb der Übung. Entspricht dem bisherigen <c>ItemIndex</c> des Lehrplan-Motors
    /// (0-basiert, lückenlos, je Übung eindeutig) – so bleibt der bestehende Leitner-/Test-Fortschritt gültig.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>Referenzierter Vokabel-Store-Eintrag (Wort/Übersetzung/Audio kommen von dort).</summary>
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }

    /// <summary>Optionaler übungslokaler Hinweis; <c>null</c> = abgeleiteter Store-Hinweis (z. B. Artikel).</summary>
    public string? Hint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
