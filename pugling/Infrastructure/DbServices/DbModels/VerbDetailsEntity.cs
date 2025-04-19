using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record VerbDetailsEntity : IVerbDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string BaseFormRef { get; set; } = string.Empty;

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

        Dictionary<string, Dictionary<string, IConjugationDetails>>? IVerbDetails.Conjugations => throw new NotImplementedException();

        public VerbDetailsEntity()
        {
        }

        public VerbDetailsEntity(string baseFormRef, string infinitiv, bool isBaseForm, string person, string tense, Dictionary<string, Dictionary<string, IConjugationDetails>> conjugations)
        {
            BaseFormRef = baseFormRef;
            Infinitiv = infinitiv;
            IsBaseForm = isBaseForm;
            Person = person;
            Tense = tense;
            //Conjugations = conjugations;
        }

        public VerbDetailsEntity(IVerbDetails verbDetails)
        {
            BaseFormRef = verbDetails.BaseFormRef;
            Infinitiv = verbDetails.Infinitiv;
            IsBaseForm = verbDetails.IsBaseForm;
            Person = verbDetails.Person;
            Tense = verbDetails.Tense;
            //Conjugations = verbDetails.Conjugations;
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
}