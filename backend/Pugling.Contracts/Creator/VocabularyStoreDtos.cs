namespace Pugling.Contracts.Creator;

// Vertrag des atomaren Vokabel-Stores („Single Source of Truth" für Wörter). Neben dem klassischen
// CRUD stehen die Agenten-Primitive: Existenzprüfung (Lookup, gegen Dubletten) und Batch-Anlegen/-Nachtragen.

/// <summary>Ein Store-Eintrag mit allen lexikalischen Angaben und den verknüpften Tag-Namen.</summary>
public record VocabularyResponse(int Id, string Key, string Version, string SourceLanguage,
    string TargetLanguage, string Word, string Translation, PartOfSpeech PartOfSpeech,
    NounInfo? Noun, VerbInfo? Verb, int? BaseFormId, string? BaseFormKey, string? BaseFormRelation,
    string? PronunciationAudioUrl, IReadOnlyList<string> Tags, DateTime CreatedAt);

/// <summary>
/// Anlegen einer Vokabel. „Einfach" genügt <c>Word</c> (+ Sprachen): <c>Key</c> darf leer
/// bleiben (der Server generiert einen eindeutigen Slug), <c>Translation</c> darf entfallen
/// (bleibt leer und ist per <c>?untranslated=true</c> auffindbar) und <c>PartOfSpeech</c> darf
/// entfallen (Default <see cref="Contracts.PartOfSpeech.Other"/>). „Komplex" füllt zusätzlich
/// Noun/Verb/BaseForm/Audio; fehlende Details lassen sich später per PATCH nachliefern.
/// <c>Tags</c> (Namen) werden create-if-missing verknüpft.
/// </summary>
public record CreateVocabularyDto(string? Key, string SourceLanguage, string TargetLanguage,
    string Word, string? Translation = null, PartOfSpeech? PartOfSpeech = null, string? Version = null,
    NounInfo? Noun = null, VerbInfo? Verb = null, string? BaseFormKey = null,
    string? BaseFormRelation = null, string? PronunciationAudioUrl = null, List<string>? Tags = null);

/// <summary>Nur gesetzte Felder werden geändert. BaseFormKey = "" hebt die Verknüpfung (und ihr Label) auf; Tags werden ergänzt (nicht ersetzt).</summary>
public record UpdateVocabularyDto(string? Version, string? SourceLanguage, string? TargetLanguage,
    string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
    string? BaseFormKey, string? BaseFormRelation, string? PronunciationAudioUrl, List<string>? Tags);

/// <summary>Eine Übung, die diese Vokabel referenziert (Vokabel-Refs bzw. Lückentext-Lücke).</summary>
public record VocabUsage(int ExerciseId, string Title, string Type, int ChapterId, int SubjectId);

/// <summary>Anfrage der Existenzprüfung: Wörter (für die Text-Extraktion) und/oder Keys (für Ref-Validierung).</summary>
public record LookupRequest(string? SourceLanguage, string? TargetLanguage, List<string>? Words, List<string>? Keys);

/// <summary>Treffer je angefragtem Wort inkl. bereits vorhandener Store-Einträge.</summary>
public record LookupResult(string Word, bool Exists, IReadOnlyList<VocabularyResponse> Matches);

/// <summary>Antwort der Existenzprüfung: pro Wort ein Ergebnis plus die Menge existierender Keys.</summary>
public record LookupResponse(IReadOnlyList<LookupResult> Words, IReadOnlyList<string> ExistingKeys);

/// <summary>Ergebnis eines einzelnen Batch-Elements (Teilerfolg möglich).</summary>
public record BatchItemResult(int Index, string Status, int? Id, string? Key, string? Error);

/// <summary>Ein Batch-Änderungselement: die Ziel-Id plus dieselben partiellen Felder wie beim Einzel-PATCH.</summary>
public record BatchUpdateItem(int Id, string? Version, string? SourceLanguage, string? TargetLanguage,
    string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
    string? BaseFormKey, string? BaseFormRelation, string? PronunciationAudioUrl, List<string>? Tags);
