using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record VocabularyBaseEntity : IVocabularyBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Word { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Translation { get; set; } = string.Empty;

        // Additional properties can be added here if needed
    }
}