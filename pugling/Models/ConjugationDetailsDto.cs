namespace pugling.Models
{
    /// <summary>
    /// Contains details for a single conjugation form (only within the Vocabulary item of the base form).
    /// </summary>
    /// <example>
    /// {
    ///   "form": "gehe",
    ///   "vocObjRef": "/api/en/de/vocabularies/en_go_de_Präsens_ich.json"
    /// }
    /// </example>
    public record ConjugationDetailsDto : IConjugationDetails
    {
        /// <summary>
        /// The conjugated form of the verb.
        /// </summary>
        /// <example>
        /// "gehe"
        /// </example>
        public string Form { get; init; }

        /// <summary>
        /// An optional reference (RESTful URL) to the Vocabulary item of this specific conjugated form.
        /// </summary>
        /// <example>
        /// "/api/en/de/vocabularies/en_go_de_Präsens_ich.json"
        /// </example>
        public string? VocObjRef { get; init; }
    }
}
