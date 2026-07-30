namespace Pugling.Contracts;

/// <summary>
/// Classification of a remark. <see cref="Unspecified"/> is the common case when capturing it: categorizing
/// while testing costs more time than it's worth – the follow-up skill derives the classification from
/// the text afterward.
/// </summary>
public enum RemarkCategory
{
    /// <summary>Not classified (default) – the skill derives the category from the text later.</summary>
    Unspecified = 0,
    /// <summary>Something doesn't work as expected.</summary>
    Bug = 1,
    /// <summary>Usability/display: labeling, layout, clarity.</summary>
    Ui = 2,
    /// <summary>Question or observation about the implementation in the code.</summary>
    Code = 3,
    /// <summary>Domain content: exercises, vocabulary, learning material.</summary>
    Content = 4,
    /// <summary>Suggestion for something new.</summary>
    Idea = 5,
    /// <summary>Pure knowledge question – expects an answer, not a change.</summary>
    Question = 6,
}

/// <summary>
/// Processing state of a remark. Deliberately lean: no assignment, no milestones – four states
/// are enough so the follow-up skill doesn't present the same remarks again on every run.
/// </summary>
public enum RemarkStatus
{
    /// <summary>Captured, not yet reviewed.</summary>
    Open = 0,
    /// <summary>Deferred: there is something to do, but not now. An existing answer is kept as preliminary work.</summary>
    Planned = 1,
    /// <summary>Done – question answered or change implemented.</summary>
    Done = 2,
    /// <summary>Rejected: no action needed.</summary>
    Rejected = 3,
}

/// <summary>
/// Origin of a contribution in a remark's history.
/// <para>
/// Deliberately a dedicated field and <b>not</b> derived from the author account: Claude writes via the
/// skill using the human's token, so both contributions would carry the same account. A rule hinges on
/// this distinction, though – a <see cref="Human"/> contribution reopens a done remark, while an
/// <see cref="Assistant"/> contribution leaves the state untouched.
/// </para>
/// </summary>
public enum RemarkCommentAuthor
{
    /// <summary>The human – written in the widget or on the remarks page (default).</summary>
    Human = 0,
    /// <summary>Claude Code, via the skill.</summary>
    Assistant = 1,
}
