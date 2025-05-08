using PugLing.Core.Application.Vocabularies;
using PugLing.Model.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PugLing.Core.Infrastructure.Persistance.DbModels.Vocabularies;

public record VerbDetailsEntity : IVerbDetails, IFillAndValidateable<VerbDetailsEntity, VerbDetails>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public Uri? BaseFormRef { get; set; }

    [Required]
    [MaxLength(100)]
    public string Infinitiv { get; set; } = string.Empty;

    public bool IsBaseForm { get; set; }

    [MaxLength(50)]
    public string Person { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Tense { get; set; } = string.Empty;

    [NotMapped]
    public Dictionary<string, Dictionary<string, ConjugationDetailsEntity>> Conjugations { get; set; } = new();

    Dictionary<string, Dictionary<string, IConjugationDetails>>? IVerbDetails.Conjugations =>
        Conjugations.ToDictionary(
            outer => outer.Key,
            outer => outer.Value.ToDictionary(
                inner => inner.Key,
                inner => (IConjugationDetails)inner.Value
            )
        );

    public VerbDetailsEntity()
    {
    }

    public VerbDetailsEntity(Uri? baseFormRef, string infinitiv, bool isBaseForm, string person, string tense, Dictionary<string, Dictionary<string, IConjugationDetails>> conjugations)
    {
        this.BaseFormRef = baseFormRef;
        this.Infinitiv = infinitiv;
        this.IsBaseForm = isBaseForm;
        this.Person = person;
        this.Tense = tense;
        //Conjugations = conjugations;
    }

    public VerbDetailsEntity(IVerbDetails verbDetails)
    {
        this.BaseFormRef = verbDetails.BaseFormRef;
        this.Infinitiv = verbDetails.Infinitiv;
        this.IsBaseForm = verbDetails.IsBaseForm;
        this.Person = verbDetails.Person;
        this.Tense = verbDetails.Tense;
        //Conjugations = verbDetails.Conjugations;
    }

    public VerbDetailsEntity? FillAndValidate(VerbDetails? verb)
    {
        if (verb == null)
        {
            return null;
        }

        this.BaseFormRef = verb.BaseFormRef;
        this.Infinitiv = verb.Infinitiv;
        this.IsBaseForm = verb.IsBaseForm;
        this.Person = verb.Person ?? string.Empty;
        this.Tense = verb.Tense ?? string.Empty;

        // Assuming Conjugations need to be mapped if provided
        if (verb.Conjugations != null)
        {
            Conjugations = verb.Conjugations.ToDictionary(
                outer => outer.Key,
                outer => outer.Value.ToDictionary(
                    inner => inner.Key,
                    inner => new ConjugationDetailsEntity(inner.Value.Form, inner.Value.VocObjRef ?? string.Empty)
                )
            );
        }

        var validateResult = Validate(new ValidationContext(this));
        if (validateResult.Any())
            throw new ArgumentException("The following constraints were violated: " + string.Join(';', validateResult.Select(r => r.ErrorMessage)));

        return this;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        //if (!string.IsNullOrWhiteSpace(BaseFormRef) && BaseFormRef.Length > 100)
        //{
        //    validationResults.Add(new ValidationResult(
        //        "BaseFormRef is either null, empty, or exceeds the maximum length of 100.",
        //        new[] { nameof(BaseFormRef) }));
        //}

        if (!string.IsNullOrWhiteSpace(this.Infinitiv) && this.Infinitiv.Length > 100)
        {
            validationResults.Add(new ValidationResult(
                "Infinitiv is either null, empty, or exceeds the maximum length of 100.",
                [nameof(this.Infinitiv)]));
        }

        if (!string.IsNullOrWhiteSpace(this.Person) && this.Person.Length > 50)
        {
            validationResults.Add(new ValidationResult(
                "Person exceeds the maximum length of 50.",
                [nameof(this.Person)]));
        }

        if (!string.IsNullOrWhiteSpace(this.Tense) && this.Tense.Length > 50)
        {
            validationResults.Add(new ValidationResult(
                "Tense exceeds the maximum length of 50.",
                [nameof(this.Tense)]));
        }

        return validationResults;
    }
}

public class ConjugationDetailsEntity : IConjugationDetails
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Form { get; set; } = string.Empty;

    [MaxLength(100)]
    public string VocObjRef { get; set; } = string.Empty;

    public ConjugationDetailsEntity()
    {
    }

    public ConjugationDetailsEntity(string form, string vocObjRef)
    {
        Form = form;
        VocObjRef = vocObjRef;
    }
}