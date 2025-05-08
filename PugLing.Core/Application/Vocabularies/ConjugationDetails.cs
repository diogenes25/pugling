using PugLing.Model.Models;

namespace PugLing.Core.Application.Vocabularies;

/// <summary>
/// Represents the details of a conjugated verb, including its form and an optional reference to a vocabulary item.
/// </summary>
public sealed class ConjugationDetails : IConjugationDetails, IEquatable<IConjugationDetails?>
{
    /// <summary>
    /// Gets the conjugated form of the verb.
    /// </summary>
    public string Form { get; private set; } = string.Empty;

    /// <summary>
    /// Gets an optional reference (RESTful URL) to the Vocabulary item of this specific conjugated form.
    /// </summary>
    public string? VocObjRef { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConjugationDetails"/> class.
    /// </summary>
    public ConjugationDetails()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ConjugationDetails"/> with the specified form and vocabulary object reference.
    /// </summary>
    /// <param name="form">The conjugated form of the verb.</param>
    /// <param name="vocObjRef">An optional reference to the vocabulary item.</param>
    /// <returns>A new instance of <see cref="ConjugationDetails"/>.</returns>
    public static ConjugationDetails Create(string form, string? vocObjRef)
    {
        return new ConjugationDetails
        {
            Form = form,
            VocObjRef = vocObjRef
        };
    }

    /// <summary>
    /// Creates a new instance of <see cref="ConjugationDetails"/> by copying the properties from an existing <see cref="IConjugationDetails"/> instance.
    /// </summary>
    /// <param name="conjugationDetails">The source <see cref="IConjugationDetails"/> instance.</param>
    /// <returns>A new instance of <see cref="ConjugationDetails"/>.</returns>
    public static ConjugationDetails Create(IConjugationDetails conjugationDetails)
    {
        return new ConjugationDetails
        {
            Form = conjugationDetails.Form,
            VocObjRef = conjugationDetails.VocObjRef
        };
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as IConjugationDetails);
    }

    /// <summary>
    /// Determines whether the specified <see cref="IConjugationDetails"/> instance is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="IConjugationDetails"/> instance to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified instance is equal to the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(IConjugationDetails? other)
    {
        return other is not null &&
               this.Form == other.Form &&
               this.VocObjRef == other.VocObjRef;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Form, this.VocObjRef);
    }

    /// <summary>
    /// Determines whether two <see cref="ConjugationDetails"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ConjugationDetails"/> instance.</param>
    /// <param name="right">The second <see cref="IConjugationDetails"/> instance.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ConjugationDetails? left, IConjugationDetails? right)
    {
        return EqualityComparer<IConjugationDetails>.Default.Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="ConjugationDetails"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ConjugationDetails"/> instance.</param>
    /// <param name="right">The second <see cref="IConjugationDetails"/> instance.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ConjugationDetails? left, IConjugationDetails? right)
    {
        return !(left == right);
    }
}