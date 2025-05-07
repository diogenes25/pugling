namespace PugLingDataTransfer.Models.Exercises
{
    /// <summary>
    /// Represents a fill-in-the-blanks exercise based on a Sentence object,
    /// with optional hints like letter jumbles, multiple choices, and pre-filled letters.
    /// </summary>
    public record FillInTextDto
    {
        /// <summary>
        /// A unique identifier for the fill-in-the-blanks exercise.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The underlying Sentence object on which the fill-in-the-blanks exercise is based.
        /// The 'Text' property of the SentenceComponents for the blanks will contain a special placeholder (e.g., "[BLANK]").
        /// </summary>
        public ISentence BaseSentence { get; init; }

        /// <summary>
        /// An ordered list of the correct answers for each gap.
        /// </summary>
        public string[] CorrectAnswers { get; init; }

        /// <summary>
        /// Optional hints for each gap. Each element in the array corresponds to a gap.
        /// The value can be null if no hint of this type is provided for a gap.
        /// </summary>
        public GapHint[]? GapSpecificHints { get; init; }

        /// <summary>
        /// An optional difficulty level for the exercise.
        /// </summary>
        public string? Difficulty { get; init; }

        /// <summary>
        /// Optional instructions specific to this fill-in-the-blanks exercise.
        /// </summary>
        public string? Instructions { get; init; }

        /// <summary>
        /// Optional metadata.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; init; }
    }

    /// <summary>
    /// Represents different types of hints that can be provided for a single gap in a fill-in-the-blanks exercise.
    /// </summary>
    public record GapHint
    {
        /// <summary>
        /// A letter jumble containing all the letters needed to fill the gap, in a scrambled order.
        /// </summary>
        public string? LetterJumble { get; init; }

        /// <summary>
        /// A list of multiple-choice options, one of which is the correct answer.
        /// </summary>
        public string[]? MultipleChoiceOptions { get; init; }

        /// <summary>
        /// The correct answer with some letters pre-filled (e.g., "H__m__l").
        /// </summary>
        public string? PreFilledLetters { get; init; }
    }
}