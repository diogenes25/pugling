using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record VocabularyEntity : VocabularyBaseEntity, IVocabulary<IdiomaticUsageEntity, NounDetailsEntity, VocabularyBaseEntity, VerbDetailsEntity>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

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
    }
}