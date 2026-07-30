namespace Pugling.Contracts.Creator;

// Vertrag des atomaren Vokabel-Stores („Single Source of Truth" für Wörter). Neben dem klassischen
// CRUD stehen die Agenten-Primitive: Existenzprüfung (Lookup, gegen Dubletten) und Batch-Anlegen/-Nachtragen.

/// <summary>A store entry with all lexical details and the linked tag names.</summary>
public record VocabularyResponse(int Id, string Key, string Version, string SourceLanguage,
    string TargetLanguage, string Word, string Translation, PartOfSpeech PartOfSpeech,
    NounInfo? Noun, VerbInfo? Verb, int? BaseFormId, string? BaseFormKey, string? BaseFormRelation,
    string? PronunciationAudioUrl, IReadOnlyList<string> Tags, DateTime CreatedAt);

/// <summary>
/// Creating a vocabulary entry. "Simple" only needs <c>Word</c> (+ languages): <c>Key</c> may stay
/// empty (the server generates a unique slug), <c>Translation</c> may be omitted
/// (stays empty and is findable via <c>?untranslated=true</c>) and <c>PartOfSpeech</c> may be
/// omitted (default <see cref="Contracts.PartOfSpeech.Other"/>). "Complex" additionally fills in
/// noun/verb/base form/audio; missing details can be supplied later via PATCH.
/// <c>Tags</c> (names) are linked create-if-missing.
/// </summary>
public record CreateVocabularyDto(string? Key, string SourceLanguage, string TargetLanguage,
    string Word, string? Translation = null, PartOfSpeech? PartOfSpeech = null, string? Version = null,
    NounInfo? Noun = null, VerbInfo? Verb = null, string? BaseFormKey = null,
    string? BaseFormRelation = null, string? PronunciationAudioUrl = null, List<string>? Tags = null);

/// <summary>Only fields that are set are changed. BaseFormKey = "" removes the link (and its label); tags are appended (not replaced).</summary>
public record UpdateVocabularyDto(string? Version, string? SourceLanguage, string? TargetLanguage,
    string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
    string? BaseFormKey, string? BaseFormRelation, string? PronunciationAudioUrl, List<string>? Tags);

/// <summary>An exercise that references this vocabulary entry (vocabulary refs resp. cloze gap).</summary>
public record VocabUsage(int ExerciseId, string Title, string Type, int ChapterId, int SubjectId);

/// <summary>Request for the existence check: words (for text extraction) and/or keys (for ref validation).</summary>
public record LookupRequest(string? SourceLanguage, string? TargetLanguage, List<string>? Words, List<string>? Keys);

/// <summary>Hit per requested word incl. already existing store entries.</summary>
public record LookupResult(string Word, bool Exists, IReadOnlyList<VocabularyResponse> Matches);

/// <summary>Response of the existence check: one result per word plus the set of existing keys.</summary>
public record LookupResponse(IReadOnlyList<LookupResult> Words, IReadOnlyList<string> ExistingKeys);

/// <summary>Result of a single batch element (partial success possible).</summary>
public record BatchItemResult(int Index, string Status, int? Id, string? Key, string? Error);

/// <summary>A batch change element: the target id plus the same partial fields as the single PATCH.</summary>
public record BatchUpdateItem(int Id, string? Version, string? SourceLanguage, string? TargetLanguage,
    string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
    string? BaseFormKey, string? BaseFormRelation, string? PronunciationAudioUrl, List<string>? Tags);
