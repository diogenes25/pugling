namespace Pugling.Contracts;

/// <summary>Target cadence of a study plan position: at what interval it must be fulfilled.</summary>
public enum GoalCadence
{
    /// <summary>No mandatory goal – free practice, does not count toward the daily/weekly goal.</summary>
    None = 0,
    /// <summary>Must be fulfilled on every practice day (daily goal).</summary>
    Daily = 1,
    /// <summary>Must be fulfilled once per week (weekly goal).</summary>
    Weekly = 2,
}

/// <summary>Selection scope of a position's content from the exercise pool.</summary>
public enum ItemScope
{
    /// <summary>All content of the exercise.</summary>
    All = 0,
    /// <summary>Only not-yet-introduced (new) content.</summary>
    New = 1,
    /// <summary>Only already introduced (old) content – review.</summary>
    Old = 2,
}

/// <summary>
/// Ordering strategy in which the server plays out a position's (due) content. The order
/// is materialized (frozen) <b>once</b> at the start of the session/test, so it doesn't shift mid-run
/// when boxes change due to answers.
/// </summary>
public enum PracticeOrder
{
    /// <summary>Weakest first: ascending by Leitner box, then index (default, previous behavior).</summary>
    WeakestFirst = 0,
    /// <summary>Strictly serial by item index.</summary>
    Serial = 1,
    /// <summary>Random order (shuffled once at freeze time).</summary>
    Random = 2,
    /// <summary>Weighted draw: most recently introduced (or never introduced) content is strongly preferred.</summary>
    NewestWeighted = 3,
}

/// <summary>
/// A time slot with a points multiplier: learning in the morning yields more than late in the evening.
/// <para>
/// The same type serves <b>two</b> carriers, deliberately so: the global slots of the configuration
/// (section <c>Scoring</c>) and the slots of a single study plan position ("homework counts double between
/// 13:00 and 15:00"). Both flow into the <i>one</i> ordered list the score is read from, so a second type
/// would only be a second place to get the ordering wrong.
/// </para>
/// <para>
/// Overlapping slots are <b>allowed</b> - the choice is fixed nonetheless: the slot starting latest (i.e. the
/// narrowest) wins, on a tie the one ending earlier. Without that ordering the same correct answer would
/// yield a different number of points depending on the order.
/// </para>
/// <para>
/// A mutable class with settable properties, because that is the shape the configuration binder has always
/// filled from <c>appsettings.json</c> - the type moved here unchanged rather than being reshaped on the way.
/// </para>
/// </summary>
public class ScoringTimeSlot
{
    /// <summary>Descriptive name, purely for the readability of the configuration ("Vormittag").</summary>
    public string Name { get; set; } = "";
    /// <summary>Start (inclusive).</summary>
    public TimeOnly Start { get; set; }
    /// <summary>End (exclusive).</summary>
    public TimeOnly End { get; set; }
    /// <summary>Factor applied to the base points.</summary>
    public double Multiplier { get; set; } = 1.0;
}
