namespace Pugling.Contracts.Student;

// Contract of the frozen image choice. It is deliberately *not* part of the card: the card carries only
// the finished image, re-choosing is a separate, explicit action of the child.

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
