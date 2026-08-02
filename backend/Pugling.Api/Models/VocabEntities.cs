namespace Pugling.Api.Models;

// Language learning: the atomic vocabulary store as the single source of truth.
// Every form (conjugated/inflected ones too) is its own entry; conjugated forms point at their base form
// entry through BaseFormId.
// Sentences and exercises later reference entries through their key (or FK).

// PartOfSpeech/Genus/NounInfo/VerbInfo live in the contract project (Pugling.Contracts).

/// <summary>Atomic vocabulary entry (the lexical backbone).</summary>
public class Vocabulary
{
    public int Id { get; set; }
    /// <summary>Stable, unique reference key (e.g. "en_run_verb_laufen").</summary>
    public string Key { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    /// <summary>Word in the source language.</summary>
    public string Word { get; set; } = "";
    /// <summary>Translation into the target language.</summary>
    public string Translation { get; set; } = "";

    /// <summary>
    /// Further translations that count as <b>equally correct</b> for <see cref="Translation"/> (JSON column).
    /// Only the target side: they answer the question "word → ?", never the reverse (the direction swap drops
    /// them, see <c>ExerciseContentProvider.WithDirection</c>).
    /// <para>
    /// Equivalence is <b>declared here, never derived</b> from two entries sharing the same <see cref="Word"/>:
    /// those are homonyms (<c>bank → Bank</c> / <c>bank → Ufer</c>), and accepting them for one another would
    /// turn a visible defect ("right answer marked wrong") into an invisible one.
    /// </para>
    /// </summary>
    public List<string>? TranslationAlternatives { get; set; }

    public PartOfSpeech PartOfSpeech { get; set; }

    /// <summary>Only set for nouns (JSON column).</summary>
    public NounInfo? Noun { get; set; }
    /// <summary>Only set for verbs (JSON column).</summary>
    public VerbInfo? Verb { get; set; }

    /// <summary>Reference to the base form entry (for inflected forms).</summary>
    public int? BaseFormId { get; set; }
    public Vocabulary? BaseForm { get; set; }

    /// <summary>
    /// Explains the relation to the base form (e.g. "past tense", "past participle", "plural").
    /// Only meaningful together with <see cref="BaseFormId"/>; it describes the edge inflected form → base form.
    /// </summary>
    public string? BaseFormRelation { get; set; }

    /// <summary>URL of the pronunciation audio file (no base64 in the payload).</summary>
    public string? PronunciationAudioUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Free, child-neutral keywords (chapter/grade/topic) for searching and grouping.</summary>
    public List<VocabTagLink> TagLinks { get; set; } = new();
}

/// <summary>
/// Child-neutral keyword for the shared vocabulary catalog (e.g. "Kapitel 5", "Klasse 7",
/// "unregelmäßige Verben"). Deliberately separate from the child-scoped <see cref="Tag"/> (class test
/// relevance), because the vocabulary store – like its tags – is child-neutral.
/// </summary>
public class VocabTag
{
    public int Id { get; set; }
    /// <summary>Globally unique name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Optional display color (hex, e.g. "#3b82f6") for the UI.</summary>
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<VocabTagLink> Links { get; set; } = new();
}

/// <summary>Links a <see cref="Vocabulary"/> entry to a <see cref="VocabTag"/> (n:m).</summary>
public class VocabTagLink
{
    public int Id { get; set; }
    public int VocabTagId { get; set; }
    public VocabTag? VocabTag { get; set; }
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }
}
