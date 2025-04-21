using pugling.Application;
using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record VocabularyEntity : VocabularyBaseEntity, IVocabulary<IdiomaticUsageEntity, NounDetailsEntity, VocabularyBaseEntity, VerbDetailsEntity>, IFillAndValidateable<VocabularyEntity, Vocabulary>
    {
        [Required]
        [MaxLength(500)]
        public string PartOfSpeech { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(2000)]
        public string? ExampleSentenceSrc { get; set; }

        [MaxLength(2000)]
        public string? ExampleSentenceTarget { get; set; }

        [MaxLength(100)]
        public string? ExampleSentenceTense { get; set; }

        [MaxLength(500)]
        public string? Pronunciation { get; set; }

        [Url]
        public string? PronunciationAudioUrl { get; set; }

        [Required]
        [MaxLength(100)]
        public string SourceLanguage { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TargetLanguage { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        public IdiomaticUsageEntity[]? IdiomaticUsages { get; set; }

        public VocabularyBaseEntity[]? RelatedForms { get; set; }

        public NounDetailsEntity? Noun { get; set; }

        public VerbDetailsEntity? Verb { get; set; }

        public VocabularyEntity FillAndValidate([NotNull] Vocabulary vocabulary)
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
            this.Verb = new VerbDetailsEntity().FillAndValidate(vocabulary.Verb);

            var validateResult = this.Validate(new ValidationContext(this));
            if (validateResult.Any())
                throw new ArgumentException("The following constraints were violated: " + string.Join(';', validateResult.Select(r => r.ErrorMessage)));

            return this;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(this.Word) || this.Word.Length > 500)
                yield return new ValidationResult($"{nameof(this.Word)} must be non-empty and at most 500 characters.", [nameof(this.Word)]);

            if (string.IsNullOrWhiteSpace(this.Translation) || this.Translation.Length > 500)
                yield return new ValidationResult($"{nameof(this.Translation)} must be non-empty and at most 500 characters.", [nameof(this.Translation)]);

            if (string.IsNullOrWhiteSpace(this.PartOfSpeech) || this.PartOfSpeech.Length > 500)
                yield return new ValidationResult($"{nameof(this.PartOfSpeech)} must be non-empty and at most 500 characters.", [nameof(this.PartOfSpeech)]);

            if (this.Description?.Length > 1000)
                yield return new ValidationResult($"{nameof(this.Description)} must be at most 1000 characters.", new[] { nameof(this.Description) });

            if (this.ExampleSentenceSrc?.Length > 2000)
                yield return new ValidationResult($"{nameof(this.ExampleSentenceSrc)} must be at most 2000 characters.", new[] { nameof(this.ExampleSentenceSrc) });

            if (this.ExampleSentenceTarget?.Length > 2000)
                yield return new ValidationResult($"{nameof(this.ExampleSentenceTarget)} must be at most 2000 characters.", new[] { nameof(this.ExampleSentenceTarget) });

            if (this.ExampleSentenceTense?.Length > 100)
                yield return new ValidationResult($"{nameof(this.ExampleSentenceTense)} must be at most 100 characters.", new[] { nameof(this.ExampleSentenceTense) });

            if (this.Pronunciation?.Length > 500)
                yield return new ValidationResult($"{nameof(this.Pronunciation)} must be at most 500 characters.", new[] { nameof(this.Pronunciation) });

            if (!string.IsNullOrWhiteSpace(this.PronunciationAudioUrl) && !Uri.IsWellFormedUriString(this.PronunciationAudioUrl, UriKind.Absolute))
                yield return new ValidationResult($"{nameof(this.PronunciationAudioUrl)} must be a valid URL.", new[] { nameof(this.PronunciationAudioUrl) });

            if (string.IsNullOrWhiteSpace(this.SourceLanguage) || this.SourceLanguage.Length > 100)
                yield return new ValidationResult($"{nameof(this.SourceLanguage)} must be non-empty and at most 100 characters.", new[] { nameof(this.SourceLanguage) });

            if (string.IsNullOrWhiteSpace(this.TargetLanguage) || this.TargetLanguage.Length > 100)
                yield return new ValidationResult($"{nameof(this.TargetLanguage)} must be non-empty and at most 100 characters.", new[] { nameof(this.TargetLanguage) });

            if (string.IsNullOrWhiteSpace(this.Version) || this.Version.Length > 50)
                yield return new ValidationResult($"{nameof(this.Version)} must be non-empty and at most 50 characters.", new[] { nameof(this.Version) });
        }
    }
}