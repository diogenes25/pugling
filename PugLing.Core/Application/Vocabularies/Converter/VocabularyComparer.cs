﻿using PugLing.Model.Models;

namespace PugLing.Core.Application.Vocabularies.Converter;

/// <summary>
/// Provides extension methods for comparing various vocabulary-related objects.
/// </summary>
public static class VocabularyComparer
{
    #region compare

    /// <summary>
    /// Compares two <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TVocabularyBase, TVerbDetails}"/> objects for equality.
    /// </summary>
    /// <param name="orig">The original vocabulary object.</param>
    /// <param name="other">The vocabulary object to compare against.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public static bool Compare(this IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> orig, IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>? other)
    {
        if (orig is null && other is null)
        {
            return true;
        }

        if (orig is null || other is null)
        {
            return false;
        }

        return orig.Id == other.Id &&
                orig.SourceLanguage == other.SourceLanguage &&
                orig.TargetLanguage == other.TargetLanguage &&
                orig.Translation == other.Translation &&
                orig.Word == other.Word &&
                (orig.SourceLanguage?.Equals(value: other.SourceLanguage, StringComparison.Ordinal) ?? false) &&
                (orig.TargetLanguage?.Equals(other.TargetLanguage, StringComparison.Ordinal) ?? false) &&
                orig.Noun.Compare(other.Noun) &&
                orig.Verb.Compare(other.Verb);
    }

    /// <summary>
    /// Compares two <see cref="IVocabularyBase"/> objects for equality.
    /// </summary>
    /// <param name="orig">The original vocabulary base object.</param>
    /// <param name="other">The vocabulary base object to compare against.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public static bool Compare(this IVocabularyBase orig, IVocabularyBase other)
    {
        if (orig is null && other is null)
        {
            return true;
        }

        if (orig is null || other is null)
        {
            return false;
        }

        return orig.Id == other.Id &&
                orig.Word == other.Word &&
                orig.Translation == other.Translation &&
                (orig.SourceLanguage?.Equals(value: other.SourceLanguage, StringComparison.Ordinal) ?? false) &&
                (orig.TargetLanguage?.Equals(other.TargetLanguage, StringComparison.Ordinal) ?? false);
    }

    /// <summary>
    /// Compares two <see cref="INounDetails"/> objects for equality.
    /// </summary>
    /// <param name="orig">The original noun details object.</param>
    /// <param name="other">The noun details object to compare against.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public static bool Compare(this INounDetails? orig, INounDetails? other)
    {
        if (orig is null && other is null)
        {
            return true;
        }
        if (orig is null || other is null)
        {
            return false;
        }
        return orig.Genus == other.Genus &&
                orig.DeterminedArticle == other.DeterminedArticle &&
                orig.UndeterminedArticle == other.UndeterminedArticle;
    }

    /// <summary>
    /// Compares two <see cref="IIdiomaticUsage"/> objects for equality.
    /// </summary>
    /// <param name="orig">The original idiomatic usage object.</param>
    /// <param name="other">The idiomatic usage object to compare against.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public static bool Compare(this IIdiomaticUsage orig, IIdiomaticUsage? other)
    {
        if (orig is null && other is null)
        {
            return true;
        }
        return other is not null &&
        orig?.Phrase == other.Phrase &&
        orig.Translation == other.Translation;
    }

    /// <summary>
    /// Compares two <see cref="IVerbDetails"/> objects for equality.
    /// </summary>
    /// <param name="orig">The original verb details object.</param>
    /// <param name="other">The verb details object to compare against.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public static bool Compare(this IVerbDetails? orig, IVerbDetails? other)
    {
        if (orig is null && other is null)
        {
            return true;
        }
        if (orig is null || other is null)
        {
            return false;
        }
        return
              orig.IsBaseForm == other.IsBaseForm &&
              orig.Person == other.Person &&
              orig.Infinitiv == other.Infinitiv &&
              orig.Tense == other.Tense;
    }

    #endregion compare

    #region HashCode

    public static int GetHashCode(this IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + (vocabulary?.Id?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = hash * 23 + (vocabulary?.PartOfSpeech.GetHashCode() ?? 0);
            hash = hash * 23 + (vocabulary?.SourceLanguage?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = hash * 23 + (vocabulary?.TargetLanguage?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = hash * 23 + (vocabulary?.Translation?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = hash * 23 + (vocabulary?.Word?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = hash * 23 + (vocabulary?.Noun?.GetHashCode() ?? 0);
            hash = hash * 23 + (vocabulary?.Verb?.GetHashCode() ?? 0);

            return hash;
        }
    }

    #endregion HashCode
}