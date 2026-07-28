namespace Pugling.Api.Models;

// Die Unterrichts-Seite des Katalogs: welches Werk ein Fach abdeckt (TextbookSeries -> SeriesUnit) und
// wer es unterrichtet (CreatorProfile). Beides ist kindneutral und wird EINMAL gepflegt – das Kind
// verweist über sein Textbook nur darauf. Eigentum ist wie bei der Übung geregelt: global lesbar,
// schreiben darf nur der Owner; ein gelöschter Owner leert nur die FK (SetNull), damit fremde
// Referenzen nicht brechen.

/// <summary>
/// Eine Lehrwerk-Reihe („Access", „Green Line") als <b>geteilte</b> Größe. Erst dadurch ist die Frage
/// „welcher Creator kennt das Material dieses Kindes?" maschinell beantwortbar: Kind-<see cref="Textbook"/>
/// und <see cref="CreatorProfile"/> zeigen auf denselben Datensatz, statt Freitext-Titel zu vergleichen.
/// Der <see cref="Slug"/> macht das Anlegen idempotent (Muster: <c>InterestTag</c>).
/// </summary>
public class TextbookSeries
{
    public int Id { get; set; }
    /// <summary>Anzeigename der Reihe, z. B. „Access".</summary>
    public string Name { get; set; } = "";
    /// <summary>Normalisierter, global eindeutiger Schlüssel der Reihe („access"). Unveränderlich.</summary>
    public string Slug { get; set; } = "";
    /// <summary>Verlag, z. B. „Cornelsen".</summary>
    public string? Publisher { get; set; }
    /// <summary>Fach als Freitext („Englisch") – das Fach muss nicht im Katalog existieren.</summary>
    public string? SubjectName { get; set; }
    /// <summary>Optionaler Katalog-Link auf ein <see cref="Subject"/>, wo eine exakte Zuordnung möglich ist.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Schularten, für die die Reihe gedacht ist; <see cref="SchoolTypes.None"/> = für alle.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>Bei Sprachreihen die Lernsprache (Sprachcode, z. B. <c>en</c>).</summary>
    public string? SourceLanguage { get; set; }
    /// <summary>Bei Sprachreihen die Muttersprache (Sprachcode, z. B. <c>de</c>).</summary>
    public string? TargetLanguage { get; set; }
    /// <summary>Freie Notizen zum Werk (Aufbau, Besonderheiten) – Kontext für den KI-Creator.</summary>
    public string? Notes { get; set; }
    /// <summary>Wer die Reihe angelegt hat und sie ändern darf; <c>null</c> = geseedet, gehört niemandem.</summary>
    public int? OwnerAdultId { get; set; }
    public Adult? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SeriesUnit> Units { get; set; } = [];
}

/// <summary>
/// Eine Unit der Reihe, samt Band. Band und Unit liegen bewusst in <b>einer</b> Ebene
/// (<see cref="Grade"/> = Band): „Access 8, Unit 3" ist eine Zeile, kein zweistufiger Baum.
/// <see cref="Topics"/>, <see cref="Grammar"/> und <see cref="VocabularyNotes"/> sind der eigentliche
/// Gewinn dieser Tabelle – sie machen den Creator <i>materialkundig</i>, statt ihn den Stoff der Unit
/// erraten zu lassen.
/// </summary>
public class SeriesUnit
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public TextbookSeries? Series { get; set; }
    /// <summary>Band der Reihe, ausgedrückt als Klassenstufe (Access 8 → 8); null = bandlos.</summary>
    public int? Grade { get; set; }
    /// <summary>Reihenfolge innerhalb des Bandes.</summary>
    public int OrderIndex { get; set; }
    /// <summary>Bezeichnung wie im Buch, z. B. „Unit 3 – Growing up".</summary>
    public string Label { get; set; } = "";
    /// <summary>Themen/Inhalte der Unit (Freitext, gern Stichpunkte).</summary>
    public string? Topics { get; set; }
    /// <summary>Grammatik, die die Unit einführt oder übt.</summary>
    public string? Grammar { get; set; }
    /// <summary>Wortschatz-Notiz der Unit (Wortfelder oder konkrete Wörter, kommagetrennt).</summary>
    public string? VocabularyNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ein Creator-Profil ist der <b>Lehrer</b>: ein Fach, ein Schulzweig, ein Klassenstufen-Bereich und
/// optional eine Buchreihe – dazu die didaktische Haltung, mit der Übungen entstehen
/// (<see cref="Persona"/>/<see cref="Didactics"/> gehen in den System-Prompt des KI-Creators).
/// Der Sinn ist die Passung: zu einem Kind lässt sich damit der fachkundige Creator <i>finden</i>
/// (<c>CreatorProfileService</c>), statt jedes Mal denselben Generalisten zu befragen.
/// </summary>
public class CreatorProfile
{
    public int Id { get; set; }
    /// <summary>Sprechender Name, z. B. „Englisch 8 Gymnasium – Access".</summary>
    public string Name { get; set; } = "";
    /// <summary>Wer das Profil angelegt hat und es ändern darf; <c>null</c> = geseedet.</summary>
    public int? OwnerAdultId { get; set; }
    public Adult? Owner { get; set; }
    /// <summary>Fach als Freitext („Englisch") – für Profile ohne Katalog-Fach.</summary>
    public string? SubjectName { get; set; }
    /// <summary>Optionaler Katalog-Link auf ein <see cref="Subject"/>.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Schularten, für die das Profil zuständig ist; <see cref="SchoolTypes.None"/> = für alle.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>Unterste unterrichtete Klassenstufe (inklusive); null = keine Untergrenze.</summary>
    public int? GradeMin { get; set; }
    /// <summary>Oberste unterrichtete Klassenstufe (inklusive); null = keine Obergrenze.</summary>
    public int? GradeMax { get; set; }
    /// <summary>Die Reihe, auf die das Profil optimiert ist; null = werkunabhängig.</summary>
    public int? SeriesId { get; set; }
    public TextbookSeries? Series { get; set; }
    /// <summary>Lernsprache (Sprachcode) für Sprachfächer.</summary>
    public string SourceLang { get; set; } = "en";
    /// <summary>Muttersprache (Sprachcode).</summary>
    public string TargetLang { get; set; } = "de";
    /// <summary>
    /// Rollenbeschreibung des Lehrers in eigenen Worten („Du bist Englischlehrer am Gymnasium …").
    /// Wird dem festen Regelblock des Creators <b>vorangestellt</b>, ersetzt ihn nie.
    /// </summary>
    public string? Persona { get; set; }
    /// <summary>Didaktische Vorgaben, die über einen Auftrag hinaus gelten (Satzlänge, Progression, Tabus).</summary>
    public string? Didactics { get; set; }
    /// <summary>
    /// Übungstypen, die dieses Profil bevorzugt erzeugt (Schlüssel aus dem Typ-Manifest). Als JSON-Liste
    /// gespeichert – im Controller <b>neu zuweisen</b>, nicht in-place mutieren (fehlender ValueComparer).
    /// </summary>
    public List<string> DefaultTypes { get; set; } = [];
    /// <summary>Inaktive Profile werden beim Matching nie vorgeschlagen.</summary>
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
