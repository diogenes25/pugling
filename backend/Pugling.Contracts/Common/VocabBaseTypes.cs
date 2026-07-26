namespace Pugling.Contracts;

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
