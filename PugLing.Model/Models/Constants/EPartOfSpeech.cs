using System.Text.Json.Serialization;

namespace PugLing.Model.Models.Constants;

/// <summary>
/// Represents the various parts of speech in a language.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))] // Enables JSON string conversion for enums
public enum EPartOfSpeech
{
    /// <summary>
    /// Default value indicating that the part of speech is not set.
    /// </summary>
    NotSet = 0,

    /// <summary>
    /// Represents a noun, which is a word used to identify people, places, or things.
    /// </summary>
    Noun = 1,

    /// <summary>
    /// Represents a verb, which is a word used to describe an action, state, or occurrence.
    /// </summary>
    Verb = 2,

    /// <summary>
    /// Represents an adjective, which is a word used to describe or modify a noun.
    /// </summary>
    Adjective = 3,

    /// <summary>
    /// Represents an adverb, which is a word used to modify a verb, adjective, or other adverb.
    /// </summary>
    Adverb = 4,

    /// <summary>
    /// Represents a pronoun, which is a word used in place of a noun.
    /// </summary>
    Pronoun = 5,

    /// <summary>
    /// Represents a preposition, which is a word used to link nouns, pronouns, or phrases to other words.
    /// </summary>
    Preposition = 6,

    /// <summary>
    /// Represents a conjunction, which is a word used to connect clauses or sentences.
    /// </summary>
    Conjunction = 7,

    /// <summary>
    /// Represents an interjection, which is a word or phrase that expresses emotion or exclamation.
    /// </summary>
    Interjection = 8,

    /// <summary>
    /// Represents a determiner, which is a word used to introduce a noun.
    /// </summary>
    Determiner = 9,

    /// <summary>
    /// Represents an article, which is a type of determiner used to indicate specificity of a noun.
    /// </summary>
    Article = 10,

    /// <summary>
    /// Represents an auxiliary verb, which is a verb used to form tenses, moods, or voices of other verbs.
    /// </summary>
    AuxiliaryVerb = 11,

    /// <summary>
    /// Represents a modal verb, which is a type of auxiliary verb used to express ability, possibility, permission, or obligation.
    /// </summary>
    ModalVerb = 12,

    /// <summary>
    /// Represents a particle, which is a function word that does not fit into the standard parts of speech.
    /// </summary>
    Particle = 13,

    /// <summary>
    /// Represents a phrase, which is a group of words that work together as a unit.
    /// </summary>
    Phrase = 14,

    /// <summary>
    /// Represents idiomatic usage, which is a phrase or expression with a meaning not deducible from its individual words.
    /// </summary>
    IdiomaticUsage = 15,

    /// <summary>
    /// Represents a conjugated verb, which is a verb that has been modified to express tense, mood, or aspect.
    /// </summary>
    ConjugatedVerb = 16,

    /// <summary>
    /// Represents an infinitive, which is the base form of a verb.
    /// </summary>
    Infinitive = 17,

    /// <summary>
    /// Represents a numeral, which is a word or phrase that represents a number.
    /// </summary>
    Numeral = 18,

    /// <summary>
    /// No specific part of speech is applicable.
    /// </summary>
    Other = 99,
}

public enum EPartOfSpeechSubcategory
{
    /// <summary>
    /// Represents an adverb of time, which indicates when an action occurs.
    /// </summary>
    Adverb_Time = 19,

    /// <summary>
    /// Represents an adverb of place, which indicates where an action occurs.
    /// </summary>
    Adverb_Place = 20,

    /// <summary>
    /// Represents an adverb of manner, which indicates how an action occurs.
    /// </summary>
    Adverb_Manner = 21,

    /// <summary>
    /// Represents an adverb of reason, which indicates why an action occurs.
    /// </summary>
    Adverb_Reason = 22,

    /// <summary>
    /// Represents an adverb of purpose, which indicates the purpose of an action.
    /// </summary>
    Adverb_Purpose = 23,

    /// <summary>
    /// Represents an adverb of concession, which indicates a contrast or unexpected result.
    /// </summary>
    Adverb_Concession = 24,

