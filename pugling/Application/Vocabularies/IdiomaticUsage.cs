using pugling.Models;
using pugling.Models.Converter;

namespace pugling.Application.Vocabularies;

public sealed class IdiomaticUsage : IIdiomaticUsage, IEquatable<IIdiomaticUsage?>
{
    /// <summary>
    /// The idiomatic phrase in the source language.
    /// </summary>
    public string Phrase { get; private set; }

    /// <summary>
    /// The translation of the idiomatic phrase in the target language.
    /// </summary>
    public string Translation { get; private set; }

    public IdiomaticUsage(string phrase, string translation)
    {
        Phrase = phrase;
        Translation = translation;
    }

    public static IdiomaticUsage Create(IIdiomaticUsage idiomaticUsage)
    {
        return new IdiomaticUsage(phrase: idiomaticUsage.Phrase, translation: idiomaticUsage.Translation);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as IIdiomaticUsage);
    }

    public bool Equals(IIdiomaticUsage? other)
    {
        return other is not null && this.Compare(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Phrase, this.Translation);
    }

    public static bool operator ==(IdiomaticUsage? left, IIdiomaticUsage? right)
    {
        return EqualityComparer<IIdiomaticUsage>.Default.Equals(left, right);
    }

    public static bool operator !=(IdiomaticUsage? left, IIdiomaticUsage? right)
    {
        return !(left == right);
    }
}