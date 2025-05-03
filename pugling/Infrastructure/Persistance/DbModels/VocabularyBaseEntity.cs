using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace pugling.Infrastructure.DbServices.DbModels
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

        internal VocabularyBaseEntity? Check()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add($"{nameof(Id)} cannot be null, empty, or whitespace.");

            if (string.IsNullOrWhiteSpace(Word))
                errors.Add($"{nameof(Word)} cannot be null, empty, or whitespace.");
            else if (Word.Length > 100)
                errors.Add($"{nameof(Word)} exceeds the maximum length of 100 characters.");

            if (string.IsNullOrWhiteSpace(Translation))
                errors.Add($"{nameof(Translation)} cannot be null, empty, or whitespace.");
            else if (Translation.Length > 200)
                errors.Add($"{nameof(Translation)} exceeds the maximum length of 200 characters.");

            if (errors.Any())
                throw new InvalidOperationException($"Validation failed: {string.Join("; ", errors)}");

            return this;
        }
    }
}