using PugLing.Core.Application.Vocabularies;
using PugLing.Model.Models.Constants;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace PugLing.Core.Infrastructure.Persistance.DbModels.Vocabularies;

/// <summary>
/// Represents a vocabulary entity with various linguistic details and validation logic.
/// </summary>
public record VocabularyEntity : VocabularyBaseEntity, IVocabularyEntity
{
    //public string id;

    /// <summary>
    /// Gets or sets the part of speech for the vocabulary.
    /// </summary>
    public EPartOfSpeech PartOfSpeech { get; set; } = EPartOfSpeech.NotSet;

    /// <summary>
    /// Gets or sets the description of the vocabulary.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the example sentence in the source language.
    /// </summary>
    [MaxLength(2000)]
    public string? ExampleSentenceSrc { get; set; }

    /// <summary>
    /// Gets or sets the example sentence in the target language.
    /// </summary>
    [MaxLength(2000)]
    public string? ExampleSentenceTarget { get; set; }

    /// <summary>
    /// Gets or sets the tense of the example sentence.
    /// </summary>
    [MaxLength(100)]
    public string? ExampleSentenceTense { get; set; }

    /// <summary>
    /// Gets or sets the pronunciation of the vocabulary.
    /// </summary>
    [MaxLength(500)]
    public string? Pronunciation { get; set; }

    /// <summary>
    /// Gets or sets the URL for the pronunciation audio.
    /// </summary>
    [Url]
    public Uri? PronunciationAudioUrl { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp for the vocabulary.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the version of the vocabulary entity.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the idiomatic usages associated with the vocabulary.
    /// </summary>
    public IdiomaticUsageEntity[]? IdiomaticUsages { get; set; }

    /// <summary>
    /// Gets or sets the related forms of the vocabulary.
    /// </summary>
    public VocabularyBaseEntity[]? RelatedForms { get; set; }

    /// <summary>
    /// Gets or sets the noun details of the vocabulary.
    /// </summary>
    public NounDetailsEntity? Noun { get; set; }

    /// <summary>
    /// Gets or sets the verb details of the vocabulary.
    /// </summary>
    public VerbDetailsEntity? Verb { get; set; }

    /// <summary>
    /// Gets or sets the URL for the example sentence in the target language.
    /// </summary>
    public Uri? ExampleSentenceTargetUrl { get; set; }

    /// <summary>
    /// Gets or sets the subcategory part of speech of the vocabulary.
    /// </summary>
    public EPartOfSpeechSubcategory? PartOfSpeechSubcategory { get; set; }

    /// <summary>
    /// Fills the entity with data from the provided vocabulary and validates it.
    /// </summary>
    /// <param name="vocabulary">The vocabulary to fill the entity with.</param>
    /// <returns>The filled and validated <see cref="VocabularyEntity"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided vocabulary is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation constraints are violated.</exception>
    public virtual VocabularyEntity FillAndValidate([NotNull] Vocabulary vocabulary)
    {
        if (vocabulary == null)
        {
            throw new ArgumentNullException(nameof(vocabulary), "The provided vocabulary cannot be null.");
        }

        this.Id = vocabulary.Id;
        this.Version = vocabulary.Version;
        this.Word = vocabulary.Word;
        this.Translation = vocabulary.Translation;
        this.Description = vocabulary.Description;
        this.ExampleSentenceSrc = vocabulary.ExampleSentenceSrc;
        this.ExampleSentenceTarget = vocabulary.ExampleSentenceTarget;
        this.ExampleSentenceTargetUrl = vocabulary.ExampleSentenceTargetUrl;
        this.ExampleSentenceTense = vocabulary.ExampleSentenceTense;
        this.IdiomaticUsages = vocabulary.IdiomaticUsages?.Select(i => new IdiomaticUsageEntity(i.Phrase, i.Translation)).ToArray();
        this.Noun = new NounDetailsEntity().FillAndValidate(vocabulary.Noun);
        this.PartOfSpeech = vocabulary.PartOfSpeech;
        this.Pronunciation = vocabulary.Pronunciation;
        this.PronunciationAudioUrl = vocabulary.PronunciationAudioUrl;
        this.RelatedForms = vocabulary.RelatedForms?.Select(v => new VocabularyBaseEntity()
        {
            Id = v.Id,
            Translation = v.Translation,
            Word = v.Word,
        }).ToArray();
        this.SourceLanguage = vocabulary.SourceLanguage;
        this.TargetLanguage = vocabulary.TargetLanguage;
        this.UpdatedAt = vocabulary.UpdatedAt;
        this.PartOfSpeechSubcategory = vocabulary.PartOfSpeechSubcategory;
        this.Verb = new VerbDetailsEntity().FillAndValidate(vocabulary.Verb);

        var validateResult = Validate(new ValidationContext(this));
        if (validateResult.Any())
            throw new ArgumentException("The following constraints were violated: " + string.Join(';', validateResult.Select(r => r.ErrorMessage)));

        //id = Id;
        return this;
    }

    /// <summary>
    /// Validates the entity against its constraints.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(this.Word) || this.Word.Length > 500)
            yield return new ValidationResult($"{nameof(this.Word)} must be non-empty and at most 500 characters.", [nameof(this.Word)]);

        if (string.IsNullOrWhiteSpace(this.Translation) || this.Translation.Length > 500)
            yield return new ValidationResult($"{nameof(this.Translation)} must be non-empty and at most 500 characters.", [nameof(this.Translation)]);

        if (this.Description?.Length > 1000)
            yield return new ValidationResult($"{nameof(this.Description)} must be at most 1000 characters.", [nameof(this.Description)]);

        if (this.ExampleSentenceSrc?.Length > 2000)
            yield return new ValidationResult($"{nameof(this.ExampleSentenceSrc)} must be at most 2000 characters.", [nameof(this.ExampleSentenceSrc)]);

        if (this.ExampleSentenceTarget?.Length > 2000)
            yield return new ValidationResult($"{nameof(this.ExampleSentenceTarget)} must be at most 2000 characters.", [nameof(this.ExampleSentenceTarget)]);

        if (this.ExampleSentenceTense?.Length > 100)
            yield return new ValidationResult($"{nameof(this.ExampleSentenceTense)} must be at most 100 characters.", [nameof(this.ExampleSentenceTense)]);

        if (this.Pronunciation?.Length > 500)
            yield return new ValidationResult($"{nameof(this.Pronunciation)} must be at most 500 characters.", [nameof(this.Pronunciation)]);

        //if (this.PronunciationAudioUrl!= null)
        //    yield return new ValidationResult($"{nameof(this.PronunciationAudioUrl)} must be a valid URL.", [nameof(this.PronunciationAudioUrl)]);

        if (string.IsNullOrWhiteSpace(this.Version) || this.Version.Length > 50)
            yield return new ValidationResult($"{nameof(this.Version)} must be non-empty and at most 50 characters.", [nameof(this.Version)]);
    }
}