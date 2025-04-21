using pugling.Models.Constants;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace pugling.Models
{
    /// <summary>
    /// Represents a vocabulary item with information about the source and target language,
    /// the word itself, its translation, descriptions, example sentences, and optional
    /// language-specific details for nouns and verbs.
    /// </summary>
    /// <example>
    /// {
    ///    "id": "en_go_de",
    ///    "sourceLanguage": "en",
    ///    "word": "go",
    ///    "targetLanguage": "de",
    ///    "translation": "gehen",
    ///    "partOfSpeech": "Verb",
    ///    "verb": {
    ///      "isBaseForm": true,
    ///      "conjugations": {
    ///        "Präsens": {
    ///          "ich": {
    ///            "form": "gehe",
    ///            "vocObjRef": "/api/en/de/vocabularies/en_go_de_Präsens_ich.json"
    ///          }
    ///        }
    ///      }
    ///    },
    ///    "exampleSentenceSrc": "I go to the park.",
    ///    "exampleSentenceTarget": "Ich gehe in den Park.",
    ///    "exampleSentenceTense": "present"
    /// }
    /// </example>
    public record VocabularyDto : VocabularyBaseDto, IVocabulary<IdiomaticUsageDto, NounDetailsDto, VocabularyBaseDto, VerbDetailsDto>
    {       
        /// <summary>
        /// Version of structure. This is used to ensure that the client and server are using the same.
        /// </summary>
        /// <example>"1.0"</example>
        [Required]
        [RegularExpression(@"^\d+\.\d+$", ErrorMessage = "Version must be in the format 'X.Y'.")]
        public string Version { get; init; } = "1.0";

        /// <summary>
        /// The language code of the source language (e.g., "en" for English).
        /// </summary>
        /// <example>"en"</example>
        [Required]
        [StringLength(3)]
        public required string SourceLanguage { get; init; }

        /// <summary>
        /// The language code of the target language (e.g., "de" for German).
        /// </summary>
        /// <example>"de"</example>
        [Required]
        [StringLength(3)]
        public required string TargetLanguage { get; init; }

        /// <summary>
        /// The part of speech of the vocabulary item (e.g., "Noun", "Verb", "Adjective", "Phrase").
        /// </summary>
        /// <example>"Verb"</example>
        public EPartOfSpeech PartOfSpeech { get; init; }

        /// <summary>
        /// Optional details for nouns, such as the article. Null if the vocabulary item is not a noun.
        /// </summary>
        /// <example>{ "genus": "maskulin", "determinedArticle": "der", "undeterminedArticle": "ein" }</example>
        public NounDetailsDto? Noun { get; init; }

        /// <summary>
        /// Optional details for verbs, including information about the base form, conjugations, and person.
        /// Null if the vocabulary item is not a verb.
        /// </summary>
        /// <example>
        /// {
        ///    "isBaseForm": true,
        ///    "conjugations": {
        ///      "Präsens": {
        ///        "ich": {
        ///          "form": "gehe",
        ///          "vocObjRef": "/api/en/de/vocabularies/en_go_de_Präsens_ich.json"
        ///        }
        ///      }
        ///    }
        /// }
        /// </example>
        public VerbDetailsDto? Verb { get; init; }

        /// <summary>
        /// An optional, additional description or context for the meaning of the vocabulary item.
        /// </summary>
        /// <example>"A verb used to describe movement."</example>
        [StringLength(500)]
        public string? Description { get; init; }

        /// <summary>
        /// An optional pronunciation of the word or phrase (e.g., in IPA).
        /// </summary>
        /// <example>"ɡoʊ"</example>
        [StringLength(100)]
        public string? Pronunciation { get; init; }

        /// <summary>
        /// An optional example sentence in the source language.
        /// </summary>
        /// <example>"I go to the park."</example>
        [StringLength(500)]
        public string? ExampleSentenceSrc { get; init; }

        /// <summary>
        /// The optional translation of the example sentence into the target language.
        /// </summary>
        /// <example>"Ich gehe in den Park."</example>
        [StringLength(500)]
        public string? ExampleSentenceTarget { get; init; }

        /// <summary>
        /// The optional tense of the verb in the example sentence (e.g., "present", "past").
        /// </summary>
        /// <example>"present"</example>
        [StringLength(50)]
        public string? ExampleSentenceTense { get; init; }

        /// <summary>
        /// An optional array of related vocabulary items. This can be used for synonyms, antonyms,
        /// or words with a similar meaning in a different context (e.g., "laufen" and "rennen" both translate to "running").
        /// </summary>
        /// <example>
        /// [
        ///    { "id": "en_run_de", "word": "run", "translation": "laufen" },
        ///    { "id": "en_sprint_de", "word": "sprint", "translation": "sprinten" }
        /// ]
        /// </example>
        public VocabularyBaseDto[]? RelatedForms { get; init; }

        /// <summary>
        /// An optional array of idiomatic usages where the word or phrase has a different meaning
        /// than its literal translation.
        /// </summary>
        /// <example>
        /// [
        ///    { "phrase": "go for it", "translation": "Mach es!" },
        ///    { "phrase": "go ahead", "translation": "Fahr fort!" }
        /// ]
        /// </example>
        public IdiomaticUsageDto[]? IdiomaticUsages { get; init; }

        /// <summary>
        /// Url to the audio file for pronunciation of the word or phrase.
        /// </summary>
        /// <example>"https://example.com/audio/go.mp3"</example>
        [Url]
        public string? PronunciationAudioUrl { get; init; }

        /// <summary>
        /// The optional timestamp of when the vocabulary item was created.
        /// </summary>
        /// <example>"2023-01-01T12:00:00Z"</example>
        public DateTime? CreatedAt { get; init; }

        /// <summary>
        /// The optional timestamp of when the vocabulary item was last updated.
        /// </summary>
        /// <example>"2023-01-02T12:00:00Z"</example>
        public DateTime? UpdatedAt { get; init; }

        /// <summary>
        /// The optional URL to the example sentence in the target language.
        /// </summary>
        [Url]
        [UIHint("url")]
        public Uri? ExampleSentenceTargetUrl { get; init; }
    }
}