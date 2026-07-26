namespace Pugling.Api.Models;

// Medien-Store: ein Motiv, viele Bilder. Zwei Achsen, die strikt getrennt bleiben müssen –
//   MediaAsset   = eine *Darstellung*         („laufendes Einhorn, Comic")  → inhaltlich (Stil/Zielgruppe)
//   MediaVariant = eine *technische Ausprägung* derselben Darstellung        → Auflösung/Format
// Wie beim Vokabel-Store liegen keine Bytes in der DB, nur URLs (vgl. Vocabulary.PronunciationAudioUrl).
//
// Bewusst gibt es KEIN eigenes „Motiv"-Entity: die Menge „alle Bilder, die *laufen* meinen" ist genau die
// Menge der MediaLinks auf dieselbe Vokabel – der Träger ist das Motiv.
//
// MediaKind/ContentRating/MediaPurpose/MediaOrigin leben im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Eine konkrete Darstellung eines Motivs – nicht „das Bild zu laufen", sondern „das laufende Einhorn
/// im Comic-Stil". Trägt Bedeutung, Stil (über <see cref="TagLinks"/>) und Eignung
/// (<see cref="Rating"/>); die Dateien selbst hängen als <see cref="Variants"/> daran.
/// </summary>
public class MediaAsset
{
    public int Id { get; set; }

    /// <summary>Stabiler, global eindeutiger Referenz-Key (z. B. "run_unicorn_comic") – wie <see cref="Vocabulary.Key"/>.</summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Was zu sehen ist. Doppelrolle: <b>Alt-Text</b> für die Barrierefreiheit (er geht später mit der
    /// Karte an den Client) und Suchtext für Creator und KI-Agenten.
    /// </summary>
    public string Description { get; set; } = "";

    public MediaKind Kind { get; set; } = MediaKind.Image;

    /// <summary>Eignung. Die Auswahl filtert später hart dagegen, bevor sie überhaupt nach Interessen sortiert.</summary>
    public ContentRating Rating { get; set; } = ContentRating.Everyone;

    /// <summary>Lizenz-Kurzbezeichnung (z. B. "CC-BY-4.0") – Pflicht bei fremden Quellen.</summary>
    public string? License { get; set; }

    /// <summary>Nennung des Urhebers, sofern die Lizenz sie verlangt.</summary>
    public string? Attribution { get; set; }

    public MediaOrigin Origin { get; set; } = MediaOrigin.Unknown;

    /// <summary>Herkunftsdetail: URL der Fremdquelle bzw. Modell + Prompt bei <see cref="MediaOrigin.Generated"/>.</summary>
    public string? Source { get; set; }

    /// <summary>Dominante Farbe (Hex) oder winziger Blur-Hash – erlaubt ruckelfreies Nachladen im Client.</summary>
    public string? Placeholder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Dieselbe Darstellung in mehreren Auflösungen/Formaten.</summary>
    public List<MediaVariant> Variants { get; set; } = [];

    /// <summary>Themen- und Stil-Schlagworte aus der geteilten Taxonomie (<see cref="InterestTag"/>).</summary>
    public List<MediaTagLink> TagLinks { get; set; } = [];

    /// <summary>Wo dieses Bild zugeordnet ist (Vokabeln, Übungs-Items, Übungen).</summary>
    public List<MediaLink> Links { get; set; } = [];
}

