using pugling.Models;

namespace pugling.Work
{
    public class VerbDetails : IVerbDetails
    {
        /// <summary>
        /// Indicates whether this Vocabulary item represents the base form (infinitive) of the verb.
        /// </summary>
        public bool IsBaseForm { get; private set; }
        /// <summary>
        /// An optional reference (RESTful URL) to the Vocabulary item of the base form (infinitive)
        /// of the verb, if this Vocabulary item represents a conjugated form.
        /// </summary>
        public string? BaseFormRef { get;  private set; }
        /// <summary>
        /// Optional the person of the conjugated verb form (e.g., "ich", "du"). Only set if IsBaseForm is false.
        /// </summary>
        public string? Person { get;  private set; }
        /// <summary>
        /// Optional the infinitive of the verb. Only set if IsBaseForm is false.
        /// </summary>
        public string? Infinitiv { get;  private set; }
        /// <summary>
        /// Optional the tense of the verb. Only set if IsBaseForm is false.
        /// </summary>
        public string? Tense { get;  private set; }
        public Dictionary<string, Dictionary<string, IConjugationDetails>>? Conjugations { get; private set; }
    }
    
    
}