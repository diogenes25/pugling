namespace pugling.Models
{
    /// <summary>
    /// Represents a related vocabulary item.
    /// </summary>
    public record RelatedFormDto : IRelatedForm
    {
        /// <summary>
        /// The ID of the related vocabulary item.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The word or phrase of the related vocabulary item.
        /// </summary>
        public string Word { get; init; }

        /// <summary>
        /// The translation of the related vocabulary item.
        /// </summary>
        public string Translation { get; init; }
    }

}