/// <summary>
/// Eine technische Ausprägung eines <see cref="MediaAsset"/> – dieselbe Darstellung, andere Bytes.
/// Adressiert wird über den semantischen <see cref="Purpose"/>, nicht über Pixelmaße: so kann die
/// Auslieferung später auf andere Größen umstellen, ohne den Vertrag zu brechen.
/// </summary>
public class MediaVariant
{
    public int Id { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    public MediaPurpose Purpose { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Dateiformat ("webp", "avif", "png", "jpg"). Mehrere Formate je Zweck erlauben <c>&lt;picture&gt;</c>/srcset.</summary>
    public string Format { get; set; } = "webp";

    /// <summary>URL zur Datei – kein Base64 im Payload (gleiche Regel wie bei der Aussprache-Audioquelle).</summary>
    public string Url { get; set; } = "";

    /// <summary>Dateigröße in Bytes, falls bekannt (Budget-Entscheidungen im Client).</summary>
    public long? Bytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Verknüpft ein <see cref="MediaAsset"/> mit einem <see cref="InterestTag"/> (n:m).</summary>
public class MediaTagLink
{
    public int Id { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    public int InterestTagId { get; set; }
    public InterestTag? InterestTag { get; set; }
}

/// <summary>
/// Zuordnung eines <see cref="MediaAsset"/> zu dem, was es bebildert – <b>n:m in beide Richtungen</b>.
/// Eine Vokabel trägt viele Darstellungen (genau der Punkt: das Kind bekommt die passende), und ein
/// Asset dient vielen Vokabeln: „run" (en→de) und „laufen" (de→en) sind getrennte Store-Zeilen, das
/// laufende Einhorn soll beiden dienen. Eine Spalte am Träger (wie
/// <see cref="Vocabulary.PronunciationAudioUrl"/>) könnte das nicht – Audio ist 1:1, weil es eine
/// korrekte Aussprache gibt; bei Bildern ist die Vielfalt die Anforderung.
/// <para>
/// Genau <b>eine</b> der drei Träger-FKs ist gesetzt (Check-Constraint). Die drei bilden eine
/// Genauigkeits-Kaskade, die der Resolver später von unten nach oben liest:
/// <see cref="ExerciseItemId"/> (nur diese Übung) schlägt <see cref="VocabularyId"/> (gilt überall);
/// <see cref="ExerciseId"/> ist das Titelbild einer Text-/Leseübung und steht daneben.
/// </para>
/// </summary>
public class MediaLink
{
    public int Id { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    /// <summary>Store-Zuordnung: gilt in <b>allen</b> Übungen, die diese Vokabel nutzen (der Regelfall).</summary>
    public int? VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }

    /// <summary>Übungslokale Übersteuerung: gilt nur für dieses eine Item, ohne den Store zu verbiegen.</summary>
    public int? ExerciseItemId { get; set; }
    public ExerciseItem? ExerciseItem { get; set; }

    /// <summary>Titelbild einer Übung (Text/Satz/Lesen) – kein Item-Bezug.</summary>
    public int? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>
    /// Redaktioneller Rang. Er entscheidet erst <b>bei Gleichstand</b> der Interessens-Bewertung – der
    /// Creator kann damit ein Lieblingsbild nach vorn ziehen, ohne die Auswahl je Kind auszuhebeln.
    /// </summary>
    public int Weight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Die <b>eingefrorene</b> Bildwahl eines Kindes für einen Träger. Der nicht offensichtliche Teil des
/// ganzen Entwurfs: beim Vokabellernen ist Bildkonstanz <i>gewollt</i> – das Kind soll bei jeder
/// Wiederholung dasselbe Bild sehen, Wiedererkennung <b>ist</b> der Merkeffekt. Würde die Auswahl bei
/// jedem Abruf neu rechnen, zerstörte ein nachträglich hinzugefügtes Bild genau ihn. Dasselbe Muster wie
/// die eingefrorene Ausspiel-Reihenfolge einer Übungssitzung.
/// <para>
/// Eine Zeile je <b>Kandidat</b>, nicht je Träger: die aktive Wahl ist die Zeile mit
/// <see cref="Rejected"/> = <c>false</c>, abgelehnte Bilder bleiben als Zeile stehen und werden nie
/// wieder gezogen. Damit ist „anderes Bild" zugleich das billigste Feedback-Signal, das wir bekommen können.
/// </para>
/// </summary>
public class ChildMediaPick
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Träger der Wahl – genau eine der beiden ist gesetzt (wie bei <see cref="MediaLink"/>).</summary>
    public int? VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }

    public int? ExerciseItemId { get; set; }
    public ExerciseItem? ExerciseItem { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    /// <summary>Vom Kind/Vater abgelehnt („anderes Bild") – wird für diesen Träger nie wieder gezogen.</summary>
    public bool Rejected { get; set; }

    public DateTime PickedAt { get; set; } = DateTime.UtcNow;
}
