namespace Pugling.Contracts;

/// <summary>
/// Part of speech of a store vocabulary item. Determines which additional details make sense
/// (<see cref="NounInfo"/> for nouns, <see cref="VerbInfo"/> for verbs) and is a filter criterion
/// in the vocabulary store. <see cref="Other"/> is the default when the author specifies nothing.
/// </summary>
public enum PartOfSpeech
{
    /// <summary>Noun – usually carries article/gender/plural in <see cref="NounInfo"/>.</summary>
    Noun = 0,
    /// <summary>Verb – carries infinitive/tense in <see cref="VerbInfo"/>.</summary>
    Verb = 1,
    /// <summary>Adjective.</summary>
    Adjective = 2,
    /// <summary>Adverb.</summary>
    Adverb = 3,
    /// <summary>Pronoun.</summary>
    Pronoun = 4,
    /// <summary>Preposition.</summary>
    Preposition = 5,
    /// <summary>Conjunction.</summary>
    Conjunction = 6,
    /// <summary>Article.</summary>
    Article = 7,
    /// <summary>Numeral.</summary>
    Numeral = 8,
    /// <summary>Interjection.</summary>
    Interjection = 9,
    /// <summary>Multi-word phrase ("at the weekend") – deliberately not a single part of speech.</summary>
    Phrase = 10,
    /// <summary>Not determined, or none of the other parts of speech. Default when creating.</summary>
    Other = 11,
}

/// <summary>Grammatical gender of a noun in the target language.</summary>
public enum Genus
{
    /// <summary>Masculine ("der").</summary>
    Masculine,
    /// <summary>Feminine ("die").</summary>
    Feminine,
    /// <summary>Neuter ("das").</summary>
    Neuter,
}

/// <summary>Noun-specific details.</summary>
public class NounInfo
{
    /// <summary>Definite article in the target language (e.g. "der", "die", "das").</summary>
    public string? Article { get; set; }
    /// <summary>Grammatical gender in the target language.</summary>
    public Genus? Genus { get; set; }
    /// <summary>Plural form in the target language (e.g. "die Pferde").</summary>
    public string? Plural { get; set; }
}

/// <summary>Verb-specific details / conjugation metadata.</summary>
public class VerbInfo
{
    /// <summary>true = base form (infinitive), false = inflected form.</summary>
    public bool IsBaseForm { get; set; }
    /// <summary>Base form this form belongs to (e.g. "to go" for "went").</summary>
    public string? Infinitive { get; set; }
    /// <summary>Tense of the inflected form (e.g. "present", "past").</summary>
    public string? Tense { get; set; }
    /// <summary>Person (e.g. "1", "2", "3").</summary>
    public string? Person { get; set; }
    /// <summary>Number (e.g. "singular", "plural").</summary>
    public string? Number { get; set; }
}
