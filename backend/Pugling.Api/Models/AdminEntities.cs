namespace Pugling.Api.Models;

// Admin-Bereich: Personen-Verwaltung.
//   Father -> Child  (+ Punkte pro Kind)
// Der Lern-Inhalt (Subject -> Chapter -> Exercise) liegt separat im gemeinsamen
// learn-Katalog (siehe LearnEntities.cs). Die Zuordnung Kind <-> Katalog folgt später.

/// <summary>Elternteil / Verwalter im Admin-Bereich.</summary>
public class Father
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    /// <summary>Einfacher PIN-Login. Später durch echtes Auth ersetzen.</summary>
    public string Pin { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Child> Children { get; set; } = new();
}

/// <summary>Kind, das genau einem Vater zugeordnet ist.</summary>
public class Child
{
    public int Id { get; set; }
    public int FatherId { get; set; }
    public Father? Father { get; set; }
    public string Name { get; set; } = "";
    public int? BirthYear { get; set; }
    /// <summary>Einfacher PIN-Login des Kindes. Später durch echtes Auth ersetzen.</summary>
    public string Pin { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ChildPointsEntry> PointsEntries { get; set; } = new();
}

/// <summary>
/// Kategorie einer Punkte-Buchung – macht Boni auswertbar/deckelbar (z. B. "wie viele Punkte
/// kamen aus Combo vs. Uhrzeit?"). <see cref="Base"/> ist der Standard für Altbuchungen.
/// </summary>
public enum PointKind
{
    /// <summary>Basispunkte einer richtigen Wiederholung (inkl. Zeitfenster-Faktor).</summary>
    Base = 0,
    /// <summary>Manuelle Vater-Buchung (Gutschrift/Einlösung).</summary>
    Manual = 1,
    /// <summary>Tagesziel Übungszeit erreicht.</summary>
    Minutes = 2,
    /// <summary>Abschlusstest bestanden.</summary>
    Test = 3,
    /// <summary>Tag vollständig (Zeit + Test).</summary>
    DayComplete = 4,
    /// <summary>Combo-Bonus (Treffer in Folge).</summary>
    Combo = 5,
    /// <summary>Bonus für schnelle Antwort.</summary>
    Speed = 6,
    /// <summary>Bonus für durchgehende Lernzeit.</summary>
    Duration = 7,
    /// <summary>Belohnung für eine erfüllte Mission (Tages-/Wochen-/Zusatzziel).</summary>
    Mission = 8,
    /// <summary>Belohnung für eine erreichte Auszeichnung.</summary>
    Achievement = 9,
}

/// <summary>Punkte-Buchung eines Kindes (positiv = gutgeschrieben, negativ = eingelöst).</summary>
public class ChildPointsEntry
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int Amount { get; set; }
    /// <summary>Kategorie der Buchung (für Auswertung/Deckelung der Bonusquellen).</summary>
    public PointKind Kind { get; set; } = PointKind.Base;
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
