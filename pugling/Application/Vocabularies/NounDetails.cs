using pugling.Models;
using pugling.Models.Constants;
using pugling.Models.Converter;

namespace pugling.Application.Vocabularies;

/// <summary>
/// Represents the details of a noun, including its grammatical gender and articles.
/// </summary>
public sealed class NounDetails : INounDetails, IEquatable<INounDetails?>
{
    /// <summary>
    /// Gets the determined article of the noun (e.g., "the").
    /// </summary>
    public string? DeterminedArticle { get; private set; }

    /// <summary>
    /// Gets the grammatical gender of the noun.
    /// </summary>
    public EGenus Genus { get; private set; }

    /// <summary>
    /// Gets the undetermined article of the noun (e.g., "a" or "an").
    /// </summary>
    public string? UndeterminedArticle { get; private set; }

    /// <summary>
    /// The parent vocabulary of the noun.
    /// </summary>
    public Vocabulary ParentVocabulary { get; internal set; }

    /// <summary>
    /// Creates a new instance of <see cref="NounDetails"/> with the specified properties.
    /// </summary>
    /// <param name="determinedArticle">The determined article of the noun.</param>
    /// <param name="genus">The grammatical gender of the noun.</param>
    /// <param name="undeterminedArticle">The undetermined article of the noun.</param>
    /// <returns>A new instance of <see cref="NounDetails"/>.</returns>
    public static NounDetails Create(string? determinedArticle, EGenus genus, string? undeterminedArticle) =>
        new()
        {
            DeterminedArticle = determinedArticle,
            Genus = genus,
            UndeterminedArticle = undeterminedArticle
        };

    /// <summary>
    /// Creates a new instance of <see cref="NounDetails"/> by copying the properties from an existing <see cref="INounDetails"/> instance.
    /// </summary>
    /// <param name="nounDetails">The <see cref="INounDetails"/> instance to copy from.</param>
    /// <returns>A new instance of <see cref="NounDetails"/>, or <c>null</c> if <paramref name="nounDetails"/> is <c>null</c>.</returns>
    public static NounDetails? Create(INounDetails? nounDetails) =>
        nounDetails == null ? null : new NounDetails
        {
            DeterminedArticle = nounDetails.DeterminedArticle,
            Genus = nounDetails.Genus,
            UndeterminedArticle = nounDetails.UndeterminedArticle
        };

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as INounDetails);

    /// <summary>
    /// Determines whether the current instance is equal to another <see cref="INounDetails"/> instance.
    /// </summary>
    /// <param name="other">The other <see cref="INounDetails"/> instance to compare with.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(INounDetails? other) => this.Compare(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(this.DeterminedArticle, this.Genus, this.UndeterminedArticle);

    /// <summary>
    /// Determines whether two <see cref="NounDetails"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="NounDetails"/> instance.</param>
    /// <param name="right">The second <see cref="INounDetails"/> instance.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(NounDetails? left, INounDetails? right) =>
        EqualityComparer<INounDetails>.Default.Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="NounDetails"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="NounDetails"/> instance.</param>
    /// <param name="right">The second <see cref="INounDetails"/> instance.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(NounDetails? left, INounDetails? right) =>
        !(left == right);
}