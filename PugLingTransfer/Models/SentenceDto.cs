namespace PugLingTransfer.Models;

/// <summary>
/// Represents a whole sentence in the source language along with its translation,
/// tense, and the vocabulary objects of its components with their sentence parts.
/// </summary>
public record SentenceDto : ISentence
{
    /// <summary>
    /// IDentifier for the sentence, which can be used to reference it in a RESTful API.
    /// </summary>
    string Id { get; init; } // Unique identifier for the sentence
    /// <summary>
    /// The complete sentence in the source language.
    /// </summary>
    public string SourceSentence { get; init; }

    /// <summary>
    /// The translation of the entire source sentence into the target language.
    /// </summary>
    public string Translation { get; init; }

    /// <summary>
    /// The tense of the sentence, if applicable (e.g., using the constants from GermanTenses or EnglishTenses).
    /// This might be null if the sentence doesn't primarily express a specific tense (e.g., questions, exclamations).
    /// </summary>
    public string? Tense { get; init; }

    /// <summary>
    /// A collection of SentenceComponent objects, detailing each word or phrase in the
    /// SourceSentence and its corresponding Vocabulary object and sentence part.
    /// </summary>
    public SentenceComponentDto[]? Components { get; init; }

    /// <summary>
    /// Path to the audio file for the source sentence, if available.
    /// </summary>
    public string? SentenceAudio { get; init; } // Optional, falls Audio-URL für den Satz vorhanden ist
}