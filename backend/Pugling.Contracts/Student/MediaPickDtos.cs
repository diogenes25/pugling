namespace Pugling.Contracts.Student;

// Vertrag der eingefrorenen Bildwahl. Sie ist bewusst *nicht* Teil der Karte: die Karte trägt nur das
// fertige Bild, das Umwählen ist eine eigene, ausdrückliche Handlung des Kindes.

/// <summary>
/// "Different picture": rejects the currently chosen image of a carrier and draws a new one. Exactly one of
/// the two fields must be set – <paramref name="ExerciseItemId"/> if the exercise carries its own
/// mapping, otherwise <paramref name="VocabularyId"/> for the word itself (then the new pick applies everywhere).
/// </summary>
public record ReshuffleMediaDto(int? VocabularyId = null, int? ExerciseItemId = null);

/// <summary>The image valid after reshuffling.</summary>
/// <param name="MediaAssetId">The chosen asset.</param>
/// <param name="ImageUrl">URL at card size.</param>
/// <param name="ImageAlt">Description of the motif (alt text).</param>
public record SelectedMediaResponse(int MediaAssetId, string ImageUrl, string ImageAlt);
