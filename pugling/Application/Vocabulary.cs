using pugling.Models;
using pugling.Models.Constants;
using pugling.Models.Converter;

namespace pugling.Application
{
    /// <summary>
    /// Represents a vocabulary item with details about its usage, translation, and related forms.
    /// </summary>
    public sealed class Vocabulary : VocabularyBase, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>, IEquatable<IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>?>
    {
        /// <summary>
        /// Gets the description of the vocabulary item.
        /// </summary>
        public string? Description { get; private init; }

        /// <summary>
        /// Gets the example sentence in the source language.
        /// </summary>
        public string? ExampleSentenceSrc { get; private init; }

        /// <summary>
        /// Gets the example sentence in the target language.
        /// </summary>
        public string? ExampleSentenceTarget { get; private init; }

        /// <summary>
        /// Gets the tense of the example sentence.
        /// </summary>
        public string? ExampleSentenceTense { get; private init; }

        /// <summary>
        /// Gets the idiomatic usages associated with the vocabulary item.
        /// </summary>
        public IdiomaticUsage[]? IdiomaticUsages { get; private init; }

        /// <summary>
        /// Gets the noun details of the vocabulary item, if applicable.
        /// </summary>
        public NounDetails? Noun { get; private init; }

        /// <summary>
        /// Gets the part of speech of the vocabulary item.
        /// </summary>
        public EPartOfSpeech PartOfSpeech { get; private init; }

        /// <summary>
        /// Gets the pronunciation of the vocabulary item.
        /// </summary>
        public string? Pronunciation { get; private init; }

        /// <summary>
        /// Gets the URL for the pronunciation audio of the vocabulary item.
        /// </summary>
        public string? PronunciationAudioUrl { get; private init; }

        /// <summary>
        /// Gets the related forms of the vocabulary item.
        /// </summary>
        public VocabularyBase[]? RelatedForms { get; private init; }

        /// <summary>
        /// Gets the source language of the vocabulary item.
        /// </summary>
        public string SourceLanguage { get; private init; }

        /// <summary>
        /// Gets the target language of the vocabulary item.
        /// </summary>
        public string TargetLanguage { get; private init; }

        /// <summary>
        /// Gets the last updated timestamp of the vocabulary item.
        /// </summary>
        public DateTime? UpdatedAt { get; private init; }

        /// <summary>
        /// Gets the verb details of the vocabulary item, if applicable.
        /// </summary>
        public VerbDetails? Verb { get; private init; }

        /// <summary>
        /// Gets the version of the vocabulary item.
        /// </summary>
        public string Version { get; private init; } = "1.0";

        /// <summary>
        /// Gets the URL for the target of the example sentence.
        /// </summary>
        public Uri? ExampleSentenceTargetUrl { get; private init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vocabulary"/> class with the specified details.
        /// </summary>
        public Vocabulary(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage)
        {
            this.Id = id;
            this.Word = word;
            this.Translation = translation;
            this.PartOfSpeech = partOfSpeech;
            this.SourceLanguage = sourceLanguage;
            this.TargetLanguage = targetLanguage;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Vocabulary"/> class with the specified details.
        /// </summary>
        public static Vocabulary Create(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage) =>
            new(id, word, translation, partOfSpeech, sourceLanguage, targetLanguage);

        /// <summary>
        /// Creates a new instance of the <see cref="Vocabulary"/> class from an existing <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TRelatedForm, TVerbDetails}"/>.
        /// </summary>
        public static Vocabulary Create(IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary) =>
            new(vocabulary.Id, vocabulary.Word, vocabulary.Translation, vocabulary.PartOfSpeech, vocabulary.SourceLanguage, vocabulary.TargetLanguage)
            {
                Version = vocabulary.Version,
                Description = vocabulary.Description,
                ExampleSentenceSrc = vocabulary.ExampleSentenceSrc,
                ExampleSentenceTarget = vocabulary.ExampleSentenceTarget,
                ExampleSentenceTense = vocabulary.ExampleSentenceTense,
                IdiomaticUsages = vocabulary.IdiomaticUsages?.Select(IdiomaticUsage.Create).ToArray(),
                Noun = NounDetails.Create(vocabulary.Noun),
                Pronunciation = vocabulary.Pronunciation,
                PronunciationAudioUrl = vocabulary.PronunciationAudioUrl,
                RelatedForms = vocabulary.RelatedForms?.Select(VocabularyBase.Create).ToArray(),
                UpdatedAt = vocabulary.UpdatedAt,
                Verb = VerbDetails.Create(vocabulary.Verb),
                ExampleSentenceTargetUrl = vocabulary.ExampleSentenceTargetUrl
            };

        /// <inheritdoc />
        public override bool Equals(object? obj) => this.Equals(obj as IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>);

        /// <inheritdoc />
        public bool Equals(IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? other) => this.Compare(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Noun, this.PartOfSpeech, this.SourceLanguage, this.TargetLanguage, this.Translation, this.Verb, this.Word);
        }

        public static bool operator ==(Vocabulary? left, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? right) =>
            EqualityComparer<IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>>.Default.Equals(left, right);

        public static bool operator !=(Vocabulary? left, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? right) => !(left == right);
    }
}