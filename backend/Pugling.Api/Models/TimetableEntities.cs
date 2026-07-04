namespace Pugling.Api.Models;

// Stundenplan-Steuerung: der Lernplan richtet sich nach dem Schul-Stundenplan des Kindes.
// Am Unterrichtstag eines Fachs wird NEUER Stoff gelernt, an den übrigen Tagen wiederholt
// (der Tag direkt davor gilt als Vorbereitung).

/// <summary>Tagesmodus, der sich aus dem Stundenplan ergibt.</summary>
public enum LessonDayMode
{
    /// <summary>Bereits eingeführten Stoff wiederholen (inkl. Vorbereitungstag vor dem Unterricht).</summary>
    Review = 0,
    /// <summary>Neuen Stoff einführen (am Unterrichtstag des Fachs).</summary>
    New = 1,
}

/// <summary>Ein Stundenplan-Eintrag: an diesem Wochentag hat das Kind dieses Fach.</summary>
public class TimetableEntry
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    /// <summary>Optionale Tageszeit als Freitext (z. B. "Nachmittag").</summary>
    public string? TimeOfDay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
