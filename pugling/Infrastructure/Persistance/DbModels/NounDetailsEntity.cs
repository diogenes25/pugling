using pugling.Application;
using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record NounDetailsEntity : INounDetails, IFillAndValidateable<NounDetailsEntity, NounDetails>
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

        public NounDetailsEntity? FillAndValidate(NounDetails? noun)
        {
            if (noun == null)
            {
                return null;
            }

            DeterminedArticle = noun.DeterminedArticle;
            Genus = noun.Genus;
            UndeterminedArticle = noun.UndeterminedArticle ?? string.Empty;

            var validateResult = this.Validate(new ValidationContext(this));
            if (validateResult.Any())
                throw new ArgumentException("The following constraints were violated: " + string.Join(';', validateResult.Select(r => r.ErrorMessage)));

            return this;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(DeterminedArticle) || DeterminedArticle.Length > 100)
            {
                yield return new ValidationResult(
                    $"{nameof(DeterminedArticle)} must be a non-empty string with a maximum length of 100.",
                    new[] { nameof(DeterminedArticle) });
            }

            if (string.IsNullOrWhiteSpace(Genus) || Genus.Length > 50)
            {
                yield return new ValidationResult(
                    $"{nameof(Genus)} must be a non-empty string with a maximum length of 50.",
                    new[] { nameof(Genus) });
            }

            if (UndeterminedArticle != null && UndeterminedArticle.Length > 100)
            {
                yield return new ValidationResult(
                    $"{nameof(UndeterminedArticle)} must be a string with a maximum length of 100.",
                    new[] { nameof(UndeterminedArticle) });
            }
        }
    }
}