namespace Pugling.Api.Models;

// Interessen-Taxonomie: EIN kontrolliertes Vokabular, das sich das Kind-Profil und der Medien-Store
// teilen. Das ist der Angelpunkt der individualisierten Bildauswahl – nur weil beide Seiten aus
// derselben Tabelle schöpfen, ist „welches Bild passt zu diesem Kind" mehr als ein Stringvergleich.
// Das freie Child.Interests bleibt daneben bestehen: der KI-Creator lebt von Freitext (er kleidet den
// Stoff sprachlich ein), die Bildauswahl braucht dagegen exakte Referenzen.
//
// InterestFacet lebt im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Ein Interessen-/Stil-Schlagwort des gemeinsamen Vokabulars („pokemon", „fussball", „comic").
/// Global und kindneutral wie der Vokabel-Store: gepflegt vom Creator, referenziert von Kindern
/// (<see cref="ChildInterest"/>) <b>und</b> von Bildern (<see cref="MediaTagLink"/>).
/// </summary>
public class InterestTag
{
    public int Id { get; set; }

    /// <summary>Stabiler, global eindeutiger Referenz-Slug (kleingeschrieben, ohne Diakritika).</summary>
    public string Slug { get; set; } = "";

    /// <summary>Anzeigename für die UI („Pokémon") – darf Groß-/Sonderzeichen tragen.</summary>
    public string Label { get; set; } = "";

    /// <summary>Fachliche Facette (Thema vs. Darstellungsstil); steuert die Gewichtung der Auswahl.</summary>
    public InterestFacet Facet { get; set; } = InterestFacet.Other;

    /// <summary>
    /// Alternative Schreibweisen („Poke", „Pikachu"). Dienen dem Freitext-Backfill und der Creator-Suche,
    /// damit dasselbe Interesse nicht mehrfach als eigener Tag landet. Als JSON-Liste gespeichert
    /// (Neuzuweisung im Controller, kein In-Place-Mutieren – fehlender ValueComparer sonst ein Fallstrick).
    /// </summary>
    public List<string> Synonyms { get; set; } = [];

    /// <summary>Optionale Anzeigefarbe (Hex) für die UI – wie beim <see cref="VocabTag"/>.</summary>
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Bilder, die dieses Schlagwort tragen (Gegenstück zu <see cref="ChildInterests"/>).</summary>
    public List<MediaTagLink> MediaLinks { get; set; } = [];

    /// <summary>Kinder, die dieses Schlagwort mögen (oder ablehnen – siehe <see cref="ChildInterest.Weight"/>).</summary>
    public List<ChildInterest> ChildInterests { get; set; } = [];
}

/// <summary>
/// Gewichtetes Interesse eines Kindes an einem <see cref="InterestTag"/>. Das Vorzeichen trägt die
/// fachliche Hauptaussage: <b>negative Gewichte sind Abneigungen</b> („keine Spinnen") und schließen
/// passende Bilder später hart aus – sie sind für ein gutes Ergebnis wichtiger als die Vorlieben,
/// weil ein abstoßendes Bild den Lerneffekt umkehrt.
/// </summary>
public class ChildInterest
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    public int InterestTagId { get; set; }
    public InterestTag? InterestTag { get; set; }

    /// <summary>
    /// <see cref="MinWeight"/> (starke Abneigung) … 0 (neutral) … <see cref="MaxWeight"/> (Lieblingsthema).
    /// Der Controller klemmt auf diesen Bereich; die Skala ist bewusst grob, weil sie ein Mensch pflegt.
    /// </summary>
    public int Weight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Untere Grenze der Gewichtsskala (Abneigung).</summary>
    public const int MinWeight = -3;

    /// <summary>Obere Grenze der Gewichtsskala (Lieblingsthema).</summary>
    public const int MaxWeight = 3;
}
