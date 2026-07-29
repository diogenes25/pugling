namespace Pugling.Contracts;

/// <summary>
/// Lernverfahren – nur noch die Selbstbeschreibung im Übungstyp-Manifest (<see cref="ExerciseTypeManifest"/>)
/// braucht diese Zuordnung. Kein plan-weites Verfahren mehr.
/// </summary>
public enum LearningMethod
{
    /// <summary>Vokabellernen (Karte mit Vorder-/Rückseite, Leitner-Boxen).</summary>
    Vocabulary = 0,
    /// <summary>Lückentext.</summary>
    Cloze = 1,
    /// <summary>Zuordnen von Paaren.</summary>
    Matching = 2,
}

/// <summary>Ein Schritt im Stufen-Fahrplan: ab Tag <c>DayNumber</c> (1-basiert) gilt Stufe <c>Stage</c>.</summary>
public record StageStep(int DayNumber, int Stage);

/// <summary>
/// Ausspiel-Modus einer Übungssitzung. <see cref="Info"/> = freies Üben: Inhalte am Stück, das Frontend
/// führt die Iteration, es fließt <b>kein</b> Lernfeedback (keine Bewertung/Punkte/Leitner, zählt nicht aufs
/// Ziel). <see cref="Lern"/> = server-geführt: der Server hält Cursor + eingefrorene Reihenfolge und bewertet.
/// </summary>
public enum PlayMode
{
    /// <summary>Freies Üben ohne Lernfeedback – das Frontend führt die Iteration.</summary>
    Info = 0,
    /// <summary>Server-geführtes Lernen mit Cursor, Bewertung, Punkten und Leitner-Terminierung.</summary>
    Lern = 1,
}
