namespace pugling.Models
{
    /// <summary>
    /// Contains specific details for verbs, including information about the base form, conjugations, and person.
    /// </summary>
    /// <example>
    /// // For infinitive:
    /// {
    ///    "isBaseForm": true,
    ///    "conjugations": {
    ///      "Präsens": {
    ///        "ich": {
    ///          "form": "gehe",
    ///          "vocObjRef": "/api/en/de/vocabularies/en_go_de_Präsens_ich.json"
    ///        }
    ///      }
    ///    }
    /// }
    /// // For conjugated form:
    /// {
    ///    "isBaseForm": false,
    ///    "baseFormRef": "/api/en/de/vocabularies/en_go_de.json",
    ///    "person": "ich",
    ///    "infinitiv": "gehen",
    ///    "tense": "Präsens"
    /// }
    /// </example>
    public record VerbDetailsDto : IVerbDetails
    {
        /// <summary>
        /// Indicates whether this Vocabulary item represents the base form (infinitive) of the verb.
        /// </summary>
        /// <example>
        /// true
        /// </example>
        public bool IsBaseForm { get; init; }

        /// <summary>
        /// An optional reference (RESTful URL) to the Vocabulary item of the base form (infinitive)
        /// of the verb, if this Vocabulary item represents a conjugated form.
        /// </summary>
        /// <example>
        /// "/api/en/de/vocabularies/en_go_de.json"
        /// </example>
        public string? BaseFormRef { get; init; }

        /// <summary>
        /// Optional the person of the conjugated verb form (e.g., "ich", "du"). Only set if IsBaseForm is false.
        /// </summary>
        /// <example>
        /// "ich"
        /// </example>
        public string? Person { get; init; }

        /// <summary>
        /// Optional the infinitive of the verb. Only set if IsBaseForm is false.
        /// </summary>
        /// <example>
        /// "gehen"
        /// </example>
        public string? Infinitiv { get; init; }

        /// <summary>
        /// Optional the tense of the conjugated verb form (e.g., "Präsens", "Präteritum").
        /// Only set if IsBaseForm is false.
        /// </summary>
        /// <example>
        /// "Praesens"
        /// </example>
        public string? Tense { get; init; }

        /// <summary>
        /// Optional conjugations of the verb (only in the Vocabulary item of the base form),
        /// structured by tense and person.
        /// </summary>
        /// <example>
        /// {
        ///   "Präsens": {
        ///     "ich": {
        ///       "form": "gehe",
        ///       "vocObjRef": "/api/en/de/vocabularies/en_go_de_Präsens_ich.json"
        ///     }
        ///   }
        /// }
        /// </example>
        public Dictionary<string, Dictionary<string, IConjugationDetails>>? Conjugations { get; init; }
    }
}