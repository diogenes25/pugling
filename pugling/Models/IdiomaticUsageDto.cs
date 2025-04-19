namespace pugling.Models
{
    /// <summary>
    /// Represents an idiomatic usage of a word or phrase.
    /// </summary>
    public record IdiomaticUsageDto : IIdiomaticUsage
    {
        /// <summary>
        /// The idiomatic phrase in the source language.
        /// </summary>
        public string Phrase { get; init; }

        /// <summary>
        /// The translation of the idiomatic phrase in the target language.
        /// </summary>
        public string Translation { get; init; }
    }
}