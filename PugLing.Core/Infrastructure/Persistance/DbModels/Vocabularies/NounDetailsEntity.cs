using PugLing.Core.Application.Vocabularies;
using PugLing.Model.Models;
using PugLing.Model.Models.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PugLing.Core.Infrastructure.Persistance.DbModels.Vocabularies;

/// <summary>
/// Represents the details of a noun, including its articles and grammatical genus.
/// </summary>
public record NounDetailsEntity : INounDetails, IFillAndValidateable<NounDetailsEntity, NounDetails>
{
    /// <summary>
    /// Gets or sets the unique identifier for the noun details entity.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the determined article of the noun.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeterminedArticle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the grammatical genus of the noun.
    /// </summary>
    [Required]
    public EGenus Genus { get; set; } = EGenus.NotSet;

    /// <summary>
    /// Gets or sets the undetermined article of the noun.
    /// </summary>
    [MaxLength(100)]
    public string UndeterminedArticle { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="NounDetailsEntity"/> class.
    /// </summary>
    public NounDetailsEntity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NounDetailsEntity"/> class with specified values.
    /// </summary>
    /// <param name="determinedArticle">The determined article of the noun.</param>
    /// <param name="genus">The grammatical genus of the noun.</param>
    /// <param name="undeterminedArticle">The undetermined article of the noun.</param>
    public NounDetailsEntity(string determinedArticle, EGenus genus, string undeterminedArticle)
    {
        this.DeterminedArticle = determinedArticle;
        this.Genus = genus;
        this.UndeterminedArticle = undeterminedArticle;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NounDetailsEntity"/> class from an existing <see cref="INounDetails"/> instance.
    /// </summary>
    /// <param name="nounDetails">The source <see cref="INounDetails"/> instance.</param>
    public NounDetailsEntity(INounDetails nounDetails)
    {
        this.DeterminedArticle = nounDetails.DeterminedArticle;
        this.Genus = nounDetails.Genus;
        this.UndeterminedArticle = nounDetails.UndeterminedArticle;
    }

    /// <summary>
    /// Fills the current entity with values from the provided <see cref="NounDetails"/> instance and validates the entity.
    /// </summary>
    /// <param name="noun">The source <see cref="NounDetails"/> instance.</param>
    /// <returns>The current <see cref="NounDetailsEntity"/> instance if valid; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    public NounDetailsEntity? FillAndValidate(NounDetails? noun)
    {
        if (noun == null)
        {
            return null;
        }

        this.DeterminedArticle = noun.DeterminedArticle;
        this.Genus = noun.Genus;
        this.UndeterminedArticle = noun.UndeterminedArticle ?? string.Empty;

        var validateResult = Validate(new ValidationContext(this));
        if (validateResult.Any())
            throw new ArgumentException("The following constraints were violated: " + string.Join(';', validateResult.Select(r => r.ErrorMessage)));

        return this;
    }

    /// <summary>
    /// Validates the current entity based on its properties.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of <see cref="ValidationResult"/> instances representing validation errors, if any.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(this.DeterminedArticle) || this.DeterminedArticle.Length > 100)
        {
            yield return new ValidationResult(
                $"{nameof(this.DeterminedArticle)} must be a non-empty string with a maximum length of 100.",
                [nameof(this.DeterminedArticle)]);
        }

        if (UndeterminedArticle != null && this.UndeterminedArticle.Length > 100)
        {
            yield return new ValidationResult(
                $"{nameof(this.UndeterminedArticle)} must be a string with a maximum length of 100.",
                [nameof(this.UndeterminedArticle)]);
        }
    }
}