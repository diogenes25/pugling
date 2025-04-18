using pugling.Models;

namespace pugling.Work
{
    public class ConjugationDetails : IConjugationDetails
    {
        /// <summary>
        /// The conjugated form of the verb.
        /// </summary>
        public string Form { get;  private set; } = string.Empty;
        /// <summary>
        /// An optional reference (RESTful URL) to the Vocabulary item of this specific conjugated form.
        /// </summary>
        public string? VocObjRef { get;  private set; }
        // Constructor
        public ConjugationDetails()
        {
        }
    }
}