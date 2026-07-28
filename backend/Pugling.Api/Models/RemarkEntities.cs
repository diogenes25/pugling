namespace Pugling.Api.Models;

// RemarkCategory/RemarkStatus leben im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Eine beim Testen erfasste <b>Anmerkung</b>: Frage, Beobachtung oder Befund, festgehalten dort, wo sie
/// aufgefallen ist. Der fachliche Wert steckt nicht im Text – den könnte auch ein Textdokument halten –,
/// sondern im <b>mitgeschnittenen Kontext</b> (Route, Rolle, Kind/Übung, letzte Fehler): Genau das schreibt
/// ein Mensch beim Testen nicht mit, und genau das kostet später die Zeit beim Nachstellen.
/// <para>
/// Anmerkungen entstehen ausschließlich über das UI-Widget. Die Test-Skills (<c>creator</c>/<c>supervisor</c>/
/// <c>student</c>, <c>/smoke-test</c>) <b>lesen</b> sie über den Markdown-Export, legen aber keine an – sie
/// laufen gegen eine Wegwerf-DB, ein dort erzeugter Eintrag würde mit ihr gelöscht.
/// </para>
/// </summary>
public class Remark
{
    /// <summary>
    /// Fachlich sichtbare „Log-Id": Das Widget zeigt sie nach dem Speichern an, damit der Mensch sie
    /// mitnehmen und in Claude Code einlösen kann („Beantworte die Frage 123").
    /// </summary>
    public int Id { get; set; }

    /// <summary>Der eigentliche Text – das einzige Pflicht-Eingabefeld.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Einordnung. Bleibt bewusst häufig <see cref="RemarkCategory.Unspecified"/>: Beim Erfassen zu
    /// kategorisieren kostet mehr Zeit, als es einbringt – das Nachziehen erledigt der Skill aus dem Text.
    /// </summary>
    public RemarkCategory Category { get; set; } = RemarkCategory.Unspecified;

    /// <summary>Bearbeitungsstand. Ohne ihn legt der Nachbereitungs-Skill bei jedem Lauf dieselben Anmerkungen wieder vor.</summary>
    public RemarkStatus Status { get; set; } = RemarkStatus.Open;

    // --- Antwort (Rückkanal aus Claude Code) ---

    /// <summary>
    /// Die Antwort auf eine Frage. Bleibt <b>auch bei <see cref="RemarkStatus.Planned"/> erhalten</b>: Ein
    /// zurückgestellter Fall ist damit kein offener Zettel mehr, sondern ein bereits analysierter
    /// Backlog-Eintrag – die Vorarbeit für die spätere Umsetzung ist getan.
    /// </summary>
    public string? Answer { get; set; }

    /// <summary>Zeitpunkt der Beantwortung (UTC).</summary>
    public DateTime? AnsweredAt { get; set; }

    /// <summary>
    /// Wer geantwortet hat, z. B. <c>claude-code</c>. Bewusst ein Protokoll-<c>string</c> und kein Enum:
    /// hier soll später auch ein Mensch stehen können, ohne dass das Schema wandert.
    /// </summary>
    public string? AnsweredBy { get; set; }

    /// <summary>
    /// Optionaler Verweis auf die Anmerkung, aus der diese hervorging – die Spur von der Frage zu der
    /// daraus entstandenen Aufgabe. Gesetzt wird sie vom Skill, nicht vom Widget.
    /// <para>
    /// Nicht zu verwechseln mit <see cref="Comments"/>: Der Verweis führt <b>zwischen</b> Vorgängen
    /// (aus der Frage wurde eine Aufgabe), der Verlauf liegt <b>innerhalb</b> eines Vorgangs.
    /// </para>
    /// </summary>
    public int? ParentRemarkId { get; set; }
    public Remark? ParentRemark { get; set; }

    /// <summary>
    /// Der Verlauf: Analyse-Nachträge, Rückfragen des Menschen, Umsetzungsnotizen. Ergänzt
    /// <see cref="Answer"/>, ersetzt es nicht – die Antwort bleibt die *eine* belegte Auflösung, der Verlauf
    /// trägt alles, was danach kommt.
    /// <para>
    /// Der Verlauf ist der Grund, warum die Anmerkung einen Arbeitsgang übersteht: Vorher überschrieb eine
    /// Umsetzungsnotiz die vorangegangene Analyse, und die Vorarbeit war verloren.
    /// </para>
    /// </summary>
    public ICollection<RemarkComment> Comments { get; set; } = [];

    // --- Autor ---

    /// <summary>Konto des Erfassers (Claim <c>aid</c>).</summary>
    public int AccountId { get; set; }
    public Account? Account { get; set; }

