using PugLingTransfer.Models;
using PugLingTransfer.Models.Converter;

namespace pugling.Application.Vocabularies;

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
    public Uri? BaseFormRef { get; private set; }

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

    /// <summary>
    /// The parent vocabulary of the verb.
    /// </summary>
    public Vocabulary ParentVocabulary { get; internal set; }

    public static VerbDetails Create(
        bool isBaseForm,
        Uri? baseFormRef,
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

    public static VerbDetails? Create(IVerbDetails? verbDetails)
    {
        if (verbDetails == null)
        {
            return null;
        }
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

    public override bool Equals(object? obj) => obj is IVerbDetails other && Equals(other);

    public bool Equals(IVerbDetails? other) => this.Compare(other as VerbDetails);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(this.IsBaseForm);
        hash.Add(this.Person, StringComparer.Ordinal);
        hash.Add(this.Infinitiv, StringComparer.Ordinal);
        hash.Add(this.Tense, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    public static bool operator ==(VerbDetails? left, IVerbDetails? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(VerbDetails? left, IVerbDetails? right) => !(left == right);
}