    /// <summary>
    /// Represents an adverb of condition, which indicates a condition for an action.
    /// </summary>
    Adverb_Condition = 25,

    /// <summary>
    /// Represents an adverb of contrast, which indicates a comparison or opposition.
    /// </summary>
    Adverb_Contrast = 26,

    /// <summary>
    /// Represents an adverb of comparison, which indicates a comparison between actions.
    /// </summary>
    Adverb_Comparison = 27,

    /// <summary>
    /// Represents an adverb of consequence, which indicates the result of an action.
    /// </summary>
    Adverb_Consequence = 28,

    /// <summary>
    /// Represents an adverb of instrument, which indicates the means by which an action occurs.
    /// </summary>
    Adverb_Instrument = 29,

    /// <summary>
    /// Represents an adverb of company, which indicates the company or group involved in an action.
    /// </summary>
    Adverb_Company = 30,

    /// <summary>
    /// Represents an adverb of quantity, which indicates the amount or degree of an action.
    /// </summary>
    Adverb_Quantity = 31,

    /// <summary>
    /// Represents a possessive pronoun, which indicates ownership.
    /// </summary>
    Pronoun_Possessive = 32,

    /// <summary>
    /// Represents a demonstrative pronoun, which points to specific things.
    /// </summary>
    Pronoun_Demonstrative = 33,

    /// <summary>
    /// Represents a reflexive pronoun, which refers back to the subject of the sentence.
    /// </summary>
    Pronoun_Reflexive = 34,

    /// <summary>
    /// Represents an indefinite pronoun, which refers to non-specific things or people.
    /// </summary>
    Pronoun_Indefinite = 35,

    /// <summary>
    /// Represents an interrogative pronoun, which is used to ask questions.
    /// </summary>
    Pronoun_Interrogative = 36,

    /// <summary>
    /// Represents a reciprocal pronoun, which indicates a mutual action or relationship.
    /// </summary>
    Pronoun_Reciprocal = 37,

    /// <summary>
    /// Represents a quantifier pronoun, which indicates quantity.
    /// </summary>
    Pronoun_Quantifier = 38,

    /// <summary>
    /// Represents an exclamatory pronoun, which is used to express strong emotion.
    /// </summary>
    Pronoun_Exclamatory = 39,

    /// <summary>
    /// Represents a pronoun used as an article.
    /// </summary>
    Pronoun_Article = 40,

    /// <summary>
    /// Represents a pronoun used as an adjective.
    /// </summary>
    Pronoun_Adjective = 41,

    /// <summary>
    /// Represents a pronoun used as an adverb.
    /// </summary>
    Pronoun_Adverb = 42,

    /// <summary>
    /// Represents a pronoun used as a conjunction.
    /// </summary>
    Pronoun_Conjunction = 43,

    /// <summary>
    /// Represents a pronoun used as a preposition.
    /// </summary>
    Pronoun_Preposition = 44,

    /// <summary>
    /// Represents a proper noun, which is the name of a specific person, place, or thing.
    /// </summary>
    Noun_Proper = 45,

    /// <summary>
    /// Represents a collective noun, which refers to a group of individuals.
    /// </summary>
    Noun_Collective = 46,

    /// <summary>
    /// Represents a compound noun, which is made up of two or more words.
    /// </summary>
    Noun_Compound = 47,

    /// <summary>
    /// Represents an abstract noun, which refers to intangible concepts or ideas.
    /// </summary>
    Noun_Abstract = 48,

    /// <summary>
    /// Represents a concrete noun, which refers to tangible objects.
    /// </summary>
    Noun_Concrete = 49,

    /// <summary>
    /// Represents a countable noun, which can be counted.
    /// </summary>
    Noun_Countable = 50,

    /// <summary>
    /// Represents an uncountable noun, which cannot be counted.
    /// </summary>
    Noun_Uncountable = 51,

    /// <summary>
    /// Represents a singular noun, which refers to one item.
    /// </summary>
    Noun_Singular = 52,

    /// <summary>
    /// Represents a plural noun, which refers to more than one item.
    /// </summary>
    Noun_Plural = 53,
}