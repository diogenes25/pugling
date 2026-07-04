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

/// <summary>Punkte-Buchung eines Kindes (positiv = gutgeschrieben, negativ = eingelöst).</summary>
public class ChildPointsEntry
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
