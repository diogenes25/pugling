namespace Pugling.Contracts;

/// <summary>Who made a tagging (for traceability in the dashboard).</summary>
public enum TaggedBy
{
    /// <summary>The supervisor tagged it.</summary>
    Vater = 0,
    /// <summary>The student tagged it themselves (e.g. "this came up in the test").</summary>
    Sohn = 1,
}

/// <summary>Status of a class test in its lifecycle.</summary>
public enum KlassenarbeitStatus
{
    /// <summary>Planned / still upcoming.</summary>
    Planned = 0,
    /// <summary>Written (grade may be added later).</summary>
    Written = 1,
    /// <summary>Cancelled / called off.</summary>
    Cancelled = 2,
}
