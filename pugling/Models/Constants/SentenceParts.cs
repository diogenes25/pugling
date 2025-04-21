namespace pugling.Models.Constants
{
    /// <summary>
    /// Provides string constants for different sentence parts (grammatical roles) with subcategories.
    /// </summary>
    public static class SentenceParts
    {
        // Hauptkategorien

        /// <summary>
        /// Represents the subject of a sentence.
        /// </summary>
        public const string Subject = "Subject";

        /// <summary>
        /// Represents the predicate of a sentence.
        /// </summary>
        public const string Predicate = "Predicate";

        /// <summary>
        /// Represents the object of a sentence.
        /// </summary>
        public const string Object = "Object";

        /// <summary>
        /// Represents a determiner in a sentence.
        /// </summary>
        public const string Determiner = "Determiner";

        /// <summary>
        /// Represents an adjective in a sentence.
        /// </summary>
        public const string Adjective = "Adjective";

        /// <summary>
        /// Represents an adverbial in a sentence.
        /// </summary>
        public const string Adverbial = "Adverbial";

        /// <summary>
        /// Represents a preposition in a sentence.
        /// </summary>
        public const string Preposition = "Preposition";

        /// <summary>
        /// Represents a conjunction in a sentence.
        /// </summary>
        public const string Conjunction = "Conjunction";

        /// <summary>
        /// Represents an interjection in a sentence.
        /// </summary>
        public const string Interjection = "Interjection";

        /// <summary>
        /// Represents a verb particle in a sentence.
        /// </summary>
        public const string VerbParticle = "VerbParticle";

        /// <summary>
        /// Represents a phrase in a sentence.
        /// </summary>
        public const string Phrase = "Phrase";

        /// <summary>
        /// Represents a complement in a sentence.
        /// </summary>
        public const string Complement = "Complement";

        // Unterkategorien für Objekte

        /// <summary>
        /// Represents a direct object in a sentence.
        /// </summary>
        public const string ObjectDirect = "Object.Direct";

        /// <summary>
        /// Represents an indirect object in a sentence.
        /// </summary>
        public const string ObjectIndirect = "Object.Indirect";

        /// <summary>
        /// Represents a prepositional object in a sentence.
        /// </summary>
        public const string ObjectPrepositional = "Object.Prepositional";

        // Unterkategorien für Determiner

        /// <summary>
        /// Represents an article determiner in a sentence.
        /// </summary>
        public const string DeterminerArticle = "Determiner.Article";

        /// <summary>
        /// Represents a possessive determiner in a sentence.
        /// </summary>
        public const string DeterminerPossessive = "Determiner.Possessive";

        /// <summary>
        /// Represents a demonstrative determiner in a sentence.
        /// </summary>
        public const string DeterminerDemonstrative = "Determiner.Demonstrative";

        /// <summary>
        /// Represents a quantifier determiner in a sentence.
        /// </summary>
        public const string DeterminerQuantifier = "Determiner.Quantifier";

        // Unterkategorien für Adverbial

        /// <summary>
        /// Represents an adverbial of time in a sentence.
        /// </summary>
        public const string AdverbialOfTime = "Adverbial.Time";

        /// <summary>
        /// Represents an adverbial of place in a sentence.
        /// </summary>
        public const string AdverbialOfPlace = "Adverbial.Place";

        /// <summary>
        /// Represents an adverbial of manner in a sentence.
        /// </summary>
        public const string AdverbialOfManner = "Adverbial.Manner";

        /// <summary>
        /// Represents an adverbial of reason in a sentence.
        /// </summary>
        public const string AdverbialOfReason = "Adverbial.Reason";

        /// <summary>
        /// Represents an adverbial of purpose in a sentence.
        /// </summary>
        public const string AdverbialOfPurpose = "Adverbial.Purpose";

        // Unterkategorien für Complement

        /// <summary>
        /// Represents a subject complement in a sentence.
        /// </summary>
        public const string ComplementSubject = "Complement.Subject";

        /// <summary>
        /// Represents an object complement in a sentence.
        /// </summary>
        public const string ComplementObject = "Complement.Object";

        // Weitere spezifische Rollen

        /// <summary>
        /// Represents an auxiliary verb in a sentence.
        /// </summary>
        public const string AuxiliaryVerb = "Verb.Auxiliary";

        /// <summary>
        /// Represents a modal verb in a sentence.
        /// </summary>
        public const string ModalVerb = "Verb.Modal";

        /// <summary>
        /// Represents a particle in a sentence, such as in separable verbs.
        /// </summary>
        public const string Particle = "Particle";
    }
}