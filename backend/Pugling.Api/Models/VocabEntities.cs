namespace Pugling.Api.Models;

// Sprachlernen: atomarer Vokabel-Store als "Single Source of Truth".
// Jede Form (auch konjugiert/flektiert) ist ein eigener Eintrag; konjugierte
// Formen verweisen per BaseFormId auf ihre Grundform-Vokabel.
// Sätze und Übungen referenzieren später Vokabeln über ihren Key (bzw. FK).

public enum PartOfSpeech
{
    Noun = 0,
    Verb = 1,
    Adjective = 2,
    Adverb = 3,
    Pronoun = 4,
    Preposition = 5,
    Conjunction = 6,
    Article = 7,
    Numeral = 8,
    Interjection = 9,
    Phrase = 10,
    Other = 11,
}

public enum Genus { Masculine, Feminine, Neuter }

/// <summary>Substantiv-spezifische Angaben.</summary>
public class NounInfo
{
    /// <summary>Bestimmter Artikel in der Zielsprache (z. B. "der", "die", "das").</summary>
    public string? Article { get; set; }
    public Genus? Genus { get; set; }
    public string? Plural { get; set; }
}

/// <summary>Verb-spezifische Angaben / Konjugations-Metadaten.</summary>
public class VerbInfo
{
    /// <summary>true = Grundform (Infinitiv), false = flektierte Form.</summary>
    public bool IsBaseForm { get; set; }
    public string? Infinitive { get; set; }
    /// <summary>Zeitform der flektierten Form (z. B. "present", "past").</summary>
    public string? Tense { get; set; }
    /// <summary>Person (z. B. "1", "2", "3").</summary>
    public string? Person { get; set; }
    /// <summary>Numerus (z. B. "singular", "plural").</summary>
    public string? Number { get; set; }
}

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
