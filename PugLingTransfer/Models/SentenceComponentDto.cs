namespace PugLingTransfer.Models;

/// <summary>
/// Represents a single component (word or phrase) within a sentence, linking it to its
/// Vocabulary object and its grammatical role, including the case if applicable.
/// </summary>
public record SentenceComponentDto : ISentenceComponent
{
    /// <summary>
    /// The specific word or phrase from the SourceSentence that this component represents.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// A reference to the Vocabulary object that corresponds to this word or phrase.
    /// This could be the ID of the Vocabulary object.
    /// </summary>
    public string? VocabularyId { get; init; }

    /// <summary>
    /// The grammatical role or sentence part of this component (e.g., "Subject", "Predicate", "Object").
    /// Use the constants from the SentenceParts class for consistency.
    /// </summary>
    public string? SentencePart { get; init; }

    /// <summary>
    /// The grammatical case of this component, if applicable (e.g., "Nominativ", "Genitiv", "Dativ", "Akkusativ" in German).
    /// This is typically relevant for nouns, pronouns, and adjectives in case-marking languages.
    /// </summary>
    public string? Case { get; init; }
}