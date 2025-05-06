using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace pugling.Infrastructure.Persistance.DbModels.Vocabularies
{
    public record VocabularyBaseEntity : IVocabularyBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Word { get; set; }

        [Required]
        [MaxLength(200)]
        public string Translation { get; set; }

        /// <summary>
        /// Gets or sets the source language of the vocabulary.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string SourceLanguage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target language of the vocabulary.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TargetLanguage { get; set; } = string.Empty;

        internal VocabularyBaseEntity? Check()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(this.Id))
                errors.Add($"{nameof(this.Id)} cannot be null, empty, or whitespace.");

            if (string.IsNullOrWhiteSpace(this.Word))
                errors.Add($"{nameof(this.Word)} cannot be null, empty, or whitespace.");
            else if (this.Word.Length > 100)
                errors.Add($"{nameof(this.Word)} exceeds the maximum length of 100 characters.");

            if (string.IsNullOrWhiteSpace(this.Translation))
                errors.Add($"{nameof(this.Translation)} cannot be null, empty, or whitespace.");
            else if (this.Translation.Length > 200)
                errors.Add($"{nameof(this.Translation)} exceeds the maximum length of 200 characters.");

            if (string.IsNullOrWhiteSpace(this.SourceLanguage) || this.SourceLanguage.Length > 100)
                errors.Add($"{nameof(this.SourceLanguage)} must be non-empty and at most 100 characters.");

            if (string.IsNullOrWhiteSpace(this.TargetLanguage) || this.TargetLanguage.Length > 100)
                errors.Add($"{nameof(this.TargetLanguage)} must be non-empty and at most 100 characters.");

            if (errors.Any())
                throw new InvalidOperationException($"Validation failed: {string.Join("; ", errors)}");

            return this;
        }
    }
}