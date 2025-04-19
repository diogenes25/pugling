using pugling.Models;

namespace pugling.Application
{
    public sealed class VerbDetails : IVerbDetails, IEquatable<IVerbDetails?>
    {
        /// <summary>
        /// Indicates whether this Vocabulary item represents the base form (infinitive) of the verb.
        /// </summary>
        public bool IsBaseForm { get; private set; }

        /// <summary>
        /// An optional reference (RESTful URL) to the Vocabulary item of the base form (infinitive)
        /// of the verb, if this Vocabulary item represents a conjugated form.
        /// </summary>
        public string? BaseFormRef { get; private set; }

        /// <summary>
        /// Optional the person of the conjugated verb form (e.g., "ich", "du"). Only set if IsBaseForm is false.
        /// </summary>
        public string? Person { get; private set; }

        /// <summary>
        /// Optional the infinitive of the verb. Only set if IsBaseForm is false.
        /// </summary>
        public string? Infinitiv { get; private set; }

        /// <summary>
        /// Optional the tense of the verb. Only set if IsBaseForm is false.
        /// </summary>
        public string? Tense { get; private set; }

        public Dictionary<string, Dictionary<string, IConjugationDetails>>? Conjugations { get; private set; }

        public static VerbDetails Create(
            bool isBaseForm,
            string? baseFormRef,
            string? person,
            string? infinitiv,
            string? tense,
            Dictionary<string, Dictionary<string, IConjugationDetails>>? conjugations)
        {
            return new VerbDetails
            {
                IsBaseForm = isBaseForm,
                BaseFormRef = baseFormRef,
                Person = person,
                Infinitiv = infinitiv,
                Tense = tense,
                Conjugations = conjugations
            };
        }

        public static VerbDetails Create(IVerbDetails verbDetails)
        {
            return new VerbDetails
            {
                IsBaseForm = verbDetails.IsBaseForm,
                BaseFormRef = verbDetails.BaseFormRef,
                Person = verbDetails.Person,
                Infinitiv = verbDetails.Infinitiv,
                Tense = verbDetails.Tense,
                Conjugations = verbDetails.Conjugations
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as IVerbDetails);
        }

        public bool Equals(IVerbDetails? other)
        {
            return other is not null &&
                   this.IsBaseForm == other.IsBaseForm &&
                   this.Person == other.Person &&
                   this.Infinitiv == other.Infinitiv &&
                   this.Tense == other.Tense;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.IsBaseForm, this.Person, this.Infinitiv, this.Tense);
        }

        public static bool operator ==(VerbDetails? left, IVerbDetails? right)
        {
            return EqualityComparer<IVerbDetails>.Default.Equals(left, right);
        }

        public static bool operator !=(VerbDetails? left, IVerbDetails? right)
        {
            return !(left == right);
        }
    }
}