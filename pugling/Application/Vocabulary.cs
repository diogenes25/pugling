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
        public string? Description { get; private set; }

        /// <summary>
        /// Gets the example sentence in the source language.
        /// </summary>
        public string? ExampleSentenceSrc { get; private set; }

        /// <summary>
        /// Gets the example sentence in the target language.
        /// </summary>
        public string? ExampleSentenceTarget { get; private set; }

        /// <summary>
        /// Gets the tense of the example sentence.
        /// </summary>
        public string? ExampleSentenceTense { get; private set; }

        /// <summary>
        /// Gets the idiomatic usages associated with the vocabulary item.
        /// </summary>
        public IdiomaticUsage[]? IdiomaticUsages { get; private set; }

        /// <summary>
        /// Gets the noun details of the vocabulary item, if applicable.
        /// </summary>
        public NounDetails? Noun { get; private set; }

        /// <summary>
        /// Gets the part of speech of the vocabulary item.
        /// </summary>
        public EPartOfSpeech PartOfSpeech { get; private set; }

        /// <summary>
        /// Gets the pronunciation of the vocabulary item.
        /// </summary>
        public string? Pronunciation { get; private set; }

        /// <summary>
        /// Gets the URL for the pronunciation audio of the vocabulary item.
        /// </summary>
        public string? PronunciationAudioUrl { get; private set; }

        /// <summary>
        /// Gets the related forms of the vocabulary item.
        /// </summary>
        public VocabularyBase[]? RelatedForms { get; private set; }

        /// <summary>
        /// Gets the source language of the vocabulary item.
        /// </summary>
        public string SourceLanguage { get; private set; }

        /// <summary>
        /// Gets the target language of the vocabulary item.
        /// </summary>
        public string TargetLanguage { get; private set; }

        /// <summary>
        /// Gets the last updated timestamp of the vocabulary item.
        /// </summary>
        public DateTime? UpdatedAt { get; private set; }

        /// <summary>
        /// Gets the verb details of the vocabulary item, if applicable.
        /// </summary>
        public VerbDetails? Verb { get; private set; }

        /// <summary>
        /// Gets the version of the vocabulary item.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the URL for the target of the example sentence.
        /// </summary>
        public Uri? ExampleSentenceTargetUrl { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vocabulary"/> class with the specified details.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary item.</param>
        /// <param name="word">The word or phrase of the vocabulary item.</param>
        /// <param name="translation">The translation of the vocabulary item.</param>
        /// <param name="partOfSpeech">The part of speech of the vocabulary item.</param>
        /// <param name="sourceLanguage">The source language of the vocabulary item.</param>
        /// <param name="targetLanguage">The target language of the vocabulary item.</param>
        public Vocabulary(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage)
        {
            Id = id;
            Word = word;
            Translation = translation;
            PartOfSpeech = partOfSpeech;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Vocabulary"/> class with the specified details.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary item.</param>
        /// <param name="word">The word or phrase of the vocabulary item.</param>
        /// <param name="translation">The translation of the vocabulary item.</param>
        /// <param name="partOfSpeech">The part of speech of the vocabulary item.</param>
        /// <param name="sourceLanguage">The source language of the vocabulary item.</param>
        /// <param name="targetLanguage">The target language of the vocabulary item.</param>
        /// <returns>A new instance of the <see cref="Vocabulary"/> class.</returns>
        public static Vocabulary Create(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage)
        {
            return new Vocabulary(id, word, translation, partOfSpeech, sourceLanguage, targetLanguage);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Vocabulary"/> class from an existing <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TRelatedForm, TVerbDetails}"/>.
        /// </summary>
        /// <param name="vocabulary">The existing vocabulary to copy.</param>
        /// <returns>A new instance of the <see cref="Vocabulary"/> class.</returns>
        public static Vocabulary Create(IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
        {
            return new Vocabulary(
                vocabulary.Id,
                vocabulary.Word,
                vocabulary.Translation,
                vocabulary.PartOfSpeech,
                vocabulary.SourceLanguage,
                vocabulary.TargetLanguage)
            {
                Version = vocabulary.Version,
                Description = vocabulary.Description,
                ExampleSentenceSrc = vocabulary.ExampleSentenceSrc,
                ExampleSentenceTarget = vocabulary.ExampleSentenceTarget,
                ExampleSentenceTense = vocabulary.ExampleSentenceTense,
                IdiomaticUsages = vocabulary.IdiomaticUsages?.Select(i => IdiomaticUsage.Create(i)).ToArray(),
                Noun = NounDetails.Create(vocabulary.Noun),
                Pronunciation = vocabulary.Pronunciation,
                PronunciationAudioUrl = vocabulary.PronunciationAudioUrl,
                RelatedForms = vocabulary.RelatedForms?.Select(r => VocabularyBase.Create(r)).ToArray(),
                UpdatedAt = vocabulary.UpdatedAt,
                Verb = VerbDetails.Create(vocabulary.Verb),
                ExampleSentenceTargetUrl = vocabulary.ExampleSentenceTargetUrl
            };
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TRelatedForm, TVerbDetails}"/> is equal to the current object.
        /// </summary>
        /// <param name="other">The vocabulary to compare with the current object.</param>
        /// <returns><c>true</c> if the specified vocabulary is equal to the current object; otherwise, <c>false</c>.</returns>
        public bool Equals(IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? other)
        {
            return this.Compare(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(this.Id);
            hash.Add(this.Noun);
            hash.Add(this.PartOfSpeech);
            hash.Add(this.SourceLanguage);
            hash.Add(this.TargetLanguage);
            hash.Add(this.Translation);
            hash.Add(this.Verb);
            hash.Add(this.Version);
            hash.Add(this.Word);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Determines whether two <see cref="Vocabulary"/> objects are equal.
        /// </summary>
        /// <param name="left">The first vocabulary to compare.</param>
        /// <param name="right">The second vocabulary to compare.</param>
        /// <returns><c>true</c> if the two vocabularies are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Vocabulary? left, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? right)
        {
            return EqualityComparer<IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>>.Default.Equals(left, right);
        }

        /// <summary>
        /// Determines whether two <see cref="Vocabulary"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first vocabulary to compare.</param>
        /// <param name="right">The second vocabulary to compare.</param>
        /// <returns><c>true</c> if the two vocabularies are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Vocabulary? left, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? right)
        {
            return !(left == right);
        }
    }
}