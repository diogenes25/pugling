namespace Pugling.Api.Models;

// Sprachlernen: atomarer Vokabel-Store als "Single Source of Truth".
// Jede Form (auch konjugiert/flektiert) ist ein eigener Eintrag; konjugierte
// Formen verweisen per BaseFormId auf ihre Grundform-Vokabel.
// Sätze und Übungen referenzieren später Vokabeln über ihren Key (bzw. FK).

// PartOfSpeech/Genus/NounInfo/VerbInfo leben im Vertrags-Projekt (Pugling.Contracts).

/// <summary>Atomarer Vokabel-Eintrag (lexikalisches Rückgrat).</summary>
public class Vocabulary
{
    public int Id { get; set; }
    /// <summary>Stabiler, eindeutiger Referenz-Key (z. B. "en_run_verb_laufen").</summary>
    public string Key { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    /// <summary>Wort in der Ausgangssprache.</summary>
    public string Word { get; set; } = "";
    /// <summary>Übersetzung in der Zielsprache.</summary>
    public string Translation { get; set; } = "";
    public PartOfSpeech PartOfSpeech { get; set; }

    /// <summary>Nur bei Substantiven gesetzt (JSON-Spalte).</summary>
    public NounInfo? Noun { get; set; }
    /// <summary>Nur bei Verben gesetzt (JSON-Spalte).</summary>
    public VerbInfo? Verb { get; set; }

    /// <summary>Verweis auf die Grundform-Vokabel (bei flektierten Formen).</summary>
    public int? BaseFormId { get; set; }
    public Vocabulary? BaseForm { get; set; }

    /// <summary>
    /// Erklärt die Beziehung zur Grundform (z. B. "Präteritum", "Partizip II", "Plural").
    /// Nur zusammen mit <see cref="BaseFormId"/> sinnvoll; beschreibt die Kante flektierte Form → Grundform.
    /// </summary>
    public string? BaseFormRelation { get; set; }

    /// <summary>URL zur Aussprache-Audiodatei (kein Base64 im Payload).</summary>
    public string? PronunciationAudioUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Freie, kindneutrale Schlagworte (Kapitel/Klasse/Thema) zum Suchen und Gruppieren.</summary>
    public List<VocabTagLink> TagLinks { get; set; } = new();
}

/// <summary>
/// Kindneutrales Schlagwort für den gemeinsamen Vokabel-Katalog (z. B. "Kapitel 5", "Klasse 7",
/// "unregelmäßige Verben"). Bewusst getrennt vom kind-skopierten <see cref="Tag"/> (Klassenarbeits-Relevanz),
/// weil der Vokabel-Store – wie seine Tags – kindneutral ist.
/// </summary>
public class VocabTag
{
    public int Id { get; set; }
    /// <summary>Global eindeutiger Name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Optionale Anzeigefarbe (Hex, z. B. "#3b82f6") für die UI.</summary>
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<VocabTagLink> Links { get; set; } = new();
}

/// <summary>Verknüpft eine <see cref="Vocabulary"/> mit einem <see cref="VocabTag"/> (n:m).</summary>
public class VocabTagLink
{
    public int Id { get; set; }
    public int VocabTagId { get; set; }
    public VocabTag? VocabTag { get; set; }
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }
}
