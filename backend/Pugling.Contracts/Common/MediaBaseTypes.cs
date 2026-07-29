namespace Pugling.Contracts;

// Geteilte Basistypen des Medien-Stores und der Interessen-Taxonomie (ebenen-neutral: der Creator
// pflegt Assets/Tags, der Supervisor die Interessen des Kindes, der Student bekommt später das Bild).

/// <summary>Medienart eines Assets. Heute werden nur Bilder ausgeliefert; der Store ist bewusst offen.</summary>
public enum MediaKind
{
    /// <summary>Bild – der einzige heute ausgespielte Typ (siehe docs/medien-bilder.md).</summary>
    Image = 0,
    /// <summary>Tonaufnahme (z. B. Aussprache). Im Store vorgesehen, noch nicht ausgespielt.</summary>
    Audio = 1,
    /// <summary>Video. Im Store vorgesehen, noch nicht ausgespielt.</summary>
    Video = 2,
}

/// <summary>
/// Eignung eines Assets für eine Altersgruppe – die tragende Achse der Zielgruppen-Differenzierung.
/// Erst sie macht <b>einen gemeinsamen Store für alle Zielgruppen</b> tragfähig: die Auswahl filtert hart
/// gegen <c>Child.AllowedContentRating</c>, bevor überhaupt nach Interessen sortiert wird.
/// <para>
/// Die Werte sind <b>aufsteigend geordnet</b> und werden numerisch verglichen (<c>Rating &lt;= Erlaubtes</c>).
/// Deshalb liegen sie als <c>int</c> in der DB (nicht als String wie die übrigen Enums) und dürfen
/// <b>nie umnummeriert</b> werden – neue Stufen nur am Ende anhängen.
/// </para>
/// </summary>
public enum ContentRating
{
    /// <summary>Für alle geeignet. Default für neue Assets <i>und</i> neue Kinder.</summary>
    Everyone = 0,
    /// <summary>Ab ca. 12: mildere Grusel-/Konflikt-Motive, jugendliche Themen.</summary>
    Teen = 1,
    /// <summary>Nur Erwachsene (Freizügigkeit, drastische Darstellung). Für ein Kindprofil nur nach ausdrücklicher Freigabe durch den Supervisor.</summary>
    Mature = 2,
}

/// <summary>
/// Semantischer Auslieferungs-Slot einer Variante. Der Client fragt nach dem <i>Zweck</i>, nicht nach
/// Pixelmaßen – so bleibt die Auflösungspolitik serverseitig änderbar, ohne den Vertrag zu brechen.
/// </summary>
public enum MediaPurpose
{
    /// <summary>Winziges Vorschaubild in Listen/Trefferlisten.</summary>
    Thumb = 0,
    /// <summary>Standardgröße auf der Übungskarte – der Regelfall beim Lernen.</summary>
    Card = 1,
    /// <summary>Große Ansicht (Vorschau/Zoom).</summary>
    Full = 2,
    /// <summary>Breites Aufmacherformat (Kapitel-/Übungskopf).</summary>
    Hero = 3,
}

/// <summary>Herkunft eines Assets – macht generierte und fremde Inhalte im Katalog unterscheidbar.</summary>
public enum MediaOrigin
{
    /// <summary>Herkunft nicht erfasst – Default für Altbestände.</summary>
    Unknown = 0,
    /// <summary>Vom Creator selbst hochgeladen/bereitgestellt.</summary>
    Upload = 1,
    /// <summary>Aus einer externen Bildquelle übernommen (Lizenz/Attribution pflegen!).</summary>
    Stock = 2,
    /// <summary>KI-generiert; der erzeugende Prompt/das Modell gehört in <c>Source</c>.</summary>
    Generated = 3,
}

/// <summary>
/// Facette eines Interessen-Schlagworts. Sie gruppiert die Taxonomie fachlich, ohne sie zu spalten:
/// Thema (<see cref="Franchise"/>, <see cref="Sport"/> …) und <see cref="Style"/> liegen bewusst in
/// <b>derselben</b> Tabelle, weil sie sich bei der Bildauswahl identisch verhalten – nur die Gewichtung
/// unterscheidet sich. Erweiterungen sind rein additiv.
/// </summary>
public enum InterestFacet
{
    /// <summary>Keiner der übrigen Facetten zuzuordnen. Default beim Anlegen eines Schlagworts.</summary>
    Other = 0,
    /// <summary>Marke/Serie/Spiel („Pokémon", „Brawl Stars", „Star Wars").</summary>
    Franchise = 1,
    /// <summary>Sportart oder Verein („Fußball", „Skateboard").</summary>
    Sport = 2,
    /// <summary>Tier oder Tiergruppe („Pferd", „Dinosaurier").</summary>
    Animal = 3,
    /// <summary>Fahrzeug („Traktor", „Feuerwehrauto", „Rakete").</summary>
    Vehicle = 4,
    /// <summary>Musik – Genre, Band oder Instrument.</summary>
    Music = 5,
    /// <summary>Freizeit/Tätigkeit („Kochen", „Angeln", „Programmieren").</summary>
    Hobby = 6,
    /// <summary>Natur und Landschaft („Wald", „Meer", „Weltraum").</summary>
    Nature = 7,
    /// <summary>Darstellungsstil („Comic", „Foto", „Pixel-Art") – orthogonal zum Thema.</summary>
    Style = 8,
}
