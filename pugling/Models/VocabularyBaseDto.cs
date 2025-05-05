using System.ComponentModel.DataAnnotations;

namespace pugling.Models;

/// <summary>
/// Represents a related vocabulary item.
/// </summary>
public record VocabularyBaseDto : IVocabularyBase
{
    /// <summary>
    /// The unique identifier of the vocabulary item.
    /// For infinitives: {sourceLanguage}_{word}_{targetLanguage} (e.g., en_go_de)
    /// For conjugated forms: {sourceLanguage}_{baseWord}_{targetLanguage}_{tense}_{person} (e.g., en_go_de_Präsens_ich)
    /// For phrases: {sourceLanguage}_{normalized_phrase}_{targetLanguage}_{normalized_translation} (e.g., de_wie_geht_es_dir_en_how_are_you)
    /// </summary>
    [StringLength(50)]
    public string Id { get; init; }

    /// <summary>
    /// The word or phrase in the source language.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Word { get; init; }

    /// <summary>
    /// The translation of the word or phrase into the target language. For conjugated verbs,
    /// this is the translation of the specific form (e.g., "gehe" for "ich gehe").
    /// </summary>
    [StringLength(200)]
    public required string Translation { get; init; }

    /// <summary>
    /// The language code of the source language (e.g., "en" for English).
    /// </summary>
    /// <example>"en"</example>
    [Required]
    [StringLength(3)]
    public required string SourceLanguage { get; init; }

    /// <summary>
    /// The language code of the target language (e.g., "de" for German).
    /// </summary>
    /// <example>"de"</example>
    [Required]
    [StringLength(3)]
    public required string TargetLanguage { get; init; }
}