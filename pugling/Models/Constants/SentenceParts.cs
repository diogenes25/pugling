namespace pugling.Models.Constants
{
    /// <summary>
    /// Provides string constants for different sentence parts (grammatical roles) with subcategories.
    /// </summary>
    public static class SentenceParts
    {
        // Hauptkategorien
        public const string Subject = "Subject";
        public const string Predicate = "Predicate";
        public const string Object = "Object";
        public const string Determiner = "Determiner";
        public const string Adjective = "Adjective";
        public const string Adverbial = "Adverbial";
        public const string Preposition = "Preposition";
        public const string Conjunction = "Conjunction";
        public const string Interjection = "Interjection";
        public const string VerbParticle = "VerbParticle";
        public const string Phrase = "Phrase";
        public const string Complement = "Complement";

        // Unterkategorien für Objekte
        public const string ObjectDirect = "Object.Direct";
        public const string ObjectIndirect = "Object.Indirect";
        public const string ObjectPrepositional = "Object.Prepositional";

        // Unterkategorien für Determiner
        public const string DeterminerArticle = "Determiner.Article";
        public const string DeterminerPossessive = "Determiner.Possessive";
        public const string DeterminerDemonstrative = "Determiner.Demonstrative";
        public const string DeterminerQuantifier = "Determiner.Quantifier";

        // Unterkategorien für Adverbial
        public const string AdverbialOfTime = "Adverbial.Time";
        public const string AdverbialOfPlace = "Adverbial.Place";
        public const string AdverbialOfManner = "Adverbial.Manner";
        public const string AdverbialOfReason = "Adverbial.Reason";
        public const string AdverbialOfPurpose = "Adverbial.Purpose";

        // Unterkategorien für Complement
        public const string ComplementSubject = "Complement.Subject";
        public const string ComplementObject = "Complement.Object";

        // Weitere spezifische Rollen
        public const string AuxiliaryVerb = "Verb.Auxiliary";
        public const string ModalVerb = "Verb.Modal";
        public const string Particle = "Particle"; // Z.B. bei trennbaren Verben
    }
}