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
