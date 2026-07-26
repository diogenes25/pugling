namespace Pugling.Api.Models;

// Admin-Bereich: Personen-Verwaltung.
//   Supervisor (Father) >-< Student (Child) über SupervisorLink (ein Student kann mehrere Supervisor haben)
//   + Punkte pro Kind (ein gemeinsames Wallet über alle Supervisor).
// Der Lern-Inhalt (Subject -> Chapter -> Exercise) liegt separat im gemeinsamen
// learn-Katalog (siehe LearnEntities.cs).

// SupervisorRelation/Gender/PointKind leben im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Betreuungs-Beziehung Supervisor↔Student. Ein Student kann mehrere Supervisor haben (Vater, Mutter,
/// Oma …); jeder betreibt seinen eigenen Familien-Shop. Das Wallet bleibt <b>gemeinsam</b> – die
/// Zuordnung „wer löst ein" liegt am Kauf (siehe <see cref="ShopPurchase"/> mit Aussteller-Snapshot),
/// nicht am Geld. Ersetzt die frühere 1:1-Bindung <c>Child.FatherId</c>.
/// </summary>
public class SupervisorLink
{
    public int Id { get; set; }
    /// <summary>Der betreuende Erwachsene (heute ein <see cref="Father"/>-Profil).</summary>
    public int SupervisorId { get; set; }
    public Father? Supervisor { get; set; }
    /// <summary>Der betreute Lernende (ein <see cref="Child"/>-Profil).</summary>
    public int StudentId { get; set; }
    public Child? Student { get; set; }
    public SupervisorRelation Relation { get; set; } = SupervisorRelation.Father;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Elternteil / Verwalter (Supervisor-Profil) im Admin-Bereich.</summary>
public class Father
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    /// <summary>Einfacher PIN-Login. Später durch echtes Auth ersetzen.</summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Plattform-Superuser (Break-Glass): trägt beim Login zusätzlich den <see cref="Auth.Roles.Admin"/>-Claim
    /// und umgeht damit die RWX-Rechteprüfung auf Übungen (z. B. um verwaiste, ownerlose Übungen zu bearbeiten).
    /// Bewusst nicht per API setzbar – nur über DB/Seed (kein Selbst-Rechteausbau).
    /// </summary>
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Von diesem Supervisor betreute Studenten (über <see cref="SupervisorLink"/>).</summary>
    public List<SupervisorLink> SupervisedLinks { get; set; } = new();
}

