using pugling.Models;

namespace pugling.Work
{
    public sealed class ConjugationDetails : IConjugationDetails, IEquatable<IConjugationDetails?>
    {
        /// <summary>
        /// The conjugated form of the verb.
        /// </summary>
        public string Form { get; private set; } = string.Empty;

        /// <summary>
        /// An optional reference (RESTful URL) to the Vocabulary item of this specific conjugated form.
        /// </summary>
        public string? VocObjRef { get; private set; }

        // Constructor
        public ConjugationDetails()
        {
        }

        public static ConjugationDetails Create(string form, string? vocObjRef)
        {
            return new ConjugationDetails
            {
                Form = form,
                VocObjRef = vocObjRef
            };
        }

        public static ConjugationDetails Create(IConjugationDetails conjugationDetails)
        {
            return new ConjugationDetails
            {
                Form = conjugationDetails.Form,
                VocObjRef = conjugationDetails.VocObjRef
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as IConjugationDetails);
        }

        public bool Equals(IConjugationDetails? other)
        {
            return other is not null &&
                   this.Form == other.Form &&
                   this.VocObjRef == other.VocObjRef;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Form, this.VocObjRef);
        }

        public static bool operator ==(ConjugationDetails? left, IConjugationDetails? right)
        {
            return EqualityComparer<IConjugationDetails>.Default.Equals(left, right);
        }

        public static bool operator !=(ConjugationDetails? left, IConjugationDetails? right)
        {
            return !(left == right);
        }
    }
}