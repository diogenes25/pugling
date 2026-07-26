namespace Pugling.Contracts;

/// <summary>Wer eine Markierung vorgenommen hat (für Nachvollziehbarkeit im Dashboard).</summary>
public enum TaggedBy
{
    Vater = 0,
    Sohn = 1,
}

/// <summary>Status einer Klassenarbeit im Lebenszyklus.</summary>
public enum KlassenarbeitStatus
{
    /// <summary>Geplant / steht noch an.</summary>
    Planned = 0,
    /// <summary>Geschrieben (Note kann nachgetragen sein).</summary>
    Written = 1,
    /// <summary>Entfällt / abgesagt.</summary>
    Cancelled = 2,
}
