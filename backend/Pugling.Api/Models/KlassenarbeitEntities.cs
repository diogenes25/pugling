namespace Pugling.Api.Models;

// Tagging + Klassenarbeiten:
//   Tag (pro Kind) --< ExerciseTag >-- Exercise (gemeinsamer Katalog)
//   Klassenarbeit (pro Kind) --< KlassenarbeitExercise >-- Exercise
//   Klassenarbeit --< KlassenarbeitTag >-- Tag  (Übungen eines Tags gelten als relevant)
//
// Der Lern-Katalog (Subject -> Chapter -> Exercise) bleibt kindneutral; die Zuordnung „welche
// Übung ist für dieses Kind / diese Klassenarbeit relevant" passiert ausschließlich über diese
// Verknüpfungstabellen. Tags dürfen Vater UND Sohn setzen, Klassenarbeiten pflegt nur der Vater.

/// <summary>Wer eine Markierung vorgenommen hat (für Nachvollziehbarkeit im Dashboard).</summary>
public enum TaggedBy
{
    Vater = 0,
    Sohn = 1,
}

/// <summary>
/// Frei benanntes Schlagwort im Kontext eines Kindes (z. B. „Unit 5", „unregelmäßige Verben").
/// Vater und Sohn markieren damit Katalog-Übungen, etwa als relevant für eine bestimmte Klassenarbeit.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Optionale Anzeigefarbe (Hex, z. B. „#3b82f6") für die UI.</summary>
    public string? Color { get; set; }
    /// <summary>Wer das Schlagwort angelegt hat.</summary>
    public TaggedBy CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ExerciseTag> ExerciseTags { get; set; } = new();
}

/// <summary>Verknüpft eine Katalog-Übung mit einem <see cref="Tag"/> und hält fest, wer sie gesetzt hat.</summary>
public class ExerciseTag
{
    public int Id { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    /// <summary>Wer diese Übung mit dem Tag markiert hat (kann von <see cref="Tag.CreatedBy"/> abweichen).</summary>
    public TaggedBy TaggedByRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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

/// <summary>
/// Eine geplante oder bereits geschriebene Klassenarbeit eines Kindes. Der Vater plant sie,
/// weist relevante Übungen zu (direkt oder über Tags) und trägt nach dem Schreiben die Note nach.
/// </summary>
public class Klassenarbeit
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Optionale Verknüpfung zum Katalog-Fach.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Freitext-Thema/Stoff der Arbeit (z. B. „Simple Past, Unit 3–4").</summary>
    public string? Topic { get; set; }
    /// <summary>Termin: geplantes bzw. tatsächliches Schreibdatum.</summary>
    public DateOnly ScheduledDate { get; set; }
    public KlassenarbeitStatus Status { get; set; } = KlassenarbeitStatus.Planned;
    /// <summary>Deutsche Schulnote 1,0 (sehr gut) … 6,0 (ungenügend). Null, solange nicht nachgetragen.</summary>
    public decimal? Grade { get; set; }
    /// <summary>Optionale Notiz zur Note (z. B. „Vokabeln saßen, Grammatik schwach").</summary>
    public string? GradeComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<KlassenarbeitExercise> Exercises { get; set; } = new();
    public List<KlassenarbeitTag> Tags { get; set; } = new();
}

/// <summary>Direkte Zuordnung einer Übung zu einer Klassenarbeit.</summary>
public class KlassenarbeitExercise
{
    public int Id { get; set; }
    public int KlassenarbeitId { get; set; }
    public Klassenarbeit? Klassenarbeit { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Verknüpft eine Klassenarbeit mit einem <see cref="Tag"/>: alle mit diesem Tag markierten Übungen
/// gelten (zusätzlich zu den direkt zugewiesenen) als relevant für die Arbeit.
/// </summary>
public class KlassenarbeitTag
{
    public int Id { get; set; }
    public int KlassenarbeitId { get; set; }
    public Klassenarbeit? Klassenarbeit { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
