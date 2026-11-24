namespace Pugling.Contracts;

/// <summary>
/// The fixed reasons a child can report about a card or an exercise (B-623, decision 4). The list is closed on purpose:
/// every reason carries a fixed <see cref="FeedbackReach"/>, so a new one is a contract change, not a free-text label.
/// </summary>
public enum FeedbackReason
{
    /// <summary>Too hard for this child.</summary>
    TooHard = 0,
    /// <summary>Too easy for this child.</summary>
    TooEasy = 1,
    /// <summary>Not covered in class yet.</summary>
    AheadOfClass = 2,
    /// <summary>The card's translation is wrong.</summary>
    WrongTranslation = 3,
    /// <summary>The card asks for more than the word itself.</summary>
    TooMuchAsked = 4,
    /// <summary>The grading was too strict (an accent, a single letter).</summary>
    TooStrict = 5,
    /// <summary>The word has several meanings and the card accepts only one.</summary>
    SeveralMeanings = 6,
    /// <summary>The card is good as it is.</summary>
    Liked = 7,
}

/// <summary>
/// Whom a feedback reason concerns when new content is created (B-623, decision 2): a reason that lies with the child
/// only shapes content made for that child; a reason that lies with the exercise holds for everyone.
/// </summary>
public enum FeedbackReach
{
    /// <summary>Only when content is created for this child.</summary>
    ThisChild = 0,
    /// <summary>For everyone, also when content is created for a profile.</summary>
    Everyone = 1,
}

/// <summary>Processing state of a feedback: open until a supervisor marks it done (for all supervisors of the child).</summary>
public enum FeedbackStatus
{
    /// <summary>Reported, not yet handled.</summary>
    Open = 0,
    /// <summary>Handled by a supervisor.</summary>
    Done = 1,
}

/// <summary>
/// How a supervisor judged a done feedback (B-767, decision 2 of B-627). Kept apart from <see cref="FeedbackStatus"/>:
/// <c>Done</c> without an outcome stays valid for feedback handled before the outcome existed.
/// </summary>
public enum FeedbackOutcome
{
    /// <summary>The child was right: the app graded the reported test answer wrongly (B-768 corrects the test).</summary>
    Justified = 0,
    /// <summary>The app was right; nothing is changed.</summary>
    Rejected = 1,
}

/// <summary>The fixed reach of every <see cref="FeedbackReason"/> - the one place B-630 reads it from.</summary>
public static class FeedbackReasons
{
    /// <summary>All reasons in the order the child sees them.</summary>
    public static readonly IReadOnlyList<FeedbackReason> All = Enum.GetValues<FeedbackReason>();

    /// <summary>The reach of <paramref name="reason"/> (B-623, decision 2).</summary>
    public static FeedbackReach ReachOf(FeedbackReason reason) => reason switch
    {
        FeedbackReason.WrongTranslation or FeedbackReason.TooMuchAsked or FeedbackReason.TooStrict
            or FeedbackReason.SeveralMeanings => FeedbackReach.Everyone,
        _ => FeedbackReach.ThisChild,
    };
}
