namespace Pugling.Contracts.Student;

// Vertrag der eingefrorenen Bildwahl. Sie ist bewusst *nicht* Teil der Karte: die Karte trägt nur das
// fertige Bild, das Umwählen ist eine eigene, ausdrückliche Handlung des Kindes.

/// <summary>
/// „Anderes Bild": lehnt das aktuell gewählte Bild eines Trägers ab und zieht ein neues. Genau eines der
/// beiden Felder ist zu setzen – <paramref name="ExerciseItemId"/>, wenn die Übung eine eigene Zuordnung
/// trägt, sonst <paramref name="VocabularyId"/> für das Wort selbst (dann wirkt die neue Wahl überall).
/// </summary>
public record ReshuffleMediaDto(int? VocabularyId = null, int? ExerciseItemId = null);

/// <summary>Das nach dem Umwählen gültige Bild.</summary>
/// <param name="MediaAssetId">Das gewählte Asset.</param>
/// <param name="ImageUrl">URL in der Kartengröße.</param>
/// <param name="ImageAlt">Beschreibung des Motivs (Alt-Text).</param>
public record SelectedMediaResponse(int MediaAssetId, string ImageUrl, string ImageAlt);
