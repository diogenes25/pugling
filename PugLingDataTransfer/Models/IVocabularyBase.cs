namespace PugLingDataTransfer.Models;

/// <summary>
/// Represents a related form with an identifier, translation, and word.
/// </summary>
public interface IVocabularyBase
{
    /// <summary>
    /// Gets the unique identifier of the related form.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the translation of the related form.
    /// </summary>
    string Translation { get; }

    /// <summary>
    /// Gets the word associated with the related form.
    /// </summary>
    string Word { get; }

    /// <summary>
    /// Gets the source language of the vocabulary item.
    /// </summary>
    string SourceLanguage { get; }

    /// <summary>
    /// Gets the target language of the vocabulary item.
    /// </summary>
    string TargetLanguage { get; }
}