    /// <summary>
    /// Rolle <b>zum Zeitpunkt der Erfassung</b> (Momentaufnahme wie <c>SupervisorId</c> auf
    /// <see cref="ShopPurchase"/>). Ein Konto kann mehrere Rollen tragen; für die Einordnung zählt die,
    /// in der gerade getestet wurde.
    /// </summary>
    public ProfileRole AuthorRole { get; set; }

    // --- Kontext-Schnappschuss (das Herzstück) ---

    /// <summary>Pfad im SPA, z. B. <c>/vater/kind/3/lernstand</c>.</summary>
    public string Route { get; set; } = "";

    /// <summary>Anwendungsbereich (<c>vater</c>/<c>sohn</c>) – explizit statt aus der Route geraten.</summary>
    public string AppArea { get; set; } = "";

    /// <summary>Kind, das beim Erfassen ausgewählt war. FK <c>SetNull</c>: Der Kontext darf verblassen, er darf nichts blockieren.</summary>
    public int? ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Übung, die beim Erfassen offen war.</summary>
    public int? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Lehrplan, der beim Erfassen offen war.</summary>
    public int? StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }

    /// <summary>Position, die beim Erfassen offen war.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }

    /// <summary>
    /// Zustands-Schnappschuss (Filter, offenes Modal, Auswahl) als rohes JSON. Bewusst <c>string</c> statt
    /// typisierter Spalte: Das Backend liest ihn nie fachlich aus – nur der Nachbereitungs-Skill tut das.
    /// So entfällt auch der <c>ValueComparer</c>, den eine gemappte JSON-Spalte bräuchte.
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// Ringpuffer der letzten fehlgeschlagenen Requests und JS-Fehler, als rohes JSON (gleiche Begründung
    /// wie <see cref="ContextJson"/>).
    /// <para>
    /// <b>Sicherheitsregel:</b> ausschließlich Metadaten – Methode, Pfad, Status, Fehler-<c>code</c>,
    /// Zeitstempel. <b>Keine</b> Request-/Response-Bodies, <b>keine</b> Header, <b>keine</b> Tokens: Der
    /// Login-Request trägt die PIN im Body, ein roher Mitschnitt legte sie im Klartext in die DB und trüge
    /// sie über den Export ins Repo.
    /// </para>
    /// </summary>
    public string? RecentErrorsJson { get; set; }

    /// <summary>Browserkennung – trennt Handy-Beobachtungen von Desktop-Beobachtungen.</summary>
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ein Beitrag im Verlauf einer <see cref="Remark"/>: Analyse-Nachtrag, Rückfrage des Menschen,
/// Umsetzungsnotiz.
/// <para>
/// <b>Warum es das gibt:</b> Mit nur einem <see cref="Remark.Answer"/>-Feld überschrieb jeder zweite
/// Arbeitsgang den ersten – die belegte Analyse verschwand hinter dem „gebaut: …". Der Verlauf hält die
/// Reihenfolge fest, die der Arbeitsweise entspricht: analysieren, zurückstellen, später umsetzen.
/// </para>
/// <para>
/// <b>Und was es nicht ist:</b> kein Chat. Es gibt keine Zustellung, keine Ungelesen-Marker und keine
/// Erwartung, dass jemand wartet – gelesen wird beim nächsten Testen oder im nächsten Skill-Lauf.
/// </para>
/// </summary>
public class RemarkComment
{
    public int Id { get; set; }

    /// <summary>Die Anmerkung, zu der der Beitrag gehört. FK <b>Cascade</b>: Ein Verlauf ohne Vorgang ist sinnlos.</summary>
    public int RemarkId { get; set; }
    public Remark? Remark { get; set; }

    /// <summary>Der Text – das einzige Pflichtfeld.</summary>
    public string Body { get; set; } = "";

    /// <summary>
    /// Mensch oder Claude. Steuert die Wiederaufnahme: Ein <see cref="RemarkCommentAuthor.Human"/>-Beitrag
    /// holt eine erledigte Anmerkung zurück auf <see cref="RemarkStatus.Open"/>.
    /// </summary>
    public RemarkCommentAuthor Author { get; set; } = RemarkCommentAuthor.Human;

    /// <summary>
    /// Anzeigename des Urhebers, z. B. <c>claude-code</c>. Bewusst ein Protokoll-<c>string</c> wie
    /// <see cref="Remark.AnsweredBy"/> – so kann später ein weiterer Beteiligter dazukommen, ohne Schema-Umbau.
    /// </summary>
    public string? AuthorLabel { get; set; }

    /// <summary>
    /// Konto des Schreibers. FK <c>SetNull</c>, denn ein gelöschtes Konto darf den Verlauf nicht mitnehmen –
    /// die fachliche Aussage des Beitrags gilt weiter.
    /// </summary>
    public int? AuthorAccountId { get; set; }
    public Account? AuthorAccount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
