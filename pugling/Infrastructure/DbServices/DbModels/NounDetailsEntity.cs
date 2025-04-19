using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record NounDetailsEntity : INounDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeterminedArticle { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Genus { get; set; } = string.Empty;

        [MaxLength(100)]
        public string UndeterminedArticle { get; set; } = string.Empty;

        public NounDetailsEntity()
        {
        }

        public NounDetailsEntity(string determinedArticle, string genus, string undeterminedArticle)
        {
            DeterminedArticle = determinedArticle;
            Genus = genus;
            UndeterminedArticle = undeterminedArticle;
        }

        public NounDetailsEntity(INounDetails nounDetails)
        {
            DeterminedArticle = nounDetails.DeterminedArticle;
            Genus = nounDetails.Genus;
            UndeterminedArticle = nounDetails.UndeterminedArticle;
        }
    }
}