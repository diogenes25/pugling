namespace Pugling.Contracts;

/// <summary>
/// Wortart einer Store-Vokabel. Steuert, welche Zusatzangaben fachlich sinnvoll sind
/// (<see cref="NounInfo"/> beim Substantiv, <see cref="VerbInfo"/> beim Verb) und ist Filterkriterium
/// im Vokabelspeicher. <see cref="Other"/> ist der Default, wenn der Anleger nichts angibt.
/// </summary>
public enum PartOfSpeech
{
    /// <summary>Substantiv – trägt üblicherweise Artikel/Genus/Plural in <see cref="NounInfo"/>.</summary>
    Noun = 0,
    /// <summary>Verb – trägt Infinitiv/Zeitform in <see cref="VerbInfo"/>.</summary>
    Verb = 1,
    /// <summary>Adjektiv.</summary>
    Adjective = 2,
    /// <summary>Adverb.</summary>
    Adverb = 3,
    /// <summary>Pronomen.</summary>
    Pronoun = 4,
    /// <summary>Präposition.</summary>
    Preposition = 5,
    /// <summary>Konjunktion.</summary>
    Conjunction = 6,
    /// <summary>Artikel.</summary>
    Article = 7,
    /// <summary>Zahlwort.</summary>
    Numeral = 8,
    /// <summary>Interjektion.</summary>
    Interjection = 9,
    /// <summary>Mehrwortige Wendung („at the weekend") – bewusst keine einzelne Wortart.</summary>
    Phrase = 10,
    /// <summary>Nicht bestimmt bzw. keine der übrigen Wortarten. Default beim Anlegen.</summary>
    Other = 11,
}

/// <summary>Grammatisches Geschlecht eines Substantivs in der Zielsprache.</summary>
public enum Genus
{
    /// <summary>Maskulin („der").</summary>
    Masculine,
    /// <summary>Feminin („die").</summary>
    Feminine,
    /// <summary>Neutrum („das").</summary>
    Neuter,
}

/// <summary>Substantiv-spezifische Angaben.</summary>
public class NounInfo
{
    /// <summary>Bestimmter Artikel in der Zielsprache (z. B. "der", "die", "das").</summary>
    public string? Article { get; set; }
    /// <summary>Grammatisches Geschlecht in der Zielsprache.</summary>
    public Genus? Genus { get; set; }
    /// <summary>Pluralform in der Zielsprache (z. B. "die Pferde").</summary>
    public string? Plural { get; set; }
}

/// <summary>Verb-spezifische Angaben / Konjugations-Metadaten.</summary>
public class VerbInfo
{
    /// <summary>true = Grundform (Infinitiv), false = flektierte Form.</summary>
    public bool IsBaseForm { get; set; }
    /// <summary>Grundform, zu der diese Form gehört (z. B. "to go" bei "went").</summary>
    public string? Infinitive { get; set; }
    /// <summary>Zeitform der flektierten Form (z. B. "present", "past").</summary>
    public string? Tense { get; set; }
    /// <summary>Person (z. B. "1", "2", "3").</summary>
    public string? Person { get; set; }
    /// <summary>Numerus (z. B. "singular", "plural").</summary>
    public string? Number { get; set; }
}
