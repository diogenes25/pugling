using System.Text.Json.Serialization;

namespace pugling.Models.Constants
{
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
        /// represents a numeral, which is a word or phrase that represents a number.
        /// </summary>
        Numeral = 18,

        /// <summary>
        /// No specific part of speech is applicable.
        /// </summary>
        Other = 99,
    }
}
