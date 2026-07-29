namespace Pugling.Contracts;

/// <summary>Wer eine Markierung vorgenommen hat (für Nachvollziehbarkeit im Dashboard).</summary>
public enum TaggedBy
{
    /// <summary>Der Supervisor hat markiert.</summary>
    Vater = 0,
    /// <summary>Der Student hat selbst markiert (etwa „das kam in der Arbeit vor").</summary>
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