/// <summary>Lernendes Kind (Student-Profil). Kann mehrere Supervisor haben (<see cref="SupervisorLinks"/>).</summary>
public class Child
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? BirthYear { get; set; }
    /// <summary>Aktuelle Klassenstufe (1–13). Steuert die Vorfilterung passender Übungen im Lehrplan-Assistenten.</summary>
    public int? Grade { get; set; }
    /// <summary>Schulart des Kindes – filtert im Assistenten Übungen, die nicht für alle Schularten gedacht sind.</summary>
    public SchoolTypes SchoolType { get; set; } = SchoolTypes.None;

    // --- Übungsunabhängiges persönliches Profil ---
    // Diese Angaben beschreiben das Kind, nicht seinen Lernstoff. Sie sind bewusst hier (am Kind) und nicht
    // an einer Übung/einem Plan angesiedelt, damit ein späterer KI-Generator daraus einen individuellen
    // Lehrplan ableiten kann: den Stoff treffen (Grade/SchoolType/Lehrbücher) und ihn in Themen einbetten,
    // die das Kind interessieren (Interests). Siehe wiki/09-llm-kochbuch.md.

    /// <summary>Geschlecht (rein deskriptiv; als String persistiert, lesbar/stabil).</summary>
    public Gender Gender { get; set; } = Gender.None;

    /// <summary>
    /// Freie Interessen/Vorlieben des Kindes („Brawl Stars", „Pokémon", „Fußball"). Dienen einem späteren
    /// Generator als Themen, in die der (feste) Lernstoff eingebettet wird – sie verändern nie, <i>was</i>
    /// gelernt wird, nur die Einkleidung (Cloze-Sätze, Textaufgaben, Kontexte). Als JSON-Liste gespeichert
    /// (Neuzuweisung im Controller, kein In-Place-Mutieren – fehlender ValueComparer sonst ein Fallstrick).
    /// </summary>
    public List<string> Interests { get; set; } = [];

    /// <summary>Optionaler Freitext für alles Unstrukturierte, das ein Generator kennen sollte
    /// (Lernschwächen, Motivationshinweise). Bewusst frei, damit ohne Schemaänderung nachtragbar.</summary>
    public string? ProfileNotes { get; set; }

    /// <summary>
    /// Gewichtete Interessen aus der kontrollierten Taxonomie (inkl. Abneigungen bei negativem Gewicht).
    /// Ergänzt das freie <see cref="Interests"/>, ersetzt es nicht: Freitext ist die Sprache des
    /// KI-Creators, referenzierte Tags sind die Grundlage der maschinellen Bildauswahl.
    /// </summary>
    public List<ChildInterest> InterestTags { get; set; } = [];

    /// <summary>
    /// Obergrenze der Bild-Eignung für dieses Kind – die Auswahl liefert nie ein Asset darüber.
    /// Nur der Supervisor darf sie heben; Default ist die strengste Stufe.
    /// </summary>
    public ContentRating AllowedContentRating { get; set; } = ContentRating.Everyone;
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

    /// <summary>Betreuende Supervisor dieses Studenten (über <see cref="SupervisorLink"/>).</summary>
    public List<SupervisorLink> SupervisorLinks { get; set; } = new();

    /// <summary>Vom Kind verwendete Lehrbücher (übungsunabhängiges Profil, siehe <see cref="Textbook"/>).</summary>
    public List<Textbook> Textbooks { get; set; } = new();
}

/// <summary>
/// Ein vom Kind verwendetes Lehrbuch (übungsunabhängiges Profil). Hält fest, aus welchem Werk und welchem
/// aktuellen Kapitel der Lernstoff kommt – die Grundlage, aus der ein späterer Generator „was ist gerade
/// dran" ableitet. <see cref="Title"/> + <see cref="CurrentChapter"/> lassen sich gegen den Freitext
/// <c>Exercise.Source</c> (z. B. „Green Line 3, Unit 4") matchen, um vorhandene Übungen wiederzuverwenden.
/// </summary>
public class Textbook
{
    public int Id { get; set; }
    /// <summary>Das Kind, dem das Buch zugeordnet ist (Cascade – verschwindet mit dem Kind).</summary>
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Titel des Werks, z. B. „Green Line 3".</summary>
    public string Title { get; set; } = "";
    /// <summary>Fach als Freitext, z. B. „Englisch" – das Fach muss nicht im Katalog existieren.</summary>
    public string? SubjectName { get; set; }
    /// <summary>Optionaler Katalog-Link auf ein <see cref="Subject"/>, wo eine exakte Zuordnung möglich ist.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Für welche Klassenstufe das Buch gedacht ist.</summary>
    public int? Grade { get; set; }
    public string? Publisher { get; set; }
    public string? Isbn { get; set; }
    /// <summary>Aktueller Lernstand im Buch, z. B. „Unit 4 – Past Tense".</summary>
    public string? CurrentChapter { get; set; }

    /// <summary>
    /// Optionaler Link auf die katalogisierte <see cref="TextbookSeries"/>. Erst er macht die Frage
    /// „welcher Creator kennt dieses Material?" entscheidbar (Reihen-Treffer im Profil-Matching);
    /// <see cref="Title"/>/<see cref="Publisher"/> bleiben als Rückfallebene für unkatalogisierte Werke.
    /// </summary>
    public int? SeriesId { get; set; }
    public TextbookSeries? Series { get; set; }
    /// <summary>
    /// Die Unit, in der das Kind gerade steckt – die strukturierte Form von <see cref="CurrentChapter"/>.
    /// Nur damit kennt der Creator den Stoff der Unit (Themen/Grammatik/Wortschatz) statt bloß ihren Namen.
    /// </summary>
    public int? CurrentUnitId { get; set; }
    public SeriesUnit? CurrentUnit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
