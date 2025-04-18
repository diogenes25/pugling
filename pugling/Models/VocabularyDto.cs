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
    public record VocabularyDto : IVocabulary<IdiomaticUsageDto, NounDetailsDto, RelatedFormDto, VerbDetailsDto>
    {
        /// <summary>
        /// The unique identifier of the vocabulary item.
        /// For infinitives: {sourceLanguage}_{word}_{targetLanguage} (e.g., en_go_de)
        /// For conjugated forms: {sourceLanguage}_{baseWord}_{targetLanguage}_{tense}_{person} (e.g., en_go_de_Präsens_ich)
        /// For phrases: {sourceLanguage}_{normalized_phrase}_{targetLanguage}_{normalized_translation} (e.g., de_wie_geht_es_dir_en_how_are_you)
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Version of structure. This is used to ensure that the client and server are using the same.
        /// </summary>
        public string Version { get; init; } = "1.0";

        /// <summary>
        /// The language code of the source language (e.g., "en" for English).
        /// </summary>
        public string SourceLanguage { get; init; }

        /// <summary>
        /// The word or phrase in the source language.
        /// </summary>
        public string Word { get; init; }

        /// <summary>
        /// The language code of the target language (e.g., "de" for German).
        /// </summary>
        public string TargetLanguage { get; init; }

        /// <summary>
        /// The translation of the word or phrase into the target language. For conjugated verbs,
        /// this is the translation of the specific form (e.g., "gehe" for "ich gehe").
        /// </summary>
        public string Translation { get; init; }

        /// <summary>
        /// The part of speech of the vocabulary item (e.g., "Noun", "Verb", "Adjective", "Phrase").
        /// </summary>
        public string PartOfSpeech { get; init; }

        /// <summary>
        /// Optional details for nouns, such as the article. Null if the vocabulary item is not a noun.
        /// </summary>
        /// <example>
        /// {
        ///    "article": "der"
        /// }
        /// </example>
        public NounDetailsDto? Noun { get; init; }

        /// <summary>
        /// Optional details for verbs, including information about the base form, conjugations, and person.
        /// Null if the vocabulary item is not a verb.
        /// </summary>
        /// <example>
        /// // For infinitive:
        /// {
        ///    "isBaseForm": true,
        ///    "conjugations": { ... }
        /// }
        /// // For conjugated form:
        /// {
        ///    "isBaseForm": false,
        ///    "baseFormRef": "/api/...",
        ///    "person": "ich",
        ///    "infinitiv": "gehen",
        ///    "tense": "Präsens"
        /// }
        /// </example>
        public VerbDetailsDto? Verb { get; init; }

        /// <summary>
        /// An optional, additional description or context for the meaning of the vocabulary item.
        /// </summary>
        /// <example>
        /// "A financial institution where one can deposit and withdraw money."
        /// </example>
        public string? Description { get; init; }

        /// <summary>
        /// An optional pronunciation of the word or phrase (e.g., in IPA).
        /// </summary>
        /// <example>
        /// "/bæŋk/"
        /// </example>
        public string? Pronunciation { get; init; }

        /// <summary>
        /// An optional example sentence in the source language.
        /// </summary>
        /// <example>
        /// "I need to go to the bank."
        /// </example>
        public string? ExampleSentenceSrc { get; init; }

        /// <summary>
        /// The optional translation of the example sentence into the target language.
        /// </summary>
        /// <example>
        /// "Ich muss zur Bank gehen."
        /// </example>
        public string? ExampleSentenceTarget { get; init; }

        /// <summary>
        /// The optional tense of the verb in the example sentence (e.g., "present", "past").
        /// </summary>
        /// <example>
        /// "present"
        /// </example>
        public string? ExampleSentenceTense { get; init; }

        /// <summary>
        /// An optional array of related vocabulary items. This can be used for synonyms, antonyms,
        /// or words with a similar meaning in a different context (e.g., "laufen" and "rennen" both translate to "running").
        /// </summary>
        public RelatedFormDto[]? RelatedForms { get; init; }

        /// <summary>
        /// An optional array of idiomatic usages where the word or phrase has a different meaning
        /// than its literal translation.
        /// </summary>
        /// <example>
        /// [
        ///   {
        ///     "phrase": "Wie geht es Dir?",
        ///     "translation": "How are you?"
        ///   }
        /// ]
        /// </example>
        public IdiomaticUsageDto[]? IdiomaticUsages { get; init; }

        /// <summary>
        /// Url to the audio file for pronunciation of the word or phrase.
        /// </summary>
        public string? PronunciationAudioUrl { get; init; }

        /// <summary>
        /// The optional timestamp of when the vocabulary item was created.
        /// </summary>
        public DateTime? CreatedAt { get; init; }

        /// <summary>
        /// The optional timestamp of when the vocabulary item was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; init; }
    }

  
 
  
   

  
}