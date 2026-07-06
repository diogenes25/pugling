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
    /// <summary>Aktuelle Klassenstufe (1–13). Steuert die Vorfilterung passender Übungen im Lehrplan-Assistenten.</summary>
    public int? Grade { get; set; }
    /// <summary>Schulart des Kindes – filtert im Assistenten Übungen, die nicht für alle Schularten gedacht sind.</summary>
    public SchoolTypes SchoolType { get; set; } = SchoolTypes.None;
    /// <summary>Einfacher PIN-Login des Kindes. Später durch echtes Auth ersetzen.</summary>
    public string Pin { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Aktuell ausgerüsteter Skin (Charakter) des Kindes. Serverseitig persistiert, damit die
    /// Auswahl geräteübergreifend gilt und nicht am localStorage eines Geräts hängt.
    /// </summary>
    public string SelectedSkin { get; set; } = SkinCatalog.Default;

    /// <summary>
    /// Freigeschaltete Skins. Der Server ist die Quelle der Wahrheit für den Besitz, damit ein
    /// Skin nur nach echter Münz-Einlösung freigeschaltet werden kann (kein Client-Betrug).
    /// Als JSON-Liste gespeichert (Neuzuweisung im Controller, kein In-Place-Mutieren).
    /// </summary>
    public List<string> OwnedSkins { get; set; } = [SkinCatalog.Default];

    /// <summary>
    /// Nebenläufigkeits-Marke: wird bei jedem Skin-Kauf/Ausrüsten neu gesetzt und als
    /// EF-Concurrency-Token geprüft. Verhindert, dass zwei parallele Käufe (Doppelklick/Retry)
    /// beide den Deckungs-Check bestehen und doppelt abbuchen bzw. die Skin-Liste überschreiben –
    /// der Verlierer läuft dann in eine <c>DbUpdateConcurrencyException</c> (→ 409) statt zu doppeln.
    /// </summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public List<ChildPointsEntry> PointsEntries { get; set; } = new();
}

/// <summary>
/// Katalog der kaufbaren Skins samt Kosten – <b>serverseitige Quelle der Wahrheit</b>. Kosten
/// werden nie dem Client geglaubt; das Frontend liefert nur die visuelle Darstellung
/// (Emoji/Farbverlauf). Die IDs müssen mit dem Frontend-Katalog (<c>frontend/src/lib/skins.ts</c>)
/// übereinstimmen.
/// </summary>
public static class SkinCatalog
{
    /// <summary>Von Anfang an freigeschalteter Gratis-Starter.</summary>
    public const string Default = "pug";

    /// <summary>Kaufbare Skins: ID → Kosten in Gems (0 = gratis).</summary>
    public static readonly IReadOnlyDictionary<string, int> Costs = new Dictionary<string, int>
    {
        ["pug"] = 0,
        ["fox"] = 300,
        ["dragon"] = 800,
        ["robot"] = 1200,
        ["ninja"] = 2000,
    };

    /// <summary>Kosten eines Skins oder <c>null</c>, wenn die ID unbekannt ist.</summary>
    public static int? CostOf(string skinId) => Costs.TryGetValue(skinId, out var c) ? c : null;
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
    /// <summary>Einlösung von Münzen für einen Skin (negative Buchung).</summary>
    SkinPurchase = 10,
    /// <summary>Einlösung von Münzen für eine reale Prämie (z. B. Fernseh-/Spielzeit; negative Buchung).</summary>
    Reward = 11,
    /// <summary>Ziel einer Lehrplan-Position erreicht (Tages-/Wochenziel der Übung).</summary>
    Goal = 12,
    /// <summary>Einlösung von Münzen für einen Familien-Shop-Artikel (negative Buchung).</summary>
    ShopCoins = 13,
    /// <summary>Einlösung von Gems für einen Familien-Shop-Artikel (negative Buchung).</summary>
    ShopGems = 14,
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
