using pugling.Models.Constants;

namespace pugling.Models
{
    /// <summary>
    /// Represents a vocabulary item with various linguistic details and metadata.
    /// </summary>
    /// <typeparam name="TIdiomaticUsage">The type representing idiomatic usage details.</typeparam>
    /// <typeparam name="TNounDetails">The type representing noun-specific details.</typeparam>
    /// <typeparam name="TVocabularyBase">The base type for related vocabulary forms.</typeparam>
    /// <typeparam name="TVerbDetails">The type representing verb-specific details.</typeparam>
    public interface IVocabulary<out TIdiomaticUsage, out TNounDetails, out TVocabularyBase, out TVerbDetails> : IVocabularyBase
    where TIdiomaticUsage : IIdiomaticUsage
    where TNounDetails : INounDetails
    where TVocabularyBase : IVocabularyBase
    where TVerbDetails : IVerbDetails
    {
        /// <summary>
        /// Gets the description of the vocabulary item.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the example sentence in the source language.
        /// </summary>
        string? ExampleSentenceSrc { get; }

        /// <summary>
        /// Gets the example sentence in the target language.
        /// </summary>
        string? ExampleSentenceTarget { get; }

        /// <summary>
        /// Gets the URL for the example sentence in the target language.
        /// </summary>
        Uri? ExampleSentenceTargetUrl { get; }

        /// <summary>
        /// Gets the tense of the example sentence.
        /// </summary>
        string? ExampleSentenceTense { get; }

        /// <summary>
        /// Gets the array of idiomatic usages associated with the vocabulary item.
        /// </summary>
        TIdiomaticUsage[]? IdiomaticUsages { get; }

        /// <summary>
        /// Gets the noun-specific details of the vocabulary item.
        /// </summary>
        TNounDetails? Noun { get; }

        /// <summary>
        /// Gets the part of speech of the vocabulary item.
        /// </summary>
        EPartOfSpeech PartOfSpeech { get; }

        /// <summary>
        /// Gets the subcategory part of speech of the vocabulary item.
        /// </summary>
        EPartOfSpeechSubcategory? PartOfSpeechSubcategory { get; }

        /// <summary>
        /// Gets the pronunciation of the vocabulary item.
        /// </summary>
        string? Pronunciation { get; }

        /// <summary>
        /// Gets the URL for the pronunciation audio of the vocabulary item.
        /// </summary>
        string? PronunciationAudioUrl { get; }

        /// <summary>
        /// Gets the array of related vocabulary forms.
        /// </summary>
        TVocabularyBase[]? RelatedForms { get; }

        /// <summary>
        /// Gets the source language of the vocabulary item.
        /// </summary>
        string SourceLanguage { get; }

        /// <summary>
        /// Gets the target language of the vocabulary item.
        /// </summary>
        string TargetLanguage { get; }

        /// <summary>
        /// Gets the date and time when the vocabulary item was last updated.
        /// </summary>
        DateTime? UpdatedAt { get; }

        /// <summary>
        /// Gets the verb-specific details of the vocabulary item.
        /// </summary>
        TVerbDetails? Verb { get; }

        /// <summary>
        /// Gets the version of the vocabulary item.
        /// </summary>
        string Version { get; }
    }
}