namespace Pugling.Api.Services.Shared;

/// <summary>
/// Punkte-Einstellungen aus der Konfiguration (Abschnitt <c>Scoring</c>).
/// <para>
/// Die Zeitfenster waren bis E12 eine <b>Tabelle</b> (<c>TimeSlotRule</c>) – ohne API, ohne Schreibpfad
/// außer dem Seed, ohne Index und ohne Überlappungsprüfung. Die Test-Factory musste ihre Zeilen sogar
/// <i>löschen</i>, um deterministische Punktzahlen zu bekommen. Eine Tabelle, deren Zeilen die Suite
/// wegräumen muss, um sinnvolle Ergebnisse zu erhalten, ist Konfiguration.
/// </para>
/// </summary>
public class ScoringOptions
{
    /// <summary>Konfigurationsabschnitt.</summary>
    public const string SectionName = "Scoring";

    /// <summary>
    /// Ob die Zeitfenster überhaupt gelten. <c>false</c> heißt: Faktor 1,0 zu jeder Uhrzeit.
    /// <para>
    /// Der Schalter existiert für die Test-Suite, und zwar aus einem harten Grund: mit Fenstern hängt die
    /// Punktzahl derselben richtigen Antwort an der <b>Uhrzeit des Laufs</b> (vormittags ×1,5, abends ×0,8).
    /// Für die von <c>DocsCaptureTests</c> eingecheckte Doku ist das Diff-Rauschen. Gleiche Bauart wie
    /// <c>RateLimiting:LoginEnabled</c>, das aus demselben Grund existiert.
    /// </para>
    /// </summary>
    public bool TimeSlotsEnabled { get; set; } = true;

    /// <summary>Zeitfenster mit Punkte-Multiplikator; Überlappung ist erlaubt (siehe <see cref="ScoringTimeSlot"/>).</summary>
    public List<ScoringTimeSlot> TimeSlots { get; set; } = [];
}

/// <summary>
/// Ein Zeitfenster mit Punkte-Multiplikator: wer vormittags lernt, bekommt mehr als spätabends.
/// <para>
/// Überlappende Fenster sind <b>erlaubt</b> – die Auswahl liegt trotzdem fest: das am spätesten beginnende
/// (also engste) Fenster gewinnt, bei Gleichstand das früher endende. Ohne diese Ordnung brächte dieselbe
/// richtige Antwort je nach Reihenfolge unterschiedlich viele Punkte.
/// </para>
/// </summary>
public class ScoringTimeSlot
{
    /// <summary>Sprechender Name, nur zur Lesbarkeit der Konfiguration („Vormittag").</summary>
    public string Name { get; set; } = "";
    /// <summary>Beginn (einschließlich).</summary>
    public TimeOnly Start { get; set; }
    /// <summary>Ende (ausschließlich).</summary>
    public TimeOnly End { get; set; }
    /// <summary>Faktor auf die Basispunkte.</summary>
    public double Multiplier { get; set; } = 1.0;
}
