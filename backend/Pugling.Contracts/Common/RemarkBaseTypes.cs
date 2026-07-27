namespace Pugling.Contracts;

/// <summary>
/// Einordnung einer Anmerkung. <see cref="Unspecified"/> ist der Regelfall beim Erfassen: Beim Testen zu
/// kategorisieren kostet mehr Zeit, als es einbringt – die Einordnung zieht der Nachbereitungs-Skill aus
/// dem Text nach.
/// </summary>
public enum RemarkCategory
{
    /// <summary>Nicht eingeordnet (Vorgabe) – der Skill leitet die Kategorie später aus dem Text ab.</summary>
    Unspecified = 0,
    /// <summary>Etwas funktioniert nicht wie erwartet.</summary>
    Bug = 1,
    /// <summary>Bedienung/Darstellung: Beschriftung, Anordnung, Verständlichkeit.</summary>
    Ui = 2,
    /// <summary>Frage oder Beobachtung zur Umsetzung im Code.</summary>
    Code = 3,
    /// <summary>Fachlicher Inhalt: Übungen, Vokabeln, Lernstoff.</summary>
    Content = 4,
    /// <summary>Vorschlag für etwas Neues.</summary>
    Idea = 5,
    /// <summary>Reine Wissensfrage – erwartet eine Antwort, keine Änderung.</summary>
    Question = 6,
}

/// <summary>
/// Bearbeitungsstand einer Anmerkung. Bewusst schlank: kein Zuweisen, keine Meilensteine – vier Zustände
/// reichen, damit der Nachbereitungs-Skill nicht bei jedem Lauf dieselben Anmerkungen wieder vorlegt.
/// </summary>
public enum RemarkStatus
{
    /// <summary>Erfasst, noch nicht angesehen.</summary>
    Open = 0,
    /// <summary>Zurückgestellt: Es ist etwas zu tun, aber nicht jetzt. Eine vorhandene Antwort bleibt als Vorarbeit erhalten.</summary>
    Planned = 1,
    /// <summary>Erledigt – Frage beantwortet oder Änderung umgesetzt.</summary>
    Done = 2,
    /// <summary>Verworfen: kein Handlungsbedarf.</summary>
    Rejected = 3,
}
